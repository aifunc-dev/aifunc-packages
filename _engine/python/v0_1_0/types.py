# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

from typing import Any
from dataclasses import dataclass, field


@dataclass
class AIFuncConfig:
    base_url: str | None = None
    api_key: str | None = None
    model: str | None = None
    temperature: float | None = None
    max_tokens: int | None = None
    timeout: int = 30000
    mock: bool = False
    mock_data: Any = None


JSONSchema = dict[str, Any]

_ARTIFACT_CAMEL_MAP: dict[str, str] = {
    "engineVersion": "engine_version",
    "schemaVersion": "schema_version",
    "artifactVersion": "artifact_version",
    "modelParams": "model_params",
    "modelRouting": "model_routing",
    "mockFile": "mock_file",
}


@dataclass
class AIFuncArtifact:
    api: dict[str, Any]
    name: str | None = None
    description: str | None = None
    engine_version: str | None = None
    schema_version: str | None = None
    artifact_version: str | None = None
    package: dict[str, Any] | None = None
    prompt: str | None = None
    prompts: dict[str, str] | None = None
    provider: str | None = None
    model: dict[str, Any] | None = None
    model_params: dict[str, Any] | None = None
    model_routing: dict[str, Any] | None = None
    mock_file: str | None = None
    metadata: dict[str, Any] | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "AIFuncArtifact":
        if not isinstance(data, dict):
            raise ValueError(
                f"AIFuncArtifact.from_dict expects a dict, got {type(data).__name__}"
            )

        api = data.get("api")
        if api is None:
            raise ValueError(
                "Artifact is missing the required 'api' field. "
                "The artifact file may be corrupted or in an unsupported format."
            )
        if not isinstance(api, dict):
            raise ValueError(
                f"Artifact 'api' field must be an object, got {type(api).__name__}"
            )

        kwargs: dict[str, Any] = {"api": api}
        for camel_key, snake_key in _ARTIFACT_CAMEL_MAP.items():
            if camel_key in data:
                kwargs[snake_key] = data[camel_key]

        for key in ("name", "description", "package", "prompt", "prompts",
                    "provider", "model", "metadata"):
            if key in data:
                kwargs[key] = data[key]

        return cls(**kwargs)


@dataclass
class ModelRequestParams:
    model: str
    messages: list[dict[str, str]]
    temperature: float | None = None
    max_tokens: int | None = None
    response_format: dict[str, str] | None = None


@dataclass
class ModelResponse:
    id: str
    object: str
    created: int
    model: str
    choices: list[dict[str, Any]]
    usage: dict[str, int] | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "ModelResponse":
        return cls(
            id=data["id"],
            object=data["object"],
            created=data["created"],
            model=data["model"],
            choices=data["choices"],
            usage=data.get("usage"),
        )


@dataclass
class ValidationResult:
    valid: bool
    errors: list[str] = field(default_factory=list)


@dataclass
class MockEntry:
    output: dict[str, Any]
    input: dict[str, Any] | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "MockEntry":
        return cls(
            output=data["output"],
            input=data.get("input"),
        )
