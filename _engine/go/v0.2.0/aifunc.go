// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

// Package engine is the AIFunc Go runtime engine (v0.2.0).
// It is generated into your project by the aifn CLI and has zero external dependencies.
//
// v0.2.0 adds streaming output support via ExecuteStream, which returns a
// token channel and an error channel following Go's standard context-based
// cancellation pattern.
//
// Typical usage (from generated wrapper code):
//
//	artifact, _ := engine.ArtifactFromMap(artifactData)
//
//	// Non-streaming:
//	result, err := engine.Execute(ctx, artifact, inputMap, config)
//
//	// Streaming:
//	ctx, cancel := context.WithCancel(context.Background())
//	defer cancel()
//	tokens, errc := engine.ExecuteStream(ctx, artifact, inputMap, config)
//	for token := range tokens {
//	    fmt.Print(token)
//	}
//	if err := <-errc; err != nil {
//	    log.Fatal(err)
//	}
package engine
