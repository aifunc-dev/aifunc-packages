// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import aifunc.AIFuncException;
import aifunc.AIFuncConfig;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.Duration;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * HTTP transport and request/response handling for the model API.
 *
 * <p>Uses {@link java.net.http.HttpClient} (Java 11+) — zero external dependencies.
 */
public final class Request {

    private Request() {}

    private static final HttpClient SHARED_CLIENT = HttpClient.newBuilder()
            .connectTimeout(Duration.ofSeconds(30))
            .build();

    private static final Pattern FENCE = Pattern.compile(
            "(?s)^```(?:json)?\\s*\\n?(.*?)\\n?```$");

    /**
     * Builds the {@link Types.ModelRequestParams} for the given artifact, rendered prompt,
     * and runtime config.
     *
     * @throws AIFuncException if {@code config.model} is not set
     */
    @SuppressWarnings("unchecked")
    public static Types.ModelRequestParams buildRequest(
            Types.AIFuncArtifact artifact, String prompt, AIFuncConfig config) {

        if (config.getModel() == null || config.getModel().isBlank()) {
            throw new AIFuncException(
                    "AIFuncConfig.model is required when mock mode is disabled");
        }

        List<Map<String, String>> messages = new ArrayList<>();
        Map<String, String> userMsg = new LinkedHashMap<>();
        userMsg.put("role",    "user");
        userMsg.put("content", prompt);
        messages.add(userMsg);

        Map<String, String> responseFormat = new LinkedHashMap<>();
        responseFormat.put("type", "json_object");

        StandardParams resolved = resolveModelParams(artifact, config.getModel());

        Double temperature = config.getTemperature();
        if (temperature == null) temperature = resolved.temperature;
        if (temperature == null) temperature = legacyTemperature(artifact);

        Double topP = config.getTopP();
        if (topP == null) topP = resolved.topP;

        Integer maxTokens = config.getMaxTokens();
        if (maxTokens == null) maxTokens = resolved.maxTokens;
        if (maxTokens == null) maxTokens = legacyMaxTokens(artifact);

        return new Types.ModelRequestParams(
                config.getModel(), messages, temperature, topP, maxTokens, responseFormat);
    }

    /**
     * Sends the request to the model API asynchronously.
     *
     * @return a future that resolves to the parsed response map, or fails with {@link AIFuncException}
     */
    public static CompletableFuture<Map<String, Object>> sendRequestAsync(
            AIFuncConfig config, Types.ModelRequestParams params, int timeoutMs) {

        if (config.getBaseUrl() == null || config.getBaseUrl().isBlank()) {
            return failedFuture(new AIFuncException(
                    "AIFuncConfig.baseUrl is required when mock mode is disabled"));
        }
        if (config.getApiKey() == null || config.getApiKey().isBlank()) {
            return failedFuture(new AIFuncException(
                    "AIFuncConfig.apiKey is required when mock mode is disabled"));
        }

        String base = config.getBaseUrl().stripTrailing();
        if (base.endsWith("/")) base = base.substring(0, base.length() - 1);
        String endpoint = base.endsWith("/chat/completions")
                ? base : base + "/chat/completions";

        String bodyJson = Json.stringify(params.toJsonMap());

        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(endpoint))
                .timeout(Duration.ofMillis(timeoutMs))
                .header("Content-Type",  "application/json")
                .header("Authorization", "Bearer " + config.getApiKey())
                .POST(HttpRequest.BodyPublishers.ofString(bodyJson))
                .build();

