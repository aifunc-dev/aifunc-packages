# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

import asyncio
import random
from dataclasses import dataclass
from typing import Any, AsyncGenerator, Callable
from .types import AIFuncArtifact, AIFuncConfig, MockEntry
from .validator import validate
from .prompt import render_prompt
from .request import send_request, send_stream_request, ProjectDefaults
from .providers.general import build_request, build_stream_request, parse_response
from .mock import find_mock_output, generate_from_schema


async def execute(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig | None = None,
    project_defaults: ProjectDefaults | None = None,
) -> dict[str, Any]:

    if config is None:
        config = AIFuncConfig()

    input_validation = validate(input_data, artifact.api["input"])
    if not input_validation.valid:
        details = "\n".join(input_validation.errors)
        raise ValueError(f"Input validation failed:\n{details}")

    if config.mock:
        return await _execute_mock(artifact, input_data, config)

    prompt = render_prompt(artifact, input_data)
    request_params = build_request(artifact, prompt, config)
    response = await send_request(config, request_params, project_defaults)
    output = parse_response(response)

    output_validation = validate(output, artifact.api["output"])
    if not output_validation.valid:
        details = "\n".join(output_validation.errors)
        raise ValueError(f"Output validation failed:\n{details}")

    return output


async def execute_stream(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig | None = None,
    project_defaults: ProjectDefaults | None = None,
    cancel_event: asyncio.Event | None = None,
) -> AsyncGenerator[str, None]:
    """
    Coroutine that resolves to an AsyncGenerator yielding raw text tokens.

    Callers must await this function, then iterate the returned generator:

        async for token in await execute_stream(artifact, input_data, config):
            ...

    In mock mode, the mock output text is yielded word-by-word with a small
    delay to simulate realistic streaming behavior.

    cancel_event: if set, streaming stops gracefully after the current token.
    """
    if config is None:
        config = AIFuncConfig()

    input_validation = validate(input_data, artifact.api["input"])
    if not input_validation.valid:
        details = "\n".join(input_validation.errors)
        raise ValueError(f"Input validation failed:\n{details}")

    if config.mock:
        return _mock_token_generator(artifact, input_data, config, cancel_event)

    prompt = render_prompt(artifact, input_data)
    request_params = build_stream_request(artifact, prompt, config)
    return await send_stream_request(config, request_params, project_defaults, cancel_event)


@dataclass
class StreamingHandle:
    """Wraps a streaming async generator with a cancel() function."""
    tokens: AsyncGenerator[str, None]
    cancel: Callable[[], None]


def execute_stream_with_cancel(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig | None = None,
    project_defaults: ProjectDefaults | None = None,
) -> StreamingHandle:
    """
    Returns a StreamingHandle with .tokens (AsyncGenerator) and .cancel().

    cancel() sets an asyncio.Event that causes the stream to stop after the
    current token. It is idempotent and safe to call multiple times.
    """
    cancel_event = asyncio.Event()

    async def _gen() -> AsyncGenerator[str, None]:
        gen = await execute_stream(artifact, input_data, config, project_defaults, cancel_event)
        async for token in gen:
            if cancel_event.is_set():
                return
            yield token

    return StreamingHandle(
        tokens=_gen(),
        cancel=cancel_event.set,
    )


async def _execute_mock(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig,
) -> dict[str, Any]:
    mock_entries = _resolve_mock_entries(config)
    output = find_mock_output(mock_entries, input_data)

    if output is None:
        generated = generate_from_schema(artifact.api["output"])
        output_validation = validate(generated, artifact.api["output"])
        if not output_validation.valid:
            details = "\n".join(output_validation.errors)
            raise ValueError(f"Auto-generated mock output validation failed:\n{details}")
        return generated

    output_validation = validate(output, artifact.api["output"])
    if not output_validation.valid:
        details = "\n".join(output_validation.errors)
        raise ValueError(f"Mock output validation failed:\n{details}")
    return output


def _mock_token_generator(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig,
    cancel_event: asyncio.Event | None,
) -> AsyncGenerator[str, None]:
    """Returns an async generator that yields mock tokens word-by-word."""
    mock_entries = _resolve_mock_entries(config)
    output = find_mock_output(mock_entries, input_data)

    if isinstance(output, str):
        text = output
    elif output is not None and isinstance(output, dict):
        first = next((v for v in output.values() if isinstance(v, str)), None)
        text = first if first is not None else str(output)
    else:
        text = str(generate_from_schema(artifact.api["output"]) or "(mock output)")

    return _yield_mock_tokens(text, cancel_event)


async def _yield_mock_tokens(
    text: str,
    cancel_event: asyncio.Event | None,
) -> AsyncGenerator[str, None]:
    words = text.split(" ")
    for i, word in enumerate(words):
        if cancel_event is not None and cancel_event.is_set():
            return
        token = word if i == 0 else " " + word
        yield token
        await asyncio.sleep(0.03 + random.random() * 0.06)


def _resolve_mock_entries(config: AIFuncConfig) -> list[MockEntry]:
    if config.mock_data is None:
        return []

    if isinstance(config.mock_data, list):
        return [
            MockEntry.from_dict(e) if isinstance(e, dict) else e
            for e in config.mock_data
        ]

    if isinstance(config.mock_data, dict) and "cases" in config.mock_data:
        return [
            MockEntry.from_dict(e) if isinstance(e, dict) else e
            for e in config.mock_data["cases"]
        ]

    raise ValueError("mock_data must be a list of MockEntry or a dict with 'cases' key")
