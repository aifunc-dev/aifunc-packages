# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

import json
import re
from typing import Any
from .types import AIFuncArtifact, JSONSchema


def render_prompt(artifact: AIFuncArtifact, input_data: dict[str, Any]) -> str:
    prompt = _select_prompt(artifact)

    input_json = json.dumps(input_data, ensure_ascii=False, indent=2)
    prompt = prompt.replace("{{input_json}}", input_json)

    def replace_input_field(match: re.Match) -> str:
        field_name = match.group(1)
        value = input_data.get(field_name)
        if value is None:
            return match.group(0)
        return str(value)

    prompt = re.sub(r"\{\{input\.([a-zA-Z0-9_]+)\}\}", replace_input_field, prompt)

    def replace_field(match: re.Match) -> str:
        field_name = match.group(1)
        value = input_data.get(field_name)
        if value is None:
            return ""
        return str(value)

    prompt = re.sub(r"\{\{([a-zA-Z0-9_]+)\}\}", replace_field, prompt)

    if artifact.api.get("injectOutputSchema") is not False:
        schema_instruction = _build_schema_instruction(artifact.api["output"])
        prompt += f"\n\n{schema_instruction}"

    return prompt


def _select_prompt(artifact: AIFuncArtifact) -> str:
    if artifact.prompt:
        return artifact.prompt

    if artifact.prompts and "general" in artifact.prompts:
        return artifact.prompts["general"]

    if artifact.prompts:
        first = next(iter(artifact.prompts.values()), None)
        if isinstance(first, str):
            return first

    raise ValueError("Artifact missing prompt template")


def _build_schema_instruction(schema: JSONSchema) -> str:
    schema_json = json.dumps(schema, ensure_ascii=False, indent=2)
    return (
        f"Please respond with a JSON object that matches the following schema:\n\n"
        f"{schema_json}\n\n"
        f"Your response must be valid JSON only, with no additional text."
    )
