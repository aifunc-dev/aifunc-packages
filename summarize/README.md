# Text Summarization

`summarize` is an AIFunc package that compresses input text into a short, clear summary. The output language matches the source text.

## Function Info

- Name: `summarize`
- Type: `standalone`
- Purpose: Generate concise summaries

## Input

```json
{
  "text": "The full text to summarize.",
  "maxLength": 80
}
```

Fields:

- `text`: Required. The original text.
- `maxLength`: Optional. Maximum word count for the summary. Default: `80`.

## Output

```json
{
  "summary": "The generated summary.",
  "wordCount": 12
}
```

Fields:

- `summary`: The summary content, in the same language as the input.
- `wordCount`: Approximate word count of the summary.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
