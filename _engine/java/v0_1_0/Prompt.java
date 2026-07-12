// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import aifunc.AIFuncException;

import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/** Renders the prompt template by substituting input field placeholders. */
public final class Prompt {

    private Prompt() {}

    private static final Pattern INPUT_FIELD = Pattern.compile("\\{\\{input\\.([a-zA-Z0-9_]+)\\}\\}");
    private static final Pattern BARE_FIELD  = Pattern.compile("\\{\\{([a-zA-Z0-9_]+)\\}\\}");

    /**
     * Renders the prompt template for the given artifact and input.
     *
     * <p>Substitution rules (applied in order):
     * <ol>
     *   <li>{@code {{input_json}}} → full input serialized as JSON
     *   <li>{@code {{input.fieldName}}} → the string value of {@code input["fieldName"]}
     *   <li>{@code {{fieldName}}} → same, but empty string when not found
     * </ol>
     *
     * <p>If {@code injectOutputSchema} is enabled (default), a JSON-schema instruction
     * is appended to the rendered prompt.
     *
     * @throws AIFuncException if no prompt template is found in the artifact
     */
    public static String render(Types.AIFuncArtifact artifact, Map<String, Object> input) {
        String template = selectPrompt(artifact);

        String inputJson = Json.prettyPrint(input);
        String prompt = template.replace("{{input_json}}", inputJson);

        // {{input.fieldName}}
        Matcher m1 = INPUT_FIELD.matcher(prompt);
        StringBuffer sb1 = new StringBuffer();
        while (m1.find()) {
            String field = m1.group(1);
            Object val   = input.get(field);
            m1.appendReplacement(sb1, Matcher.quoteReplacement(val != null ? String.valueOf(val) : m1.group(0)));
        }
        m1.appendTail(sb1);
        prompt = sb1.toString();

        // {{fieldName}}
        Matcher m2 = BARE_FIELD.matcher(prompt);
        StringBuffer sb2 = new StringBuffer();
        while (m2.find()) {
            String field = m2.group(1);
            Object val   = input.get(field);
            m2.appendReplacement(sb2, Matcher.quoteReplacement(val != null ? String.valueOf(val) : ""));
        }
        m2.appendTail(sb2);
        prompt = sb2.toString();

        if (shouldInjectSchema(artifact)) {
            @SuppressWarnings("unchecked")
            Map<String, Object> outputSchema = (Map<String, Object>) artifact.getApi().get("output");
            prompt = prompt + "\n\n" + buildSchemaInstruction(outputSchema);
        }

        return prompt;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static String selectPrompt(Types.AIFuncArtifact artifact) {
        if (artifact.getPrompt() != null && !artifact.getPrompt().isBlank()) {
            return artifact.getPrompt();
        }
        Map<String, String> prompts = artifact.getPrompts();
        if (prompts != null) {
            String general = prompts.get("general");
            if (general != null && !general.isBlank()) return general;
            for (String p : prompts.values()) {
                if (p != null && !p.isBlank()) return p;
            }
        }
        throw new AIFuncException("Artifact missing prompt template");
    }

    @SuppressWarnings("unchecked")
    private static boolean shouldInjectSchema(Types.AIFuncArtifact artifact) {
        Map<String, Object> api = artifact.getApi();
        if (api != null) {
            Object inject = api.get("injectOutputSchema");
            if (inject instanceof Boolean && !((Boolean) inject)) return false;
        }
        Map<String, Object> pkg = artifact.getPackage();
        if (pkg != null) {
            Object opts = pkg.get("engineOptions");
            if (opts instanceof Map) {
                Object inject = ((Map<String, Object>) opts).get("injectOutputSchema");
                if (inject instanceof Boolean && !((Boolean) inject)) return false;
            }
        }
        return true;
    }

    private static String buildSchemaInstruction(Map<String, Object> schema) {
        return "Please respond with a JSON object that matches the following schema:\n\n"
                + Json.prettyPrint(schema)
                + "\n\nYour response must be valid JSON only, with no additional text.";
    }
}
