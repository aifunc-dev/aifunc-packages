# Named Entity Recognition

`extract-entities` is an AIFunc package that extracts named entities (people, locations, organizations, dates, etc.) from text.

## Function Info

- Name: `extract-entities`
- Type: `standalone`
- Purpose: Information extraction, content indexing, knowledge graph construction

## Input

```json
{
  "text": "Apple CEO Tim Cook announced the event in Cupertino in March 2024.",
  "entityTypes": ["person", "organization", "location"]
}
```

Fields:

- `text`: Required. The text to extract entities from.
- `entityTypes`: Optional. Extract only these entity types. If omitted, all types are extracted.

## Output

```json
{
  "entities": [
    { "text": "Apple", "type": "organization", "start": 0, "end": 5 },
    { "text": "Tim Cook", "type": "person", "start": 10, "end": 18 },
    { "text": "Cupertino", "type": "location", "start": 41, "end": 50 }
  ]
}
```

Fields:

- `entities`: List of entities in order of appearance.
  - `text`: The original string from the input text.
  - `type`: Entity type (person, location, organization, date, etc.).
  - `start`: Start character offset (0-based).
  - `end`: End character offset (exclusive).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
