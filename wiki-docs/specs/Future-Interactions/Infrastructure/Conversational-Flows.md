# Conversational Flows

Generalisation of the existing `PendingDuties` pattern to support multi-step user input flows (used by [Infest-and-Purge](../Infest-and-Purge.md) for custom parasite definition).

**Status:** Infrastructure spec — implement before custom-parasite-definition support in Infest. Hand-authored parasites only is an acceptable v1 fallback if this proves expensive.

## Background

`!work` already uses a `PendingDuties` collection to suspend an interaction while awaiting user response (single-choice branch). That mechanism is single-question; we need a multi-step flow with branches and a confirm-or-loop terminator.

## API

```csharp
public class ConversationalFlow
{
    public string Id;                          // GUID
    public string OwnerName;                   // userName driving the flow
    public string Channel;                     // where prompts/responses go
    public string FlowType;                    // "define_parasite", future flow types
    public Dictionary<string, string> State;   // accumulated answers, key = question id
    public string CurrentStep;                 // current question id; "confirm" is the terminal step
    public DateTime CreatedAt;
    public DateTime LastActivityAt;
}

public interface IConversationalFlowHandler
{
    string FlowType { get; }
    FlowStep GetNextStep(ConversationalFlow flow, string userInput);  // returns next prompt and updated flow, or null when complete
    void OnComplete(ConversationalFlow flow);                         // applies the side effect (e.g. saves the parasite definition)
}

public class FlowStep
{
    public string PromptText;     // shown to user
    public string NextStepId;     // null = waiting on confirmation
    public bool IsTerminal;       // true once OnComplete has run
}
```

A `ConversationalFlowRegistry` parallel to `InteractionProcessorRegistry` maps `FlowType → handler`.

A `ConversationalFlowDispatcher`:
- Receives every channel/PM message.
- If the sender is the `OwnerName` of an active flow in their channel, routes the message to the handler instead of normal command parsing.
- A user has at most **one active flow at a time** across all channels. Starting a new flow auto-cancels any existing one with a "previous flow cancelled" message.
- Inactivity timeout: 10 minutes (matches consent expiry).

## Flow lifecycle

1. A command (e.g. `!definep arasite`) calls `ConversationalFlowDispatcher.Start(ownerName, channel, flowType)`.
2. The handler's `GetNextStep(flow, null)` returns the first prompt.
3. Each user message routes through the handler until `IsTerminal == true`.
4. On terminal step, `OnComplete` runs the side effect, the flow is deleted from MongoDB.

## Cancellation

Special inputs that always exit:
- `!cancel` — abort, no side effect.
- `!flowstatus` — show current accumulated state without advancing.

## Persistence

MongoDB collection `ConversationalFlows`. Indexed on `OwnerName` (unique). Records auto-purge after 30 minutes of inactivity (TTL index on `LastActivityAt`).

## Tests

- `ConversationalFlowDispatcherTests.cs`:
  - Starting a new flow cancels existing one.
  - Routing: messages from owner go to handler, others go through normal command parsing.
  - `!cancel` exits cleanly.
  - Timeout removes the record.
- `DefineParasiteFlowHandlerTests.cs` (in the Infest spec):
  - Walks through all questions, confirms, parasite is saved.
  - User asks to change an answer at confirm step, jumps back to that question.

## Assumptions

- One flow per user globally. **Override:** add `(channel, ownerName)` composite key if cross-channel concurrency is desired.
- Handlers are fully stateful (state in `Dictionary<string, string>`); no rich types per answer. Simplifies persistence; complex fields can be JSON-encoded by the handler.
- "Change an answer" at the confirm step is implemented by the handler returning a `NextStepId` pointing back to that question.

## Files to create/modify

- `FChatDicebot/Conversational/ConversationalFlow.cs` *(new)*
- `FChatDicebot/Conversational/IConversationalFlowHandler.cs` *(new)*
- `FChatDicebot/Conversational/ConversationalFlowRegistry.cs` *(new)*
- `FChatDicebot/Conversational/ConversationalFlowDispatcher.cs` *(new)*
- `FChatDicebot/BotCommandController.cs` *(modify — route to dispatcher before normal parsing if flow active)*
- `FChatDicebot.Tests/Unit/ConversationalFlowDispatcherTests.cs` *(new)*
