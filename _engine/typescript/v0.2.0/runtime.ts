// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { AIFuncArtifact, AIFuncConfig, MockEntry } from './types';
import type { ProjectDefaults } from './request';
import { validate } from './validator';
import { renderPrompt } from './prompt';
import { sendRequest, sendStreamRequest } from './request';
import { buildRequest, buildStreamRequest, parseResponse } from './providers/general';
import { findMockOutput, generateFromSchema } from './mock';

export async function execute<TOutput = Record<string, unknown>>(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>,
  config: AIFuncConfig = {},
  projectDefaults: ProjectDefaults = {}
): Promise<TOutput> {
  const inputValidation = validate(input, artifact.api.input);
  if (!inputValidation.valid) {
    throw new Error(`Input validation failed:\n${inputValidation.errors.join('\n')}`);
  }

  if (config.mock) {
    return executeMock<TOutput>(artifact, input, config);
  }

  const prompt = renderPrompt(artifact, input);
  const requestParams = buildRequest(artifact, prompt, config);
  const response = await sendRequest(config, requestParams, projectDefaults);
  const output = parseResponse(response);

  const outputValidation = validate(output, artifact.api.output);
  if (!outputValidation.valid) {
    throw new Error(`Output validation failed:\n${outputValidation.errors.join('\n')}`);
  }

  return output as TOutput;
}

/**
 * Stream the raw text tokens from the model as they arrive.
 * Uses native async iteration (for await...of) over SSE.
 *
 * In mock mode, the mock output text is yielded word-by-word with
 * a small delay to simulate realistic streaming behavior.
 */
export async function* executeStream(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>,
  config: AIFuncConfig = {},
  projectDefaults: ProjectDefaults = {}
): AsyncGenerator<string> {
  const inputValidation = validate(input, artifact.api.input);
  if (!inputValidation.valid) {
    throw new Error(`Input validation failed:\n${inputValidation.errors.join('\n')}`);
  }

  if (config.mock) {
    yield* executeMockStream(artifact, input, config);
    return;
  }

  const prompt = renderPrompt(artifact, input);
  const requestParams = buildStreamRequest(artifact, prompt, config);

  yield* sendStreamRequest(config, requestParams, projectDefaults);
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
      throw new Error(`Auto-generated mock output validation failed:\n${outputValidation.errors.join('\n')}`);
    }
    return generated as TOutput;
  }

  const outputValidation = validate(output, artifact.api.output);
  if (!outputValidation.valid) {
    throw new Error(`Mock output validation failed:\n${outputValidation.errors.join('\n')}`);
  }

  return output as TOutput;
}

async function* executeMockStream(
  artifact: AIFuncArtifact,
  input: Record<string, unknown>,
  config: AIFuncConfig
): AsyncGenerator<string> {
  const mockEntries = resolveMockEntries(config);
  const output = findMockOutput(mockEntries, input);

  // Stream output schema is type:string, so mock value is a plain string.
  // For legacy object mocks, grab the first string field; fall back to generated placeholder.
  let text: string;
  if (typeof output === 'string') {
    text = output;
  } else if (output !== null && typeof output === 'object') {
    const first = Object.values(output as Record<string, unknown>).find(v => typeof v === 'string');
    text = typeof first === 'string' ? first : JSON.stringify(output, null, 2);
  } else {
    text = String(generateFromSchema(artifact.api.output) ?? '(mock output)');
  }

  const words = text.split(/(\s+)/);
  for (const word of words) {
    if (word) {
      yield word;
      await sleep(30 + Math.random() * 60);
    }
  }
}

function resolveMockEntries(config: AIFuncConfig): MockEntry[] {
  if (!config.mockData) return [];
  return Array.isArray(config.mockData) ? config.mockData : config.mockData.cases ?? [];
}

function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
