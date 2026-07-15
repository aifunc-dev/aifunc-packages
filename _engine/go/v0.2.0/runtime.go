// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"context"
	"fmt"
	"math/rand"
	"strings"
	"time"
)

const (
	defaultTimeoutMs = 7000
	defaultRetries   = 1
)

func Execute(ctx context.Context, artifact AIFuncArtifact, input map[string]any, config AIFuncConfig) (map[string]any, error) {
	if err := ValidateArtifact(artifact); err != nil {
		return nil, fmt.Errorf("invalid artifact: %w", err)
	}

	inputValidation := Validate(input, artifact.API.Input, "root")
	if !inputValidation.Valid {
		return nil, fmt.Errorf("input validation failed:\n%s", strings.Join(inputValidation.Errors, "\n"))
	}

	if config.Mock {
		return executeMock(artifact, input, config)
	}

	timeoutMs := config.Timeout
	if timeoutMs <= 0 {
		timeoutMs = defaultTimeoutMs
	}
	maxRetries := config.MaxRetries
	if maxRetries <= 0 {
		maxRetries = defaultRetries
	}

	var lastErr error
	for attempt := 0; attempt <= maxRetries; attempt++ {
		output, err := executeOnce(ctx, artifact, input, config, timeoutMs)
		if err == nil {
			return output, nil
		}
		lastErr = err
	}
	return nil, lastErr
}

// ExecuteStream streams text tokens from the model as they arrive via SSE.
// ctx controls cancellation and timeout — cancel it to stop the stream.
// Returns (tokensCh, errCh): read all tokens from tokensCh, then check errCh.
//
// In mock mode, the mock output text is yielded word-by-word with a small
// random delay to simulate realistic streaming behaviour.
func ExecuteStream(ctx context.Context, artifact AIFuncArtifact, input map[string]any, config AIFuncConfig) (<-chan string, <-chan error) {
	// Use a buffered error channel so the goroutine can always send without blocking.
	tokens := make(chan string, 64)
	errc := make(chan error, 1)

	go func() {
		defer close(tokens)
		defer close(errc)

		if err := ValidateArtifact(artifact); err != nil {
			errc <- fmt.Errorf("invalid artifact: %w", err)
			return
		}

		inputValidation := Validate(input, artifact.API.Input, "root")
		if !inputValidation.Valid {
			errc <- fmt.Errorf("input validation failed:\n%s", strings.Join(inputValidation.Errors, "\n"))
			return
		}

		if config.Mock {
			executeMockStream(ctx, artifact, input, config, tokens)
			errc <- nil
			return
		}

		timeoutMs := config.Timeout
		if timeoutMs <= 0 {
			timeoutMs = defaultTimeoutMs
		}

		streamCtx, cancel := context.WithTimeout(ctx, time.Duration(timeoutMs)*time.Millisecond)
		defer cancel()

		prompt, err := RenderPrompt(artifact, input)
		if err != nil {
			errc <- err
			return
		}

		requestParams, err := BuildStreamRequest(artifact, prompt, config)
		if err != nil {
			errc <- err
			return
		}

		tokCh, streamErr := SendStreamRequest(streamCtx, config, requestParams)
		for token := range tokCh {
			select {
			case tokens <- token:
			case <-ctx.Done():
				// drain remaining tokens to unblock the sender goroutine
				go func() { for range tokCh {} }()
				errc <- nil
				return
			}
		}

		if err := <-streamErr; err != nil {
			if streamCtx.Err() == context.DeadlineExceeded {
				errc <- fmt.Errorf("stream timeout after %dms", timeoutMs)
			} else {
				errc <- err
			}
			return
		}
		errc <- nil
	}()

	return tokens, errc
}

