# AIFunc 官方包 — 完整 API 参考

## 包分类

```
aifunc-packages
├── 文本分析
│   ├── analyze-sentiment    情感分析（支持自定义标签）
│   ├── classify             零样本文本分类
│   ├── detect-language      语言检测
│   ├── extract-entities     命名实体识别（NER）
│   ├── extract-keywords     关键词提取
│   ├── extract-json         从文本中提取结构化 JSON
│   └── score-quality        文本质量评分与改进建议
│
├── 文本转换
│   ├── summarize            文本摘要
│   ├── translate            多语言翻译
│   ├── rewrite              风格改写
│   └── generate-slug        生成 SEO slug、meta 描述和标签
│
├── 内容生成
│   ├── generate-reply       生成上下文回复
│   ├── generate-post        生成社交媒体内容
│   ├── generate-email       根据意图生成邮件
│   ├── generate-title       生成标题候选
│   └── answer-question      问答（基于上下文或通用知识）
│
└── 语义理解
    └── recognize-intent     用户意图识别
```

## 目录

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
- [recognize-intent](#recognize-intent)

## 命名规则

包名（kebab-case）会自动转换为各语言的惯用函数名：

| 包名 | Python | TypeScript |
|------|--------|------------|
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

生成输入文本的简短摘要。输出语言与源文本一致。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要摘要的文本。 |
| `maxLength` | integer | 否 | 摘要最大字数。默认：80。范围：20-300。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `summary` | string | 生成的摘要，语言与输入一致。 |
| `wordCount` | integer | 摘要的大致字数。 |

---

## translate

将文本翻译为指定目标语言，自动检测源语言。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要翻译的文本。 |
| `targetLang` | string | 是 | 目标语言（如 `"Chinese"`、`"English"`、`"zh-CN"`）。 |
| `sourceLang` | string | 否 | 源语言。省略时自动检测。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `translation` | string | 翻译后的文本。 |
| `detectedLang` | string | 检测到的源语言。 |

---

## rewrite

按指定风格或指令改写文本，保留原意。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要改写的原始文本。 |
| `style` | string | 是 | 目标风格（如 `"formal"`、`"casual"`、`"concise"`、`"expanded"`、`"academic"`）。 |
| `instructions` | string | 否 | 附加改写约束或指令。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `rewritten` | string | 改写后的文本。 |

---

## classify

使用零样本分类将文本归入用户定义的类别，附带置信度分数。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要分类的文本。 |
| `categories` | string[] | 是 | 候选类别列表（至少 2 个）。 |
| `allowMultiple` | boolean | 否 | 为 true 时允许归入多个类别。默认：false。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `classifications` | array | 分类结果，按置信度从高到低排序。 |
| `classifications[].category` | string | 类别标签。 |
| `classifications[].confidence` | number | 置信度分数（0-1）。 |

---

## extract-json

根据用户定义的字段 Schema，从自然语言文本中提取结构化 JSON。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要从中提取信息的自然语言文本。 |
| `fields` | array | 是 | 描述要提取的字段的 Schema。 |
| `fields[].name` | string | 是 | 字段名（作为输出 JSON 的 key）。 |
| `fields[].description` | string | 是 | 字段含义描述，用于引导提取。 |
| `fields[].type` | string | 是 | 期望类型：`"string"`、`"number"`、`"boolean"`、`"array"`、`"object"`。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `extracted` | object | 成功提取的字段键值对。 |
| `missing` | string[] | 无法从文本中确定的字段名列表。 |

---

## extract-keywords

从文本中提取关键词和关键短语，按相关性排序。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要提取关键词的文本。 |
| `maxKeywords` | integer | 否 | 最多返回关键词数量。默认：10。范围：1-50。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `keywords` | array | 按相关性从高到低排序的关键词列表。 |
| `keywords[].word` | string | 关键词或关键短语。 |
| `keywords[].relevance` | number | 相关性分数（0-1）。 |

---

## extract-entities

从文本中提取命名实体（人名、地点、组织、日期等）。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要提取实体的文本。 |
| `entityTypes` | string[] | 否 | 仅提取这些实体类型。省略时提取所有类型。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `entities` | array | 提取到的实体列表。 |
| `entities[].text` | string | 输入文本中的原始字符串。 |
| `entities[].type` | string | 实体类型（如 `"person"`、`"location"`、`"organization"`、`"date"`）。 |
| `entities[].start` | integer | 起始字符偏移量（从 0 开始）。 |
| `entities[].end` | integer | 结束字符偏移量（不含）。 |

---

## analyze-sentiment

分析输入文本的情感，支持自定义标签（零样本分类）。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要分析的文本。 |
| `labels` | string[] | 否 | 候选标签列表。默认：`["positive", "negative", "neutral"]`。至少 2 个。 |
| `topK` | integer | 否 | 仅返回得分最高的前 K 个标签。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `label` | string | 得分最高的情感标签。 |
| `confidence` | number | 最高标签的置信度（0-1）。 |
| `rankings` | array | 所有标签按分数排序，受 topK 限制。 |
| `rankings[].label` | string | 情感标签。 |
| `rankings[].score` | number | 分数（0-1）。 |

---

## detect-language

检测输入文本的语言。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要检测语言的文本。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `language` | string | 检测到的语言代码（如 `"en"`、`"zh-CN"`、`"ja"`）。 |
| `languageName` | string | 人类可读的语言名称（如 `"English"`）。 |
| `confidence` | number | 置信度分数（0-1）。 |

---

## generate-slug

根据标题生成 SEO 友好的 URL slug、meta 描述和标签建议。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `title` | string | 是 | 文章或页面标题。 |
| `description` | string | 否 | 补充描述，用于提升 slug 和 meta 描述质量。 |
| `language` | string | 否 | 内容语言提示（如 `"en"`、`"zh-CN"`）。slug 始终为 ASCII。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `slug` | string | URL 安全的 slug（小写 ASCII，仅含连字符）。 |
| `metaDescription` | string | SEO meta 描述（建议 120-160 字符）。 |
| `tags` | string[] | 建议的标签。 |

---

## score-quality

从多个维度评估文本质量，返回结构化评分和改进建议。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要评估的文本内容。 |
| `targetAudience` | string | 否 | 目标受众（如 `"developers"`、`"customers"`）。默认：`"general readers"`。 |
| `purpose` | string | 否 | 写作目的（如 `"informational"`、`"marketing"`）。默认：`"general communication"`。 |
| `maxSuggestions` | integer | 否 | 最多改进建议条数。默认：3。范围：1-10。 |
| `strictness` | integer | 否 | 严格程度，1（宽松）到 5（非常严格）。默认：3。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `overallScore` | integer | 综合质量分数（0-100）。 |
| `clarityScore` | integer | 清晰度分数（0-100）。 |
| `structureScore` | integer | 结构分数（0-100）。 |
| `toneScore` | integer | 语气适当性分数（0-100）。 |
| `actionabilityScore` | integer | 可操作性分数（0-100）。 |
| `level` | string | 质量等级：`"excellent"`、`"good"`、`"fair"`、`"poor"`。 |
| `summary` | string | 一句话评估总结。 |
| `suggestions` | string[] | 按重要性排序的改进建议。 |

---

## generate-reply

生成与消息或评论上下文匹配的回复。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `message` | string | 是 | 要回复的原始消息或评论。 |
| `tone` | string | 否 | 期望语气：`"friendly"`、`"formal"`、`"empathetic"`、`"concise"`。默认：`"friendly"`。 |
| `context` | string | 否 | 帮助生成回复的背景信息（如角色、场景）。 |
| `language` | string | 否 | 回复语言。省略时匹配输入消息语言。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `reply` | string | 生成的回复文本。 |
| `tone` | string | 实际使用的语气。 |

---

## generate-post

根据主题或简述生成社交媒体帖子或短内容。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `topic` | string | 是 | 帖子的主题或核心想法。 |
| `platform` | string | 否 | 目标平台：`"twitter"`、`"linkedin"`、`"instagram"`、`"general"`。默认：`"general"`。 |
| `tone` | string | 否 | 期望语气（如 `"professional"`、`"casual"`、`"inspirational"`）。默认：`"casual"`。 |
| `maxLength` | integer | 否 | 最大字符数。默认值因平台而异。 |
| `includeHashtags` | boolean | 否 | 是否附加相关话题标签。默认：`false`。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `post` | string | 生成的帖子内容。 |
| `hashtags` | string[] | 建议的话题标签（`includeHashtags` 为 false 时为空数组）。 |
| `charCount` | integer | 帖子内容的字符数。 |

---

## generate-email

根据简短的意图描述和上下文生成完整邮件。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `intent` | string | 是 | 邮件目的（如 `"follow up on a job application"`、`"request a refund"`）。 |
| `tone` | string | 否 | 期望语气：`"formal"`、`"friendly"`、`"assertive"`。默认：`"formal"`。 |
| `senderName` | string | 否 | 发件人姓名，用于落款。 |
| `recipientName` | string | 否 | 收件人姓名或角色，用于称呼。 |
| `keyPoints` | string[] | 否 | 邮件正文中需要包含的要点。 |
| `language` | string | 否 | 邮件语言。默认：`"English"`。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `subject` | string | 建议的邮件主题行。 |
| `body` | string | 完整邮件正文，含称呼和落款。 |

---

## generate-title

为内容生成标题或标题候选列表。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `content` | string | 是 | 要生成标题的文本、摘要或主题。 |
| `style` | string | 否 | 标题风格：`"neutral"`、`"clickbait"`、`"seo"`、`"academic"`。默认：`"neutral"`。 |
| `count` | integer | 否 | 生成标题候选数量。默认：3。范围：1-10。 |
| `maxLength` | integer | 否 | 每个标题最大字符数。默认：80。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `titles` | string[] | 生成的标题候选，按推荐程度从高到低排列。 |

---

## answer-question

基于提供的上下文或通用知识回答问题。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `question` | string | 是 | 要回答的问题。 |
| `context` | string | 否 | 用于基于上下文回答的源文本或文档。省略时使用通用知识。 |
| `maxLength` | integer | 否 | 回答最大字数。默认：100。范围：20-500。 |
| `language` | string | 否 | 回答语言。省略时匹配问题语言。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `answer` | string | 生成的回答。 |
| `grounded` | boolean | `true` 表示基于提供的上下文，`false` 表示基于通用知识。 |
| `confidence` | number | 置信度分数（0-1）。 |

---

## recognize-intent

从对话文本中识别用户意图，附带置信度分数。

**输入：**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `text` | string | 是 | 要识别意图的用户消息。 |
| `intents` | string[] | 是 | 候选意图列表（至少 2 个）。 |
| `context` | string | 否 | 对话上下文或系统描述，用于提升识别准确度。 |

**输出：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `intent` | string | 置信度最高的意图。 |
| `confidence` | number | 最高意图的置信度分数（0-1）。 |
| `rankings` | array | 所有意图按置信度从高到低排序。 |
| `rankings[].intent` | string | 意图标签。 |
| `rankings[].confidence` | number | 置信度分数（0-1）。 |
