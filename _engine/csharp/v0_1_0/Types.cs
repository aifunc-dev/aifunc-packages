// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>Engine-internal types. Users should not import from this package directly.</summary>
public static class Types
{
    /// <summary>Compiled representation of an AIFunc package.</summary>
    public sealed class AIFuncArtifact
    {
        public string? SchemaVersion { get; }
        public string? ArtifactVersion { get; }
        public Dictionary<string, object?>? Package { get; }
        public Dictionary<string, object?>? Api { get; }
        public Dictionary<string, object?>? ModelParams { get; }
        public Dictionary<string, object?>? ModelRouting { get; }
        public Dictionary<string, string> Prompts { get; }
        public Dictionary<string, object?>? Metadata { get; }
        public string? Name { get; }
        public string? EngineVersion { get; }
        public string? Prompt { get; }
        public Dictionary<string, object?>? Model { get; }

        public AIFuncArtifact(Dictionary<string, object?> data)
        {
            SchemaVersion   = Str(data, "schemaVersion");
            ArtifactVersion = Str(data, "artifactVersion");
            Package         = Obj(data, "package");
            Api             = Obj(data, "api");
            ModelParams     = Obj(data, "modelParams");
            ModelRouting    = Obj(data, "modelRouting");
            Metadata        = Obj(data, "metadata");
            Name            = Str(data, "name");
            EngineVersion   = Str(data, "engineVersion");
            Prompt          = Str(data, "prompt");
            Model           = Obj(data, "model");

            if (data.TryGetValue("prompts", out var rawPrompts) && rawPrompts is Dictionary<string, object?> promptsMap)
            {
                var pm = new Dictionary<string, string>();
                foreach (var e in promptsMap)
                {
                    pm[e.Key] = e.Value?.ToString() ?? "";
                }
                Prompts = pm;
            }
            else if (rawPrompts is System.Collections.IDictionary rawDict)
            {
                var pm = new Dictionary<string, string>();
                foreach (System.Collections.DictionaryEntry e in rawDict)
                {
                    pm[Convert.ToString(e.Key) ?? ""] = Convert.ToString(e.Value) ?? "";
                }
                Prompts = pm;
            }
            else
            {
                Prompts = new Dictionary<string, string>();
            }
        }

        public string ResolveName()
        {
            if (Package != null && Package.TryGetValue("name", out var n)
                && n is string ns && !string.IsNullOrWhiteSpace(ns))
            {
                return ns;
            }
            return Name ?? "";
        }

        public string ResolveEngineVersion()
        {
            if (Package != null && Package.TryGetValue("engine", out var e)
                && e is string es && !string.IsNullOrWhiteSpace(es))
            {
                return es;
            }
            return EngineVersion ?? "";
        }

        private static Dictionary<string, object?>? Obj(Dictionary<string, object?> m, string key)
        {
            if (!m.TryGetValue(key, out var v)) return null;
            if (v is Dictionary<string, object?> dict) return dict;
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

        private static string? Str(Dictionary<string, object?> m, string key)
        {
            if (!m.TryGetValue(key, out var v)) return null;
            return v is string s ? s : null;
        }
    }

    /// <summary>A single mock test case.</summary>
    public sealed class MockEntry
    {
        public Dictionary<string, object?>? Input { get; }
        public Dictionary<string, object?> Output { get; }

        public MockEntry(Dictionary<string, object?>? input, Dictionary<string, object?> output)
        {
            Input  = input;
            Output = output;
        }

        public static MockEntry FromMap(Dictionary<string, object?> m)
        {
            m.TryGetValue("input", out var rawIn);
            m.TryGetValue("output", out var rawOut);

            Dictionary<string, object?>? input = null;
            if (rawIn is Dictionary<string, object?> inDict)
            {
                input = inDict;
            }
            else if (rawIn is System.Collections.IDictionary rawInDict)
            {
                input = new Dictionary<string, object?>();
                foreach (System.Collections.DictionaryEntry e in rawInDict)
                {
                    input[Convert.ToString(e.Key) ?? ""] = e.Value;
                }
            }

            Dictionary<string, object?> output;
            if (rawOut is Dictionary<string, object?> outDict)
            {
                output = outDict;
            }
            else if (rawOut is System.Collections.IDictionary rawOutDict)
            {
                output = new Dictionary<string, object?>();
                foreach (System.Collections.DictionaryEntry e in rawOutDict)
                {
                    output[Convert.ToString(e.Key) ?? ""] = e.Value;
                }
            }
            else
            {
                output = new Dictionary<string, object?>();
            }

            return new MockEntry(input, output);
        }
    }

    /// <summary>Parameters sent to the model API endpoint.</summary>
    public sealed class ModelRequestParams
    {
        public string Model { get; }
        public List<Dictionary<string, string>> Messages { get; }
        public double? Temperature { get; }
        public double? TopP { get; }
        public int? MaxTokens { get; }
        public Dictionary<string, string>? ResponseFormat { get; }

        public ModelRequestParams(
            string model,
            List<Dictionary<string, string>> messages,
            double? temperature,
            double? topP,
            int? maxTokens,
            Dictionary<string, string>? responseFormat)
        {
            Model          = model;
            Messages       = messages;
            Temperature    = temperature;
            TopP           = topP;
            MaxTokens      = maxTokens;
            ResponseFormat = responseFormat;
        }

        public Dictionary<string, object?> ToJsonMap()
        {
            var m = new Dictionary<string, object?>
            {
                ["model"]    = Model,
                ["messages"] = Messages,
            };
            if (Temperature    != null) m["temperature"]     = Temperature;
            if (TopP           != null) m["top_p"]           = TopP;
            if (MaxTokens      != null) m["max_tokens"]      = MaxTokens;
            if (ResponseFormat != null) m["response_format"] = ResponseFormat;
            return m;
        }
    }

    /// <summary>Result of validating a data map against a JSON Schema.</summary>
    public sealed class ValidationResult
    {
        public bool Valid { get; }
        public List<string> Errors { get; }

        public ValidationResult(bool valid, List<string> errors)
        {
            Valid  = valid;
            Errors = errors;
        }
    }
}
