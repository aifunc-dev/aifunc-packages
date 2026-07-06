# Structured JSON Extraction

`extract-json` is an AIFunc package that extracts structured data from natural language text according to a user-defined field schema.

## Function Info

- Name: `extract-json`
- Type: `standalone`
- Purpose: Form filling, resume parsing, invoice extraction, data entry automation

## Input

```json
{
  "text": "Hi, I'm Wei Zhang (zhangwei@example.com), with 5 years of experience in Python, machine learning, and SQL.",
  "fields": [
    { "name": "name", "description": "Person's full name", "type": "string" },
    { "name": "email", "description": "Email address", "type": "string" },
    { "name": "yearsOfExperience", "description": "Years of work experience", "type": "number" },
    { "name": "skills", "description": "List of technical skills", "type": "array" }
  ]
}
```

Fields:

- `text`: Required. The natural language text to extract information from.
- `fields`: Required. Array of field descriptors (at least 1).
  - `name`: Field name (becomes the key in the output JSON).
  - `description`: What this field represents.
  - `type`: Expected type — `"string"`, `"number"`, `"boolean"`, `"array"`, or `"object"`.

## Output

```json
{
  "extracted": {
    "name": "Wei Zhang",
    "email": "zhangwei@example.com",
    "yearsOfExperience": 5,
    "skills": ["Python", "machine learning", "SQL"]
  },
  "missing": []
}
```

Fields:

- `extracted`: Key-value pairs of successfully extracted fields.
- `missing`: List of field names that could not be determined from the text.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
