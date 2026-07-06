# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

import json
import re
from typing import Any
from ..types import AIFuncArtifact, AIFuncConfig, ModelRequestParams, ModelResponse


def build_request(
    artifact: AIFuncArtifact, prompt: str, config: AIFuncConfig
) -> ModelRequestParams:
    if not config.model:
        raise ValueError("AIFuncConfig.model is required when mock mode is disabled")

    params = ModelRequestParams(
        model=config.model,
        messages=[{"role": "user", "content": prompt}],
        response_format={"type": "json_object"},
    )

    resolved = _resolve_model_params(artifact, config.model)

    temperature = config.temperature
    if temperature is None:
        temperature = resolved.get("temperature")
    if temperature is None and artifact.model:
        temperature = artifact.model.get("temperature")
    if temperature is not None:
        params.temperature = temperature

    max_tokens = config.max_tokens
    if max_tokens is None:
        max_tokens = resolved.get("maxTokens")
    if max_tokens is None and artifact.model:
        max_tokens = artifact.model.get("maxTokens")
    if max_tokens is not None:
        params.max_tokens = max_tokens

    return params


def _resolve_model_params(artifact: AIFuncArtifact, model: str) -> dict[str, Any]:
    if not artifact.model_params:
        return {}

    rules = artifact.model_params.get("rules")
    if not rules:
        return {}

    for rule in rules:
        if _matches_rule(rule, model):
            params = rule.get("params", {})
            return {
                "temperature": params.get("temperature"),
                "maxTokens": params.get("maxTokens"),
            }

    return {}


def _matches_rule(rule: dict[str, Any], model: str) -> bool:
    match = rule.get("match", {})

    if match.get("model") and match["model"] == model:
        return True

    if match.get("models") and model in match["models"]:
        return True

    if match.get("pattern"):
        return _glob_match(match["pattern"], model)

    if not match.get("model") and not match.get("models") and not match.get("pattern"):
        return True

    return False


def _glob_match(pattern: str, value: str) -> bool:
    escaped = re.escape(pattern).replace(r"\*", ".*")
    return re.fullmatch(escaped, value) is not None


def parse_response(response: ModelResponse) -> dict[str, Any]:
    if not response.choices:
        raise ValueError("Model response contains no choices")

    choice = response.choices[0]
    content: str = choice.get("message", {}).get("content", "")
    content = content.strip()

    fence_match = re.match(r"^```(?:json)?\s*\n?([\s\S]*?)\n?```$", content)
    if fence_match:
        content = fence_match.group(1).strip()

    try:
        parsed = json.loads(content)
        if not isinstance(parsed, dict):
            raise ValueError("Expected JSON object")
        return parsed
    except (json.JSONDecodeError, ValueError):
        raise ValueError(f"Failed to parse model output as JSON: {content[:200]}")
