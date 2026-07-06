# Keyword Extraction

`extract-keywords` is an AIFunc package that extracts keywords and key phrases from text, with relevance scores.

## Function Info

- Name: `extract-keywords`
- Type: `standalone`
- Purpose: SEO optimization, content tagging, search indexing, content analysis

## Input

```json
{
  "text": "Machine learning is a subfield of artificial intelligence that enables systems to learn autonomously from data.",
  "maxKeywords": 5
}
```

Fields:

- `text`: Required. The text to extract keywords from.
- `maxKeywords`: Optional. Maximum number of keywords to return. Default: `10`. Max: `50`.

## Output

```json
{
  "keywords": [
    { "word": "machine learning", "relevance": 0.95 },
    { "word": "artificial intelligence", "relevance": 0.88 },
    { "word": "data", "relevance": 0.72 }
  ]
}
```

Fields:

- `keywords`: Keywords sorted by relevance (highest first).
  - `word`: Keyword or key phrase.
  - `relevance`: Relevance score (0–1).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
