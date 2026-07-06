# Social Media Post Generation

`generate-post` is an AIFunc package that generates a social media post or short-form content from a topic or brief.

## Function Info

- Name: `generate-post`
- Type: `standalone`
- Purpose: Social media content creation, marketing copy, short-form writing

## Input

```json
{
  "topic": "Benefits of remote work for software developers",
  "platform": "linkedin",
  "tone": "professional",
  "maxLength": 300,
  "includeHashtags": true
}
```

Fields:

- `topic`: Required. The subject or key idea of the post.
- `platform`: Optional. Target platform: `"twitter"`, `"linkedin"`, `"instagram"`, `"general"`. Default: `"general"`.
- `tone`: Optional. Desired tone (e.g. `"professional"`, `"casual"`, `"inspirational"`). Default: `"casual"`.
- `maxLength`: Optional. Maximum character count. Range: 10–2000.
- `includeHashtags`: Optional. Whether to append relevant hashtags. Default: `false`.

## Output

```json
{
  "post": "Remote work has transformed how developers build software. Fewer distractions, flexible schedules, and the freedom to design your own workspace lead to deeper focus and better code.",
  "hashtags": ["#RemoteWork", "#SoftwareDevelopment", "#DevLife"],
  "charCount": 178
}
```

Fields:

- `post`: The generated post content.
- `hashtags`: Suggested hashtags (empty array if `includeHashtags` is false).
- `charCount`: Character count of the generated post.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
