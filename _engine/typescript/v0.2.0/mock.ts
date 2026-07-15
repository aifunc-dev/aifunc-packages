// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { JSONSchema, MockEntry } from './types';

export function findMockOutput(
  mockData: MockEntry[],
  input: Record<string, unknown>
): Record<string, unknown> | null {
  let fallback: Record<string, unknown> | null = null;

  for (const entry of mockData) {
    if (!entry.input && fallback === null) {
      fallback = entry.output;
      continue;
    }
    if (entry.input && deepEqual(entry.input, input)) {
      return entry.output;
    }
  }

  return fallback;
}

export function generateFromSchema(schema: JSONSchema): unknown {
  if (schema.default !== undefined) return schema.default;
  if (schema.enum && schema.enum.length > 0) return schema.enum[0];

  const type = Array.isArray(schema.type) ? schema.type[0] : schema.type;

  switch (type) {
    case 'string':
      return schema.description || '';
    case 'number':
    case 'integer':
      return 0;
    case 'boolean':
      return false;
    case 'null':
      return null;
    case 'array':
      return schema.items ? [generateFromSchema(schema.items)] : [];
    case 'object': {
      const obj: Record<string, unknown> = {};
      if (schema.properties) {
        for (const [key, propSchema] of Object.entries(schema.properties)) {
          obj[key] = generateFromSchema(propSchema);
        }
      }
      return obj;
    }
    default:
      return null;
  }
}

function deepEqual(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  if (typeof a !== typeof b) return false;
  if (a === null || b === null) return a === b;

  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
      if (!deepEqual(a[i], b[i])) return false;
    }
    return true;
  }

  if (typeof a === 'object' && typeof b === 'object') {
    const aObj = a as Record<string, unknown>;
    const bObj = b as Record<string, unknown>;
    const keysA = Object.keys(aObj);
    const keysB = Object.keys(bObj);
    if (keysA.length !== keysB.length) return false;
    for (const key of keysA) {
      if (!keysB.includes(key)) return false;
      if (!deepEqual(aObj[key], bObj[key])) return false;
    }
    return true;
  }

  return false;
}
