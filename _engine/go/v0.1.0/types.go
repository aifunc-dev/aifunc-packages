// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"encoding/json"
	"net/http"
)

type AIFuncConfig struct {
	BaseURL     string
	APIKey      string
	Model       string
	Temperature *float64
	TopP        *float64
	MaxTokens   *int
	Timeout     int          // milliseconds; 0 → engine default (7000)
	MaxRetries  int          // 0 → engine default (1)
	Mock        bool
	MockData    any          // []MockEntry or MockFile
	HTTPClient  *http.Client // nil → package-level default client
}

type JSONSchema = map[string]any

type AIFuncArtifact struct {
	SchemaVersion   string         `json:"schemaVersion"`
	ArtifactVersion string         `json:"artifactVersion"`
	Package         *PackageInfo   `json:"package"`
	API             APISpec        `json:"api"`
	ModelParams     *ModelParams   `json:"modelParams"`
	ModelRouting    *ModelRouting  `json:"modelRouting"`
	Prompts         map[string]string `json:"prompts"`
	Metadata        map[string]any `json:"metadata"`

	// Legacy flat fields
	Name         string         `json:"name"`
	Description  string         `json:"description"`
	EngineVersion string        `json:"engineVersion"`
	Prompt       string         `json:"prompt"`
	Model        *ModelDefaults `json:"model"`
	MockFile     string         `json:"mockFile"`
}

type PackageInfo struct {
	SchemaVersion string         `json:"schemaVersion"`
	Type          string         `json:"type"`
	Name          string         `json:"name"`
	Version       string         `json:"version"`
	Description   string         `json:"description"`
	Engine        string         `json:"engine"`
	EngineOptions *EngineOptions `json:"engineOptions"`
}

type EngineOptions struct {
	InjectOutputSchema *bool `json:"injectOutputSchema"`
}

type APISpec struct {
	Name               string         `json:"name"`
	Description        string         `json:"description"`
	Input              map[string]any `json:"input"`
	Output             map[string]any `json:"output"`
	InjectOutputSchema *bool          `json:"injectOutputSchema"`
}

type ModelDefaults struct {
	Temperature *float64 `json:"temperature"`
	MaxTokens   *int     `json:"maxTokens"`
}

type ModelParams struct {
	SchemaVersion string        `json:"schemaVersion"`
	Rules         []ParamPreset `json:"rules"`
}

type ParamPreset struct {
	Match  MatchRule      `json:"match"`
	Params StandardParams `json:"params"`
}

type MatchRule struct {
	Model   string   `json:"model"`
	Models  []string `json:"models"`
	Pattern string   `json:"pattern"`
}

type StandardParams struct {
	Temperature *float64 `json:"temperature"`
	TopP        *float64 `json:"topP"`
	MaxTokens   *int     `json:"maxTokens"`
}

type ModelRouting struct {
	SchemaVersion string   `json:"schemaVersion"`
	Default       string   `json:"default"`
	Fallback      []string `json:"fallback"`
	Allowed       []string `json:"allowed"`
	Denied        []string `json:"denied"`
}

type ModelRequestParams struct {
	Model          string            `json:"model"`
	Messages       []ChatMessage     `json:"messages"`
	Temperature    *float64          `json:"temperature,omitempty"`
	TopP           *float64          `json:"top_p,omitempty"`
	MaxTokens      *int              `json:"max_tokens,omitempty"`
	ResponseFormat *ResponseFormat   `json:"response_format,omitempty"`
}

type ChatMessage struct {
	Role    string `json:"role"`
	Content string `json:"content"`
}

type ResponseFormat struct {
	Type string `json:"type"`
}

type ModelResponse struct {
	ID      string   `json:"id"`
	Object  string   `json:"object"`
	Created int64    `json:"created"`
	Model   string   `json:"model"`
	Choices []Choice `json:"choices"`
	Usage   *Usage   `json:"usage"`
}

type Choice struct {
	Index        int         `json:"index"`
	Message      ChatMessage `json:"message"`
	FinishReason string      `json:"finish_reason"`
}

type Usage struct {
	PromptTokens     int `json:"prompt_tokens"`
	CompletionTokens int `json:"completion_tokens"`
	TotalTokens      int `json:"total_tokens"`
}

type ValidationResult struct {
	Valid  bool
	Errors []string
}

type MockEntry struct {
	Input  map[string]any `json:"input"`
	Output map[string]any `json:"output"`
}

type MockFile struct {
	Version string      `json:"version"`
	Cases   []MockEntry `json:"cases"`
}

func ArtifactFromMap(m map[string]any) (AIFuncArtifact, error) {
	data, err := json.Marshal(m)
	if err != nil {
		return AIFuncArtifact{}, err
	}
	var a AIFuncArtifact
	if err := json.Unmarshal(data, &a); err != nil {
		return AIFuncArtifact{}, err
	}
	return a, nil
}
