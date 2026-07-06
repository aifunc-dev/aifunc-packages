# Question Answering

`answer-question` is an AIFunc package that answers a question based on provided context or general knowledge.

## Function Info

- Name: `answer-question`
- Type: `standalone`
- Purpose: Knowledge retrieval, FAQ systems, reading comprehension

## Input

```json
{
  "question": "What is the capital of France?",
  "context": "France is a country in Western Europe. Its capital and largest city is Paris.",
  "maxLength": 100,
  "language": "English"
}
```

Fields:

- `question`: Required. The question to answer.
- `context`: Optional. Source text or document to base the answer on. If omitted, uses general knowledge.
- `maxLength`: Optional. Maximum word count for the answer. Default: `100`. Range: 20–500.
- `language`: Optional. Answer language. If omitted, matches the question language.

## Output

```json
{
  "answer": "The capital of France is Paris.",
  "grounded": true,
  "confidence": 0.98
}
```

Fields:

- `answer`: The generated answer.
- `grounded`: `true` if the answer is based on the provided context, `false` if from general knowledge.
- `confidence`: Confidence score (0–1).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