func executeOnce(ctx context.Context, artifact AIFuncArtifact, input map[string]any, config AIFuncConfig, timeoutMs int) (map[string]any, error) {
	ctx, cancel := context.WithTimeout(ctx, time.Duration(timeoutMs)*time.Millisecond)
	defer cancel()

	prompt, err := RenderPrompt(artifact, input)
	if err != nil {
		return nil, err
	}

	requestParams, err := BuildRequest(artifact, prompt, config)
	if err != nil {
		return nil, err
	}

	response, err := SendRequest(ctx, config, requestParams)
	if err != nil {
		if ctx.Err() == context.DeadlineExceeded {
			return nil, fmt.Errorf("request timeout after %dms", timeoutMs)
		}
		return nil, err
	}

	output, err := ParseResponse(response)
	if err != nil {
		return nil, err
	}

	outputValidation := Validate(output, artifact.API.Output, "root")
	if !outputValidation.Valid {
		return nil, fmt.Errorf("output validation failed:\n%s", strings.Join(outputValidation.Errors, "\n"))
	}

	return output, nil
}

func executeMock(artifact AIFuncArtifact, input map[string]any, config AIFuncConfig) (map[string]any, error) {
	entries := resolveMockEntries(config)

	output := FindMockOutput(entries, input)
	if output == nil {
		generated, ok := GenerateFromSchema(artifact.API.Output).(map[string]any)
		if !ok {
			generated = map[string]any{}
		}

		v := Validate(generated, artifact.API.Output, "root")
		if !v.Valid {
			return nil, fmt.Errorf("auto-generated mock output validation failed:\n%s",
				strings.Join(v.Errors, "\n"))
		}
		return generated, nil
	}

	v := Validate(output, artifact.API.Output, "root")
	if !v.Valid {
		return nil, fmt.Errorf("mock output validation failed:\n%s", strings.Join(v.Errors, "\n"))
	}
	return output, nil
}

// executeMockStream yields mock output text word-by-word with small random delays.
func executeMockStream(ctx context.Context, artifact AIFuncArtifact, input map[string]any, config AIFuncConfig, ch chan<- string) {
	entries := resolveMockEntries(config)
	raw := FindMockOutput(entries, input)

	var text string
	switch v := any(raw).(type) {
	case string:
		text = v
	case map[string]any:
		// grab the first string field from the mock object
		for _, val := range v {
			if s, ok := val.(string); ok {
				text = s
				break
			}
		}
		if text == "" {
			text = marshalJSON(v)
		}
	default:
		generated := GenerateFromSchema(artifact.API.Output)
		if s, ok := generated.(string); ok {
			text = s
		} else {
			text = "(mock output)"
		}
	}

	words := strings.Fields(text)
	for i, word := range words {
		if ctx.Err() != nil {
			return
		}
		token := word
		if i > 0 {
			token = " " + word
		}
		select {
		case ch <- token:
		case <-ctx.Done():
			return
		}
		// 30–90 ms delay per word, matching Python/TS mock behaviour
		delay := time.Duration(30+rand.Intn(61)) * time.Millisecond
		select {
		case <-time.After(delay):
		case <-ctx.Done():
			return
		}
	}
}

func resolveMockEntries(config AIFuncConfig) []MockEntry {
	if config.MockData == nil {
		return nil
	}

	switch v := config.MockData.(type) {
	case []MockEntry:
		return v
	case []any:
		var entries []MockEntry
		for _, item := range v {
			if m, ok := item.(map[string]any); ok {
				var entry MockEntry
				if out, ok := m["output"].(map[string]any); ok {
					entry.Output = out
				}
				if inp, ok := m["input"].(map[string]any); ok {
					entry.Input = inp
				}
				entries = append(entries, entry)
			}
		}
		return entries
	case map[string]any:
		if casesVal, ok := v["cases"]; ok {
			if cases, ok := casesVal.([]any); ok {
				var entries []MockEntry
				for _, item := range cases {
					if m, ok := item.(map[string]any); ok {
						var entry MockEntry
						if out, ok := m["output"].(map[string]any); ok {
							entry.Output = out
						}
						if inp, ok := m["input"].(map[string]any); ok {
							entry.Input = inp
						}
						entries = append(entries, entry)
					}
				}
				return entries
			}
		}
	case MockFile:
		return v.Cases
	}
	return nil
}
