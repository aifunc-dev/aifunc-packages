// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
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

	base := strings.TrimRight(config.BaseURL, "/")
	endpoint := base
	if !strings.HasSuffix(base, "/chat/completions") {
		endpoint = base + "/chat/completions"
	}

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
