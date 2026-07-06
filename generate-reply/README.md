# Contextual Reply Generation

`generate-reply` is an AIFunc package that generates a contextually appropriate reply to a message or comment.

## Function Info

- Name: `generate-reply`
- Type: `standalone`
- Purpose: Customer support, chat assistants, comment replies, conversational AI

## Input

```json
{
  "message": "Hi, I've been waiting for my order for two weeks. Can you help?",
  "tone": "empathetic",
  "context": "You are a customer support agent for an online store.",
  "language": "English"
}
```

Fields:

- `message`: Required. The original message or comment to reply to.
- `tone`: Optional. Desired tone: `"friendly"`, `"formal"`, `"empathetic"`, `"concise"`. Default: `"friendly"`.
- `context`: Optional. Background context to inform the reply (e.g. role, situation).
- `language`: Optional. Reply language. If omitted, matches the input message language.

## Output

```json
{
  "reply": "I'm sorry to hear about the delay with your order. I understand how frustrating that must be. Let me look into this for you right away and get back to you with an update."
}
```

Fields:

- `reply`: The generated reply text.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
