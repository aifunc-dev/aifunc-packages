// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { AIFuncArtifact, AIFuncConfig, MockEntry } from './types';
import { validate } from './validator';
import { renderPrompt } from './prompt';
import { sendRequest } from './request';
import { buildRequest, parseResponse } from './providers/general';
import { findMockOutput, generateFromSchema } from './mock';

export async function execute<TOutput = Record<string, unknown>>(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>,
  config: AIFuncConfig = {}
): Promise<TOutput> {
  const inputValidation = validate(input, artifact.api.input);
  if (!inputValidation.valid) {
    throw new Error(
      `Input validation failed:\n${inputValidation.errors.join('\n')}`
    );
  }

  if (config.mock) {
    return executeMock<TOutput>(artifact, input, config);
  }

  const prompt = renderPrompt(artifact, input);

  const requestParams = buildRequest(artifact, prompt, config);

  const response = await sendRequest(config, requestParams);

  const output = parseResponse(response);

  const outputValidation = validate(output, artifact.api.output);
  if (!outputValidation.valid) {
    throw new Error(
      `Output validation failed:\n${outputValidation.errors.join('\n')}`
    );
  }

  return output as TOutput;
}

function executeMock<TOutput>(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>,
  config: AIFuncConfig
): TOutput {
  const mockEntries = resolveMockEntries(config);

  const output = findMockOutput(mockEntries, input);

  if (output === null) {
    const generated = generateFromSchema(artifact.api.output) as Record<string, unknown>;

    const outputValidation = validate(generated, artifact.api.output);
    if (!outputValidation.valid) {
      throw new Error(
        `Auto-generated mock output validation failed:\n${outputValidation.errors.join('\n')}`
      );
    }

    return generated as TOutput;
  }

  const outputValidation = validate(output, artifact.api.output);
  if (!outputValidation.valid) {
    throw new Error(
      `Mock output validation failed:\n${outputValidation.errors.join('\n')}`
    );
  }

  return output as TOutput;
}

function resolveMockEntries(config: AIFuncConfig): MockEntry[] {
  if (!config.mockData) {
    return [];
  }

  return Array.isArray(config.mockData) ? config.mockData : config.mockData.cases ?? [];
}
