# SEO URL Slug Generation

`generate-slug` is an AIFunc package that generates SEO-friendly URL slugs, meta descriptions, and tag suggestions from a title.

## Function Info

- Name: `generate-slug`
- Type: `standalone`
- Purpose: Content publishing, CMS systems, blog platforms, SEO optimization

## Input

```json
{
  "title": "Getting Started with Machine Learning",
  "description": "A beginner-friendly introduction to the fundamentals of machine learning",
  "language": "en"
}
```

Fields:

- `title`: Required. The article or page title.
- `description`: Optional. Additional detail to improve slug and meta description quality.
- `language`: Optional. Content language hint. The slug is always output as ASCII characters.

## Output

```json
{
  "slug": "getting-started-with-machine-learning",
  "metaDescription": "A beginner-friendly guide to machine learning fundamentals, covering core concepts and practical applications.",
  "tags": ["machine learning", "beginner", "artificial intelligence", "tutorial"]
}
```

Fields:

- `slug`: URL-safe slug (lowercase ASCII, hyphens only).
- `metaDescription`: SEO meta description (recommended 120–160 characters).
- `tags`: Suggested tags for categorization and discoverability.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
