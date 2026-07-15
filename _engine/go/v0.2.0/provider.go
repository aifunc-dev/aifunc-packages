// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"encoding/json"
	"fmt"
	"regexp"
	"strings"
)

func BuildRequest(artifact AIFuncArtifact, prompt string, config AIFuncConfig) (ModelRequestParams, error) {
	if config.Model == "" {
		return ModelRequestParams{}, fmt.Errorf("AIFuncConfig.Model is required when mock mode is disabled")
	}

	params := ModelRequestParams{
		Model: config.Model,
		Messages: []ChatMessage{
			{Role: "user", Content: prompt},
		},
		ResponseFormat: &ResponseFormat{Type: "json_object"},
	}

	resolved := resolveModelParams(artifact, config.Model)

	temperature := config.Temperature
	if temperature == nil {
		temperature = resolved.Temperature
	}
	if temperature == nil && artifact.Model != nil {
		temperature = artifact.Model.Temperature
	}
	if temperature != nil {
		params.Temperature = temperature
	}

	topP := config.TopP
	if topP == nil {
		topP = resolved.TopP
	}
	if topP != nil {
		params.TopP = topP
	}

	maxTokens := config.MaxTokens
	if maxTokens == nil {
		maxTokens = resolved.MaxTokens
	}
	if maxTokens == nil && artifact.Model != nil {
		maxTokens = artifact.Model.MaxTokens
	}
	if maxTokens != nil {
		params.MaxTokens = maxTokens
	}

	return params, nil
}

// BuildStreamRequest builds a streaming request — no response_format so the
// model returns raw text rather than a JSON object.
func BuildStreamRequest(artifact AIFuncArtifact, prompt string, config AIFuncConfig) (ModelRequestParams, error) {
	if config.Model == "" {
		return ModelRequestParams{}, fmt.Errorf("AIFuncConfig.Model is required when mock mode is disabled")
	}

	params := ModelRequestParams{
		Model: config.Model,
		Messages: []ChatMessage{
			{Role: "user", Content: prompt},
		},
		// Deliberately no ResponseFormat: stream output is plain text.
	}

	resolved := resolveModelParams(artifact, config.Model)

	temperature := config.Temperature
	if temperature == nil {
		temperature = resolved.Temperature
	}
	if temperature == nil && artifact.Model != nil {
		temperature = artifact.Model.Temperature
	}
	if temperature != nil {
		params.Temperature = temperature
	}

	topP := config.TopP
	if topP == nil {
		topP = resolved.TopP
	}
	if topP != nil {
		params.TopP = topP
	}

	maxTokens := config.MaxTokens
	if maxTokens == nil {
		maxTokens = resolved.MaxTokens
	}
	if maxTokens == nil && artifact.Model != nil {
		maxTokens = artifact.Model.MaxTokens
	}
	if maxTokens != nil {
		params.MaxTokens = maxTokens
	}

	return params, nil
}

func resolveModelParams(artifact AIFuncArtifact, model string) StandardParams {
	if artifact.ModelParams == nil || len(artifact.ModelParams.Rules) == 0 {
		return StandardParams{}
	}
	for _, rule := range artifact.ModelParams.Rules {
		if matchesRule(rule, model) {
			return rule.Params
		}
	}
	return StandardParams{}
}

func matchesRule(rule ParamPreset, model string) bool {
	m := rule.Match
	if m.Model != "" && m.Model == model {
		return true
	}
	for _, v := range m.Models {
		if v == model {
			return true
		}
	}
	if m.Pattern != "" {
		return globMatch(m.Pattern, model)
	}
	// empty match = wildcard
	if m.Model == "" && len(m.Models) == 0 && m.Pattern == "" {
		return true
	}
	return false
}

func globMatch(pattern, value string) bool {
	escaped := regexp.QuoteMeta(pattern)
	escaped = strings.ReplaceAll(escaped, `\*`, ".*")
	re, err := regexp.Compile("^" + escaped + "$")
	if err != nil {
		return false
	}
	return re.MatchString(value)
}

func ParseResponse(resp ModelResponse) (map[string]any, error) {
	if len(resp.Choices) == 0 {
		return nil, fmt.Errorf("model response contains no choices")
	}

	content := strings.TrimSpace(resp.Choices[0].Message.Content)

	reFence := regexp.MustCompile(`(?s)^` + "```" + `(?:json)?\s*\n?(.*?)\n?` + "```" + `$`)
	if m := reFence.FindStringSubmatch(content); len(m) == 2 {
		content = strings.TrimSpace(m[1])
	}

	var result map[string]any
	if err := json.Unmarshal([]byte(content), &result); err != nil {
		preview := content
		if len(preview) > 200 {
			preview = preview[:200]
		}
		return nil, fmt.Errorf("failed to parse model output as JSON: %s", preview)
	}
	return result, nil
}
