# Copyright 2026 GildenEye
# SPDX-License-Identifier: Apache-2.0

from .types import AIFuncArtifact


def validate_artifact(artifact: AIFuncArtifact) -> None:
    name = artifact.name or (artifact.package or {}).get("name")
    if not name:
        raise ValueError("Artifact missing required field: name or package.name")

    has_engine = artifact.engine_version or (artifact.package or {}).get("engine")
    if not has_engine:
        raise ValueError("Artifact missing required field: engineVersion or package.engine")

    if not artifact.prompt and not artifact.prompts:
        raise ValueError("Artifact missing required field: prompt or prompts")

    if not artifact.api:
        raise ValueError("Artifact missing required field: api")

    if "input" not in artifact.api:
        raise ValueError("Artifact missing required field: api.input")

    if "output" not in artifact.api:
        raise ValueError("Artifact missing required field: api.output")
