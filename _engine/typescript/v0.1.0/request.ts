// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { ModelRequestParams, ModelResponse, AIFuncConfig } from './types';

const ENGINE_DEFAULT_TIMEOUT = 7000;
const ENGINE_DEFAULT_MAX_RETRIES = 1;

/** Subset of aifunc.json fields injected at build time by the CLI. */
export interface ProjectDefaults {
  timeout?: number;
  maxRetries?: number;
}

export async function sendRequest(
  config: AIFuncConfig,
  params: ModelRequestParams,
  projectDefaults: ProjectDefaults = {}
): Promise<ModelResponse> {
  if (!config.baseURL) {
    throw new Error('AIFuncConfig.baseURL is required when mock mode is disabled');
  }
  if (!config.apiKey) {
    throw new Error('AIFuncConfig.apiKey is required when mock mode is disabled');
  }

  const base = config.baseURL.replace(/\/$/, '');
  const url = base.endsWith('/chat/completions')
    ? base
    : `${base}/chat/completions`;

  // Priority: user config > aifunc.json (projectDefaults) > engine default
  const timeout = config.timeout ?? projectDefaults.timeout ?? ENGINE_DEFAULT_TIMEOUT;
  const maxRetries = config.maxRetries ?? projectDefaults.maxRetries ?? ENGINE_DEFAULT_MAX_RETRIES;

  let lastError: Error = new Error('Unknown error during model request');

  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await _doRequest(url, config.apiKey, params, timeout);
    } catch (err) {
      lastError = err instanceof Error ? err : new Error(String(err));
      if (attempt < maxRetries) {
        continue;
      }
    }
  }

  throw lastError;
}

async function _doRequest(
  url: string,
  apiKey: string,
  params: ModelRequestParams,
  timeout: number
): Promise<ModelResponse> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`,
      },
      body: JSON.stringify(params),
      signal: controller.signal,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Model API returned ${response.status}: ${errorText.slice(0, 500)}`);
    }

    return await response.json() as ModelResponse;
  } catch (error) {
    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        throw new Error(`Request timeout after ${timeout}ms`);
      }
      throw error;
    }
    throw new Error('Unknown error during model request');
  } finally {
    clearTimeout(timeoutId);
  }
}
