// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_2_0;

/// <summary>
/// HTTP transport and request/response handling for the model API.
/// Uses <see cref="HttpClient"/> — zero external dependencies.
/// </summary>
public static class Request
{
    private static readonly HttpClient SharedClient = CreateClient();

    private static readonly Regex Fence = new(
        @"^```(?:json)?\s*\n?(.*?)\n?```$",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.Timeout = Timeout.InfiniteTimeSpan;
        return client;
    }

    // -------------------------------------------------------------------------
    // Request builders
    // -------------------------------------------------------------------------

    /// <summary>Builds request params for a standard (non-streaming) call.</summary>
    public static Types.ModelRequestParams BuildRequest(
        Types.AIFuncArtifact artifact, string prompt, AIFuncConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new AIFuncException(
                "AIFuncConfig.model is required when mock mode is disabled");

        var messages = new List<Dictionary<string, string>>
        {
            new() { ["role"] = "user", ["content"] = prompt },
        };
        var responseFormat = new Dictionary<string, string> { ["type"] = "json_object" };
        var resolved = ResolveModelParams(artifact, config.Model!);

        return new Types.ModelRequestParams(
            config.Model!,
            messages,
            config.Temperature ?? resolved.Temperature ?? LegacyTemperature(artifact),
            config.TopP        ?? resolved.TopP,
            config.MaxTokens   ?? resolved.MaxTokens   ?? LegacyMaxTokens(artifact),
            responseFormat,
            stream: false);
    }

