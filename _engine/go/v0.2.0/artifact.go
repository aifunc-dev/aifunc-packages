// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import "fmt"

func ValidateArtifact(a AIFuncArtifact) error {
	name := a.Name
	if name == "" && a.Package != nil {
		name = a.Package.Name
	}
	if name == "" {
		return fmt.Errorf("artifact missing required field: name or package.name")
	}

	hasEngine := a.EngineVersion != ""
	if !hasEngine && a.Package != nil {
		hasEngine = a.Package.Engine != ""
	}
	if !hasEngine {
		return fmt.Errorf("artifact missing required field: engineVersion or package.engine")
	}

	if a.Prompt == "" && len(a.Prompts) == 0 {
		return fmt.Errorf("artifact missing required field: prompt or prompts")
	}

	if a.API.Input == nil {
		return fmt.Errorf("artifact missing required field: api.input")
	}
	if a.API.Output == nil {
		return fmt.Errorf("artifact missing required field: api.output")
	}

	return nil
}
