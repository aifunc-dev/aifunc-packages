// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>Mock data lookup and schema-based value generation.</summary>
public static class Mock
{
    /// <summary>
    /// Finds a matching mock output for the given input.
    ///
    /// Lookup order:
    /// <list type="number">
    ///   <item>First entry whose <c>input</c> field deep-equals the actual input</item>
    ///   <item>First entry with no <c>input</c> field (fallback default)</item>
    ///   <item><c>null</c> when no entries match</item>
    /// </list>
    /// </summary>
    public static Dictionary<string, object?>? FindMockOutput(
        List<Types.MockEntry> entries, Dictionary<string, object?> input)
    {
        Dictionary<string, object?>? fallback = null;
        foreach (var entry in entries)
        {
            if (entry.Input is null)
            {
                if (fallback is null) fallback = entry.Output;
                continue;
            }
            if (Validator.DeepEqual(entry.Input, input))
            {
                return entry.Output;
            }
        }
        return fallback;
    }

    /// <summary>
    /// Generates a zero-value instance conforming to the given JSON Schema.
    ///
    /// <list type="bullet">
    ///   <item>Returns the <c>default</c> value if present</item>
    ///   <item>Returns the first <c>enum</c> value if present</item>
    ///   <item>Otherwise generates: <c>""</c> / <c>0.0</c> / <c>false</c> /
    ///         <c>[]</c> / <c>{}</c> based on type</item>
    /// </list>
    /// </summary>
    public static object? GenerateFromSchema(Dictionary<string, object?>? schema)
    {
        if (schema is null) return null;

        if (schema.ContainsKey("default")) return schema["default"];

        if (schema.TryGetValue("enum", out var enumVal) && enumVal is System.Collections.IList arr
            && arr.Count > 0)
        {
            return arr[0];
        }

        var type = ResolveType(schema);
        switch (type)
        {
            case "string":
            {
                if (schema.TryGetValue("description", out var desc) && desc is string ds)
                    return ds;
                return "";
            }
            case "number":
            case "integer":
                return 0.0;
            case "boolean":
                return false;
            case "null":
                return null;
            case "array":
            {
                if (schema.TryGetValue("items", out var items) && AsDict(items) is { } itemSchema)
                {
                    return new List<object?> { GenerateFromSchema(itemSchema) };
                }
                return new List<object?>();
            }
            case "object":
            {
                var obj = new Dictionary<string, object?>();
                if (schema.TryGetValue("properties", out var propsVal) && AsDict(propsVal) is { } props)
                {
                    foreach (var e in props)
                    {
                        if (AsDict(e.Value) is { } propSchema)
                        {
                            obj[e.Key] = GenerateFromSchema(propSchema);
                        }
                    }
                }
                return obj;
            }
            default:
                return null;
        }
    }

    /// <summary>
    /// Resolves mock entries from an <c>AIFuncConfig</c>'s <c>MockData</c> field.
    /// Accepts a <c>List&lt;Types.MockEntry&gt;</c>, a raw <c>List</c> of maps, or a
    /// map with a <c>"cases"</c> key.
    /// </summary>
    public static List<Types.MockEntry> ResolveMockEntries(object? mockData)
    {
        var result = new List<Types.MockEntry>();
        if (mockData is null) return result;

        if (mockData is System.Collections.IList list)
        {
            foreach (var item in list)
            {
                AddEntry(result, item);
            }
            return result;
        }

        if (mockData is System.Collections.IDictionary map)
        {
            if (map.Contains("cases"))
            {
                var casesVal = map["cases"];
                if (casesVal is System.Collections.IList cases)
                {
                    foreach (var item in cases)
                    {
                        AddEntry(result, item);
                    }
                }
            }
        }
        return result;
    }

    private static void AddEntry(List<Types.MockEntry> result, object? item)
    {
        if (item is Types.MockEntry entry)
        {
            result.Add(entry);
        }
        else if (AsDict(item) is { } map)
        {
            result.Add(Types.MockEntry.FromMap(map));
        }
    }

    private static string ResolveType(Dictionary<string, object?> schema)
    {
        if (!schema.TryGetValue("type", out var typeVal) || typeVal is null) return "";
        if (typeVal is string s) return s;
        if (typeVal is System.Collections.IList list && list.Count > 0 && list[0] is string first)
            return first;
        return "";
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
}
