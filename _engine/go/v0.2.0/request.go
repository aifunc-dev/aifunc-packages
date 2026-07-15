// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"bufio"
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"strings"
)

// defaultHTTPClient has no timeout; per-request context handles cancellation.
var defaultHTTPClient = &http.Client{}

const maxBodySize = 64 * 1024 * 1024

func SendRequest(ctx context.Context, config AIFuncConfig, params ModelRequestParams) (ModelResponse, error) {
	if config.BaseURL == "" {
		return ModelResponse{}, fmt.Errorf("AIFuncConfig.BaseURL is required when mock mode is disabled")
	}
	if config.APIKey == "" {
		return ModelResponse{}, fmt.Errorf("AIFuncConfig.APIKey is required when mock mode is disabled")
	}

	endpoint := resolveURL(config.BaseURL)

	bodyBytes, err := json.Marshal(params)
	if err != nil {
		return ModelResponse{}, fmt.Errorf("marshal request: %w", err)
	}

	req, err := http.NewRequestWithContext(ctx, http.MethodPost, endpoint, bytes.NewReader(bodyBytes))
	if err != nil {
		return ModelResponse{}, fmt.Errorf("build request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Authorization", "Bearer "+config.APIKey)

	client := config.HTTPClient
	if client == nil {
		client = defaultHTTPClient
	}
	resp, err := client.Do(req)
	if err != nil {
		return ModelResponse{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 400 {
		preview, _ := io.ReadAll(io.LimitReader(resp.Body, 500))
		return ModelResponse{}, fmt.Errorf("model API returned %d: %s", resp.StatusCode, string(preview))
	}

	respBody, err := io.ReadAll(io.LimitReader(resp.Body, maxBodySize))
	if err != nil {
		return ModelResponse{}, fmt.Errorf("read response body: %w", err)
	}

	var modelResp ModelResponse
	if err := json.Unmarshal(respBody, &modelResp); err != nil {
		return ModelResponse{}, fmt.Errorf("failed to parse model response: %w", err)
	}
	return modelResp, nil
}

// SendStreamRequest sends a streaming request and returns a channel that emits
// text tokens as they arrive via Server-Sent Events. The error channel receives
// at most one value: nil on clean completion, non-nil on failure.
// Cancelling ctx causes the stream to stop; the error channel will receive nil.
func SendStreamRequest(ctx context.Context, config AIFuncConfig, params ModelRequestParams) (<-chan string, <-chan error) {
	tokens := make(chan string, 64)
	errc := make(chan error, 1)

	go func() {
		defer close(tokens)
		defer close(errc)

		if config.BaseURL == "" {
			errc <- fmt.Errorf("AIFuncConfig.BaseURL is required when mock mode is disabled")
			return
		}
		if config.APIKey == "" {
			errc <- fmt.Errorf("AIFuncConfig.APIKey is required when mock mode is disabled")
			return
		}

		endpoint := resolveURL(config.BaseURL)

		streamParams := params
		streamParams.Stream = true
		streamParams.ResponseFormat = nil

		bodyBytes, err := json.Marshal(streamParams)
		if err != nil {
			errc <- fmt.Errorf("marshal request: %w", err)
			return
		}

		req, err := http.NewRequestWithContext(ctx, http.MethodPost, endpoint, bytes.NewReader(bodyBytes))
		if err != nil {
			errc <- fmt.Errorf("build request: %w", err)
			return
		}
		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("Authorization", "Bearer "+config.APIKey)
		req.Header.Set("Accept", "text/event-stream")
		req.Header.Set("Cache-Control", "no-cache")

		client := config.HTTPClient
		if client == nil {
			client = defaultHTTPClient
		}
		resp, err := client.Do(req)
		if err != nil {
			if ctx.Err() != nil {
				errc <- nil
			} else {
				errc <- err
			}
			return
		}
		defer resp.Body.Close()

		if resp.StatusCode >= 400 {
			preview, _ := io.ReadAll(io.LimitReader(resp.Body, 500))
			errc <- fmt.Errorf("model API returned %d: %s", resp.StatusCode, string(preview))
			return
		}

		if err := readSSEStream(ctx, resp.Body, tokens); err != nil {
			errc <- err
			return
		}
		errc <- nil
	}()

	return tokens, errc
}

// readSSEStream reads Server-Sent Events from body, sending content tokens to ch.
// Returns nil on clean end-of-stream or context cancellation.
func readSSEStream(ctx context.Context, body io.Reader, ch chan<- string) error {
	scanner := bufio.NewScanner(body)
	scanner.Buffer(make([]byte, 64*1024), 64*1024)

	for scanner.Scan() {
		if ctx.Err() != nil {
			return nil
		}

		line := scanner.Text()
		if !strings.HasPrefix(line, "data:") {
			continue
		}

		payload := strings.TrimSpace(line[5:])
		if payload == "[DONE]" {
			return nil
		}

		var chunk StreamChunk
		if err := json.Unmarshal([]byte(payload), &chunk); err != nil {
			continue
		}

		if len(chunk.Choices) == 0 {
			continue
		}
		content := chunk.Choices[0].Delta.Content
		if content == "" {
			continue
		}

		select {
		case ch <- content:
		case <-ctx.Done():
			return nil
		}
	}

	if err := scanner.Err(); err != nil {
		if ctx.Err() != nil {
			return nil
		}
		return fmt.Errorf("reading SSE stream: %w", err)
	}
	return nil
}

func resolveURL(baseURL string) string {
	base := strings.TrimRight(baseURL, "/")
	if strings.HasSuffix(base, "/chat/completions") {
		return base
	}
	return base + "/chat/completions"
}
