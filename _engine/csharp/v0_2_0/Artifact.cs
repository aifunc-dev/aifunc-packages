// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Aifunc;

namespace Aifunc.Engine.Csharp.V0_2_0;

/// <summary>Validates and constructs <see cref="Types.AIFuncArtifact"/> instances from raw data maps.</summary>
public static class Artifact
{
    /// <summary>
    /// Constructs an <see cref="Types.AIFuncArtifact"/> from a raw dictionary.
    /// </summary>
    /// <exception cref="AIFuncException">Thrown if the map is missing required fields.</exception>
    public static Types.AIFuncArtifact FromMap(Dictionary<string, object?>? data)
    {
        if (data is null)
        {
            throw new AIFuncException("Artifact data map is null");
        }
        if (!data.ContainsKey("api"))
        {
            throw new AIFuncException(
                "Artifact is missing the required 'api' field. "
                + "The artifact file may be corrupted or in an unsupported format.");
        }
        return new Types.AIFuncArtifact(data);
    }

    /// <summary>
    /// Validates that an artifact contains all required fields.
    /// </summary>
    /// <exception cref="AIFuncException">Thrown if any required field is absent.</exception>
    public static void Validate(Types.AIFuncArtifact a)
    {
        var name = a.ResolveName();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AIFuncException(
                "Artifact missing required field: name or package.name");
        }

        var engine = a.ResolveEngineVersion();
        if (string.IsNullOrWhiteSpace(engine))
        {
            throw new AIFuncException(
                "Artifact missing required field: engineVersion or package.engine");
        }

        var hasPrompt = !string.IsNullOrWhiteSpace(a.Prompt);
        var hasPrompts = a.Prompts != null && a.Prompts.Count > 0;
        if (!hasPrompt && !hasPrompts)
        {
            throw new AIFuncException(
                "Artifact missing required field: prompt or prompts");
        }

        var api = a.Api;
        if (api is null)
        {
            throw new AIFuncException("Artifact missing required field: api");
        }
        if (!api.ContainsKey("input") || api["input"] is null)
        {
            throw new AIFuncException("Artifact missing required field: api.input");
        }
        if (!api.ContainsKey("output") || api["output"] is null)
        {
            throw new AIFuncException("Artifact missing required field: api.output");
        }
    }
}
