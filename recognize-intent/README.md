# Intent Recognition

`recognize-intent` is an AIFunc package that recognizes user intent from conversational text with confidence scores.

## Function Info

- Name: `recognize-intent`
- Type: `standalone`
- Purpose: Chatbot routing, dialogue management, command classification

## Input

```json
{
  "text": "I'd like to cancel my subscription and get a refund.",
  "intents": ["cancel_subscription", "upgrade_plan", "billing_inquiry", "technical_support"],
  "context": "Customer support chatbot for a SaaS product."
}
```

Fields:

- `text`: Required. The user message to recognize intent from.
- `intents`: Required. List of candidate intents to recognize from (at least 2).
- `context`: Optional. Conversation context or system description to improve recognition accuracy.

## Output

```json
{
  "intent": "cancel_subscription",
  "confidence": 0.91,
  "rankings": [
    { "intent": "cancel_subscription", "confidence": 0.91 },
    { "intent": "billing_inquiry", "confidence": 0.06 },
    { "intent": "upgrade_plan", "confidence": 0.02 },
    { "intent": "technical_support", "confidence": 0.01 }
  ]
}
```

Fields:

- `intent`: The highest-confidence recognized intent.
- `confidence`: Confidence score of the top intent (0–1).
- `rankings`: All intents ranked by confidence (highest first).
  - `intent`: Intent label.
  - `confidence`: Confidence score (0–1).

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
