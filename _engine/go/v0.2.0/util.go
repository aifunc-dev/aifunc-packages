// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package engine

import "encoding/json"

func marshalJSON(v any) string {
	b, err := json.MarshalIndent(v, "", "  ")
	if err != nil {
		return "{}"
	}
	return string(b)
}
