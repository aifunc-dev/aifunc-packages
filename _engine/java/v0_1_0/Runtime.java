// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import aifunc.AIFuncException;
import aifunc.AIFuncConfig;

import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

/**
 * Main entry point for the AIFunc Java engine.
 *
 * <p>Typical usage from generated wrapper code:
 * <pre>{@code
 * Types.AIFuncArtifact artifact = Artifact.fromMap(ARTIFACT_DATA);
 * Runtime.executeAsync(artifact, inputMap, config)
 *        .thenAccept(result -> { ... })
 *        .exceptionally(e -> { ... });
 * }</pre>
 */
public final class Runtime {

    private Runtime() {}

    private static final int DEFAULT_TIMEOUT_MS = 7_000;
    private static final int DEFAULT_RETRIES    = 1;

    /**
     * Executes an AI function end-to-end asynchronously.
     *
     * <p>Flow:
     * <ol>
     *   <li>Validate artifact
     *   <li>Validate input
     *   <li>If mock → return already-resolved future
     *   <li>Render prompt → build request → send async → parse response → validate output
     * </ol>
     *
     * @param artifact the compiled artifact produced by the CLI
     * @param input    input fields as a {@code Map<String, Object>}
     * @param config   runtime configuration (model connection or mock mode)
     * @return a future that resolves to the validated output map
     */
    @SuppressWarnings("unchecked")
    public static CompletableFuture<Map<String, Object>> executeAsync(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {

        try {
            Artifact.validate(artifact);

            Map<String, Object> inputSchema = (Map<String, Object>) artifact.getApi().get("input");
            Types.ValidationResult inputValidation = Validator.validate(input, inputSchema, "root");
            if (!inputValidation.isValid()) {
                return failedFuture(new AIFuncException(
                        "Input validation failed:\n" + String.join("\n", inputValidation.getErrors())));
            }

            if (config.isMock()) {
                try {
                    return CompletableFuture.completedFuture(executeMock(artifact, input, config));
                } catch (AIFuncException e) {
                    return failedFuture(e);
                }
            }
        } catch (Exception e) {
            return failedFuture(e);
        }

        int timeoutMs  = config.getTimeoutMs()  > 0 ? config.getTimeoutMs()  : DEFAULT_TIMEOUT_MS;
        int maxRetries = config.getMaxRetries() > 0 ? config.getMaxRetries() : DEFAULT_RETRIES;

        return executeWithRetry(artifact, input, config, timeoutMs, maxRetries, 0, null);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /** Java 11–compatible replacement for {@code CompletableFuture.failedFuture} (Java 12+). */
    private static <T> CompletableFuture<T> failedFuture(Throwable ex) {
        CompletableFuture<T> future = new CompletableFuture<>();
        future.completeExceptionally(ex);
        return future;
    }

    private static Throwable unwrap(Throwable ex) {
        if (ex instanceof java.util.concurrent.CompletionException && ex.getCause() != null) {
            return ex.getCause();
        }
        return ex;
    }

    private static CompletableFuture<Map<String, Object>> executeWithRetry(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config,
            int timeoutMs,
            int maxRetries,
            int attempt,
            AIFuncException previousError) {

        if (attempt > maxRetries) {
            return failedFuture(previousError);
        }

        // handle + thenCompose replaces exceptionallyCompose (Java 12+)
        return executeOnceAsync(artifact, input, config, timeoutMs)
                .handle((result, ex) -> {
                    if (ex == null) {
                        return CompletableFuture.completedFuture(result);
                    }
                    Throwable unwrapped = unwrap(ex);
                    AIFuncException cause = (unwrapped instanceof AIFuncException)
                            ? (AIFuncException) unwrapped
                            : new AIFuncException(unwrapped.getMessage(), unwrapped);
                    return executeWithRetry(
                            artifact, input, config, timeoutMs, maxRetries, attempt + 1, cause);
                })
                .thenCompose(f -> f);
    }

    @SuppressWarnings("unchecked")
    private static CompletableFuture<Map<String, Object>> executeOnceAsync(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config,
            int timeoutMs) {

        String prompt = Prompt.render(artifact, input);
        Types.ModelRequestParams params = Request.buildRequest(artifact, prompt, config);

        return Request.sendRequestAsync(config, params, timeoutMs)
                .thenApply(responseMap -> {
                    Map<String, Object> output = Request.parseResponse(responseMap);

                    Map<String, Object> outputSchema = (Map<String, Object>) artifact.getApi().get("output");
                    Types.ValidationResult outputValidation = Validator.validate(output, outputSchema, "root");
                    if (!outputValidation.isValid()) {
                        throw new AIFuncException(
                                "Output validation failed:\n" + String.join("\n", outputValidation.getErrors()));
                    }
                    return output;
                });
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> executeMock(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {

        List<Types.MockEntry> entries = Mock.resolveMockEntries(config.getMockData());
        Map<String, Object> output = Mock.findMockOutput(entries, input);

        Map<String, Object> outputSchema = (Map<String, Object>) artifact.getApi().get("output");

        if (output == null) {
            Object generated = Mock.generateFromSchema(outputSchema);
            if (!(generated instanceof Map)) {
                generated = new java.util.LinkedHashMap<String, Object>();
            }
            output = (Map<String, Object>) generated;
            Types.ValidationResult v = Validator.validate(output, outputSchema, "root");
            if (!v.isValid()) {
                throw new AIFuncException(
                        "Auto-generated mock output validation failed:\n"
                        + String.join("\n", v.getErrors()));
            }
            return output;
        }

        Types.ValidationResult v = Validator.validate(output, outputSchema, "root");
        if (!v.isValid()) {
            throw new AIFuncException(
                    "Mock output validation failed:\n" + String.join("\n", v.getErrors()));
        }
        return output;
    }
}
