// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aifunc.Engine.Csharp.V0_2_0;

/// <summary>
/// Zero-dependency JSON parser and serializer built on System.Text.Json.Nodes.
///
/// Parsed values are represented as standard BCL types:
/// <list type="bullet">
///   <item>JSON object  â†?<c>Dictionary&lt;string, object?&gt;</c></item>
///   <item>JSON array   â†?<c>List&lt;object?&gt;</c></item>
///   <item>JSON string  â†?<c>string</c></item>
///   <item>JSON number  â†?<c>double</c></item>
///   <item>JSON boolean â†?<c>bool</c></item>
///   <item>JSON null    â†?<c>null</c></item>
/// </list>
/// </summary>
public static class Json
{
    /// <summary>Parses a JSON string and returns the corresponding BCL object.</summary>
    public static object? Parse(string json)
    {
        if (json is null) throw new JsonParseException("Input is null");
        try
        {
            var node = JsonNode.Parse(json.Trim());
            return FromNode(node);
        }
        catch (JsonException e)
        {
            throw new JsonParseException(e.Message);
        }
    }

    /// <summary>Parses a JSON string as a <c>Dictionary&lt;string, object?&gt;</c>.</summary>
    public static Dictionary<string, object?> ParseObject(string json)
    {
        var v = Parse(json);
        if (v is not Dictionary<string, object?> map)
        {
            throw new JsonParseException("Expected JSON object, got: " + TypeName(v));
        }
        return map;
    }

    /// <summary>Serializes a BCL value to a compact JSON string.</summary>
    public static string Stringify(object? value)
    {
        var sb = new StringBuilder();
        WriteValue(value, sb, -1);
        return sb.ToString();
    }

    /// <summary>Serializes a BCL value to a pretty-printed JSON string with 2-space indentation.</summary>
    public static string PrettyPrint(object? value)
    {
        var sb = new StringBuilder();
        WriteValue(value, sb, 0);
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Node â†?BCL
    // -------------------------------------------------------------------------

    private static object? FromNode(JsonNode? node)
    {
        if (node is null) return null;

        if (node is JsonObject obj)
        {
            var map = new Dictionary<string, object?>();
            foreach (var prop in obj)
            {
                map[prop.Key] = FromNode(prop.Value);
            }
            return map;
        }

        if (node is JsonArray arr)
        {
            var list = new List<object?>();
            foreach (var item in arr)
            {
                list.Add(FromNode(item));
            }
            return list;
        }

        if (node is JsonValue val)
        {
            // Prefer explicit typed getters so numbers always become double.
            if (val.TryGetValue<bool>(out var b)) return b;
            if (val.TryGetValue<string>(out var s)) return s;
            if (val.TryGetValue<double>(out var d)) return d;
            // JsonValue may hold JsonElement for some payloads.
            var element = val.GetValue<object?>();
            if (element is JsonElement je) return FromElement(je);
            if (element is null) return null;
            if (element is bool bb) return bb;
            if (element is string ss) return ss;
            if (element is IConvertible)
            {
                try { return Convert.ToDouble(element, CultureInfo.InvariantCulture); }
                catch { /* fall through */ }
            }
            return element.ToString();
        }

        return null;
    }

    private static object? FromElement(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                return el.GetDouble();
            case JsonValueKind.Object:
            {
                var map = new Dictionary<string, object?>();
                foreach (var prop in el.EnumerateObject())
                {
                    map[prop.Name] = FromElement(prop.Value);
                }
                return map;
            }
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in el.EnumerateArray())
                {
                    list.Add(FromElement(item));
                }
                return list;
            }
            default:
                return null;
        }
    }

    // -------------------------------------------------------------------------
    // Serializer
    // -------------------------------------------------------------------------

    private static void WriteValue(object? value, StringBuilder sb, int indent)
    {
        if (value is null)
        {
            sb.Append("null");
        }
        else if (value is bool b)
        {
            sb.Append(b ? "true" : "false");
        }
        else if (value is byte or sbyte or short or ushort or int or uint or long or ulong
                 or float or double or decimal)
        {
            var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (d == Math.Floor(d) && !double.IsInfinity(d) && Math.Abs(d) < 1e15)
            {
                sb.Append((long)d);
            }
            else
            {
                sb.Append(d.ToString(CultureInfo.InvariantCulture));
            }
        }
        else if (value is string s)
        {
            WriteString(s, sb);
        }
        else if (value is IList<object?> list)
        {
            WriteList(list, sb, indent);
        }
        else if (value is System.Collections.IList rawList)
        {
            var converted = new List<object?>();
            foreach (var item in rawList) converted.Add(item);
            WriteList(converted, sb, indent);
        }
        else if (value is IDictionary<string, object?> map)
        {
            WriteMap(map, sb, indent);
        }
        else if (value is System.Collections.IDictionary rawMap)
        {
            var converted = new Dictionary<string, object?>();
            foreach (System.Collections.DictionaryEntry entry in rawMap)
            {
                converted[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? ""] = entry.Value;
            }
            WriteMap(converted, sb, indent);
        }
        else
        {
            WriteString(value.ToString() ?? "", sb);
        }
    }

    private static void WriteString(string s, StringBuilder sb)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
    }

    private static void WriteList(IList<object?> list, StringBuilder sb, int indent)
    {
        if (list.Count == 0) { sb.Append("[]"); return; }
        sb.Append('[');
        var child = indent >= 0 ? indent + 1 : -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (indent >= 0) { sb.Append('\n'); AppendIndent(sb, child); }
            WriteValue(list[i], sb, child);
            if (i < list.Count - 1) sb.Append(',');
        }
        if (indent >= 0) { sb.Append('\n'); AppendIndent(sb, indent); }
        sb.Append(']');
    }

    private static void WriteMap(IDictionary<string, object?> map, StringBuilder sb, int indent)
    {
        if (map.Count == 0) { sb.Append("{}"); return; }
        sb.Append('{');
        var child = indent >= 0 ? indent + 1 : -1;
        var i = 0;
        foreach (var entry in map)
        {
            if (indent >= 0) { sb.Append('\n'); AppendIndent(sb, child); }
            WriteString(entry.Key, sb);
            sb.Append(':');
            if (indent >= 0) sb.Append(' ');
            WriteValue(entry.Value, sb, child);
            if (i < map.Count - 1) sb.Append(',');
            i++;
        }
        if (indent >= 0) { sb.Append('\n'); AppendIndent(sb, indent); }
        sb.Append('}');
    }

    private static void AppendIndent(StringBuilder sb, int level)
    {
        for (var i = 0; i < level * 2; i++) sb.Append(' ');
    }

    private static string TypeName(object? v)
    {
        if (v is null) return "null";
        if (v is IDictionary<string, object?> or System.Collections.IDictionary) return "object";
        if (v is IList<object?> or System.Collections.IList) return "array";
        if (v is string) return "string";
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal) return "number";
        if (v is bool) return "boolean";
        return v.GetType().Name;
    }

    /// <summary>Thrown when the JSON input cannot be parsed.</summary>
    public sealed class JsonParseException : Exception
    {
        public JsonParseException(string message) : base(message) {}
    }
}
