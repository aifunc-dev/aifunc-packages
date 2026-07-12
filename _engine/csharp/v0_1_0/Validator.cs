// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>
/// JSON Schema subset validator.
///
/// Supported keywords: <c>type</c>, <c>enum</c>, <c>required</c>,
/// <c>properties</c>, <c>items</c>.
/// </summary>
public static class Validator
{
    /// <summary>
    /// Validates <paramref name="data"/> against <paramref name="schema"/>.
    /// </summary>
    /// <param name="data">The value to validate (any JSON-mapped BCL type).</param>
    /// <param name="schema">The JSON Schema as a dictionary.</param>
    /// <param name="path">Human-readable path for error messages (use <c>"root"</c> at the top level).</param>
    public static Types.ValidationResult Validate(object? data, Dictionary<string, object?>? schema, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) path = "root";
        var errors = new List<string>();
        if (schema != null)
        {
            ValidateValue(data, schema, path, errors);
        }
        return new Types.ValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateValue(
        object? data, Dictionary<string, object?> schema, string path, List<string> errors)
    {
        // --- type check ---
        if (schema.TryGetValue("type", out var typeVal) && typeVal != null)
        {
            var types = ToStringList(typeVal);
            var actual = JsonType(data);

            var matched = false;
            foreach (var t in types)
            {
                if (t == actual) { matched = true; break; }
                // integer satisfies "number" in JSON Schema
                if (actual == "integer" && t == "number") { matched = true; break; }
            }
            if (!matched)
            {
                errors.Add(path + ": expected type " + string.Join(" | ", types) + ", got " + actual);
                return;
            }
        }

        // --- enum check ---
        if (schema.TryGetValue("enum", out var enumVal) && enumVal is System.Collections.IList allowed)
        {
            var found = false;
            foreach (var v in allowed)
            {
                if (DeepEqual(v, data)) { found = true; break; }
            }
            if (!found)
            {
                errors.Add(path + ": value must be one of " + FormatList(allowed) + ", got " + FormatValue(data));
                return;
            }
        }

        // --- object ---
        if (HasType(schema, "object") && AsDict(data) is { } obj)
        {
            if (schema.TryGetValue("required", out var reqVal) && reqVal != null)
            {
                foreach (var key in ToStringList(reqVal))
                {
                    if (!obj.ContainsKey(key))
                    {
                        errors.Add(path + ": missing required property '" + key + "'");
                    }
                }
            }

            if (schema.TryGetValue("properties", out var propsVal) && AsDict(propsVal) is { } props)
            {
                foreach (var entry in props)
                {
                    if (obj.ContainsKey(entry.Key) && AsDict(entry.Value) is { } propSchema)
                    {
                        ValidateValue(obj[entry.Key], propSchema, path + "." + entry.Key, errors);
                    }
                }
            }
        }

        // --- array ---
        if (HasType(schema, "array") && data is System.Collections.IList arr)
        {
            if (schema.TryGetValue("items", out var itemsVal) && AsDict(itemsVal) is { } itemSchema)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    ValidateValue(arr[i], itemSchema, path + "[" + i + "]", errors);
                }
            }
        }
    }

    private static bool HasType(Dictionary<string, object?> schema, string typeName)
    {
        if (!schema.TryGetValue("type", out var typeVal) || typeVal is null) return false;
        return ToStringList(typeVal).Contains(typeName);
    }

    /// <summary>Returns the JSON Schema type name for a BCL value.</summary>
    internal static string JsonType(object? v)
    {
        if (v is null) return "null";
        if (v is bool) return "boolean";
        if (v is string) return "string";
        if (v is System.Collections.IList && v is not string) return "array";
        if (v is System.Collections.IDictionary) return "object";
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal)
        {
            var d = Convert.ToDouble(v);
            if (d == Math.Floor(d) && !double.IsInfinity(d)) return "integer";
            return "number";
        }
        return "unknown";
    }

    private static List<string> ToStringList(object val)
    {
        var result = new List<string>();
        if (val is string s)
        {
            result.Add(s);
        }
        else if (val is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                if (item is string ss) result.Add(ss);
            }
        }
        return result;
    }

    private static Dictionary<string, object?>? AsDict(object? v)
    {
        if (v is Dictionary<string, object?> d) return d;
        if (v is System.Collections.IDictionary raw)
        {
            var converted = new Dictionary<string, object?>();
            foreach (System.Collections.DictionaryEntry e in raw)
            {
                converted[Convert.ToString(e.Key) ?? ""] = e.Value;
            }
            return converted;
        }
        return null;
    }

    private static string FormatList(System.Collections.IList list)
    {
        var parts = new List<string>();
        foreach (var item in list) parts.Add(FormatValue(item));
        return "[" + string.Join(", ", parts) + "]";
    }

    private static string FormatValue(object? v) => v?.ToString() ?? "null";

    /// <summary>Deep-equality check compatible with JSON-mapped BCL types.</summary>
    public static bool DeepEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        if (a is System.Collections.IDictionary ma && b is System.Collections.IDictionary mb)
        {
            if (ma.Count != mb.Count) return false;
            foreach (System.Collections.DictionaryEntry e in ma)
            {
                var key = e.Key;
                if (!mb.Contains(key)) return false;
                if (!DeepEqual(e.Value, mb[key])) return false;
            }
            return true;
        }

        if (a is System.Collections.IList la && b is System.Collections.IList lb
            && a is not string && b is not string)
        {
            if (la.Count != lb.Count) return false;
            for (var i = 0; i < la.Count; i++)
            {
                if (!DeepEqual(la[i], lb[i])) return false;
            }
            return true;
        }

        // Numeric comparison across different Number subtypes
        if (IsNumber(a) && IsNumber(b))
        {
            return Convert.ToDouble(a) == Convert.ToDouble(b);
        }

        return a.Equals(b);
    }

    private static bool IsNumber(object v) =>
        v is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal;
}
