// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_2_0;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * JSON Schema subset validator.
 *
 * <p>Supported keywords: {@code type}, {@code enum}, {@code required},
 * {@code properties}, {@code items}.
 */
public final class Validator {

    private Validator() {}

    /**
     * Validates {@code data} against {@code schema}.
     *
     * @param data   the value to validate (any JSON-mapped Java type)
     * @param schema the JSON Schema as a {@code Map<String, Object>}
     * @param path   human-readable path for error messages (use {@code "root"} at the top level)
     */
    @SuppressWarnings("unchecked")
    public static Types.ValidationResult validate(Object data, Map<String, Object> schema, String path) {
        if (path == null || path.isBlank()) path = "root";
        List<String> errors = new ArrayList<>();
        validateValue(data, schema, path, errors);
        return new Types.ValidationResult(errors.isEmpty(), errors);
    }

    // -------------------------------------------------------------------------
    // Internal implementation
    // -------------------------------------------------------------------------

    @SuppressWarnings("unchecked")
    private static void validateValue(
            Object data, Map<String, Object> schema, String path, List<String> errors) {

        // --- type check ---
        Object typeVal = schema.get("type");
        if (typeVal != null) {
            List<String> types = toStringList(typeVal);
            String actual = jsonType(data);

            boolean matched = false;
            for (String t : types) {
                if (t.equals(actual)) { matched = true; break; }
                // integer satisfies "number" in JSON Schema
                if ("integer".equals(actual) && "number".equals(t)) { matched = true; break; }
            }
            if (!matched) {
                errors.add(path + ": expected type " + String.join(" | ", types) + ", got " + actual);
                return;
            }
        }

        // --- enum check ---
        Object enumVal = schema.get("enum");
        if (enumVal instanceof List) {
            List<?> allowed = (List<?>) enumVal;
            boolean found = false;
            for (Object v : allowed) {
                if (deepEqual(v, data)) { found = true; break; }
            }
            if (!found) {
                errors.add(path + ": value must be one of " + allowed + ", got " + data);
                return;
            }
        }

        // --- object ---
        if (hasType(schema, "object") && data instanceof Map) {
            Map<String, Object> obj = (Map<String, Object>) data;

            Object reqVal = schema.get("required");
            if (reqVal instanceof List) {
                for (String key : toStringList(reqVal)) {
                    if (!obj.containsKey(key)) {
                        errors.add(path + ": missing required property '" + key + "'");
                    }
                }
            }

            Object propsVal = schema.get("properties");
            if (propsVal instanceof Map) {
                Map<String, Object> props = (Map<String, Object>) propsVal;
                for (Map.Entry<String, Object> entry : props.entrySet()) {
                    String key = entry.getKey();
                    if (obj.containsKey(key) && entry.getValue() instanceof Map) {
                        validateValue(obj.get(key), (Map<String, Object>) entry.getValue(),
                                path + "." + key, errors);
                    }
                }
            }
        }

        // --- array ---
        if (hasType(schema, "array") && data instanceof List) {
            List<?> arr = (List<?>) data;
            Object itemsVal = schema.get("items");
            if (itemsVal instanceof Map) {
                Map<String, Object> itemSchema = (Map<String, Object>) itemsVal;
                for (int i = 0; i < arr.size(); i++) {
                    validateValue(arr.get(i), itemSchema, path + "[" + i + "]", errors);
                }
            }
        }
    }

    private static boolean hasType(Map<String, Object> schema, String typeName) {
        Object typeVal = schema.get("type");
        if (typeVal == null) return false;
        return toStringList(typeVal).contains(typeName);
    }

    /** Returns the JSON Schema type name for a Java value. */
    static String jsonType(Object v) {
        if (v == null)             return "null";
        if (v instanceof Boolean)  return "boolean";
        if (v instanceof String)   return "string";
        if (v instanceof List)     return "array";
        if (v instanceof Map)      return "object";
        if (v instanceof Number) {
            double d = ((Number) v).doubleValue();
            if (d == Math.floor(d) && !Double.isInfinite(d)) return "integer";
            return "number";
        }
        return "unknown";
    }

    private static List<String> toStringList(Object val) {
        List<String> result = new ArrayList<>();
        if (val instanceof String) {
            result.add((String) val);
        } else if (val instanceof List) {
            for (Object item : (List<?>) val) {
                if (item instanceof String) result.add((String) item);
            }
        }
        return result;
    }

    /** Deep-equality check compatible with JSON-mapped Java types. */
    @SuppressWarnings("unchecked")
    static boolean deepEqual(Object a, Object b) {
        if (a == b) return true;
        if (a == null || b == null) return false;

        if (a instanceof Map && b instanceof Map) {
            Map<String, Object> ma = (Map<String, Object>) a;
            Map<String, Object> mb = (Map<String, Object>) b;
            if (ma.size() != mb.size()) return false;
            for (Map.Entry<String, Object> e : ma.entrySet()) {
                if (!mb.containsKey(e.getKey())) return false;
                if (!deepEqual(e.getValue(), mb.get(e.getKey()))) return false;
            }
            return true;
        }

        if (a instanceof List && b instanceof List) {
            List<Object> la = (List<Object>) a;
            List<Object> lb = (List<Object>) b;
            if (la.size() != lb.size()) return false;
            for (int i = 0; i < la.size(); i++) {
                if (!deepEqual(la.get(i), lb.get(i))) return false;
            }
            return true;
        }

        // Numeric comparison across different Number subtypes
        if (a instanceof Number && b instanceof Number) {
            return ((Number) a).doubleValue() == ((Number) b).doubleValue();
        }

        return a.equals(b);
    }
}
