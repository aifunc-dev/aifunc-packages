# Sentiment Analysis

`analyze-sentiment` is an AIFunc package that classifies the sentiment or emotion of input text. It supports custom candidate labels (zero-shot classification).

## Function Info

- Name: `analyze-sentiment`
- Type: `standalone`
- Purpose: Text sentiment classification

## Input

```json
{
  "text": "What a beautiful day — I'm in a great mood!",
  "labels": ["positive", "negative", "neutral"],
  "topK": 2
}
```

Fields:

- `text`: Required. The text to analyze.
- `labels`: Optional. Candidate sentiment labels (at least 2). Default: `["positive", "negative", "neutral"]`.
- `topK`: Optional. Return only the top K highest-scoring labels. Default: return all.

## Output

```json
{
  "label": "positive",
  "confidence": 0.92,
  "rankings": [
    { "label": "positive", "score": 0.92 },
    { "label": "neutral", "score": 0.05 },
    { "label": "negative", "score": 0.03 }
  ]
}
```

Fields:

- `label`: The highest-scoring sentiment label.
- `confidence`: Confidence score for the top label (0–1).
- `rankings`: All labels ranked by score, subject to `topK`.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
