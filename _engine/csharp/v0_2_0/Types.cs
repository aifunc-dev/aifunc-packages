// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Aifunc.Engine.Csharp.V0_2_0;

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

            if (data.TryGetValue("prompts", out var rawPrompts)
                && rawPrompts is Dictionary<string, object?> promptsMap)
            {
                var pm = new Dictionary<string, string>();
                foreach (var e in promptsMap)
                    pm[e.Key] = e.Value?.ToString() ?? "";
                Prompts = pm;
            }
            else if (rawPrompts is IDictionary rawDict)
            {
                var pm = new Dictionary<string, string>();
                foreach (DictionaryEntry e in rawDict)
                    pm[Convert.ToString(e.Key) ?? ""] = Convert.ToString(e.Value) ?? "";
                Prompts = pm;
            }
            else
            {
                Prompts = new Dictionary<string, string>();
            }
        }

        /// <summary>Returns true when the output schema declares x-delivery-mode: stream.</summary>
        public bool IsStreamOutput()
        {
            if (Api is null) return false;
            if (!Api.TryGetValue("output", out var output)) return false;
            var outMap = AsDict(output);
            if (outMap is null) return false;
            return outMap.TryGetValue("x-delivery-mode", out var mode) && "stream".Equals(mode);
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
            return AsDict(v);
        }

        private static string? Str(Dictionary<string, object?> m, string key)
        {
            if (!m.TryGetValue(key, out var v)) return null;
            return v is string s ? s : null;
        }

        private static Dictionary<string, object?>? AsDict(object? v)
        {
            if (v is Dictionary<string, object?> d) return d;
            if (v is IDictionary raw)
            {
                var c = new Dictionary<string, object?>();
                foreach (DictionaryEntry e in raw)
                    c[Convert.ToString(e.Key) ?? ""] = e.Value;
                return c;
            }
            return null;
        }
    }

    /// <summary>A single mock test case.</summary>
    public sealed class MockEntry
    {
        public Dictionary<string, object?>? Input { get; }
        public object? Output { get; }

        public MockEntry(Dictionary<string, object?>? input, object? output)
        {
            Input  = input;
            Output = output;
        }

        public static MockEntry FromMap(Dictionary<string, object?> m)
        {
            m.TryGetValue("input",  out var rawIn);
            m.TryGetValue("output", out var rawOut);
            return new MockEntry(AsDict(rawIn), rawOut);
        }

        private static Dictionary<string, object?>? AsDict(object? v)
        {
            if (v is Dictionary<string, object?> d) return d;
            if (v is IDictionary raw)
            {
                var c = new Dictionary<string, object?>();
                foreach (DictionaryEntry e in raw)
                    c[Convert.ToString(e.Key) ?? ""] = e.Value;
                return c;
            }
            return null;
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
        public bool Stream { get; }

        public ModelRequestParams(
            string model,
            List<Dictionary<string, string>> messages,
            double? temperature,
            double? topP,
            int? maxTokens,
            Dictionary<string, string>? responseFormat,
            bool stream = false)
        {
            Model          = model;
            Messages       = messages;
            Temperature    = temperature;
            TopP           = topP;
            MaxTokens      = maxTokens;
            ResponseFormat = responseFormat;
            Stream         = stream;
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
            if (Stream)                m["stream"]           = true;
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

    /// <summary>
    /// Handle returned by <see cref="Runtime.ExecuteStreamAsync"/> when a
    /// <see cref="CancellationToken"/> is wired in externally.
    /// Provides the token stream as <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public sealed class StreamingHandle : IAsyncDisposable
    {
        private readonly IAsyncEnumerable<string> _tokens;
        private readonly CancellationTokenSource  _cts;

        internal StreamingHandle(
            IAsyncEnumerable<string> tokens,
            CancellationTokenSource cts)
        {
            _tokens = tokens;
            _cts    = cts;
        }

        /// <summary>The async-enumerable token stream. Consume with <c>await foreach</c>.</summary>
        public IAsyncEnumerable<string> Tokens => _tokens;

        /// <summary>
        /// Cancels the stream. Idempotent — safe to call multiple times.
        /// After cancellation <see cref="Tokens"/> will stop yielding tokens.
        /// </summary>
        public void Cancel() => _cts.Cancel();

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
