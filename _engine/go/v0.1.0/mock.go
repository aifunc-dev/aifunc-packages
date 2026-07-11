// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

func FindMockOutput(entries []MockEntry, inputData map[string]any) map[string]any {
	var fallback map[string]any

	for _, entry := range entries {
		if entry.Input == nil {
			if fallback == nil {
				fallback = entry.Output
			}
			continue
		}
		if deepEqual(entry.Input, inputData) {
			return entry.Output
		}
	}

	return fallback
}

func GenerateFromSchema(schema map[string]any) any {
	if def, ok := schema["default"]; ok {
		return def
	}
	if enumVal, ok := schema["enum"]; ok {
		if arr, ok := enumVal.([]any); ok && len(arr) > 0 {
			return arr[0]
		}
	}

	schemaType := ""
	if t, ok := schema["type"]; ok {
		switch v := t.(type) {
		case string:
			schemaType = v
		case []any:
			if len(v) > 0 {
				if s, ok := v[0].(string); ok {
					schemaType = s
				}
			}
		}
	}

	switch schemaType {
	case "string":
		if desc, ok := schema["description"].(string); ok {
			return desc
		}
		return ""
	case "number", "integer":
		return float64(0)
	case "boolean":
		return false
	case "null":
		return nil
	case "array":
		if items, ok := schema["items"].(map[string]any); ok {
			return []any{GenerateFromSchema(items)}
		}
		return []any{}
	case "object":
		obj := map[string]any{}
		if props, ok := schema["properties"].(map[string]any); ok {
			for key, propSchema := range props {
				if ps, ok := propSchema.(map[string]any); ok {
					obj[key] = GenerateFromSchema(ps)
				}
			}
		}
		return obj
	default:
		return nil
	}
}
