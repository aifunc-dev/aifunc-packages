# Text Classification

`classify` is an AIFunc package that supports zero-shot classification of text into user-defined categories.

## Function Info

- Name: `classify`
- Type: `standalone`
- Purpose: Ticket routing, content moderation, intent detection, topic tagging

## Input

```json
{
  "text": "I was charged twice for my subscription this month.",
  "categories": ["billing", "technical", "general"],
  "allowMultiple": false
}
```

Fields:

- `text`: Required. The text to classify.
- `categories`: Required. List of candidate category labels (at least 2).
- `allowMultiple`: Optional. Whether the text can belong to multiple categories. Default: `false`.

## Output

```json
{
  "classifications": [
    { "category": "billing", "confidence": 0.85 },
    { "category": "technical", "confidence": 0.10 },
    { "category": "general", "confidence": 0.05 }
  ]
}
```

Fields:

- `classifications`: Classification results sorted by confidence (highest first).
  - `category`: Category label.
  - `confidence`: Confidence score (0–1).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
