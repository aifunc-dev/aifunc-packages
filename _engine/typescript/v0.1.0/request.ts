// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { ModelRequestParams, ModelResponse, AIFuncConfig } from './types';

export async function sendRequest(
  config: AIFuncConfig,
  params: ModelRequestParams
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
  const timeout = config.timeout ?? 30000;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${config.apiKey}`,
      },
      body: JSON.stringify(params),
      signal: controller.signal,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(
        `Model API returned ${response.status}: ${errorText.slice(0, 500)}`
      );
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
