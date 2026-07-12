// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import aifunc.AIFuncException;

import java.util.Map;

/** Validates and constructs {@link Types.AIFuncArtifact} instances from raw data maps. */
public final class Artifact {

    private Artifact() {}

    /**
     * Constructs an {@link Types.AIFuncArtifact} from a raw {@code Map<String, Object>}.
     *
     * @throws AIFuncException if the map is missing required fields
     */
    public static Types.AIFuncArtifact fromMap(Map<String, Object> data) {
        if (data == null) {
            throw new AIFuncException("Artifact data map is null");
        }
        if (!data.containsKey("api")) {
            throw new AIFuncException(
                    "Artifact is missing the required 'api' field. "
                    + "The artifact file may be corrupted or in an unsupported format.");
        }
        return new Types.AIFuncArtifact(data);
    }

    /**
     * Validates that an artifact contains all required fields.
     *
     * @throws AIFuncException if any required field is absent
     */
    public static void validate(Types.AIFuncArtifact a) {
        String name = a.resolveName();
        if (name == null || name.isBlank()) {
            throw new AIFuncException(
                    "Artifact missing required field: name or package.name");
        }

        String engine = a.resolveEngineVersion();
        if (engine == null || engine.isBlank()) {
            throw new AIFuncException(
                    "Artifact missing required field: engineVersion or package.engine");
        }

        boolean hasPrompt = a.getPrompt() != null && !a.getPrompt().isBlank();
        boolean hasPrompts = a.getPrompts() != null && !a.getPrompts().isEmpty();
        if (!hasPrompt && !hasPrompts) {
            throw new AIFuncException(
                    "Artifact missing required field: prompt or prompts");
        }

        Map<String, Object> api = a.getApi();
        if (api == null) {
            throw new AIFuncException("Artifact missing required field: api");
        }
        if (api.get("input") == null) {
            throw new AIFuncException("Artifact missing required field: api.input");
        }
        if (api.get("output") == null) {
            throw new AIFuncException("Artifact missing required field: api.output");
        }
    }
}
