// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

export interface AIFuncConfig {
  baseURL?: string;
  apiKey?: string;
  model?: string;
  temperature?: number;
  topP?: number;
  maxTokens?: number;
  timeout?: number;
  maxRetries?: number;
  mock?: boolean;
  mockData?: MockEntry[] | MockFile;
}

export interface JSONSchema {
  type?: string | string[];
  description?: string;
  required?: string[];
  enum?: unknown[];
  properties?: Record<string, JSONSchema>;
  items?: JSONSchema;
  default?: unknown;
  [key: string]: unknown;
}

export interface AIFuncArtifact {
  schemaVersion?: string;
  artifactVersion?: string;
  package?: {
    schemaVersion?: string;
    type?: string;
    name?: string;
    version?: string;
    description?: string;
    author?: { name?: string } | null;
    engine?: string;
    engineOptions?: {
      injectOutputSchema?: boolean;
    } | null;
  };
  api: {
    name?: string;
    description?: string;
    input: JSONSchema;
    output: JSONSchema;
    injectOutputSchema?: boolean;
  };
  modelParams?: {
    schemaVersion?: string;
    rules?: ModelParamPreset[] | null;
  } | null;
  modelRouting?: {
    schemaVersion?: string;
    default?: string;
    fallback?: string[] | null;
    allowed?: string[] | null;
    denied?: string[] | null;
  } | null;
  prompts?: Record<string, string>;
  metadata?: {
    sourcePackageVersion?: string;
    generatedAt?: string;
    contentHash?: string;
  };

  name?: string;
  description?: string;
  engineVersion?: string;
  prompt?: string;
  provider?: string;
  model?: {
    temperature?: number;
    maxTokens?: number;
  };
  mockFile?: string;
}

export interface ModelParamPreset {
  match: {
    model?: string;
    models?: string[] | null;
    pattern?: string;
  };
  params: {
    temperature?: number;
    topP?: number;
    maxTokens?: number;
    structuredOutput?: boolean;
  };
  providerParams?: Record<string, unknown>;
}

export interface ModelRequestParams {
  model: string;
  messages: Array<{
    role: 'system' | 'user' | 'assistant';
    content: string;
  }>;
  temperature?: number;
  top_p?: number;
  max_tokens?: number;
  response_format?: { type: 'json_object' | 'text' };
}

export interface ModelResponse {
  id: string;
  object: string;
  created: number;
  model: string;
  choices: Array<{
    index: number;
    message: {
      role: string;
      content: string;
    };
    finish_reason: string;
  }>;
  usage?: {
    prompt_tokens: number;
    completion_tokens: number;
    total_tokens: number;
  };
}

export interface ValidationResult {
  valid: boolean;
  errors: string[];
}

export interface MockEntry {
  input?: Record<string, unknown>;
  output: Record<string, unknown>;
}

export interface MockFile {
  version?: string;
  cases?: MockEntry[];
  [key: string]: unknown;
}