        return SHARED_CLIENT.sendAsync(request, HttpResponse.BodyHandlers.ofString())
                .thenApply(response -> {
                    if (response.statusCode() >= 400) {
                        String preview = response.body();
                        if (preview.length() > 500) preview = preview.substring(0, 500);
                        throw new AIFuncException(
                                "Model API returned " + response.statusCode() + ": " + preview);
                    }
                    return Json.parseObject(response.body());
                });
    }

    /** Java 11–compatible replacement for {@code CompletableFuture.failedFuture} (Java 12+). */
    private static <T> CompletableFuture<T> failedFuture(Throwable ex) {
        CompletableFuture<T> future = new CompletableFuture<>();
        future.completeExceptionally(ex);
        return future;
    }

    /**
     * Parses a model response map into the content string, then extracts the
     * JSON object from it.
     *
     * @throws AIFuncException if the response has no choices or the content is not valid JSON
     */
    @SuppressWarnings("unchecked")
    public static Map<String, Object> parseResponse(Map<String, Object> responseMap) {
        Object choicesVal = responseMap.get("choices");
        if (!(choicesVal instanceof List) || ((List<?>) choicesVal).isEmpty()) {
            throw new AIFuncException("Model response contains no choices");
        }

        Object first = ((List<?>) choicesVal).get(0);
        if (!(first instanceof Map)) {
            throw new AIFuncException("Model response choice is not an object");
        }

        Object msgVal = ((Map<?, ?>) first).get("message");
        if (!(msgVal instanceof Map)) {
            throw new AIFuncException("Model response message is not an object");
        }

        Object contentVal = ((Map<?, ?>) msgVal).get("content");
        String content = (contentVal instanceof String) ? ((String) contentVal).strip() : "";

        // Strip optional markdown code fence
        Matcher fence = FENCE.matcher(content);
        if (fence.matches()) {
            content = fence.group(1).strip();
        }

        try {
            Object parsed = Json.parse(content);
            if (!(parsed instanceof Map)) {
                throw new AIFuncException("Model output is not a JSON object");
            }
            return (Map<String, Object>) parsed;
        } catch (Json.JsonParseException e) {
            String preview = content.length() > 200 ? content.substring(0, 200) : content;
            throw new AIFuncException(
                    "Failed to parse model output as JSON: " + preview, e);
        }
    }

    // -------------------------------------------------------------------------
    // Model param resolution (mirrors Go/TS provider logic)
    // -------------------------------------------------------------------------

    private static final class StandardParams {
        final Double  temperature;
        final Double  topP;
        final Integer maxTokens;

        StandardParams(Double temperature, Double topP, Integer maxTokens) {
            this.temperature = temperature;
            this.topP        = topP;
            this.maxTokens   = maxTokens;
        }
    }

    @SuppressWarnings("unchecked")
    private static StandardParams resolveModelParams(Types.AIFuncArtifact artifact, String model) {
        Map<String, Object> mp = artifact.getModelParams();
        if (mp == null) return new StandardParams(null, null, null);

        Object rulesVal = mp.get("rules");
        if (!(rulesVal instanceof List)) return new StandardParams(null, null, null);

        for (Object ruleObj : (List<?>) rulesVal) {
            if (!(ruleObj instanceof Map)) continue;
            Map<String, Object> rule = (Map<String, Object>) ruleObj;

            Object matchVal = rule.get("match");
            if (!(matchVal instanceof Map)) continue;
            Map<String, Object> match = (Map<String, Object>) matchVal;

            if (!matchesRule(match, model)) continue;

            Object paramsVal = rule.get("params");
            if (!(paramsVal instanceof Map)) continue;
            Map<String, Object> params = (Map<String, Object>) paramsVal;

            return new StandardParams(
                    toDouble(params.get("temperature")),
                    toDouble(params.get("topP")),
                    toInt(params.get("maxTokens")));
        }
        return new StandardParams(null, null, null);
    }

    private static boolean matchesRule(Map<String, Object> match, String model) {
        Object mModel = match.get("model");
        if (mModel instanceof String && ((String) mModel).equals(model)) return true;

        Object mModels = match.get("models");
        if (mModels instanceof List) {
            for (Object v : (List<?>) mModels) {
                if (model.equals(v)) return true;
            }
        }

        Object mPattern = match.get("pattern");
        if (mPattern instanceof String) {
            return globMatch((String) mPattern, model);
        }

        // Empty match = wildcard
        boolean noModel   = !(mModel   instanceof String) || ((String) mModel).isBlank();
        boolean noModels  = !(mModels  instanceof List)   || ((List<?>) mModels).isEmpty();
        boolean noPattern = !(mPattern instanceof String) || ((String) mPattern).isBlank();
        return noModel && noModels && noPattern;
    }

    private static boolean globMatch(String pattern, String value) {
        String regex = "^" + Pattern.quote(pattern).replace("\\*", ".*") + "$";
        return value.matches(regex);
    }

    @SuppressWarnings("unchecked")
    private static Double legacyTemperature(Types.AIFuncArtifact artifact) {
        Map<String, Object> m = artifact.getModel();
        return (m != null) ? toDouble(m.get("temperature")) : null;
    }

    @SuppressWarnings("unchecked")
    private static Integer legacyMaxTokens(Types.AIFuncArtifact artifact) {
        Map<String, Object> m = artifact.getModel();
        return (m != null) ? toInt(m.get("maxTokens")) : null;
    }

    private static Double toDouble(Object v) {
        if (v instanceof Number) return ((Number) v).doubleValue();
        return null;
    }

    private static Integer toInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        return null;
    }
}
