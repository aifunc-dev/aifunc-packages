# Text Translation

`translate` is an AIFunc package that translates text into a specified target language with automatic source language detection.

## Function Info

- Name: `translate`
- Type: `standalone`
- Purpose: Multi-language translation, content localization

## Input

```json
{
  "text": "Hello, how are you today?",
  "targetLang": "Chinese",
  "sourceLang": "English"
}
```

Fields:

- `text`: Required. The text to be translated.
- `targetLang`: Required. Target language (e.g. `"English"`, `"日本語"`, `"zh-CN"`).
- `sourceLang`: Optional. Source language. If omitted, it will be auto-detected.

## Output

```json
{
  "translation": "你好，你今天怎么样？",
  "sourceLang": "English"
}
```

Fields:

- `translation`: The translated text.
- `sourceLang`: The source language (auto-detected if not provided in input).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
