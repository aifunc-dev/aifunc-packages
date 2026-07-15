# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

from typing import Any
from .types import JSONSchema, ValidationResult


def validate(data: Any, schema: JSONSchema, path: str = "root") -> ValidationResult:
    errors: list[str] = []

    if "type" in schema:
        types = schema["type"] if isinstance(schema["type"], list) else [schema["type"]]
        actual_type = _get_type(data)

        type_match = (
            actual_type in types
            or (actual_type == "integer" and "number" in types)
        )
        if not type_match:
            errors.append(f"{path}: expected type {' | '.join(types)}, got {actual_type}")
            return ValidationResult(valid=False, errors=errors)

    if "enum" in schema:
        if data not in schema["enum"]:
            enum_values = ", ".join(repr(v) for v in schema["enum"])
            errors.append(f"{path}: value must be one of [{enum_values}], got {repr(data)}")
            return ValidationResult(valid=False, errors=errors)

    if _has_type(schema, "object") and isinstance(data, dict):
        if "required" in schema:
            for key in schema["required"]:
                if key not in data:
                    errors.append(f"{path}: missing required property '{key}'")

        if "properties" in schema:
            for key, prop_schema in schema["properties"].items():
                if key in data:
                    result = validate(data[key], prop_schema, f"{path}.{key}")
                    errors.extend(result.errors)

    if _has_type(schema, "array") and isinstance(data, list):
        if "items" in schema:
            for i, item in enumerate(data):
                result = validate(item, schema["items"], f"{path}[{i}]")
                errors.extend(result.errors)

    return ValidationResult(valid=len(errors) == 0, errors=errors)


def _has_type(schema: JSONSchema, type_name: str) -> bool:
    if "type" not in schema:
        return False
    t = schema["type"]
    if isinstance(t, list):
        return type_name in t
    return t == type_name


def _get_type(value: Any) -> str:
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "boolean"
    if isinstance(value, int):
        return "integer"
    if isinstance(value, float):
        return "number"
    if isinstance(value, str):
        return "string"
    if isinstance(value, list):
        return "array"
    if isinstance(value, dict):
        return "object"
    return type(value).__name__