    /// <summary>
    /// Builds request params for a streaming SSE call.
    /// Sets <c>stream: true</c> and omits <c>response_format</c>.
    /// </summary>
    public static Types.ModelRequestParams BuildStreamRequest(
        Types.AIFuncArtifact artifact, string prompt, AIFuncConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Model))
            throw new AIFuncException(
                "AIFuncConfig.model is required when mock mode is disabled");

        var messages = new List<Dictionary<string, string>>
        {
            new() { ["role"] = "user", ["content"] = prompt },
        };
        var resolved = ResolveModelParams(artifact, config.Model!);

        return new Types.ModelRequestParams(
            config.Model!,
            messages,
            config.Temperature ?? resolved.Temperature ?? LegacyTemperature(artifact),
            config.TopP        ?? resolved.TopP,
            config.MaxTokens   ?? resolved.MaxTokens   ?? LegacyMaxTokens(artifact),
            responseFormat: null,
            stream: true);
    }

    // -------------------------------------------------------------------------
    // Standard (non-streaming) send
    // -------------------------------------------------------------------------

    /// <summary>Sends a standard request to the model API asynchronously.</summary>
    public static async Task<Dictionary<string, object?>> SendRequestAsync(
        AIFuncConfig config, Types.ModelRequestParams parameters, int timeoutMs)
    {
        var endpoint = ResolveEndpoint(config);
        var bodyJson = Json.Stringify(parameters.ToJsonMap());

        using var cts     = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await SharedClient.SendAsync(request, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new AIFuncException("Model API request timed out after " + timeoutMs + "ms", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AIFuncException("Model API request failed: " + ex.Message, ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if ((int)response.StatusCode >= 400)
            {
                var preview = body.Length > 500 ? body.Substring(0, 500) : body;
                throw new AIFuncException(
                    "Model API returned " + (int)response.StatusCode + ": " + preview);
            }
            return Json.ParseObject(body);
        }
    }

    // -------------------------------------------------------------------------
    // Streaming SSE send
    // -------------------------------------------------------------------------

    /// <summary>
    /// Opens an SSE stream to the model API and yields text tokens as they arrive.
    /// Cancellation is handled via <paramref name="cancellationToken"/>.
    /// </summary>
    public static async IAsyncEnumerable<string> SendStreamRequestAsync(
        AIFuncConfig config,
        Types.ModelRequestParams parameters,
        int timeoutMs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveEndpoint(config);
        var bodyJson = Json.Stringify(parameters.ToJsonMap());

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

        HttpResponseMessage response;
        try
        {
            response = await SharedClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            throw new AIFuncException(
                "Model API stream request timed out after " + timeoutMs + "ms", ex);
        }
        catch (OperationCanceledException) { yield break; }
        catch (HttpRequestException ex)
        {
            throw new AIFuncException("Model API request failed: " + ex.Message, ex);
        }

        using (response)
        {
            if ((int)response.StatusCode >= 400)
            {
                var errBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var preview = errBody.Length > 500 ? errBody.Substring(0, 500) : errBody;
                throw new AIFuncException(
                    "Model API returned " + (int)response.StatusCode + ": " + preview);
            }

            await using var stream = await response.Content
                .ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !linkedCts.Token.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { yield break; }
                catch (IOException)               { yield break; }

                if (line is null) break;
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                var payload = line.Substring(5).Trim();
                if (payload == "[DONE]") yield break;

                var content = ParseSseDelta(payload);
                if (content is not null && content.Length > 0)
                    yield return content;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Response parsing (non-streaming)
    // -------------------------------------------------------------------------

    /// <summary>Parses a non-streaming model response map into the output dictionary.</summary>
    public static Dictionary<string, object?> ParseResponse(Dictionary<string, object?> responseMap)
    {
        if (!responseMap.TryGetValue("choices", out var choicesVal)
            || choicesVal is not IList choices || choices.Count == 0)
        {
            throw new AIFuncException("Model response contains no choices");
        }
        if (AsDict(choices[0]) is not { } firstMap)
            throw new AIFuncException("Model response choice is not an object");
        if (!firstMap.TryGetValue("message", out var msgVal) || AsDict(msgVal) is not { } msgMap)
            throw new AIFuncException("Model response message is not an object");

        msgMap.TryGetValue("content", out var contentVal);
        var content = contentVal is string cs ? cs.Trim() : "";
        var fence = Fence.Match(content);
        if (fence.Success) content = fence.Groups[1].Value.Trim();

        try
        {
            var parsed = Json.Parse(content);
            if (parsed is not Dictionary<string, object?> result)
                throw new AIFuncException("Model output is not a JSON object");
            return result;
        }
        catch (Json.JsonParseException e)
        {
            var preview = content.Length > 200 ? content.Substring(0, 200) : content;
            throw new AIFuncException("Failed to parse model output as JSON: " + preview, e);
        }
    }

    // -------------------------------------------------------------------------
    // SSE delta parser
    // -------------------------------------------------------------------------

    private static string? ParseSseDelta(string payload)
    {
        try
        {
            var parsed = Json.Parse(payload);
            if (parsed is not Dictionary<string, object?> obj) return null;
            if (!obj.TryGetValue("choices", out var cv) || cv is not IList choices || choices.Count == 0)
                return null;
            if (AsDict(choices[0]) is not { } first) return null;
            if (!first.TryGetValue("delta", out var dv) || AsDict(dv) is not { } delta) return null;
            if (!delta.TryGetValue("content", out var content) || content is not string s) return null;
            return s;
        }
        catch (Json.JsonParseException) { return null; }
    }

    // -------------------------------------------------------------------------
    // Model param resolution
    // -------------------------------------------------------------------------

    private sealed class StandardParams
    {
        public double? Temperature { get; }
        public double? TopP { get; }
        public int? MaxTokens { get; }

        public StandardParams(double? temperature, double? topP, int? maxTokens)
        {
            Temperature = temperature;
            TopP        = topP;
            MaxTokens   = maxTokens;
        }
    }

    private static StandardParams ResolveModelParams(Types.AIFuncArtifact artifact, string model)
    {
        var mp = artifact.ModelParams;
        if (mp is null) return new StandardParams(null, null, null);
        if (!mp.TryGetValue("rules", out var rulesVal) || rulesVal is not IList rules)
            return new StandardParams(null, null, null);

        foreach (var ruleObj in rules)
        {
            if (AsDict(ruleObj) is not { } rule) continue;
            if (!rule.TryGetValue("match", out var matchVal) || AsDict(matchVal) is not { } match)
                continue;
            if (!MatchesRule(match, model)) continue;
            if (!rule.TryGetValue("params", out var paramsVal) || AsDict(paramsVal) is not { } parameters)
                continue;

            parameters.TryGetValue("temperature", out var temp);
            parameters.TryGetValue("topP",        out var topP);
            parameters.TryGetValue("maxTokens",   out var maxTokens);
            return new StandardParams(ToDouble(temp), ToDouble(topP), ToInt(maxTokens));
        }
        return new StandardParams(null, null, null);
    }

    private static bool MatchesRule(Dictionary<string, object?> match, string model)
    {
        match.TryGetValue("model",   out var mModel);
        match.TryGetValue("models",  out var mModels);
        match.TryGetValue("pattern", out var mPattern);

        if (mModel is string ms && ms == model) return true;
        if (mModels is IList modelsList)
            foreach (var v in modelsList)
                if (model.Equals(v)) return true;
        if (mPattern is string pattern && !string.IsNullOrWhiteSpace(pattern))
            return GlobMatch(pattern, model);

        var noModel   = mModel   is not string mStr || string.IsNullOrWhiteSpace(mStr);
        var noModels  = mModels  is not IList  ml   || ml.Count == 0;
        var noPattern = mPattern is not string pStr || string.IsNullOrWhiteSpace(pStr);
        return noModel && noModels && noPattern;
    }

    private static bool GlobMatch(string pattern, string value)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regex);
    }

    private static double? LegacyTemperature(Types.AIFuncArtifact artifact) =>
        ToDouble(artifact.Model is null ? null
            : artifact.Model.TryGetValue("temperature", out var t) ? t : null);

    private static int? LegacyMaxTokens(Types.AIFuncArtifact artifact) =>
        ToInt(artifact.Model is null ? null
            : artifact.Model.TryGetValue("maxTokens", out var t) ? t : null);

    private static string ResolveEndpoint(AIFuncConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new AIFuncException(
                "AIFuncConfig.baseUrl is required when mock mode is disabled");
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new AIFuncException(
                "AIFuncConfig.apiKey is required when mock mode is disabled");

        var base_ = config.BaseUrl!.TrimEnd();
        if (base_.EndsWith("/")) base_ = base_.Substring(0, base_.Length - 1);
        return base_.EndsWith("/chat/completions", StringComparison.Ordinal)
            ? base_
            : base_ + "/chat/completions";
    }

    private static double? ToDouble(object? v)
    {
        if (v is null) return null;
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
               or float or double or decimal)
            return Convert.ToDouble(v);
        return null;
    }

    private static int? ToInt(object? v)
    {
        if (v is null) return null;
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
               or float or double or decimal)
            return Convert.ToInt32(v);
        return null;
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
