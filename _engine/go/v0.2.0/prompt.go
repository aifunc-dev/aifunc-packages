// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import (
	"fmt"
	"regexp"
	"strings"
)

func RenderPrompt(artifact AIFuncArtifact, input map[string]any) (string, error) {
	prompt, err := selectPrompt(artifact)
	if err != nil {
		return "", err
	}

	inputJSON := marshalJSON(input)
	prompt = strings.ReplaceAll(prompt, "{{input_json}}", inputJSON)

	reInputField := regexp.MustCompile(`\{\{input\.([a-zA-Z0-9_]+)\}\}`)
	prompt = reInputField.ReplaceAllStringFunc(prompt, func(match string) string {
		sub := reInputField.FindStringSubmatch(match)
		if len(sub) < 2 {
			return match
		}
		fieldName := sub[1]
		val, ok := input[fieldName]
		if !ok {
			return match
		}
		return fmt.Sprintf("%v", val)
	})

	reField := regexp.MustCompile(`\{\{([a-zA-Z0-9_]+)\}\}`)
	prompt = reField.ReplaceAllStringFunc(prompt, func(match string) string {
		sub := reField.FindStringSubmatch(match)
		if len(sub) < 2 {
			return match
		}
		fieldName := sub[1]
		val, ok := input[fieldName]
		if !ok || val == nil {
			return ""
		}
		return fmt.Sprintf("%v", val)
	})

	injectSchema := true
	if artifact.API.InjectOutputSchema != nil && !*artifact.API.InjectOutputSchema {
		injectSchema = false
	}
	if artifact.Package != nil && artifact.Package.EngineOptions != nil &&
		artifact.Package.EngineOptions.InjectOutputSchema != nil &&
		!*artifact.Package.EngineOptions.InjectOutputSchema {
		injectSchema = false
	}
	if injectSchema {
		instruction := buildSchemaInstruction(artifact.API.Output)
		prompt += "\n\n" + instruction
	}

	return prompt, nil
}

func selectPrompt(artifact AIFuncArtifact) (string, error) {
	if artifact.Prompt != "" {
		return artifact.Prompt, nil
	}
	if p, ok := artifact.Prompts["general"]; ok && p != "" {
		return p, nil
	}
	for _, p := range artifact.Prompts {
		if p != "" {
			return p, nil
		}
	}
	return "", fmt.Errorf("artifact missing prompt template")
}

func buildSchemaInstruction(schema map[string]any) string {
	schemaJSON := marshalJSON(schema)
	return "Please respond with a JSON object that matches the following schema:\n\n" +
		schemaJSON +
		"\n\nYour response must be valid JSON only, with no additional text."
}
