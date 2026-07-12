// Copyright 2026 GildenEye
// SPDX-License-Identifier: Apache-2.0

package aifunc._engine.java.v0_1_0;

import java.util.List;
import java.util.Map;


/** Engine-internal types. Users should not import from this package directly. */
public final class Types {

    private Types() {}

    /** Compiled representation of an AIFunc package. */
    public static final class AIFuncArtifact {

        private final String schemaVersion;
        private final String artifactVersion;
        private final Map<String, Object> pkg;
        private final Map<String, Object> api;
        private final Map<String, Object> modelParams;
        private final Map<String, Object> modelRouting;
        private final Map<String, String> prompts;
        private final Map<String, Object> metadata;
        private final String name;
        private final String engineVersion;
        private final String prompt;
        private final Map<String, Object> model;

        @SuppressWarnings("unchecked")
        AIFuncArtifact(Map<String, Object> data) {
            this.schemaVersion   = str(data, "schemaVersion");
            this.artifactVersion = str(data, "artifactVersion");
            this.pkg             = obj(data, "package");
            this.api             = obj(data, "api");
            this.modelParams     = obj(data, "modelParams");
            this.modelRouting    = obj(data, "modelRouting");
            this.metadata        = obj(data, "metadata");
            this.name            = str(data, "name");
            this.engineVersion   = str(data, "engineVersion");
            this.prompt          = str(data, "prompt");
            this.model           = obj(data, "model");

            Object rawPrompts = data.get("prompts");
            if (rawPrompts instanceof Map) {
                Map<String, String> pm = new java.util.LinkedHashMap<>();
                for (Map.Entry<?, ?> e : ((Map<?, ?>) rawPrompts).entrySet()) {
                    pm.put(String.valueOf(e.getKey()), String.valueOf(e.getValue()));
                }
                this.prompts = pm;
            } else {
                this.prompts = java.util.Collections.emptyMap();
            }
        }

        public String              getSchemaVersion()   { return schemaVersion;   }
        public String              getArtifactVersion() { return artifactVersion; }
        public Map<String, Object> getPackage()         { return pkg;             }
        public Map<String, Object> getApi()             { return api;             }
        public Map<String, Object> getModelParams()     { return modelParams;     }
        public Map<String, Object> getModelRouting()    { return modelRouting;    }
        public Map<String, String> getPrompts()         { return prompts;         }
        public Map<String, Object> getMetadata()        { return metadata;        }
        public String              getName()            { return name;            }
        public String              getEngineVersion()   { return engineVersion;   }
        public String              getPrompt()          { return prompt;          }
        public Map<String, Object> getModel()           { return model;           }

        public String resolveName() {
            if (pkg != null) {
                Object n = pkg.get("name");
                if (n instanceof String && !((String) n).isBlank()) return (String) n;
            }
            return name != null ? name : "";
        }

        public String resolveEngineVersion() {
            if (pkg != null) {
                Object e = pkg.get("engine");
                if (e instanceof String && !((String) e).isBlank()) return (String) e;
            }
            return engineVersion != null ? engineVersion : "";
        }

        @SuppressWarnings("unchecked")
        private static Map<String, Object> obj(Map<String, Object> m, String key) {
            Object v = m.get(key);
            return (v instanceof Map) ? (Map<String, Object>) v : null;
        }

        private static String str(Map<String, Object> m, String key) {
            Object v = m.get(key);
            return (v instanceof String) ? (String) v : null;
        }
    }

    /** A single mock test case. */
    public static final class MockEntry {
        private final Map<String, Object> input;
        private final Map<String, Object> output;

        public MockEntry(Map<String, Object> input, Map<String, Object> output) {
            this.input  = input;
            this.output = output;
        }

        public Map<String, Object> getInput()  { return input;  }
        public Map<String, Object> getOutput() { return output; }

        @SuppressWarnings("unchecked")
        public static MockEntry fromMap(Map<String, Object> m) {
            Object rawIn  = m.get("input");
            Object rawOut = m.get("output");
            Map<String, Object> in  = (rawIn  instanceof Map) ? (Map<String, Object>) rawIn  : null;
            Map<String, Object> out = (rawOut instanceof Map) ? (Map<String, Object>) rawOut
                                                              : new java.util.LinkedHashMap<>();
            return new MockEntry(in, out);
        }
    }

    /** Parameters sent to the model API endpoint. */
    public static final class ModelRequestParams {
        private final String model;
        private final List<Map<String, String>> messages;
        private final Double  temperature;
        private final Double  topP;
        private final Integer maxTokens;
        private final Map<String, String> responseFormat;

        public ModelRequestParams(
                String model,
                List<Map<String, String>> messages,
                Double temperature,
                Double topP,
                Integer maxTokens,
                Map<String, String> responseFormat) {
            this.model          = model;
            this.messages       = messages;
            this.temperature    = temperature;
            this.topP           = topP;
            this.maxTokens      = maxTokens;
            this.responseFormat = responseFormat;
        }

        public String                   getModel()          { return model;          }
        public List<Map<String,String>> getMessages()       { return messages;       }
        public Double                   getTemperature()    { return temperature;    }
        public Double                   getTopP()           { return topP;           }
        public Integer                  getMaxTokens()      { return maxTokens;      }
        public Map<String, String>      getResponseFormat() { return responseFormat; }

        public Map<String, Object> toJsonMap() {
            Map<String, Object> m = new java.util.LinkedHashMap<>();
            m.put("model",    model);
            m.put("messages", messages);
            if (temperature    != null) m.put("temperature",     temperature);
            if (topP           != null) m.put("top_p",           topP);
            if (maxTokens      != null) m.put("max_tokens",      maxTokens);
            if (responseFormat != null) m.put("response_format", responseFormat);
            return m;
        }
    }

    /** Result of validating a data map against a JSON Schema. */
    public static final class ValidationResult {
        private final boolean      valid;
        private final List<String> errors;

        public ValidationResult(boolean valid, List<String> errors) {
            this.valid  = valid;
            this.errors = errors;
        }

        public boolean      isValid()   { return valid;  }
        public List<String> getErrors() { return errors; }
    }
}
