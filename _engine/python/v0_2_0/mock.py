# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

from typing import Any
from .types import MockEntry


def find_mock_output(
    mock_data: list[MockEntry], input_data: dict[str, Any]
) -> dict[str, Any] | None:
    fallback: dict[str, Any] | None = None

    for entry in mock_data:
        if entry.input is None and fallback is None:
            fallback = entry.output
            continue

        if entry.input is not None and entry.input == input_data:
            return entry.output

    return fallback


def generate_from_schema(schema: dict[str, Any]) -> Any:
    if "default" in schema:
        return schema["default"]
    if "enum" in schema and schema["enum"]:
        return schema["enum"][0]

    schema_type = schema.get("type")
    if isinstance(schema_type, list):
        schema_type = schema_type[0]

    if schema_type == "string":
        return schema.get("description", "")
    elif schema_type in ("number", "integer"):
        return 0
    elif schema_type == "boolean":
        return False
    elif schema_type == "null":
        return None
    elif schema_type == "array":
        items = schema.get("items")
        return [generate_from_schema(items)] if items else []
    elif schema_type == "object":
        obj: dict[str, Any] = {}
        properties = schema.get("properties", {})
        for key, prop_schema in properties.items():
            obj[key] = generate_from_schema(prop_schema)
        return obj
    else:
        return None
