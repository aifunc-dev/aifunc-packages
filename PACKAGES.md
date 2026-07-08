# AIFunc Official Packages

## Package Categories

```
aifunc-packages
├── Text Analysis
│   ├── analyze-sentiment    Sentiment analysis with custom labels
│   ├── classify             Zero-shot text classification
│   ├── detect-language      Language detection
│   ├── extract-entities     Named entity recognition (NER)
│   ├── extract-keywords     Keyword and key phrase extraction
│   ├── extract-json         Structured JSON extraction from text
│   └── score-quality        Text quality scoring and suggestions
│
├── Text Transformation
│   ├── summarize            Text summarization
│   ├── translate            Multi-language translation
│   ├── rewrite              Style-controlled rewriting
│   └── generate-slug        SEO slug, meta description, and tags
│
├── Content Generation
│   ├── generate-reply       Contextual reply generation
│   ├── generate-post        Social media post generation
│   ├── generate-email       Email generation from intent
│   ├── generate-title       Title candidate generation
│   └── answer-question      Question answering (grounded or general)
│
└── Understanding
    └── recognize-intent     User intent recognition
```

## Table of Contents

- [summarize](#summarize)
- [translate](#translate)
- [rewrite](#rewrite)
- [classify](#classify)
- [extract-json](#extract-json)
- [extract-keywords](#extract-keywords)
- [extract-entities](#extract-entities)
- [analyze-sentiment](#analyze-sentiment)
- [detect-language](#detect-language)
- [generate-slug](#generate-slug)
- [score-quality](#score-quality)
- [generate-reply](#generate-reply)
- [generate-post](#generate-post)
- [generate-email](#generate-email)
- [generate-title](#generate-title)
- [answer-question](#answer-question)

## Naming Convention

Package names (kebab-case) are automatically converted to each language's idiomatic function name:

| Package | Python | TypeScript |
|---------|--------|------------|
| `summarize` | `summarize()` | `summarize()` |
| `translate` | `translate()` | `translate()` |
| `rewrite` | `rewrite()` | `rewrite()` |
| `classify` | `classify()` | `classify()` |
| `extract-json` | `extract_json()` | `extractJson()` |
| `extract-keywords` | `extract_keywords()` | `extractKeywords()` |
| `extract-entities` | `extract_entities()` | `extractEntities()` |
| `analyze-sentiment` | `analyze_sentiment()` | `analyzeSentiment()` |
| `detect-language` | `detect_language()` | `detectLanguage()` |
| `generate-slug` | `generate_slug()` | `generateSlug()` |
| `score-quality` | `score_quality()` | `scoreQuality()` |
| `generate-reply` | `generate_reply()` | `generateReply()` |
| `generate-post` | `generate_post()` | `generatePost()` |
| `generate-email` | `generate_email()` | `generateEmail()` |
| `generate-title` | `generate_title()` | `generateTitle()` |
| `answer-question` | `answer_question()` | `answerQuestion()` |
| `recognize-intent` | `recognize_intent()` | `recognizeIntent()` |

---

## summarize

Generates a short summary of the input text. Output language matches the source text.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to summarize. |
| `maxLength` | integer | No | Maximum word count for the summary. Default: 80. Range: 20-300. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `summary` | string | The generated summary, in the same language as the input. |
| `wordCount` | integer | Approximate word count of the summary. |

---

## translate

Translates text into a specified target language with automatic source language detection.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to translate. |
| `targetLang` | string | Yes | Target language (e.g. `"Chinese"`, `"English"`, `"zh-CN"`). |
| `sourceLang` | string | No | Source language. If omitted, it is auto-detected. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `translation` | string | The translated text. |
| `detectedLang` | string | The detected source language. |

---

## rewrite

Rewrites text in a specified style or with given instructions, preserving the original meaning.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The original text to rewrite. |
| `style` | string | Yes | Target style or tone (e.g. `"formal"`, `"casual"`, `"concise"`, `"expanded"`, `"academic"`). |
| `instructions` | string | No | Additional rewriting constraints or instructions. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `rewritten` | string | The rewritten text in the target style. |

---

## classify

Classifies text into user-defined categories using zero-shot classification, with confidence scores.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to classify. |
| `categories` | string[] | Yes | List of candidate category labels (at least 2). |
| `allowMultiple` | boolean | No | If true, allows assignment to multiple categories. Default: false. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `classifications` | array | Classification results sorted by confidence (highest first). |
| `classifications[].category` | string | Category label. |
| `classifications[].confidence` | number | Confidence score (0-1). |

---

## extract-json

Extracts structured JSON from natural language text according to a user-defined field schema.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The natural language text to extract information from. |
| `fields` | array | Yes | Schema describing the fields to extract. |
| `fields[].name` | string | Yes | Field name (becomes the key in the output JSON). |
| `fields[].description` | string | Yes | What this field represents, used to guide extraction. |
| `fields[].type` | string | Yes | Expected type: `"string"`, `"number"`, `"boolean"`, `"array"`, `"object"`. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `extracted` | object | Key-value pairs of successfully extracted fields. |
| `missing` | string[] | List of field names that could not be determined from the text. |

---

## extract-keywords

Extracts keywords and key phrases from text, sorted by relevance.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to extract keywords from. |
| `maxKeywords` | integer | No | Maximum number of keywords to return. Default: 10. Range: 1-50. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `keywords` | array | Keywords sorted by relevance (highest first). |
| `keywords[].word` | string | Keyword or key phrase. |
| `keywords[].relevance` | number | Relevance score (0-1). |

---

## extract-entities

Extracts named entities (people, locations, organizations, dates, etc.) from text.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to extract entities from. |
| `entityTypes` | string[] | No | Extract only these entity types. If omitted, all types are extracted. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `entities` | array | List of extracted entities. |
| `entities[].text` | string | The original string from the input text. |
| `entities[].type` | string | Entity type (e.g. `"person"`, `"location"`, `"organization"`, `"date"`). |
| `entities[].start` | integer | Start character offset (0-based). |
| `entities[].end` | integer | End character offset (exclusive). |

---

## analyze-sentiment

Analyzes the sentiment of input text with support for custom labels (zero-shot classification).

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text to analyze. |
| `labels` | string[] | No | Candidate label list. Default: `["positive", "negative", "neutral"]`. At least 2. |
| `topK` | integer | No | Return only the top K highest-scoring labels. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `label` | string | The highest-scoring sentiment label. |
| `confidence` | number | Confidence score for the top label (0-1). |
| `rankings` | array | All labels ranked by score, subject to topK. |
| `rankings[].label` | string | Sentiment label. |
| `rankings[].score` | number | Score (0-1). |

---

## detect-language

Detects the language of input text.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text whose language should be detected. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `language` | string | Detected language code (e.g. `"en"`, `"zh-CN"`, `"ja"`). |
| `languageName` | string | Human-readable language name (e.g. `"English"`). |
| `confidence` | number | Confidence score (0-1). |

---

## generate-slug

Generates an SEO-friendly URL slug, meta description, and tag suggestions from a title.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `title` | string | Yes | The article or page title. |
| `description` | string | No | Additional detail to improve slug and meta description quality. |
| `language` | string | No | Content language hint (e.g. `"en"`, `"zh-CN"`). The slug is always ASCII. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `slug` | string | URL-safe slug (lowercase ASCII, hyphens only). |
| `metaDescription` | string | SEO meta description (recommended 120-160 characters). |
| `tags` | string[] | Suggested tags. |

---

## score-quality

Evaluates text quality across multiple dimensions, returning structured scores and improvement suggestions.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | The text content to evaluate. |
| `targetAudience` | string | No | Intended audience (e.g. `"developers"`, `"customers"`). Default: `"general readers"`. |
| `purpose` | string | No | Writing purpose (e.g. `"informational"`, `"marketing"`). Default: `"general communication"`. |
| `maxSuggestions` | integer | No | Maximum number of improvement suggestions. Default: 3. Range: 1-10. |
| `strictness` | integer | No | Strictness level, 1 (lenient) to 5 (very strict). Default: 3. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `overallScore` | integer | Overall quality score (0-100). |
| `clarityScore` | integer | Clarity score (0-100). |
| `structureScore` | integer | Structure score (0-100). |
| `toneScore` | integer | Tone appropriateness score (0-100). |
| `actionabilityScore` | integer | Actionability score (0-100). |
| `level` | string | Quality level: `"excellent"`, `"good"`, `"fair"`, `"poor"`. |
| `summary` | string | One-sentence summary of the evaluation. |
| `suggestions` | string[] | Improvement suggestions ranked by importance. |

---

## generate-reply

Generates a contextually appropriate reply to a message or comment.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `message` | string | Yes | The original message or comment to reply to. |
| `tone` | string | No | Desired tone: `"friendly"`, `"formal"`, `"empathetic"`, `"concise"`. Default: `"friendly"`. |
| `context` | string | No | Background context to help generate the reply (e.g. role, situation). |
| `language` | string | No | Reply language. If omitted, matches the input message language. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `reply` | string | The generated reply text. |
| `tone` | string | The tone actually used. |

---

## generate-post

Generates a social media post or short-form content from a topic or brief.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `topic` | string | Yes | The topic or core idea for the post. |
| `platform` | string | No | Target platform: `"twitter"`, `"linkedin"`, `"instagram"`, `"general"`. Default: `"general"`. |
| `tone` | string | No | Desired tone (e.g. `"professional"`, `"casual"`, `"inspirational"`). Default: `"casual"`. |
| `maxLength` | integer | No | Maximum character count. Default varies by platform. |
| `includeHashtags` | boolean | No | Whether to append relevant hashtags. Default: `false`. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `post` | string | The generated post content. |
| `hashtags` | string[] | Suggested hashtags (empty array if `includeHashtags` is false). |
| `charCount` | integer | Character count of the post content. |

---

## generate-email

Generates a complete email from a short description of intent and context.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `intent` | string | Yes | The email's purpose (e.g. `"follow up on a job application"`, `"request a refund"`). |
| `tone` | string | No | Desired tone: `"formal"`, `"friendly"`, `"assertive"`. Default: `"formal"`. |
| `senderName` | string | No | Sender's name, used in the sign-off. |
| `recipientName` | string | No | Recipient's name or role, used in the salutation. |
| `keyPoints` | string[] | No | Specific points or details to include in the email body. |
| `language` | string | No | Email language. Default: `"English"`. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `subject` | string | Suggested email subject line. |
| `body` | string | Complete email body including salutation and sign-off. |

---

## generate-title

Generates a title or list of title candidates for a piece of content.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `content` | string | Yes | The text, summary, or topic to generate titles for. |
| `style` | string | No | Title style: `"neutral"`, `"clickbait"`, `"seo"`, `"academic"`. Default: `"neutral"`. |
| `count` | integer | No | Number of title candidates to generate. Default: 3. Range: 1-10. |
| `maxLength` | integer | No | Maximum characters per title. Default: 80. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `titles` | string[] | Generated title candidates, ranked by recommendation (highest first). |

---

## answer-question

Answers a question based on provided context or general knowledge.

**Input:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `question` | string | Yes | The question to answer. |
| `context` | string | No | Source text or document for grounded answering. If omitted, uses general knowledge. |
| `maxLength` | integer | No | Maximum word count for the answer. Default: 100. Range: 20-500. |
| `language` | string | No | Answer language. If omitted, matches the question language. |

**Output:**

| Field | Type | Description |
|-------|------|-------------|
| `answer` | string | The generated answer. |
| `grounded` | boolean | `true` if based on provided context, `false` if based on general knowledge. |
| `confidence` | number | Confidence score (0-1). |
