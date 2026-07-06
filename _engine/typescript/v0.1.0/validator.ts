// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

import type { JSONSchema, ValidationResult } from './types';

export function validate(
  data: unknown,
  schema: JSONSchema,
  path: string = 'root'
): ValidationResult {
  const errors: string[] = [];

  if (schema.type) {
    const types = Array.isArray(schema.type) ? schema.type : [schema.type];
    const actualType = getType(data);

    const typeMatch = types.includes(actualType) ||
      (actualType === 'integer' && types.includes('number'));
    if (!typeMatch) {
      errors.push(`${path}: expected type ${types.join(' | ')}, got ${actualType}`);
      return { valid: false, errors };
    }
  }

  if (schema.enum) {
    if (!schema.enum.includes(data)) {
      const enumValues = schema.enum.map(v => JSON.stringify(v)).join(', ');
      errors.push(`${path}: value must be one of [${enumValues}], got ${JSON.stringify(data)}`);
      return { valid: false, errors };
    }
  }

  if (hasType(schema, 'object') && typeof data === 'object' && data !== null && !Array.isArray(data)) {
    const obj = data as Record<string, unknown>;

    if (schema.required) {
      for (const key of schema.required) {
        if (!(key in obj)) {
          errors.push(`${path}: missing required property '${key}'`);
        }
      }
    }

    if (schema.properties) {
      for (const [key, propSchema] of Object.entries(schema.properties)) {
        if (key in obj) {
          const result = validate(obj[key], propSchema, `${path}.${key}`);
          errors.push(...result.errors);
        }
      }
    }
  }

  if (hasType(schema, 'array') && Array.isArray(data)) {
    if (schema.items) {
      for (let i = 0; i < data.length; i++) {
        const result = validate(data[i], schema.items, `${path}[${i}]`);
        errors.push(...result.errors);
      }
    }
  }

  return {
    valid: errors.length === 0,
    errors,
  };
}

function hasType(schema: JSONSchema, type: string): boolean {
  if (!schema.type) return false;
  return Array.isArray(schema.type) ? schema.type.includes(type) : schema.type === type;
}

function getType(value: unknown): string {
  if (value === null) return 'null';
  if (Array.isArray(value)) return 'array';
  if (typeof value === 'number') return Number.isInteger(value) ? 'integer' : 'number';
  return typeof value;
}
