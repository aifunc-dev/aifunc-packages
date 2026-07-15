// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_2_0;

import aifunc.AIFuncException;
import aifunc.AIFuncConfig;

import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicReference;

/**
 * Main entry point for the AIFunc Java engine v0.2.0.
 *
 * <p>v0.2 adds streaming via {@link #executeStreamWithCancel} and
 * {@link #executeStream}.
 */
public final class Runtime {

    private Runtime() {}

    private static final int DEFAULT_TIMEOUT_MS = 7_000;
    private static final int DEFAULT_RETRIES    = 1;

    /** Sentinel placed on the queue to signal end-of-stream. */
    private static final String STREAM_DONE = "\u0000__DONE__\u0000";

    // -------------------------------------------------------------------------
    // Non-streaming
    // -------------------------------------------------------------------------

    @SuppressWarnings("unchecked")
    public static CompletableFuture<Map<String, Object>> executeAsync(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {

        try {
            Artifact.validate(artifact);
            Map<String, Object> inputSchema =
                    (Map<String, Object>) artifact.getApi().get("input");
            Types.ValidationResult iv = Validator.validate(input, inputSchema, "root");
            if (!iv.isValid()) {
                return failedFuture(new AIFuncException(
                        "Input validation failed:\n"
                        + String.join("\n", iv.getErrors())));
            }
            if (config.isMock()) {
                try {
                    return CompletableFuture.completedFuture(
                            executeMock(artifact, input, config));
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
    // Streaming
    // -------------------------------------------------------------------------

    /**
     * Returns a {@link Types.TokenStream} — a blocking iterator over text tokens
     * that also implements {@link AutoCloseable}.
     * Use in a try-with-resources block so that {@code break} cancels the stream
     * and releases the background thread automatically:
     * <pre>
     *   try (TokenStream tokens = Runtime.executeStream(artifact, input, config)) {
     *       while (tokens.hasNext()) {
     *           String token = tokens.next();
     *           if (shouldStop) break;   // close() cancels automatically
     *       }
     *   }
     * </pre>
     */
    @SuppressWarnings("unchecked")
    public static Types.TokenStream executeStream(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {

        BlockingQueue<String>      queue     = new ArrayBlockingQueue<>(128);
        AtomicBoolean              cancelled = new AtomicBoolean(false);
        AtomicReference<Throwable> error     = new AtomicReference<>();

        Runnable cancelFn = () -> {
            if (cancelled.compareAndSet(false, true)) {
                queue.offer(STREAM_DONE);
            }
        };

        try {
            Artifact.validate(artifact);
            Map<String, Object> inputSchema =
                    (Map<String, Object>) artifact.getApi().get("input");
            Types.ValidationResult v = Validator.validate(input, inputSchema, "root");
            if (!v.isValid()) {
                throw new AIFuncException(
                        "Input validation failed:\n"
                        + String.join("\n", v.getErrors()));
            }
        } catch (Exception e) {
            error.set(e);
            queue.offer(STREAM_DONE);
            return new Types.TokenStream(
                    blockingQueueIterator(queue, cancelled, error), cancelFn);
        }

        if (config.isMock()) {
            executeMockStreamAsync(artifact, input, config, queue, cancelled);
        } else {
            int timeoutMs = config.getTimeoutMs() > 0
                    ? config.getTimeoutMs() : DEFAULT_TIMEOUT_MS;
            executeRealStreamAsync(
                    artifact, input, config, timeoutMs, queue, cancelled, error);
        }

        return new Types.TokenStream(
                blockingQueueIterator(queue, cancelled, error), cancelFn);
    }

    /** @deprecated Use {@link #executeStream} in a try-with-resources block. */
    @Deprecated
    public static Types.StreamingHandle executeStreamWithCancel(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {
        return new Types.StreamingHandle(executeStream(artifact, input, config));
    }

    // -------------------------------------------------------------------------
    // Background streaming workers
    // -------------------------------------------------------------------------

    private static void executeRealStreamAsync(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config,
            int timeoutMs,
            BlockingQueue<String> queue,
            AtomicBoolean cancelled,
            AtomicReference<Throwable> error) {

        CompletableFuture.runAsync(() -> {
            try {
                String prompt = Prompt.render(artifact, input);
                Types.ModelRequestParams params =
                        Request.buildStreamRequest(artifact, prompt, config);

                Request.sendStreamRequestAsync(config, params, timeoutMs,
                        token -> {
                            if (!cancelled.get()) {
                                try { queue.put(token); }
                                catch (InterruptedException e) {
                                    Thread.currentThread().interrupt();
                                }
                            }
                        },
                        cancelled
                ).get();
            } catch (Exception e) {
                Throwable cause =
                        (e instanceof java.util.concurrent.ExecutionException
                                && e.getCause() != null)
                        ? e.getCause() : e;
                error.set(cause);
            } finally {
                queue.offer(STREAM_DONE);
            }
        });
    }

    @SuppressWarnings("unchecked")
    private static void executeMockStreamAsync(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config,
            BlockingQueue<String> queue,
            AtomicBoolean cancelled) {

        CompletableFuture.runAsync(() -> {
            try {
                List<Types.MockEntry> entries =
                        Mock.resolveMockEntries(config.getMockData());
                Object raw = Mock.findMockOutput(entries, input);

                String text;
                if (raw instanceof String) {
                    text = (String) raw;
                } else if (raw instanceof Map) {
                    text = ((Map<?, ?>) raw).values().stream()
                            .filter(v -> v instanceof String)
                            .map(v -> (String) v)
                            .findFirst()
                            .orElse(Json.stringify(raw));
                } else {
                    Object generated = Mock.generateFromSchema(
                            (Map<String, Object>) artifact.getApi().get("output"));
                    text = (generated instanceof String)
                            ? (String) generated : "(mock output)";
                }

                String[] words = text.split(" ");
                for (int i = 0; i < words.length; i++) {
                    if (cancelled.get()) break;
                    String token = (i == 0) ? words[i] : " " + words[i];
                    try {
                        queue.put(token);
                        Thread.sleep(30 + (long) (Math.random() * 61));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        break;
                    }
                }
            } finally {
                queue.offer(STREAM_DONE);
            }
        });
    }

    // -------------------------------------------------------------------------
    // Queue → blocking Iterator
    // -------------------------------------------------------------------------

    private static Iterator<String> blockingQueueIterator(
            BlockingQueue<String> queue,
            AtomicBoolean cancelled,
            AtomicReference<Throwable> error) {

        return new Iterator<String>() {
            private String  next = null;
            private boolean done = false;

            private boolean advance() {
                if (done) return false;
                if (next != null) return true;
                while (!done) {
                    try {
                        String item = queue.take();
                        if (STREAM_DONE.equals(item) || cancelled.get()) {
                            done = true;
                            rethrowIfError();
                            return false;
                        }
                        next = item;
                        return true;
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        done = true;
                        return false;
                    }
                }
                return false;
            }

            private void rethrowIfError() {
                Throwable t = error.get();
                if (t == null) return;
                if (t instanceof AIFuncException) throw (AIFuncException) t;
                throw new AIFuncException(t.getMessage(), t);
            }

            @Override public boolean hasNext() { return advance(); }

            @Override public String next() {
                if (!advance()) throw new NoSuchElementException("Stream ended");
                String token = next;
                next = null;
                return token;
            }
        };
    }

    // -------------------------------------------------------------------------
    // Non-streaming retry helpers
    // -------------------------------------------------------------------------

    private static CompletableFuture<Map<String, Object>> executeWithRetry(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config,
            int timeoutMs,
            int maxRetries,
            int attempt,
            AIFuncException previousError) {

        if (attempt > maxRetries) return failedFuture(previousError);

        return executeOnceAsync(artifact, input, config, timeoutMs)
                .handle((result, ex) -> {
                    if (ex == null) return CompletableFuture.completedFuture(result);
                    Throwable u = unwrap(ex);
                    AIFuncException cause = (u instanceof AIFuncException)
                            ? (AIFuncException) u
                            : new AIFuncException(u.getMessage(), u);
                    return executeWithRetry(artifact, input, config,
                            timeoutMs, maxRetries, attempt + 1, cause);
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
                    Map<String, Object> outputSchema =
                            (Map<String, Object>) artifact.getApi().get("output");
                    Types.ValidationResult v =
                            Validator.validate(output, outputSchema, "root");
                    if (!v.isValid()) {
                        throw new AIFuncException("Output validation failed:\n"
                                + String.join("\n", v.getErrors()));
                    }
                    return output;
                });
    }

    // -------------------------------------------------------------------------
    // Mock (non-streaming)
    // -------------------------------------------------------------------------

    @SuppressWarnings("unchecked")
    private static Map<String, Object> executeMock(
            Types.AIFuncArtifact artifact,
            Map<String, Object> input,
            AIFuncConfig config) {

        List<Types.MockEntry> entries = Mock.resolveMockEntries(config.getMockData());
        Object raw = Mock.findMockOutput(entries, input);
        Map<String, Object> outputSchema =
                (Map<String, Object>) artifact.getApi().get("output");

        Map<String, Object> output;
        if (raw == null) {
            Object generated = Mock.generateFromSchema(outputSchema);
            if (!(generated instanceof Map)) generated = new java.util.LinkedHashMap<>();
            output = (Map<String, Object>) generated;
        } else if (raw instanceof Map) {
            output = (Map<String, Object>) raw;
        } else {
            throw new AIFuncException(
                    "Mock output must be a JSON object for non-streaming calls");
        }

        Types.ValidationResult v = Validator.validate(output, outputSchema, "root");
        if (!v.isValid()) {
            throw new AIFuncException("Mock output validation failed:\n"
                    + String.join("\n", v.getErrors()));
        }
        return output;
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    private static <T> CompletableFuture<T> failedFuture(Throwable ex) {
        CompletableFuture<T> f = new CompletableFuture<>();
        f.completeExceptionally(ex);
        return f;
    }

    private static Throwable unwrap(Throwable ex) {
        if (ex instanceof java.util.concurrent.CompletionException
                && ex.getCause() != null) {
            return ex.getCause();
        }
        return ex;
    }
}
