# Title Generation

`generate-title` is an AIFunc package that generates title or headline candidates for a piece of content.

## Function Info

- Name: `generate-title`
- Type: `standalone`
- Purpose: Blog titles, article headlines, SEO optimization, content marketing

## Input

```json
{
  "content": "An article about how meditation reduces stress and improves focus for busy professionals.",
  "style": "seo",
  "count": 3,
  "maxLength": 80
}
```

Fields:

- `content`: Required. The text, summary, or topic to generate titles for.
- `style`: Optional. Title style: `"neutral"`, `"clickbait"`, `"seo"`, `"academic"`. Default: `"neutral"`.
- `count`: Optional. Number of title candidates to generate. Default: `3`. Range: 1–10.
- `maxLength`: Optional. Maximum character count per title. Default: `80`.

## Output

```json
{
  "titles": [
    "How Meditation Helps Busy Professionals Reduce Stress and Sharpen Focus",
    "Meditation for Professionals: Less Stress, More Focus",
    "Why Every Busy Professional Should Try Meditation"
  ]
}
```

Fields:

- `titles`: Generated title candidates, ordered from most to least recommended.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
