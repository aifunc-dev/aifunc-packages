// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>Renders the prompt template by substituting input field placeholders.</summary>
public static class Prompt
{
    private static readonly Regex InputField = new(@"\{\{input\.([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex BareField  = new(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Renders the prompt template for the given artifact and input.
    ///
    /// Substitution rules (applied in order):
    /// <list type="number">
    ///   <item><c>{{input_json}}</c> → full input serialized as JSON</item>
    ///   <item><c>{{input.fieldName}}</c> → the string value of <c>input["fieldName"]</c></item>
    ///   <item><c>{{fieldName}}</c> → same, but empty string when not found</item>
    /// </list>
    ///
    /// If <c>injectOutputSchema</c> is enabled (default), a JSON-schema instruction
    /// is appended to the rendered prompt.
    /// </summary>
    /// <exception cref="AIFuncException">Thrown if no prompt template is found in the artifact.</exception>
    public static string Render(Types.AIFuncArtifact artifact, Dictionary<string, object?> input)
    {
        var template = SelectPrompt(artifact);

        var inputJson = Json.PrettyPrint(input);
        var prompt = template.Replace("{{input_json}}", inputJson);

        // {{input.fieldName}}
        prompt = InputField.Replace(prompt, m =>
        {
            var field = m.Groups[1].Value;
            if (input.TryGetValue(field, out var val) && val != null)
            {
                return Convert.ToString(val) ?? m.Value;
            }
            return m.Value;
        });

        // {{fieldName}}
        prompt = BareField.Replace(prompt, m =>
        {
            var field = m.Groups[1].Value;
            if (input.TryGetValue(field, out var val) && val != null)
            {
                return Convert.ToString(val) ?? "";
            }
            return "";
        });

        if (ShouldInjectSchema(artifact))
        {
            Dictionary<string, object?>? outputSchema = null;
            if (artifact.Api != null && artifact.Api.TryGetValue("output", out var outVal))
            {
                outputSchema = AsDict(outVal);
            }
            prompt = prompt + "\n\n" + BuildSchemaInstruction(outputSchema);
        }

        return prompt;
    }

    private static string SelectPrompt(Types.AIFuncArtifact artifact)
    {
        if (!string.IsNullOrWhiteSpace(artifact.Prompt))
        {
            return artifact.Prompt!;
        }
        var prompts = artifact.Prompts;
        if (prompts != null)
        {
            if (prompts.TryGetValue("general", out var general) && !string.IsNullOrWhiteSpace(general))
            {
                return general;
            }
            foreach (var p in prompts.Values)
            {
                if (!string.IsNullOrWhiteSpace(p)) return p;
            }
        }
        throw new AIFuncException("Artifact missing prompt template");
    }

    private static bool ShouldInjectSchema(Types.AIFuncArtifact artifact)
    {
        var api = artifact.Api;
        if (api != null && api.TryGetValue("injectOutputSchema", out var inject)
            && inject is bool injectBool && !injectBool)
        {
            return false;
        }
        var pkg = artifact.Package;
        if (pkg != null && pkg.TryGetValue("engineOptions", out var opts) && AsDict(opts) is { } optsMap)
        {
            if (optsMap.TryGetValue("injectOutputSchema", out var inject2)
                && inject2 is bool injectBool2 && !injectBool2)
            {
                return false;
            }
        }
        return true;
    }

    private static string BuildSchemaInstruction(Dictionary<string, object?>? schema)
    {
        return "Please respond with a JSON object that matches the following schema:\n\n"
               + Json.PrettyPrint(schema)
               + "\n\nYour response must be valid JSON only, with no additional text.";
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
