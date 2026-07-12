// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Zero-dependency JSON parser and serializer.
 *
 * <p>Parsed values are represented as standard Java types:
 * <ul>
 *   <li>JSON object  → {@code Map<String, Object>}
 *   <li>JSON array   → {@code List<Object>}
 *   <li>JSON string  → {@code String}
 *   <li>JSON number  → {@code Double}
 *   <li>JSON boolean → {@code Boolean}
 *   <li>JSON null    → {@code null}
 * </ul>
 */
public final class Json {

    private Json() {}

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /** Parses a JSON string and returns the corresponding Java object. */
    public static Object parse(String json) {
        if (json == null) throw new JsonParseException("Input is null");
        Parser p = new Parser(json.trim());
        Object result = p.parseValue();
        p.skipWhitespace();
        if (p.pos < p.src.length()) {
            throw new JsonParseException("Unexpected trailing content at position " + p.pos);
        }
        return result;
    }

    /** Parses a JSON string as a {@code Map<String, Object>}. */
    @SuppressWarnings("unchecked")
    public static Map<String, Object> parseObject(String json) {
        Object v = parse(json);
        if (!(v instanceof Map)) {
            throw new JsonParseException("Expected JSON object, got: " + typeName(v));
        }
        return (Map<String, Object>) v;
    }

    /** Serializes a Java value to a compact JSON string. */
    public static String stringify(Object value) {
        StringBuilder sb = new StringBuilder();
        writeValue(value, sb, -1);
        return sb.toString();
    }

    /** Serializes a Java value to a pretty-printed JSON string with 2-space indentation. */
    public static String prettyPrint(Object value) {
        StringBuilder sb = new StringBuilder();
        writeValue(value, sb, 0);
        return sb.toString();
    }

    // -------------------------------------------------------------------------
    // Serializer
    // -------------------------------------------------------------------------

    @SuppressWarnings("unchecked")
    private static void writeValue(Object value, StringBuilder sb, int indent) {
        if (value == null) {
            sb.append("null");
        } else if (value instanceof Boolean) {
            sb.append((Boolean) value ? "true" : "false");
        } else if (value instanceof Number) {
            double d = ((Number) value).doubleValue();
            if (d == Math.floor(d) && !Double.isInfinite(d) && Math.abs(d) < 1e15) {
                sb.append((long) d);
            } else {
                sb.append(d);
            }
        } else if (value instanceof String) {
            writeString((String) value, sb);
        } else if (value instanceof List) {
            writeList((List<Object>) value, sb, indent);
        } else if (value instanceof Map) {
            writeMap((Map<String, Object>) value, sb, indent);
        } else {
            writeString(value.toString(), sb);
        }
    }

    private static void writeString(String s, StringBuilder sb) {
        sb.append('"');
        for (int i = 0; i < s.length(); i++) {
            char c = s.charAt(i);
            switch (c) {
                case '"':  sb.append("\\\""); break;
                case '\\': sb.append("\\\\"); break;
                case '\b': sb.append("\\b");  break;
                case '\f': sb.append("\\f");  break;
                case '\n': sb.append("\\n");  break;
                case '\r': sb.append("\\r");  break;
                case '\t': sb.append("\\t");  break;
                default:
                    if (c < 0x20) {
                        sb.append(String.format("\\u%04x", (int) c));
                    } else {
                        sb.append(c);
                    }
            }
        }
        sb.append('"');
    }

    private static void writeList(List<Object> list, StringBuilder sb, int indent) {
        if (list.isEmpty()) { sb.append("[]"); return; }
        sb.append('[');
        int child = indent >= 0 ? indent + 1 : -1;
        for (int i = 0; i < list.size(); i++) {
            if (indent >= 0) { sb.append('\n'); appendIndent(sb, child); }
            writeValue(list.get(i), sb, child);
            if (i < list.size() - 1) sb.append(',');
        }
        if (indent >= 0) { sb.append('\n'); appendIndent(sb, indent); }
        sb.append(']');
    }

    private static void writeMap(Map<String, Object> map, StringBuilder sb, int indent) {
        if (map.isEmpty()) { sb.append("{}"); return; }
        sb.append('{');
        int child = indent >= 0 ? indent + 1 : -1;
        int i = 0;
        for (Map.Entry<String, Object> entry : map.entrySet()) {
            if (indent >= 0) { sb.append('\n'); appendIndent(sb, child); }
            writeString(entry.getKey(), sb);
            sb.append(':');
            if (indent >= 0) sb.append(' ');
            writeValue(entry.getValue(), sb, child);
            if (i < map.size() - 1) sb.append(',');
            i++;
        }
        if (indent >= 0) { sb.append('\n'); appendIndent(sb, indent); }
        sb.append('}');
    }

    private static void appendIndent(StringBuilder sb, int level) {
        for (int i = 0; i < level * 2; i++) sb.append(' ');
    }

    // -------------------------------------------------------------------------
    // Parser (recursive descent)
    // -------------------------------------------------------------------------

    static final class Parser {
        final String src;
        int pos;

        Parser(String src) { this.src = src; this.pos = 0; }

