# AIFunc 官方包仓库

[AIFunc](https://github.com/aifunc-dev/aifunc) 官方包仓库。一行命令安装，即可获得强类型、可测试的 AI 函数。

## 安装

```bash
# 简写模式
aifn install github:aifunc-dev/aifunc-packages/summarize

# 完整 URL 模式（从浏览器直接复制）
aifn install https://github.com/aifunc-dev/aifunc-packages/tree/main/summarize
```

CLI 自动识别项目语言（TypeScript/Python），生成带完整类型定义和内置 Mock 数据的代码。

## 包列表

### 文本分析

| 包名 | 用途 | 安装命令 |
|:---|:---|:---|
| `analyze-sentiment` | 情感分析（支持自定义标签） | `aifn install github:aifunc-dev/aifunc-packages/analyze-sentiment` |
| `classify` | 零样本文本分类 | `aifn install github:aifunc-dev/aifunc-packages/classify` |
| `detect-language` | 语言检测 | `aifn install github:aifunc-dev/aifunc-packages/detect-language` |
| `extract-entities` | 命名实体识别（NER） | `aifn install github:aifunc-dev/aifunc-packages/extract-entities` |
| `extract-keywords` | 关键词提取 | `aifn install github:aifunc-dev/aifunc-packages/extract-keywords` |
| `extract-json` | 从文本中提取结构化 JSON | `aifn install github:aifunc-dev/aifunc-packages/extract-json` |
| `score-quality` | 文本质量评分与改进建议 | `aifn install github:aifunc-dev/aifunc-packages/score-quality` |

### 文本转换

| 包名 | 用途 | 安装命令 |
|:---|:---|:---|
| `summarize` | 文本摘要 | `aifn install github:aifunc-dev/aifunc-packages/summarize` |
| `translate` | 多语言翻译 | `aifn install github:aifunc-dev/aifunc-packages/translate` |
| `rewrite` | 风格改写 | `aifn install github:aifunc-dev/aifunc-packages/rewrite` |
| `generate-slug` | 生成 SEO slug、meta 描述和标签 | `aifn install github:aifunc-dev/aifunc-packages/generate-slug` |

### 内容生成

| 包名 | 用途 | 安装命令 |
|:---|:---|:---|
| `generate-reply` | 生成上下文回复 | `aifn install github:aifunc-dev/aifunc-packages/generate-reply` |
| `generate-post` | 生成社交媒体内容 | `aifn install github:aifunc-dev/aifunc-packages/generate-post` |
| `generate-email` | 根据意图生成邮件 | `aifn install github:aifunc-dev/aifunc-packages/generate-email` |
| `generate-title` | 生成标题候选 | `aifn install github:aifunc-dev/aifunc-packages/generate-title` |
| `answer-question` | 问答（基于上下文或通用知识） | `aifn install github:aifunc-dev/aifunc-packages/answer-question` |

### 语义理解

| 包名 | 用途 | 安装命令 |
|:---|:---|:---|
| `recognize-intent` | 用户意图识别 | `aifn install github:aifunc-dev/aifunc-packages/recognize-intent` |

## 使用

安装后，import 调用即可：

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

`mock: true` 用于离线开发和测试。准备连接真实模型时替换为实际配置：

```typescript
const config: AIFuncConfig = {
   baseURL: 'https://your-api-endpoint/v1',
   model: 'your-model-name',
   apiKey: 'your-api-key',
};
```

## 包结构

每个包包含：

```
summarize/
├── package.json        # 元数据（名称、版本、作者、分类）
├── api.json            # 输入输出 Schema（JSON Schema 格式）
├── model-params.json   # 推荐模型参数
├── mock.json           # 离线测试用 Mock 数据
├── prompts/
│   └── general.md      # Prompt 模板
├── README.md           # 包文档
└── LICENSE
```

## 完整 API 参考

每个包的详细输入输出 Schema 见 [PACKAGES.md](./PACKAGES.cn.md)。

## 贡献

1. Fork 本仓库
2. 按照上述结构创建包目录
3. 运行 `aifn validate ./your-package` 验证格式
4. 提交 Pull Request


> **说明：** 官方包仓库目前只接收常用、通用的包。领域过于狭窄或实验性质的包建议通过个人仓库分享。

## License

每个包独立授权，详见各包目录下的 `LICENSE` 文件。
