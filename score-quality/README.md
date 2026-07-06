# Text Quality Scoring

`score-quality` is an AIFunc package that evaluates writing quality across multiple dimensions, returning structured scores and improvement suggestions.

## Function Info

- Name: `score-quality`
- Type: `standalone`
- Purpose: Evaluate text quality with integer scores, a grade level, a summary, and improvement suggestions

## Input

```json
{
  "text": "Please review the attached proposal and send your feedback by Friday.",
  "targetAudience": "team members",
  "purpose": "internal communication",
  "maxSuggestions": 3,
  "strictness": 3
}
```

Fields:

- `text`: Required. The text content to evaluate.
- `targetAudience`: Optional. Intended audience (e.g. `"developers"`, `"customers"`). Default: `"general readers"`.
- `purpose`: Optional. Writing purpose (e.g. `"informational"`, `"marketing"`). Default: `"general communication"`.
- `maxSuggestions`: Optional. Maximum number of improvement suggestions. Default: `3`. Range: 1–10.
- `strictness`: Optional. Strictness level, 1 (lenient) to 5 (very strict). Default: `3`.

## Output

```json
{
  "overallScore": 84,
  "clarityScore": 88,
  "structureScore": 82,
  "toneScore": 86,
  "actionabilityScore": 78,
  "level": "good",
  "summary": "The text is clear and well-toned, but could better specify next steps.",
  "suggestions": [
    "Add a specific deadline or owner to the next action.",
    "Consolidate related details into shorter paragraphs.",
    "Replace vague phrasing with measurable outcomes."
  ]
}
```

Fields:

- `overallScore`: Overall quality score (0–100).
- `clarityScore`: Clarity score (0–100).
- `structureScore`: Structure score (0–100).
- `toneScore`: Tone appropriateness score (0–100).
- `actionabilityScore`: Actionability score (0–100).
- `level`: Quality level: `"excellent"`, `"good"`, `"fair"`, `"poor"`.
- `summary`: One-sentence summary of the evaluation.
- `suggestions`: Improvement suggestions ranked by importance.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
