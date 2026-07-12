// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>
/// Main entry point for the AIFunc C# engine.
///
/// Typical usage from generated wrapper code:
/// <code>
/// Types.AIFuncArtifact artifact = Artifact.FromMap(ARTIFACT_DATA);
/// var result = await Runtime.ExecuteAsync(artifact, inputMap, config);
/// </code>
/// </summary>
public static class Runtime
{
    private const int DefaultTimeoutMs = 7_000;
    private const int DefaultRetries   = 1;

    /// <summary>
    /// Executes an AI function end-to-end asynchronously.
    ///
    /// Flow:
    /// <list type="number">
    ///   <item>Validate artifact</item>
    ///   <item>Validate input</item>
    ///   <item>If mock → return mock output</item>
    ///   <item>Render prompt → build request → send async → parse response → validate output</item>
    /// </list>
    /// </summary>
    /// <param name="artifact">The compiled artifact produced by the CLI.</param>
    /// <param name="input">Input fields as a dictionary.</param>
    /// <param name="config">Runtime configuration (model connection or mock mode).</param>
    /// <returns>The validated output map.</returns>
    public static async Task<Dictionary<string, object?>> ExecuteAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config)
    {
        Artifact.Validate(artifact);

        var inputSchema = AsDict(artifact.Api!["input"]);
        var inputValidation = Validator.Validate(input, inputSchema, "root");
        if (!inputValidation.Valid)
        {
            throw new AIFuncException(
                "Input validation failed:\n" + string.Join("\n", inputValidation.Errors));
        }

        if (config.Mock)
        {
            return ExecuteMock(artifact, input, config);
        }

        var timeoutMs  = config.TimeoutMs  > 0 ? config.TimeoutMs  : DefaultTimeoutMs;
        var maxRetries = config.MaxRetries > 0 ? config.MaxRetries : DefaultRetries;

        return await ExecuteWithRetryAsync(artifact, input, config, timeoutMs, maxRetries)
            .ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, object?>> ExecuteWithRetryAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config,
        int timeoutMs,
        int maxRetries)
    {
        AIFuncException? previousError = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ExecuteOnceAsync(artifact, input, config, timeoutMs)
                    .ConfigureAwait(false);
            }
            catch (AIFuncException e)
            {
                previousError = e;
            }
            catch (Exception e)
            {
                previousError = new AIFuncException(e.Message, e);
            }
        }

        throw previousError!;
    }

    private static async Task<Dictionary<string, object?>> ExecuteOnceAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config,
        int timeoutMs)
    {
        var prompt = Prompt.Render(artifact, input);
        var parameters = Request.BuildRequest(artifact, prompt, config);

        var responseMap = await Request.SendRequestAsync(config, parameters, timeoutMs)
            .ConfigureAwait(false);

        var output = Request.ParseResponse(responseMap);

        var outputSchema = AsDict(artifact.Api!["output"]);
        var outputValidation = Validator.Validate(output, outputSchema, "root");
        if (!outputValidation.Valid)
        {
            throw new AIFuncException(
                "Output validation failed:\n" + string.Join("\n", outputValidation.Errors));
        }
        return output;
    }

    private static Dictionary<string, object?> ExecuteMock(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config)
    {
        var entries = Mock.ResolveMockEntries(config.MockData);
        var output = Mock.FindMockOutput(entries, input);

        var outputSchema = AsDict(artifact.Api!["output"]);

        if (output is null)
        {
            var generated = Mock.GenerateFromSchema(outputSchema);
            if (generated is not Dictionary<string, object?> generatedMap)
            {
                generatedMap = new Dictionary<string, object?>();
            }
            output = generatedMap;
            var v = Validator.Validate(output, outputSchema, "root");
            if (!v.Valid)
            {
                throw new AIFuncException(
                    "Auto-generated mock output validation failed:\n"
                    + string.Join("\n", v.Errors));
            }
            return output;
        }

        var validation = Validator.Validate(output, outputSchema, "root");
        if (!validation.Valid)
        {
            throw new AIFuncException(
                "Mock output validation failed:\n" + string.Join("\n", validation.Errors));
        }
        return output;
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