        Object parseValue() {
            skipWhitespace();
            if (pos >= src.length()) throw new JsonParseException("Unexpected end of input");
            char c = src.charAt(pos);
            if (c == '"') return parseString();
            if (c == '{') return parseObject();
            if (c == '[') return parseArray();
            if (c == 't') return parseLiteral("true",  Boolean.TRUE);
            if (c == 'f') return parseLiteral("false", Boolean.FALSE);
            if (c == 'n') return parseLiteral("null",  null);
            if (c == '-' || (c >= '0' && c <= '9')) return parseNumber();
            throw new JsonParseException("Unexpected character '" + c + "' at position " + pos);
        }

        private Map<String, Object> parseObject() {
            expect('{');
            Map<String, Object> map = new LinkedHashMap<>();
            skipWhitespace();
            if (peek() == '}') { pos++; return map; }
            while (true) {
                skipWhitespace();
                String key = parseString();
                skipWhitespace(); expect(':');
                Object value = parseValue();
                map.put(key, value);
                skipWhitespace();
                char sep = peek();
                if (sep == '}') { pos++; break; }
                if (sep == ',') { pos++; continue; }
                throw new JsonParseException("Expected ',' or '}' at position " + pos);
            }
            return map;
        }

        private List<Object> parseArray() {
            expect('[');
            List<Object> list = new ArrayList<>();
            skipWhitespace();
            if (peek() == ']') { pos++; return list; }
            while (true) {
                list.add(parseValue());
                skipWhitespace();
                char sep = peek();
                if (sep == ']') { pos++; break; }
                if (sep == ',') { pos++; continue; }
                throw new JsonParseException("Expected ',' or ']' at position " + pos);
            }
            return list;
        }

        String parseString() {
            skipWhitespace(); expect('"');
            StringBuilder sb = new StringBuilder();
            while (pos < src.length()) {
                char c = src.charAt(pos++);
                if (c == '"') return sb.toString();
                if (c != '\\') { sb.append(c); continue; }
                if (pos >= src.length()) break;
                char esc = src.charAt(pos++);
                switch (esc) {
                    case '"':  sb.append('"');  break;
                    case '\\': sb.append('\\'); break;
                    case '/':  sb.append('/');  break;
                    case 'b':  sb.append('\b'); break;
                    case 'f':  sb.append('\f'); break;
                    case 'n':  sb.append('\n'); break;
                    case 'r':  sb.append('\r'); break;
                    case 't':  sb.append('\t'); break;
                    case 'u':
                        if (pos + 4 > src.length())
                            throw new JsonParseException("Incomplete \\u escape");
                        sb.append((char) Integer.parseInt(src.substring(pos, pos + 4), 16));
                        pos += 4; break;
                    default:
                        throw new JsonParseException("Invalid escape '\\" + esc + "'");
                }
            }
            throw new JsonParseException("Unterminated string");
        }

        private Double parseNumber() {
            int start = pos;
            if (pos < src.length() && src.charAt(pos) == '-') pos++;
            while (pos < src.length() && src.charAt(pos) >= '0' && src.charAt(pos) <= '9') pos++;
            if (pos < src.length() && src.charAt(pos) == '.') {
                pos++;
                while (pos < src.length() && src.charAt(pos) >= '0' && src.charAt(pos) <= '9') pos++;
            }
            if (pos < src.length() && (src.charAt(pos) == 'e' || src.charAt(pos) == 'E')) {
                pos++;
                if (pos < src.length() && (src.charAt(pos) == '+' || src.charAt(pos) == '-')) pos++;
                while (pos < src.length() && src.charAt(pos) >= '0' && src.charAt(pos) <= '9') pos++;
            }
            try {
                return Double.parseDouble(src.substring(start, pos));
            } catch (NumberFormatException e) {
                throw new JsonParseException("Invalid number at position " + start);
            }
        }

        private Object parseLiteral(String literal, Object value) {
            if (src.startsWith(literal, pos)) { pos += literal.length(); return value; }
            throw new JsonParseException("Expected '" + literal + "' at position " + pos);
        }

        void skipWhitespace() {
            while (pos < src.length()) {
                char c = src.charAt(pos);
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') pos++;
                else break;
            }
        }

        private char peek() {
            if (pos >= src.length())
                throw new JsonParseException("Unexpected end of input");
            return src.charAt(pos);
        }

        private void expect(char c) {
            skipWhitespace();
            if (pos >= src.length() || src.charAt(pos) != c) {
                String got = pos < src.length() ? String.valueOf(src.charAt(pos)) : "EOF";
                throw new JsonParseException("Expected '" + c + "' but got '" + got + "' at position " + pos);
            }
            pos++;
        }
    }

    private static String typeName(Object v) {
        if (v == null) return "null";
        if (v instanceof Map) return "object";
        if (v instanceof List) return "array";
        if (v instanceof String) return "string";
        if (v instanceof Number) return "number";
        if (v instanceof Boolean) return "boolean";
        return v.getClass().getSimpleName();
    }

    // -------------------------------------------------------------------------
    // Exception
    // -------------------------------------------------------------------------

    /** Thrown when the JSON input cannot be parsed. */
    public static final class JsonParseException extends RuntimeException {
        public JsonParseException(String message) { super(message); }
    }
}
