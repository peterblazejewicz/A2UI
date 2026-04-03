using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// Base for all AG-UI events. Every event carries <c>type</c>, optional
/// <c>timestamp</c> (Unix ms), and optional <c>rawEvent</c> passthrough.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RunStartedEvent),          "RUN_STARTED")]
[JsonDerivedType(typeof(RunFinishedEvent),          "RUN_FINISHED")]
[JsonDerivedType(typeof(RunErrorEvent),             "RUN_ERROR")]
[JsonDerivedType(typeof(StepStartedEvent),          "STEP_STARTED")]
[JsonDerivedType(typeof(StepFinishedEvent),         "STEP_FINISHED")]
[JsonDerivedType(typeof(TextMessageStartEvent),     "TEXT_MESSAGE_START")]
[JsonDerivedType(typeof(TextMessageContentEvent),   "TEXT_MESSAGE_CONTENT")]
[JsonDerivedType(typeof(TextMessageEndEvent),       "TEXT_MESSAGE_END")]
[JsonDerivedType(typeof(TextMessageChunkEvent),     "TEXT_MESSAGE_CHUNK")]
[JsonDerivedType(typeof(ToolCallStartEvent),        "TOOL_CALL_START")]
[JsonDerivedType(typeof(ToolCallArgsEvent),         "TOOL_CALL_ARGS")]
[JsonDerivedType(typeof(ToolCallEndEvent),          "TOOL_CALL_END")]
[JsonDerivedType(typeof(ToolCallResultEvent),       "TOOL_CALL_RESULT")]
[JsonDerivedType(typeof(ToolCallChunkEvent),        "TOOL_CALL_CHUNK")]
[JsonDerivedType(typeof(StateSnapshotEvent),        "STATE_SNAPSHOT")]
[JsonDerivedType(typeof(StateDeltaEvent),           "STATE_DELTA")]
[JsonDerivedType(typeof(MessagesSnapshotEvent),     "MESSAGES_SNAPSHOT")]
[JsonDerivedType(typeof(ActivitySnapshotEvent),     "ACTIVITY_SNAPSHOT")]
[JsonDerivedType(typeof(ActivityDeltaEvent),        "ACTIVITY_DELTA")]
[JsonDerivedType(typeof(ReasoningStartEvent),       "REASONING_START")]
[JsonDerivedType(typeof(ReasoningMessageStartEvent),   "REASONING_MESSAGE_START")]
[JsonDerivedType(typeof(ReasoningMessageContentEvent), "REASONING_MESSAGE_CONTENT")]
[JsonDerivedType(typeof(ReasoningMessageEndEvent),     "REASONING_MESSAGE_END")]
[JsonDerivedType(typeof(ReasoningEndEvent),         "REASONING_END")]
[JsonDerivedType(typeof(ReasoningEncryptedValueEvent), "REASONING_ENCRYPTED_VALUE")]
[JsonDerivedType(typeof(RawEvent),                  "RAW")]
[JsonDerivedType(typeof(CustomEvent),               "CUSTOM")]
public abstract record BaseEvent
{
    [JsonPropertyName("timestamp")]
    public long? TimestampMs { get; init; }

    [JsonPropertyName("rawEvent")]
    public JsonElement? RawEvent { get; init; }
}