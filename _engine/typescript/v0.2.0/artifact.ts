// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { AIFuncArtifact } from './types';

export function validateArtifact(artifact: AIFuncArtifact): void {
  const name = artifact.name ?? artifact.package?.name;
  if (!name) {
    throw new Error('Artifact missing required field: name or package.name');
  }

  const hasEngine = artifact.engineVersion ?? artifact.package?.engine;
  if (!hasEngine) {
    throw new Error('Artifact missing required field: engineVersion or package.engine');
  }

  if (!artifact.prompt && !artifact.prompts) {
    throw new Error('Artifact missing required field: prompt or prompts');
  }

  if (!artifact.api) {
    throw new Error('Artifact missing required field: api');
  }
  if (!artifact.api.input) {
    throw new Error('Artifact missing required field: api.input');
  }
  if (!artifact.api.output) {
    throw new Error('Artifact missing required field: api.output');
  }
}
