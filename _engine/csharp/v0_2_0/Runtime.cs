// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_2_0;

/// <summary>
/// Main entry point for the AIFunc C# engine v0.2.0.
///
/// v0.2 adds streaming via <see cref="ExecuteStreamAsync"/> which returns
/// <c>IAsyncEnumerable&lt;string&gt;</c> — consumed with <c>await foreach</c>.
/// Cancellation is handled via the standard <see cref="CancellationToken"/> parameter.
/// </summary>
public static class Runtime
{
    private const int DefaultTimeoutMs = 7_000;
    private const int DefaultRetries   = 1;

    // -------------------------------------------------------------------------
    // Non-streaming
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes an AI function end-to-end asynchronously (non-streaming).
    /// </summary>
    public static async Task<Dictionary<string, object?>> ExecuteAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config)
    {
        Artifact.Validate(artifact);

        var inputSchema = AsDict(artifact.Api!["input"]);
        var iv = Validator.Validate(input, inputSchema, "root");
        if (!iv.Valid)
            throw new AIFuncException(
                "Input validation failed:\n" + string.Join("\n", iv.Errors));

        if (config.Mock)
            return ExecuteMock(artifact, input, config);

        var timeoutMs  = config.TimeoutMs  > 0 ? config.TimeoutMs  : DefaultTimeoutMs;
        var maxRetries = config.MaxRetries > 0 ? config.MaxRetries : DefaultRetries;

        return await ExecuteWithRetryAsync(artifact, input, config, timeoutMs, maxRetries)
            .ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Streaming
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes a streaming AI function and yields text tokens as they arrive.
    /// Pass a <see cref="CancellationToken"/> (from a <see cref="CancellationTokenSource"/>)
    /// to cancel the stream at any time.
    ///
    /// <code>
    /// using var cts = new CancellationTokenSource();
    /// await foreach (var token in Runtime.ExecuteStreamAsync(artifact, input, config, cts.Token))
    ///     Console.Write(token);
    /// </code>
    /// </summary>
    public static async IAsyncEnumerable<string> ExecuteStreamAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Artifact.Validate(artifact);

        var inputSchema = AsDict(artifact.Api!["input"]);
        var iv = Validator.Validate(input, inputSchema, "root");
        if (!iv.Valid)
            throw new AIFuncException(
                "Input validation failed:\n" + string.Join("\n", iv.Errors));

        if (config.Mock)
        {
            await foreach (var token in ExecuteMockStreamAsync(artifact, input, config, cancellationToken))
                yield return token;
            yield break;
        }

        var timeoutMs = config.TimeoutMs > 0 ? config.TimeoutMs : DefaultTimeoutMs;
        var prompt    = Prompt.Render(artifact, input);
        var parameters = Request.BuildStreamRequest(artifact, prompt, config);

        await foreach (var token in Request.SendStreamRequestAsync(
            config, parameters, timeoutMs, cancellationToken))
        {
            yield return token;
        }
    }

    // -------------------------------------------------------------------------
    // Mock streaming
    // -------------------------------------------------------------------------

    private static async IAsyncEnumerable<string> ExecuteMockStreamAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entries = Mock.ResolveMockEntries(config.MockData);
        var raw = Mock.FindMockOutput(entries, input);

        string text;
        if (raw is string s)
        {
            text = s;
        }
        else if (raw is Dictionary<string, object?> map)
        {
            text = "(mock output)";
            foreach (var val in map.Values)
                if (val is string vs) { text = vs; break; }
        }
        else
        {
            var outputSchema = AsDict(artifact.Api!["output"]);
            var generated = Mock.GenerateFromSchema(outputSchema);
            text = generated is string gs ? gs : "(mock output)";
        }

        var words  = text.Split(' ');
        var random = new Random();
        for (var i = 0; i < words.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            var token = i == 0 ? words[i] : " " + words[i];
            yield return token;
            var delay = 30 + random.Next(61);
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    // -------------------------------------------------------------------------
    // Non-streaming retry
    // -------------------------------------------------------------------------

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
            catch (AIFuncException e) { previousError = e; }
            catch (Exception e)       { previousError = new AIFuncException(e.Message, e); }
        }

        throw previousError!;
    }

    private static async Task<Dictionary<string, object?>> ExecuteOnceAsync(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config,
        int timeoutMs)
    {
        var prompt     = Prompt.Render(artifact, input);
        var parameters = Request.BuildRequest(artifact, prompt, config);

        var responseMap = await Request.SendRequestAsync(config, parameters, timeoutMs)
            .ConfigureAwait(false);

        var output = Request.ParseResponse(responseMap);

        var outputSchema = AsDict(artifact.Api!["output"]);
        var ov = Validator.Validate(output, outputSchema, "root");
        if (!ov.Valid)
            throw new AIFuncException(
                "Output validation failed:\n" + string.Join("\n", ov.Errors));

        return output;
    }

    // -------------------------------------------------------------------------
    // Non-streaming mock
    // -------------------------------------------------------------------------

    private static Dictionary<string, object?> ExecuteMock(
        Types.AIFuncArtifact artifact,
        Dictionary<string, object?> input,
        AIFuncConfig config)
    {
        var entries = Mock.ResolveMockEntries(config.MockData);
        var raw     = Mock.FindMockOutput(entries, input);

        var outputSchema = AsDict(artifact.Api!["output"]);

        Dictionary<string, object?> output;
        if (raw is null)
        {
            var generated = Mock.GenerateFromSchema(outputSchema);
            output = generated is Dictionary<string, object?> gm
                ? gm
                : new Dictionary<string, object?>();
        }
        else if (raw is Dictionary<string, object?> dm)
        {
            output = dm;
        }
        else
        {
            throw new AIFuncException(
                "Mock output must be a JSON object for non-streaming calls");
        }

        var v = Validator.Validate(output, outputSchema, "root");
        if (!v.Valid)
            throw new AIFuncException(
                "Mock output validation failed:\n" + string.Join("\n", v.Errors));

        return output;
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

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
