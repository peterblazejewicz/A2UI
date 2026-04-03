namespace AgUi.Protocol.Events;

/// <summary>
/// All 26 AG-UI event type discriminators.
/// Values must match the wire-format "type" string exactly.
/// </summary>
public enum EventType
{
    // Lifecycle (stable)
    RunStarted,
    RunFinished,
    RunError,
    StepStarted,
    StepFinished,

    // Text Message (stable)
    TextMessageStart,
    TextMessageContent,
    TextMessageEnd,
    TextMessageChunk,       // convenience, auto-expands

    // Tool Call (stable)
    ToolCallStart,
    ToolCallArgs,
    ToolCallEnd,
    ToolCallResult,
    ToolCallChunk,          // convenience, auto-expands

    // State (stable)
    StateSnapshot,
    StateDelta,
    MessagesSnapshot,
    ActivitySnapshot,
    ActivityDelta,

    // Reasoning (stable, replaced deprecated THINKING_*)
    ReasoningStart,
    ReasoningMessageStart,
    ReasoningMessageContent,
    ReasoningMessageEnd,
    ReasoningMessageChunk,
    ReasoningEnd,
    ReasoningEncryptedValue,

    // Pass-through / Extension (stable)
    Raw,
    Custom,
}