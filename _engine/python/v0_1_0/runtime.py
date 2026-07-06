# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

from typing import Any
from .types import AIFuncArtifact, AIFuncConfig, MockEntry
from .validator import validate
from .prompt import render_prompt
from .request import send_request
from .providers.general import build_request, parse_response
from .mock import find_mock_output, generate_from_schema


async def execute(
    artifact: AIFuncArtifact,
    input_data: dict[str, Any],
    config: AIFuncConfig | None = None,
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
    response = await send_request(config, request_params)
    output = parse_response(response)

    output_validation = validate(output, artifact.api["output"])
    if not output_validation.valid:
        details = "\n".join(output_validation.errors)
        raise ValueError(f"Output validation failed:\n{details}")

    return output


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
            raise ValueError(
                f"Auto-generated mock output validation failed:\n{details}"
            )

        return generated

    output_validation = validate(output, artifact.api["output"])
    if not output_validation.valid:
        details = "\n".join(output_validation.errors)
        raise ValueError(f"Mock output validation failed:\n{details}")

    return output


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
