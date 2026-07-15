// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"fmt"
	"reflect"
	"strings"
)

// Validate checks data against a JSON Schema subset.
// Supported keywords: type, enum, required, properties, items.
func Validate(data any, schema map[string]any, path string) ValidationResult {
	if path == "" {
		path = "root"
	}
	var errors []string
	validateValue(data, schema, path, &errors)
	return ValidationResult{Valid: len(errors) == 0, Errors: errors}
}

func validateValue(data any, schema map[string]any, path string, errors *[]string) {
	if typeVal, ok := schema["type"]; ok {
		types := toStringSlice(typeVal)
		actual := jsonType(data)

		matched := false
		for _, t := range types {
			if t == actual {
				matched = true
				break
			}
			// integer satisfies "number" in JSON Schema
			if actual == "integer" && t == "number" {
				matched = true
				break
			}
		}
		if !matched {
			*errors = append(*errors,
				fmt.Sprintf("%s: expected type %s, got %s", path, strings.Join(types, " | "), actual))
			return
		}
	}

	if enumVal, ok := schema["enum"]; ok {
		if arr, ok := enumVal.([]any); ok {
			found := false
			for _, v := range arr {
				if deepEqual(v, data) {
					found = true
					break
				}
			}
			if !found {
				*errors = append(*errors,
					fmt.Sprintf("%s: value must be one of %v, got %v", path, arr, data))
				return
			}
		}
	}

	if hasSchemaType(schema, "object") {
		if obj, ok := data.(map[string]any); ok {
			if reqVal, ok := schema["required"]; ok {
				for _, key := range toStringSlice(reqVal) {
					if _, exists := obj[key]; !exists {
						*errors = append(*errors,
							fmt.Sprintf("%s: missing required property '%s'", path, key))
					}
				}
			}
			if propsVal, ok := schema["properties"]; ok {
				if props, ok := propsVal.(map[string]any); ok {
					for key, propSchema := range props {
						if val, exists := obj[key]; exists {
							if ps, ok := propSchema.(map[string]any); ok {
								validateValue(val, ps, path+"."+key, errors)
							}
						}
					}
				}
			}
		}
	}

	if hasSchemaType(schema, "array") {
		if arr, ok := data.([]any); ok {
			if itemsVal, ok := schema["items"]; ok {
				if itemSchema, ok := itemsVal.(map[string]any); ok {
					for i, item := range arr {
						validateValue(item, itemSchema, fmt.Sprintf("%s[%d]", path, i), errors)
					}
				}
			}
		}
	}
}

func hasSchemaType(schema map[string]any, typeName string) bool {
	typeVal, ok := schema["type"]
	if !ok {
		return false
	}
	for _, t := range toStringSlice(typeVal) {
		if t == typeName {
			return true
		}
	}
	return false
}

func jsonType(v any) string {
	if v == nil {
		return "null"
	}
	switch f := v.(type) {
	case bool:
		return "boolean"
	case float64:
		if f == float64(int64(f)) {
			return "integer"
		}
		return "number"
	case int, int8, int16, int32, int64,
		uint, uint8, uint16, uint32, uint64:
		return "integer"
	case string:
		return "string"
	case []any:
		return "array"
	case map[string]any:
		return "object"
	default:
		_ = f
		rv := reflect.ValueOf(v)
		if rv.Kind() == reflect.Slice {
			return "array"
		}
		if rv.Kind() == reflect.Map {
			return "object"
		}
		return "unknown"
	}
}

func toStringSlice(v any) []string {
	switch val := v.(type) {
	case string:
		return []string{val}
	case []any:
		var result []string
		for _, item := range val {
			if s, ok := item.(string); ok {
				result = append(result, s)
			}
		}
		return result
	case []string:
		return val
	}
	return nil
}

func deepEqual(a, b any) bool {
	if a == b {
		return true
	}
	aMap, aIsMap := a.(map[string]any)
	bMap, bIsMap := b.(map[string]any)
	if aIsMap && bIsMap {
		if len(aMap) != len(bMap) {
			return false
		}
		for k, av := range aMap {
			bv, ok := bMap[k]
			if !ok || !deepEqual(av, bv) {
				return false
			}
		}
		return true
	}
	aArr, aIsArr := a.([]any)
	bArr, bIsArr := b.([]any)
	if aIsArr && bIsArr {
		if len(aArr) != len(bArr) {
			return false
		}
		for i := range aArr {
			if !deepEqual(aArr[i], bArr[i]) {
				return false
			}
		}
		return true
	}
	// Handle numeric comparison across different Go types (float64 vs int64)
	af, aIsFloat := toFloat64(a)
	bf, bIsFloat := toFloat64(b)
	if aIsFloat && bIsFloat {
		return af == bf
	}
	return false
}

func toFloat64(v any) (float64, bool) {
	switch n := v.(type) {
	case float64:
		return n, true
	case float32:
		return float64(n), true
	case int:
		return float64(n), true
	case int64:
		return float64(n), true
	}
	return 0, false
}
