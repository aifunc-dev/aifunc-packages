# Language Detection

`detect-language` is an AIFunc package that identifies the language of input text.

## Function Info

- Name: `detect-language`
- Type: `standalone`
- Purpose: Language identification in multilingual systems

## Input

```json
{
  "text": "こんにちは、世界！"
}
```

Fields:

- `text`: Required. The text whose language should be detected.

## Output

```json
{
  "language": "ja",
  "languageName": "日本語",
  "confidence": 0.97
}
```

Fields:

- `language`: Detected language code (BCP 47 / ISO 639).
- `languageName`: Human-readable language name (e.g. `"English"`, `"日本語"`).
- `confidence`: Confidence score (0–1).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
