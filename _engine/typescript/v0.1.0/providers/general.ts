// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { ModelRequestParams, ModelResponse, AIFuncArtifact, AIFuncConfig, ModelParamPreset } from '../types';

export function buildRequest(
  artifact: AIFuncArtifact,
  prompt: string,
  config: AIFuncConfig
): ModelRequestParams {
  if (!config.model) {
    throw new Error('AIFuncConfig.model is required when mock mode is disabled');
  }

  const params: ModelRequestParams = {
    model: config.model,
    messages: [
      {
        role: 'user',
        content: prompt,
      },
    ],
    response_format: { type: 'json_object' },
  };

  const resolved = resolveModelParams(artifact, config.model);

  const temperature = config.temperature ?? resolved.temperature ?? artifact.model?.temperature;
  if (temperature !== undefined) {
    params.temperature = temperature;
  }

  const maxTokens = config.maxTokens ?? resolved.maxTokens ?? artifact.model?.maxTokens;
  if (maxTokens !== undefined) {
    params.max_tokens = maxTokens;
  }

  return params;
}

function resolveModelParams(
  artifact: AIFuncArtifact,
  model: string
): { temperature?: number; maxTokens?: number } {
  const rules = artifact.modelParams?.rules;
  if (!rules || rules.length === 0) {
    return {};
  }

  for (const rule of rules) {
    if (matchesRule(rule, model)) {
      return {
        temperature: rule.params.temperature,
        maxTokens: rule.params.maxTokens,
      };
    }
  }

  return {};
}

function matchesRule(rule: ModelParamPreset, model: string): boolean {
  const { match } = rule;

  if (match.model && match.model === model) {
    return true;
  }

  if (match.models && match.models.includes(model)) {
    return true;
  }

  if (match.pattern) {
    return globMatch(match.pattern, model);
  }

  if (!match.model && !match.models && !match.pattern) {
    return true;
  }

  return false;
}

function globMatch(pattern: string, value: string): boolean {
  const escaped = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&');
  const regex = new RegExp('^' + escaped.replace(/\*/g, '.*') + '$');
  return regex.test(value);
}

export function parseResponse(response: ModelResponse): Record<string, unknown> {
  const choice = response.choices?.[0];
  if (!choice) {
    throw new Error('Model response contains no choices');
  }

  let content = choice.message?.content ?? '';
  content = content.trim();

  const fenceMatch = content.match(/^```(?:json)?\s*\n?([\s\S]*?)\n?```$/);
  if (fenceMatch) {
    content = fenceMatch[1].trim();
  }

  try {
    const parsed = JSON.parse(content) as Record<string, unknown>;
    return parsed;
  } catch {
    throw new Error(`Failed to parse model output as JSON: ${content.slice(0, 200)}`);
  }
}
