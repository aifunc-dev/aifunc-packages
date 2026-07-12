// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import aifunc.AIFuncConfig;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/** Mock data lookup and schema-based value generation. */
public final class Mock {

    private Mock() {}

    /**
     * Finds a matching mock output for the given input.
     *
     * <p>Lookup order:
     * <ol>
     *   <li>First entry whose {@code input} field deep-equals the actual input
     *   <li>First entry with no {@code input} field (fallback default)
     *   <li>{@code null} when no entries match
     * </ol>
     */
    public static Map<String, Object> findMockOutput(List<Types.MockEntry> entries, Map<String, Object> input) {
        Map<String, Object> fallback = null;
        for (Types.MockEntry entry : entries) {
            if (entry.getInput() == null) {
                if (fallback == null) fallback = entry.getOutput();
                continue;
            }
            if (Validator.deepEqual(entry.getInput(), input)) {
                return entry.getOutput();
            }
        }
        return fallback;
    }

    /**
     * Generates a zero-value instance conforming to the given JSON Schema.
     *
     * <ul>
     *   <li>Returns the {@code default} value if present
     *   <li>Returns the first {@code enum} value if present
     *   <li>Otherwise generates: {@code ""} / {@code 0.0} / {@code false} /
     *       {@code []} / {@code {}} based on type
     * </ul>
     */
    @SuppressWarnings("unchecked")
    public static Object generateFromSchema(Map<String, Object> schema) {
        if (schema == null) return null;

        if (schema.containsKey("default")) return schema.get("default");

        Object enumVal = schema.get("enum");
        if (enumVal instanceof List) {
            List<?> arr = (List<?>) enumVal;
            if (!arr.isEmpty()) return arr.get(0);
        }

        String type = resolveType(schema);
        switch (type) {
            case "string": {
                Object desc = schema.get("description");
                return (desc instanceof String) ? (String) desc : "";
            }
            case "number":
            case "integer":
                return 0.0;
            case "boolean":
                return Boolean.FALSE;
            case "null":
                return null;
            case "array": {
                Object items = schema.get("items");
                if (items instanceof Map) {
                    List<Object> list = new ArrayList<>();
                    list.add(generateFromSchema((Map<String, Object>) items));
                    return list;
                }
                return new ArrayList<>();
            }
            case "object": {
                Map<String, Object> obj = new LinkedHashMap<>();
                Object propsVal = schema.get("properties");
                if (propsVal instanceof Map) {
                    for (Map.Entry<?, ?> e : ((Map<?, ?>) propsVal).entrySet()) {
                        if (e.getValue() instanceof Map) {
                            obj.put(String.valueOf(e.getKey()),
                                    generateFromSchema((Map<String, Object>) e.getValue()));
                        }
                    }
                }
                return obj;
            }
            default:
                return null;
        }
    }

    /**
     * Resolves mock entries from an {@link AIFuncConfig}'s {@code mockData} field.
     * Accepts a {@code List<Types.MockEntry>}, a raw {@code List<Map>}, or a
     * {@code Map} with a {@code "cases"} key.
     */
    @SuppressWarnings("unchecked")
    public static List<Types.MockEntry> resolveMockEntries(Object mockData) {
        List<Types.MockEntry> result = new ArrayList<>();
        if (mockData == null) return result;

        if (mockData instanceof List) {
            for (Object item : (List<?>) mockData) {
                if (item instanceof Types.MockEntry) {
                    result.add((Types.MockEntry) item);
                } else if (item instanceof Map) {
                    result.add(Types.MockEntry.fromMap((Map<String, Object>) item));
                }
            }
            return result;
        }

        if (mockData instanceof Map) {
            Object casesVal = ((Map<?, ?>) mockData).get("cases");
            if (casesVal instanceof List) {
                for (Object item : (List<?>) casesVal) {
                    if (item instanceof Types.MockEntry) {
                        result.add((Types.MockEntry) item);
                    } else if (item instanceof Map) {
                        result.add(Types.MockEntry.fromMap((Map<String, Object>) item));
                    }
                }
            }
        }
        return result;
    }

    private static String resolveType(Map<String, Object> schema) {
        Object typeVal = schema.get("type");
        if (typeVal instanceof String) return (String) typeVal;
        if (typeVal instanceof List) {
            List<?> list = (List<?>) typeVal;
            if (!list.isEmpty() && list.get(0) instanceof String) return (String) list.get(0);
        }
        return "";
    }
}
