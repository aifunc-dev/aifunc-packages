// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_2_0;

import aifunc.AIFuncException;
import aifunc.AIFuncConfig;

import java.io.BufferedReader;
import java.io.InputStreamReader;
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
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.function.Consumer;
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

    // -------------------------------------------------------------------------
    // Request builders
    // -------------------------------------------------------------------------

    /** Builds params for a standard (non-streaming) call. */
    public static Types.ModelRequestParams buildRequest(
            Types.AIFuncArtifact artifact, String prompt, AIFuncConfig config) {

        if (config.getModel() == null || config.getModel().isBlank()) {
            throw new AIFuncException(
                    "AIFuncConfig.model is required when mock mode is disabled");
        }

        List<Map<String, String>> messages = buildMessages(prompt);
        Map<String, String> responseFormat = new LinkedHashMap<>();
        responseFormat.put("type", "json_object");

        StandardParams resolved = resolveModelParams(artifact, config.getModel());
        Double  temperature = coalesceDouble(config.getTemperature(), resolved.temperature, legacyTemperature(artifact));
        Double  topP        = coalesceDouble(config.getTopP(),         resolved.topP,        null);
        Integer maxTokens   = coalesceInt(config.getMaxTokens(),   resolved.maxTokens,   legacyMaxTokens(artifact));

        return new Types.ModelRequestParams(
                config.getModel(), messages, temperature, topP, maxTokens, responseFormat, false);
    }

    /**
     * Builds params for a streaming call.
     * Sets {@code stream: true} and omits {@code response_format}.
     */
    public static Types.ModelRequestParams buildStreamRequest(
            Types.AIFuncArtifact artifact, String prompt, AIFuncConfig config) {

        if (config.getModel() == null || config.getModel().isBlank()) {
            throw new AIFuncException(
                    "AIFuncConfig.model is required when mock mode is disabled");
        }

        List<Map<String, String>> messages = buildMessages(prompt);
        StandardParams resolved = resolveModelParams(artifact, config.getModel());
        Double  temperature = coalesceDouble(config.getTemperature(), resolved.temperature, legacyTemperature(artifact));
        Double  topP        = coalesceDouble(config.getTopP(),         resolved.topP,        null);
        Integer maxTokens   = coalesceInt(config.getMaxTokens(),   resolved.maxTokens,   legacyMaxTokens(artifact));

        return new Types.ModelRequestParams(
                config.getModel(), messages, temperature, topP, maxTokens, null, true);
    }

    // -------------------------------------------------------------------------
    // Async send — standard
    // -------------------------------------------------------------------------

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

        String endpoint = resolveEndpoint(config.getBaseUrl());
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

    // -------------------------------------------------------------------------
    // Async send — streaming (SSE)
    // -------------------------------------------------------------------------

    /**
     * Opens an SSE stream to the model API and delivers text tokens to
     * {@code tokenConsumer} as they arrive. Runs on a background thread.
     *
     * <p>Cancellation is signalled by setting {@code cancelled} to {@code true};
     * the SSE read loop checks this flag before each line.
     *
     * @return a future that completes when the stream ends (normally or via
     *         cancellation), or fails with {@link AIFuncException} on error
     */
    public static CompletableFuture<Void> sendStreamRequestAsync(
            AIFuncConfig config,
            Types.ModelRequestParams params,
            int timeoutMs,
            Consumer<String> tokenConsumer,
            AtomicBoolean cancelled) {

        if (config.getBaseUrl() == null || config.getBaseUrl().isBlank()) {
            return failedFuture(new AIFuncException(
                    "AIFuncConfig.baseUrl is required when mock mode is disabled"));
        }
        if (config.getApiKey() == null || config.getApiKey().isBlank()) {
            return failedFuture(new AIFuncException(
                    "AIFuncConfig.apiKey is required when mock mode is disabled"));
        }

        String endpoint = resolveEndpoint(config.getBaseUrl());
        String bodyJson = Json.stringify(params.toJsonMap());

        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(endpoint))
                .timeout(Duration.ofMillis(timeoutMs))
                .header("Content-Type",  "application/json")
                .header("Authorization", "Bearer " + config.getApiKey())
                .header("Accept",        "text/event-stream")
                .header("Cache-Control", "no-cache")
                .POST(HttpRequest.BodyPublishers.ofString(bodyJson))
                .build();

        return SHARED_CLIENT
                .sendAsync(request, HttpResponse.BodyHandlers.ofInputStream())
                .thenApply(response -> {
                    if (response.statusCode() >= 400) {
                        try (java.io.InputStream body = response.body();
                             BufferedReader br = new BufferedReader(
                                     new InputStreamReader(body))) {
                            StringBuilder sb = new StringBuilder();
                            String line;
                            while ((line = br.readLine()) != null
                                    && sb.length() < 500) {
                                sb.append(line);
                            }
                            throw new AIFuncException(
                                    "Model API returned "
                                    + response.statusCode() + ": " + sb);
                        } catch (java.io.IOException e) {
                            throw new AIFuncException(
                                    "Model API returned " + response.statusCode());
                        }
                    }
                    readSSEStream(response.body(), tokenConsumer, cancelled);
                    return (Void) null;
                });
    }

    // -------------------------------------------------------------------------
    // SSE reader
    // -------------------------------------------------------------------------

    static void readSSEStream(
            java.io.InputStream inputStream,
            Consumer<String> tokenConsumer,
            AtomicBoolean cancelled) {

        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(inputStream,
                        java.nio.charset.StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) {
                if (cancelled != null && cancelled.get()) return;
                if (!line.startsWith("data:")) continue;

                String payload = line.substring(5).strip();
                if ("[DONE]".equals(payload)) return;

                try {
                    Object parsed = Json.parse(payload);
                    if (!(parsed instanceof Map)) continue;
                    @SuppressWarnings("unchecked")
                    Map<String, Object> obj = (Map<String, Object>) parsed;

                    Object choices = obj.get("choices");
                    if (!(choices instanceof List) || ((List<?>) choices).isEmpty()) continue;
                    Object first = ((List<?>) choices).get(0);
                    if (!(first instanceof Map)) continue;

                    @SuppressWarnings("unchecked")
                    Object delta = ((Map<String, Object>) first).get("delta");
                    if (!(delta instanceof Map)) continue;

                    @SuppressWarnings("unchecked")
                    Object content = ((Map<String, Object>) delta).get("content");
                    if (content instanceof String && !((String) content).isEmpty()) {
                        tokenConsumer.accept((String) content);
                    }
                } catch (Json.JsonParseException ignored) {
                    // skip malformed SSE lines
                }
            }
        } catch (java.io.IOException ignored) {
            // connection closed — normal end of stream
        }
    }

    // -------------------------------------------------------------------------
    // Response parsing
    // -------------------------------------------------------------------------

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

        Matcher fence = FENCE.matcher(content);
        if (fence.matches()) content = fence.group(1).strip();

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
    // Helpers
    // -------------------------------------------------------------------------

    static <T> CompletableFuture<T> failedFuture(Throwable ex) {
        CompletableFuture<T> future = new CompletableFuture<>();
        future.completeExceptionally(ex);
        return future;
    }

    private static String resolveEndpoint(String baseUrl) {
        String base = baseUrl.stripTrailing();
        if (base.endsWith("/")) base = base.substring(0, base.length() - 1);
        return base.endsWith("/chat/completions") ? base : base + "/chat/completions";
    }

    private static List<Map<String, String>> buildMessages(String prompt) {
        List<Map<String, String>> messages = new ArrayList<>();
        Map<String, String> msg = new LinkedHashMap<>();
        msg.put("role",    "user");
        msg.put("content", prompt);
        messages.add(msg);
        return messages;
    }

    private static Double coalesceDouble(Double... values) {
        for (Double v : values) if (v != null) return v;
        return null;
    }

    private static Integer coalesceInt(Integer... values) {
        for (Integer v : values) if (v != null) return v;
        return null;
    }

    // -------------------------------------------------------------------------
    // Model param resolution
    // -------------------------------------------------------------------------

    private static final class StandardParams {
        final Double  temperature;
        final Double  topP;
        final Integer maxTokens;

        StandardParams(Double t, Double p, Integer m) {
            this.temperature = t; this.topP = p; this.maxTokens = m;
        }
    }

    @SuppressWarnings("unchecked")
    private static StandardParams resolveModelParams(
            Types.AIFuncArtifact artifact, String model) {
        Map<String, Object> mp = artifact.getModelParams();
        if (mp == null) return new StandardParams(null, null, null);

        Object rulesVal = mp.get("rules");
        if (!(rulesVal instanceof List)) return new StandardParams(null, null, null);

        for (Object ruleObj : (List<?>) rulesVal) {
            if (!(ruleObj instanceof Map)) continue;
            Map<String, Object> rule = (Map<String, Object>) ruleObj;
            Object matchVal = rule.get("match");
            if (!(matchVal instanceof Map)) continue;
            if (!matchesRule((Map<String, Object>) matchVal, model)) continue;
            Object paramsVal = rule.get("params");
            if (!(paramsVal instanceof Map)) continue;
            Map<String, Object> params = (Map<String, Object>) paramsVal;
            return new StandardParams(toDouble(params.get("temperature")),
                    toDouble(params.get("topP")), toInt(params.get("maxTokens")));
        }
        return new StandardParams(null, null, null);
    }

    private static boolean matchesRule(Map<String, Object> match, String model) {
        Object mModel = match.get("model");
        if (mModel instanceof String && ((String) mModel).equals(model)) return true;
        Object mModels = match.get("models");
        if (mModels instanceof List) {
            for (Object v : (List<?>) mModels) if (model.equals(v)) return true;
        }
        Object mPattern = match.get("pattern");
        if (mPattern instanceof String) return globMatch((String) mPattern, model);

        boolean noModel   = !(mModel   instanceof String) || ((String) mModel).isBlank();
        boolean noModels  = !(mModels  instanceof List)   || ((List<?>) mModels).isEmpty();
        boolean noPattern = !(mPattern instanceof String) || ((String) mPattern).isBlank();
        return noModel && noModels && noPattern;
    }

    private static boolean globMatch(String pattern, String value) {
        return value.matches("^" + Pattern.quote(pattern).replace("\\*", ".*") + "$");
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
        return (v instanceof Number) ? ((Number) v).doubleValue() : null;
    }

    private static Integer toInt(Object v) {
        return (v instanceof Number) ? ((Number) v).intValue() : null;
    }
}
