# AIFunc Packages

Official package registry for [AIFunc](https://github.com/aifunc-dev/aifunc). Install any package with one command and get a typed, testable AI function in your project.

## Install

```bash
# Short form
aifn install github:aifunc-dev/aifunc-packages/summarize

# Full URL (copy from browser)
aifn install https://github.com/aifunc-dev/aifunc-packages/tree/main/summarize
```

The CLI detects your project language (TypeScript/Python) and generates fully typed code with built-in mock data.

## Packages

### Text Analysis

| Package | Description | Install |
|:---|:---|:---|
| `analyze-sentiment` | Sentiment analysis with custom labels | `aifn install github:aifunc-dev/aifunc-packages/analyze-sentiment` |
| `classify` | Zero-shot text classification | `aifn install github:aifunc-dev/aifunc-packages/classify` |
| `detect-language` | Language detection | `aifn install github:aifunc-dev/aifunc-packages/detect-language` |
| `extract-entities` | Named entity recognition (NER) | `aifn install github:aifunc-dev/aifunc-packages/extract-entities` |
| `extract-keywords` | Keyword and key phrase extraction | `aifn install github:aifunc-dev/aifunc-packages/extract-keywords` |
| `extract-json` | Structured JSON extraction from text | `aifn install github:aifunc-dev/aifunc-packages/extract-json` |
| `score-quality` | Text quality scoring and suggestions | `aifn install github:aifunc-dev/aifunc-packages/score-quality` |

### Text Transformation

| Package | Description | Install |
|:---|:---|:---|
| `summarize` | Text summarization | `aifn install github:aifunc-dev/aifunc-packages/summarize` |
| `translate` | Multi-language translation | `aifn install github:aifunc-dev/aifunc-packages/translate` |
| `rewrite` | Style-controlled rewriting | `aifn install github:aifunc-dev/aifunc-packages/rewrite` |
| `generate-slug` | SEO slug, meta description, and tags | `aifn install github:aifunc-dev/aifunc-packages/generate-slug` |

### Content Generation

| Package | Description | Install |
|:---|:---|:---|
| `generate-reply` | Contextual reply generation | `aifn install github:aifunc-dev/aifunc-packages/generate-reply` |
| `generate-post` | Social media post generation | `aifn install github:aifunc-dev/aifunc-packages/generate-post` |
| `generate-email` | Email generation from intent | `aifn install github:aifunc-dev/aifunc-packages/generate-email` |
| `generate-title` | Title candidate generation | `aifn install github:aifunc-dev/aifunc-packages/generate-title` |
| `answer-question` | Question answering (grounded or general) | `aifn install github:aifunc-dev/aifunc-packages/answer-question` |

### Understanding

| Package | Description | Install |
|:---|:---|:---|
| `recognize-intent` | User intent recognition | `aifn install github:aifunc-dev/aifunc-packages/recognize-intent` |

## Usage

After installing, import and call:

```typescript
import { summarize, AIFuncConfig, SummarizeInput } from './aifunc/summarize';

const config: AIFuncConfig = { mock: true };

async function main() {
  const result = await summarize(config, { text: 'Your text here...', maxLength: 30 } as SummarizeInput);
  console.log(result.summary);
  console.log(result.wordCount);
}

main().catch(console.error);
```

```python
import asyncio
from aifunc.summarize import summarize, AIFuncConfig, SummarizeInput

config = AIFuncConfig(mock=True)

async def main():
    result = await summarize(config, SummarizeInput(text="Your text here...", max_length=30))
    print(result.summary)
    print(result.word_count)

asyncio.run(main())
```

Use `mock: true` for offline development and testing. Replace with real model config when ready:

```typescript
const config: AIFuncConfig = {
   baseURL: 'https://your-api-endpoint/v1',
   model: 'your-model-name',
   apiKey: 'your-api-key',
};
```

## Package Structure

Each package contains:

```
summarize/
├── package.json        # Metadata (name, version, author, categories)
├── api.json            # Input/output schema (JSON Schema)
├── model-params.json   # Recommended model parameters
├── mock.json           # Mock data for offline testing
├── prompts/
│   └── general.md      # Prompt template
├── README.md           # Package documentation
└── LICENSE
```

## Full API Reference

See [PACKAGES.md](./PACKAGES.md) for detailed input/output schemas of every package.

## Contributing

1. Fork this repository
2. Create your package directory following the structure above
3. Run `aifn validate ./your-package` to verify
4. Submit a Pull Request

> **Note:** The official registry only accepts commonly used, general-purpose packages. Packages that are too domain-specific or experimental are better shared via personal repositories.

## License

Each package is individually licensed. See the `LICENSE` file in each package directory.
