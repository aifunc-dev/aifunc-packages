// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { ModelRequestParams, ModelResponse, AIFuncConfig } from './types';

const ENGINE_DEFAULT_TIMEOUT = 30000;
const ENGINE_DEFAULT_MAX_RETRIES = 1;

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

  const url = resolveUrl(config.baseURL);
  const timeout = config.timeout ?? projectDefaults.timeout ?? ENGINE_DEFAULT_TIMEOUT;
  const maxRetries = config.maxRetries ?? projectDefaults.maxRetries ?? ENGINE_DEFAULT_MAX_RETRIES;

  let lastError: Error = new Error('Unknown error during model request');

  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await doRequest(url, config.apiKey, params, timeout);
    } catch (err) {
      lastError = err instanceof Error ? err : new Error(String(err));
    }
  }

  throw lastError;
}

export async function* sendStreamRequest(
  config: AIFuncConfig,
  params: ModelRequestParams,
  projectDefaults: ProjectDefaults = {}
): AsyncGenerator<string> {
  if (!config.baseURL) {
    throw new Error('AIFuncConfig.baseURL is required when mock mode is disabled');
  }
  if (!config.apiKey) {
    throw new Error('AIFuncConfig.apiKey is required when mock mode is disabled');
  }

  const url = resolveUrl(config.baseURL);
  const timeout = config.timeout ?? projectDefaults.timeout ?? ENGINE_DEFAULT_TIMEOUT;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${config.apiKey}`,
      },
      body: JSON.stringify({ ...params, stream: true }),
      signal: controller.signal,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Model API returned ${response.status}: ${errorText.slice(0, 500)}`);
    }

    if (!response.body) {
      throw new Error('Response body is null; server may not support streaming');
    }

    yield* readSSEStream(response.body);
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      return;
    }
    throw error;
  } finally {
    controller.abort();
    clearTimeout(timeoutId);
  }
}

/** Duck-typed stream body; avoids the DOM `ReadableStream` global (missing from lib ES2020). */
type ByteReadable = {
  getReader(): {
    read(): Promise<{ done: boolean; value?: Uint8Array }>;
    releaseLock(): void;
  };
};

async function* readSSEStream(body: ByteReadable): AsyncGenerator<string> {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      const lines = buffer.split('\n');
      buffer = lines.pop() ?? '';

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed.startsWith('data:')) continue;

        const data = trimmed.slice(5).trim();
        if (data === '[DONE]') return;

        try {
          const chunk = JSON.parse(data) as {
            choices?: Array<{ delta?: { content?: string }; finish_reason?: string | null }>;
          };
          const content = chunk.choices?.[0]?.delta?.content;
          if (content) yield content;
        } catch {
          // skip malformed SSE lines
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}

function resolveUrl(baseURL: string): string {
  const base = baseURL.replace(/\/$/, '');
  return base.endsWith('/chat/completions') ? base : `${base}/chat/completions`;
}

async function doRequest(
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
    if (error instanceof Error && error.name === 'AbortError') {
      throw new Error(`Request timeout after ${timeout}ms`);
    }
    throw error;
  } finally {
    clearTimeout(timeoutId);
  }
}