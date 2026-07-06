# Email Generation

`generate-email` is an AIFunc package that generates a complete email from a brief description of intent and context.

## Function Info

- Name: `generate-email`
- Type: `standalone`
- Purpose: Email drafting, automated correspondence, business communication

## Input

```json
{
  "intent": "follow up on a job application",
  "tone": "formal",
  "senderName": "Wei Zhang",
  "recipientName": "HR Team",
  "keyPoints": ["applied two weeks ago", "very interested in the position"],
  "language": "English"
}
```

Fields:

- `intent`: Required. What the email should accomplish (e.g. `"request a refund"`, `"schedule a meeting"`).
- `tone`: Optional. Desired tone: `"formal"`, `"friendly"`, `"assertive"`. Default: `"formal"`.
- `senderName`: Optional. Name of the sender, used in the sign-off.
- `recipientName`: Optional. Name or role of the recipient, used in the greeting.
- `keyPoints`: Optional. Specific points or details to include in the email body.
- `language`: Optional. Email language. Default: `"English"`.

## Output

```json
{
  "subject": "Following Up on My Application",
  "body": "Dear HR Team,\n\nI hope this message finds you well. I am writing to follow up on my application submitted two weeks ago. I remain very interested in the position and would welcome the opportunity to discuss my qualifications further.\n\nThank you for your time and consideration.\n\nBest regards,\nWei Zhang"
}
```

Fields:

- `subject`: Suggested email subject line.
- `body`: Full email body including greeting and sign-off.

## Files

- `package.json`: Package metadata.
- `api.json`: Function contract and input/output schema.
- `model-params.json`: Recommended model parameters.
- `mock.json`: Mock output examples.
- `prompts/general.md`: General prompt template.
