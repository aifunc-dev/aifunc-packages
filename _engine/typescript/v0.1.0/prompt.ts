// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { AIFuncArtifact, JSONSchema } from './types';

export function renderPrompt(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>
): string {
  let prompt = selectPrompt(artifact);

  const inputJson = JSON.stringify(input, null, 2);
  prompt = prompt.replace(/\{\{input_json\}\}/g, inputJson);

  prompt = prompt.replace(/\{\{input\.([a-zA-Z0-9_]+)\}\}/g, (match, fieldName) => {
    const value = input[fieldName];
    if (value === undefined) {
      return match;
    }
    return String(value);
  });

  prompt = prompt.replace(/\{\{([a-zA-Z0-9_]+)\}\}/g, (match, fieldName) => {
    const value = input[fieldName];
    if (value === undefined || value === null) {
      return '';
    }
    return String(value);
  });

  if (artifact.api.injectOutputSchema !== false) {
    const schemaInstruction = buildSchemaInstruction(artifact.api.output);
    prompt += `\n\n${schemaInstruction}`;
  }

  return prompt;
}

function selectPrompt(artifact: AIFuncArtifact): string {
  if (artifact.prompt) {
    return artifact.prompt;
  }

  if (artifact.prompts?.general) {
    return artifact.prompts.general;
  }

  const firstPrompt = artifact.prompts ? Object.values(artifact.prompts)[0] : undefined;
  if (typeof firstPrompt === 'string') {
    return firstPrompt;
  }

  throw new Error('Artifact missing prompt template');
}

function buildSchemaInstruction(schema: JSONSchema): string {
  const schemaJson = JSON.stringify(schema, null, 2);
  return `Please respond with a JSON object that matches the following schema:\n\n${schemaJson}\n\nYour response must be valid JSON only, with no additional text.`;
}
