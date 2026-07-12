// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_1_0;

/// <summary>
/// HTTP transport and request/response handling for the model API.
///
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
        client.Timeout = Timeout.InfiniteTimeSpan; // per-request timeout via CancellationToken
        return client;
    }

    /// <summary>
    /// Builds the <see cref="Types.ModelRequestParams"/> for the given artifact, rendered prompt,
    /// and runtime config.
    /// </summary>
    /// <exception cref="AIFuncException">Thrown if <c>config.Model</c> is not set.</exception>
    public static Types.ModelRequestParams BuildRequest(
        Types.AIFuncArtifact artifact, string prompt, AIFuncConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Model))
        {
            throw new AIFuncException(
                "AIFuncConfig.model is required when mock mode is disabled");
        }

        var messages = new List<Dictionary<string, string>>
        {
            new()
            {
                ["role"]    = "user",
                ["content"] = prompt,
            },
        };

        var responseFormat = new Dictionary<string, string>
        {
            ["type"] = "json_object",
        };

        var resolved = ResolveModelParams(artifact, config.Model!);

        var temperature = config.Temperature ?? resolved.Temperature ?? LegacyTemperature(artifact);
        var topP        = config.TopP ?? resolved.TopP;
        var maxTokens   = config.MaxTokens ?? resolved.MaxTokens ?? LegacyMaxTokens(artifact);

        return new Types.ModelRequestParams(
            config.Model!, messages, temperature, topP, maxTokens, responseFormat);
    }

    /// <summary>
    /// Sends the request to the model API asynchronously.
    /// </summary>
    /// <returns>The parsed response map, or throws <see cref="AIFuncException"/>.</returns>
    public static async Task<Dictionary<string, object?>> SendRequestAsync(
        AIFuncConfig config, Types.ModelRequestParams parameters, int timeoutMs)
    {
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            throw new AIFuncException(
                "AIFuncConfig.baseUrl is required when mock mode is disabled");
        }
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new AIFuncException(
                "AIFuncConfig.apiKey is required when mock mode is disabled");
        }

        var baseUrl = config.BaseUrl!.TrimEnd();
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
        var endpoint = baseUrl.EndsWith("/chat/completions", StringComparison.Ordinal)
            ? baseUrl
            : baseUrl + "/chat/completions";

        var bodyJson = Json.Stringify(parameters.ToJsonMap());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
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

    /// <summary>
    /// Parses a model response map into the content string, then extracts the
    /// JSON object from it.
    /// </summary>
    /// <exception cref="AIFuncException">
    /// Thrown if the response has no choices or the content is not valid JSON.
    /// </exception>
    public static Dictionary<string, object?> ParseResponse(Dictionary<string, object?> responseMap)
    {
        if (!responseMap.TryGetValue("choices", out var choicesVal)
            || choicesVal is not System.Collections.IList choices
            || choices.Count == 0)
        {
            throw new AIFuncException("Model response contains no choices");
        }

        var first = choices[0];
        if (AsDict(first) is not { } firstMap)
        {
            throw new AIFuncException("Model response choice is not an object");
        }

        if (!firstMap.TryGetValue("message", out var msgVal) || AsDict(msgVal) is not { } msgMap)
        {
            throw new AIFuncException("Model response message is not an object");
        }

        msgMap.TryGetValue("content", out var contentVal);
        var content = contentVal is string cs ? cs.Trim() : "";

        // Strip optional markdown code fence
        var fence = Fence.Match(content);
        if (fence.Success)
        {
            content = fence.Groups[1].Value.Trim();
        }

        try
        {
            var parsed = Json.Parse(content);
            if (parsed is not Dictionary<string, object?> result)
            {
                throw new AIFuncException("Model output is not a JSON object");
            }
            return result;
        }
        catch (Json.JsonParseException e)
        {
            var preview = content.Length > 200 ? content.Substring(0, 200) : content;
            throw new AIFuncException(
                "Failed to parse model output as JSON: " + preview, e);
        }
    }

    // -------------------------------------------------------------------------
    // Model param resolution (mirrors Go/TS/Java provider logic)
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

        if (!mp.TryGetValue("rules", out var rulesVal) || rulesVal is not System.Collections.IList rules)
        {
            return new StandardParams(null, null, null);
        }

        foreach (var ruleObj in rules)
        {
            if (AsDict(ruleObj) is not { } rule) continue;

            if (!rule.TryGetValue("match", out var matchVal) || AsDict(matchVal) is not { } match)
                continue;

            if (!MatchesRule(match, model)) continue;

            if (!rule.TryGetValue("params", out var paramsVal) || AsDict(paramsVal) is not { } parameters)
                continue;

            parameters.TryGetValue("temperature", out var temp);
            parameters.TryGetValue("topP", out var topP);
            parameters.TryGetValue("maxTokens", out var maxTokens);

            return new StandardParams(ToDouble(temp), ToDouble(topP), ToInt(maxTokens));
        }
        return new StandardParams(null, null, null);
    }

    private static bool MatchesRule(Dictionary<string, object?> match, string model)
    {
        match.TryGetValue("model", out var mModel);
        match.TryGetValue("models", out var mModels);
        match.TryGetValue("pattern", out var mPattern);

        if (mModel is string ms && ms == model)
            return true;

        if (mModels is System.Collections.IList modelsList)
        {
            foreach (var v in modelsList)
            {
                if (model.Equals(v)) return true;
            }
        }

        if (mPattern is string pattern && !string.IsNullOrWhiteSpace(pattern))
        {
            return GlobMatch(pattern, model);
        }

        // Empty match = wildcard
        var noModel   = !(mModel is string mStr) || string.IsNullOrWhiteSpace(mStr);
        var noModels  = !(mModels is System.Collections.IList ml) || ml.Count == 0;
        var noPattern = !(mPattern is string pStr) || string.IsNullOrWhiteSpace(pStr);
        return noModel && noModels && noPattern;
    }

    private static bool GlobMatch(string pattern, string value)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regex);
    }

    private static double? LegacyTemperature(Types.AIFuncArtifact artifact)
    {
        var m = artifact.Model;
        if (m is null) return null;
        m.TryGetValue("temperature", out var v);
        return ToDouble(v);
    }

    private static int? LegacyMaxTokens(Types.AIFuncArtifact artifact)
    {
        var m = artifact.Model;
        if (m is null) return null;
        m.TryGetValue("maxTokens", out var v);
        return ToInt(v);
    }

    private static double? ToDouble(object? v)
    {
        if (v is null) return null;
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal)
        {
            return Convert.ToDouble(v);
        }
        return null;
    }

    private static int? ToInt(object? v)
    {
        if (v is null) return null;
        if (v is byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal)
        {
            return Convert.ToInt32(v);
        }
        return null;
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
