namespace AgUi.Protocol.Events;

/// <summary>
/// All 28 AG-UI event type discriminators.
/// Values must match the wire-format "type" string exactly.
/// </summary>
public enum EventType
{
    // Lifecycle (stable)

    /// <summary>Mandatory first event; establishes execution context.</summary>
    RunStarted,

    /// <summary>Mandatory terminal event; signals successful completion.</summary>
    RunFinished,

    /// <summary>Unrecoverable error; terminates the run.</summary>
    RunError,

    /// <summary>Optional sub-run progress start marker.</summary>
    StepStarted,

    /// <summary>Matches a corresponding <see cref="StepStarted"/> marker.</summary>
    StepFinished,

    // Text Message (stable)

    /// <summary>Opens a new text message stream.</summary>
    TextMessageStart,

    /// <summary>Carries a non-empty text chunk for an open message.</summary>
    TextMessageContent,

    /// <summary>Closes a text message stream.</summary>
    TextMessageEnd,

    /// <summary>Convenience event that auto-expands to Start, Content, End.</summary>
    TextMessageChunk,

    // Tool Call (stable)

    /// <summary>Opens a new tool call invocation.</summary>
    ToolCallStart,

    /// <summary>Carries a raw JSON string fragment for tool call arguments.</summary>
    ToolCallArgs,

    /// <summary>Closes a tool call invocation.</summary>
    ToolCallEnd,

    /// <summary>Returns the result of a completed tool call.</summary>
    ToolCallResult,

    /// <summary>Convenience event that auto-expands to Start, Args, End.</summary>
    ToolCallChunk,

    // State (stable)

    /// <summary>Replaces the entire frontend local state with a snapshot.</summary>
    StateSnapshot,

    /// <summary>RFC 6902 JSON Patch operations applied to current state.</summary>
    StateDelta,

    /// <summary>Full snapshot of the conversation message history.</summary>
    MessagesSnapshot,

    /// <summary>Full snapshot of activity state for a message.</summary>
    ActivitySnapshot,

    /// <summary>RFC 6902 JSON Patch operations applied to activity state.</summary>
    ActivityDelta,

    // Reasoning (stable, replaced deprecated THINKING_*)

    /// <summary>Opens a reasoning block.</summary>
    ReasoningStart,

    /// <summary>Opens a reasoning message stream.</summary>
    ReasoningMessageStart,

    /// <summary>Carries a non-empty reasoning text chunk.</summary>
    ReasoningMessageContent,

    /// <summary>Closes a reasoning message stream.</summary>
    ReasoningMessageEnd,

    /// <summary>Convenience reasoning event that auto-expands to Start, Content, End.</summary>
    ReasoningMessageChunk,

    /// <summary>Closes a reasoning block.</summary>
    ReasoningEnd,

    /// <summary>Carries encrypted reasoning value across turns.</summary>
    ReasoningEncryptedValue,

    // Pass-through / Extension (stable)

    /// <summary>Passthrough event from external systems.</summary>
    Raw,

    /// <summary>Application-defined extension event.</summary>
    Custom,
}
