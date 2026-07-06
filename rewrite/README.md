# Text Rewriting

`rewrite` is an AIFunc package that rewrites text in a specified style or tone.

## Function Info

- Name: `rewrite`
- Type: `standalone`
- Purpose: Content rewriting, tone adjustment, text polishing

## Input

```json
{
  "text": "The project finished ahead of schedule! Take a look when you get a chance.",
  "style": "formal",
  "instructions": "Keep it to two sentences."
}
```

Fields:

- `text`: Required. The original text to rewrite.
- `style`: Required. Target style or tone (e.g. `"formal"`, `"casual"`, `"concise"`, `"expanded"`, `"academic"`, `"humorous"`).
- `instructions`: Optional. Additional constraints or instructions for the rewrite.

## Output

```json
{
  "rewritten": "We are pleased to inform you that the project has been completed ahead of schedule. Please review the deliverables at your earliest convenience."
}
```

Fields:

- `rewritten`: The rewritten text in the target style.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
