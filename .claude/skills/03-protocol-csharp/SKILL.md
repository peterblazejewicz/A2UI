---
name: a2ui_protocol_csharp
description: Implement AG-UI 26-event C# model (AgUi.Protocol) and A2UI message types (A2Ui.Core): records, JSON polymorphism, SSE parser, SurfaceManager.
---


---

## Purpose

Implement the complete AG-UI event model (26 event types) and A2UI message model
in C# for .NET 10. These are pure library projects with no UI dependencies.
Licenses: AG-UI (MIT), A2UI (Apache 2.0).

---

## Part A: `AgUi.Protocol` — AG-UI Event Types

### Step A1 — Project NuGet references

```bash
cd /sandbox/develop/A2Ui/src/AgUi.Protocol
```

Edit `AgUi.Protocol.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>AgUi.Protocol</AssemblyName>
    <RootNamespace>AgUi.Protocol</RootNamespace>
    <PackageId>AgUi.Protocol</PackageId>
    <Description>AG-UI event types and transport for .NET</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Json" />
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Microsoft.Json.Patch" />
  </ItemGroup>
</Project>
```

### Step A2 — EventType discriminated union

Create `src/AgUi.Protocol/Events/EventType.cs`:

```csharp
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
```

### Step A3 — Base event and JSON converter

Create `src/AgUi.Protocol/Events/BaseEvent.cs`:

```csharp
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
```

### Step A4 — Lifecycle events

Create `src/AgUi.Protocol/Events/LifecycleEvents.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RUN_STARTED — mandatory first event. Establishes execution context.</summary>
public sealed record RunStartedEvent : BaseEvent
{
    [JsonPropertyName("threadId")]   public required string ThreadId  { get; init; }
    [JsonPropertyName("runId")]      public required string RunId     { get; init; }
    [JsonPropertyName("parentRunId")] public string? ParentRunId      { get; init; }
    [JsonPropertyName("input")]      public JsonElement? Input        { get; init; }
}

/// <summary>RUN_FINISHED — mandatory terminal event.</summary>
public sealed record RunFinishedEvent : BaseEvent
{
    [JsonPropertyName("threadId")]  public required string ThreadId { get; init; }
    [JsonPropertyName("runId")]     public required string RunId    { get; init; }
    [JsonPropertyName("result")]    public JsonElement? Result      { get; init; }
}

/// <summary>RUN_ERROR — unrecoverable error, terminates run.</summary>
public sealed record RunErrorEvent : BaseEvent
{
    [JsonPropertyName("message")]  public required string Message { get; init; }
    [JsonPropertyName("code")]     public string? Code             { get; init; }
}

/// <summary>STEP_STARTED — optional sub-run progress.</summary>
public sealed record StepStartedEvent : BaseEvent
{
    [JsonPropertyName("stepName")] public required string StepName { get; init; }
}

/// <summary>STEP_FINISHED — must match corresponding STEP_STARTED.</summary>
public sealed record StepFinishedEvent : BaseEvent
{
    [JsonPropertyName("stepName")] public required string StepName { get; init; }
}
```

### Step A5 — Text message events

Create `src/AgUi.Protocol/Events/TextMessageEvents.cs`:

```csharp
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record TextMessageStartEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
    [JsonPropertyName("role")]      public string Role { get; init; } = "assistant";
}

public sealed record TextMessageContentEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
    /// <summary>Non-empty text chunk. Concatenate all chunks for a messageId.</summary>
    [JsonPropertyName("delta")]     public required string Delta     { get; init; }
}

public sealed record TextMessageEndEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
}

/// <summary>
/// Convenience event: auto-expands to Start→Content→End.
/// messageId required on first chunk; role defaults to "assistant".
/// </summary>
public sealed record TextMessageChunkEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public string? MessageId { get; init; }
    [JsonPropertyName("role")]      public string? Role      { get; init; }
    [JsonPropertyName("delta")]     public string? Delta     { get; init; }
}
```

### Step A6 — Tool call events

Create `src/AgUi.Protocol/Events/ToolCallEvents.cs`:

```csharp
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record ToolCallStartEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")]      public required string ToolCallId   { get; init; }
    [JsonPropertyName("toolCallName")]    public required string ToolCallName { get; init; }
    [JsonPropertyName("parentMessageId")] public string? ParentMessageId      { get; init; }
}

/// <summary>
/// Carries raw JSON string fragments. Concatenate all deltas for a toolCallId
/// to obtain the complete JSON arguments object.
/// </summary>
public sealed record ToolCallArgsEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
    [JsonPropertyName("delta")]      public required string Delta      { get; init; }
}

public sealed record ToolCallEndEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
}

public sealed record ToolCallResultEvent : BaseEvent
{
    [JsonPropertyName("messageId")]  public required string MessageId  { get; init; }
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
    [JsonPropertyName("content")]    public required string Content    { get; init; }
    [JsonPropertyName("role")]       public string? Role               { get; init; }
}

/// <summary>Convenience: auto-expands to Start→Args→End.</summary>
public sealed record ToolCallChunkEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")]      public string? ToolCallId      { get; init; }
    [JsonPropertyName("toolCallName")]    public string? ToolCallName    { get; init; }
    [JsonPropertyName("parentMessageId")] public string? ParentMessageId { get; init; }
    [JsonPropertyName("delta")]           public string? Delta           { get; init; }
}
```

### Step A7 — State management events

Create `src/AgUi.Protocol/Events/StateEvents.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// STATE_SNAPSHOT — frontend replaces its entire local state with this snapshot.
/// </summary>
public sealed record StateSnapshotEvent : BaseEvent
{
    [JsonPropertyName("snapshot")] public required JsonElement Snapshot { get; init; }
}

/// <summary>
/// STATE_DELTA — RFC 6902 JSON Patch operations array.
/// Apply to current state in order.
/// Operations: add, remove, replace, move, copy, test.
/// </summary>
public sealed record StateDeltaEvent : BaseEvent
{
    [JsonPropertyName("delta")] public required JsonElement[] Delta { get; init; }
}

public sealed record MessagesSnapshotEvent : BaseEvent
{
    [JsonPropertyName("messages")] public required JsonElement[] Messages { get; init; }
}

public sealed record ActivitySnapshotEvent : BaseEvent
{
    [JsonPropertyName("activity")] public required JsonElement Activity { get; init; }
}

public sealed record ActivityDeltaEvent : BaseEvent
{
    [JsonPropertyName("patch")] public required JsonElement[] Patch { get; init; }
}
```

### Step A8 — Reasoning events

Create `src/AgUi.Protocol/Events/ReasoningEvents.cs`:

```csharp
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record ReasoningStartEvent       : BaseEvent { }
public sealed record ReasoningEndEvent         : BaseEvent { }

public sealed record ReasoningMessageStartEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
}

public sealed record ReasoningMessageContentEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
    [JsonPropertyName("delta")]     public required string Delta     { get; init; }
}

public sealed record ReasoningMessageEndEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
}

public sealed record ReasoningMessageChunkEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public string? MessageId { get; init; }
    [JsonPropertyName("delta")]     public string? Delta     { get; init; }
}

/// <summary>
/// Carries encrypted reasoning value across turns.
/// Inspired by OpenAI encrypted reasoning items.
/// </summary>
public sealed record ReasoningEncryptedValueEvent : BaseEvent
{
    [JsonPropertyName("subtype")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReasoningSubtype Subtype { get; init; }

    [JsonPropertyName("entityId")]       public required string EntityId       { get; init; }
    [JsonPropertyName("encryptedValue")] public required string EncryptedValue { get; init; }
}

public enum ReasoningSubtype { ToolCall, Message }
```

### Step A9 — Pass-through and custom events

Create `src/AgUi.Protocol/Events/ExtensionEvents.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RAW — passthrough from external systems.</summary>
public sealed record RawEvent : BaseEvent
{
    [JsonPropertyName("event")]  public required JsonElement Event  { get; init; }
    [JsonPropertyName("source")] public string? Source             { get; init; }
}

/// <summary>CUSTOM — application-defined extension event.</summary>
public sealed record CustomEvent : BaseEvent
{
    [JsonPropertyName("name")]  public required string      Name  { get; init; }
    [JsonPropertyName("value")] public required JsonElement Value { get; init; }
}
```

### Step A10 — RunAgentInput (request contract)

Create `src/AgUi.Protocol/RunAgentInput.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol;

/// <summary>
/// POST body for AG-UI HTTP/SSE endpoint.
/// Also used as the gRPC request message.
/// </summary>
public sealed record RunAgentInput
{
    [JsonPropertyName("threadId")]      public required string        ThreadId       { get; init; }
    [JsonPropertyName("runId")]         public required string        RunId          { get; init; }
    [JsonPropertyName("messages")]      public required JsonElement[] Messages       { get; init; }
    [JsonPropertyName("state")]         public JsonElement?           State          { get; init; }
    [JsonPropertyName("tools")]         public JsonElement[]?         Tools          { get; init; }
    [JsonPropertyName("context")]       public JsonElement[]?         Context        { get; init; }
    [JsonPropertyName("forwardedProps")] public JsonElement?          ForwardedProps { get; init; }
}
```

### Step A11 — SSE Stream Parser

Create `src/AgUi.Protocol/Transport/SseEventParser.cs`:

```csharp
using System.Text.Json;
using AgUi.Protocol.Events;

namespace AgUi.Protocol.Transport;

/// <summary>
/// Parses AG-UI Server-Sent Events stream.
/// AG-UI uses ONLY the data: field; event: and id: fields are not used.
/// Each data: line contains one complete JSON event object.
/// </summary>
public static class SseEventParser
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public static async IAsyncEnumerable<BaseEvent> ParseAsync(
        Stream sseStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(sseStream, leaveOpen: true);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;

            // SSE data line format: "data: {json}"
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            ReadOnlySpan<char> json = line.AsSpan(5).TrimStart();
            if (json.IsEmpty || json.SequenceEqual("[DONE]")) continue;

            BaseEvent? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<BaseEvent>(json, s_options);
            }
            catch (JsonException)
            {
                // Malformed event — skip, do not throw (resilient parser)
                continue;
            }

            if (evt is not null) yield return evt;
        }
    }
}
```

### Step A12 — ToolCallArgs accumulator

Create `src/AgUi.Protocol/ToolCallArgsAccumulator.cs`:

```csharp
using System.Text;
using AgUi.Protocol.Events;

namespace AgUi.Protocol;

/// <summary>
/// Accumulates TOOL_CALL_ARGS delta strings per toolCallId.
/// Call <see cref="Complete"/> after TOOL_CALL_END to get final JSON.
/// </summary>
public sealed class ToolCallArgsAccumulator
{
    private readonly Dictionary<string, StringBuilder> _buffers = new();

    public void OnArgs(ToolCallArgsEvent args)
    {
        if (!_buffers.TryGetValue(args.ToolCallId, out var sb))
        {
            sb = new StringBuilder();
            _buffers[args.ToolCallId] = sb;
        }
        sb.Append(args.Delta);
    }

    /// <summary>Returns the complete JSON string and removes from buffer.</summary>
    public string Complete(string toolCallId)
    {
        if (_buffers.Remove(toolCallId, out var sb))
            return sb.ToString();
        return string.Empty;
    }

    public bool HasPending(string toolCallId) => _buffers.ContainsKey(toolCallId);

    public void Clear() => _buffers.Clear();
}
```

---

## Part B: `A2Ui.Core` — A2UI Message Model

### Step B1 — Project setup

```bash
cd /sandbox/develop/A2Ui/src/A2Ui.Core
```

Edit `A2Ui.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>A2Ui.Core</AssemblyName>
    <RootNamespace>A2Ui.Core</RootNamespace>
    <Description>A2UI (Agent-to-UI) message model and catalog for .NET</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Json" />
    <PackageReference Include="Microsoft.Json.Patch" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../AgUi.Protocol/AgUi.Protocol.csproj" />
  </ItemGroup>
</Project>
```

### Step B2 — A2UI Message discriminated union

Create `src/A2Ui.Core/Messages/A2UiMessage.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.8/v0.9 message — one JSONL line on the wire.
/// Each message contains exactly one top-level operation key.
/// </summary>
public sealed record A2UiMessage
{
    [JsonPropertyName("version")]          public string?              Version          { get; init; }
    [JsonPropertyName("createSurface")]    public CreateSurface?       CreateSurface    { get; init; }
    [JsonPropertyName("deleteSurface")]    public DeleteSurface?       DeleteSurface    { get; init; }
    [JsonPropertyName("updateComponents")] public UpdateComponents?    UpdateComponents { get; init; }
    [JsonPropertyName("updateDataModel")]  public UpdateDataModel?     UpdateDataModel  { get; init; }
    [JsonPropertyName("userAction")]       public UserAction?          UserAction       { get; init; }
    [JsonPropertyName("dataModelUpdate")]  public DataModelUpdate?     DataModelUpdate  { get; init; }
}

public sealed record CreateSurface
{
    [JsonPropertyName("surfaceId")]  public required string SurfaceId  { get; init; }
    [JsonPropertyName("catalogId")]  public required string CatalogId  { get; init; }
}

public sealed record DeleteSurface
{
    [JsonPropertyName("surfaceId")]  public required string SurfaceId { get; init; }
}

public sealed record UpdateComponents
{
    [JsonPropertyName("surfaceId")]   public required string       SurfaceId  { get; init; }
    [JsonPropertyName("components")]  public required A2UiComponent[] Components { get; init; }
}

/// <summary>Updates the data model for BoundValue resolution.</summary>
public sealed record UpdateDataModel
{
    [JsonPropertyName("surfaceId")] public required string      SurfaceId { get; init; }
    [JsonPropertyName("path")]      public required string      Path      { get; init; }
    [JsonPropertyName("value")]     public required JsonElement Value     { get; init; }
}

/// <summary>v0.8 data model update with key-value map structure.</summary>
public sealed record DataModelUpdate
{
    [JsonPropertyName("surfaceId")] public required string             SurfaceId { get; init; }
    [JsonPropertyName("contents")]  public required DataModelContent[] Contents  { get; init; }
}

public sealed record DataModelContent
{
    [JsonPropertyName("key")]       public required string             Key      { get; init; }
    [JsonPropertyName("valueString")] public string?                  ValueString { get; init; }
    [JsonPropertyName("valueInt")]    public int?                     ValueInt    { get; init; }
    [JsonPropertyName("valueBool")]   public bool?                    ValueBool   { get; init; }
    [JsonPropertyName("valueMap")]    public DataModelContent[]?      ValueMap    { get; init; }
}

/// <summary>Client→agent: user interaction event.</summary>
public sealed record UserAction
{
    [JsonPropertyName("surfaceId")] public required string      SurfaceId { get; init; }
    [JsonPropertyName("event")]     public required ActionEvent Event     { get; init; }
}

public sealed record ActionEvent
{
    [JsonPropertyName("name")]    public required string Name    { get; init; }
    [JsonPropertyName("payload")] public JsonElement? Payload    { get; init; }
}
```

### Step B3 — A2UI Component model

Create `src/A2Ui.Core/Messages/A2UiComponent.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI component in the flat adjacency list.
/// The agent may only reference component types registered in the catalog.
/// </summary>
public sealed record A2UiComponent
{
    /// <summary>Unique ID within surface. String, agent-assigned.</summary>
    [JsonPropertyName("id")]        public required string   Id        { get; init; }

    /// <summary>Type key — must exist in the client catalog.</summary>
    [JsonPropertyName("component")] public required string   Component { get; init; }

    /// <summary>Parent component ID for tree structure. Null = root.</summary>
    [JsonPropertyName("parent")]    public string?           Parent    { get; init; }

    /// <summary>Single child component ID (for components like Button).</summary>
    [JsonPropertyName("child")]     public string?           Child     { get; init; }

    // ── Common display properties ──────────────────────────────────────────
    [JsonPropertyName("text")]      public BoundOrLiteral?  Text      { get; init; }
    [JsonPropertyName("label")]     public string?          Label     { get; init; }
    [JsonPropertyName("variant")]   public string?          Variant   { get; init; }
    [JsonPropertyName("value")]     public BoundOrLiteral?  Value     { get; init; }
    [JsonPropertyName("url")]       public string?          Url       { get; init; }
    [JsonPropertyName("action")]    public ComponentAction? Action    { get; init; }

    // ── DateTimeInput specific ─────────────────────────────────────────────
    [JsonPropertyName("enableDate")] public bool? EnableDate { get; init; }
    [JsonPropertyName("enableTime")] public bool? EnableTime { get; init; }

    // ── Table specific ────────────────────────────────────────────────────
    [JsonPropertyName("columns")]   public TableColumn[]?   Columns   { get; init; }
    [JsonPropertyName("rows")]      public BoundOrLiteral?  Rows      { get; init; }

    // ── Extension — additional arbitrary properties ────────────────────────
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

/// <summary>
/// Either a literal value (string/number/bool) or a BoundValue path.
/// On the wire: <c>{"path": "/reservation/date"}</c> for bound,
/// or a plain string <c>"Hello"</c> for literal.
/// </summary>
[JsonConverter(typeof(BoundOrLiteralConverter))]
public sealed record BoundOrLiteral
{
    public string?  Literal { get; init; }
    public string?  Path    { get; init; }   // JSON Pointer path

    public bool IsBound => Path is not null;

    public static BoundOrLiteral Literal_(string value) => new() { Literal = value };
    public static BoundOrLiteral Bound_(string path)    => new() { Path = path };
}

public sealed record ComponentAction
{
    [JsonPropertyName("event")] public ActionEvent? Event { get; init; }
}

public sealed record TableColumn
{
    [JsonPropertyName("header")] public required string Header  { get; init; }
    [JsonPropertyName("field")]  public required string Field   { get; init; }
}

// ── BoundOrLiteral JSON converter ──────────────────────────────────────────

internal sealed class BoundOrLiteralConverter : JsonConverter<BoundOrLiteral>
{
    public override BoundOrLiteral? Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return BoundOrLiteral.Literal_(reader.GetString()!);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("path", out var pathEl))
                return BoundOrLiteral.Bound_(pathEl.GetString()!);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, BoundOrLiteral value,
        JsonSerializerOptions options)
    {
        if (value.IsBound)
        {
            writer.WriteStartObject();
            writer.WriteString("path", value.Path);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(value.Literal);
        }
    }
}
```

### Step B4 — Data model store (BoundValue resolver)

Create `src/A2Ui.Core/DataModel.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Per-surface data model store.
/// Resolves BoundValue JSON Pointer paths against the stored state.
/// Applies RFC 6902-style patch operations from UpdateDataModel messages.
/// </summary>
public sealed class DataModel
{
    private JsonObject _root = new();

    /// <summary>Replace the entire data model.</summary>
    public void SetSnapshot(JsonElement snapshot)
    {
        _root = JsonObject.Create(snapshot) ?? new JsonObject();
    }

    /// <summary>Apply a single UpdateDataModel message (JSON Pointer set).</summary>
    public void Apply(UpdateDataModel update)
    {
        // Simple JSON Pointer set — "/reservation/date" → ["reservation"]["date"]
        var segments = update.Path.TrimStart('/').Split('/');
        JsonObject current = _root;

        for (int i = 0; i < segments.Length - 1; i++)
        {
            string seg = segments[i];
            if (!current.ContainsKey(seg) || current[seg] is not JsonObject child)
            {
                child = new JsonObject();
                current[seg] = child;
            }
            current = child;
        }

        current[segments[^1]] = JsonNode.Parse(update.Value.GetRawText());
    }

    /// <summary>Apply v0.8 DataModelUpdate (key-value map).</summary>
    public void Apply(DataModelUpdate update)
    {
        foreach (var content in update.Contents)
            ApplyContent(_root, content);
    }

    /// <summary>Resolve a BoundValue path. Returns null if not found.</summary>
    public string? Resolve(BoundOrLiteral? bound)
    {
        if (bound is null) return null;
        if (!bound.IsBound) return bound.Literal;

        var segments = bound.Path!.TrimStart('/').Split('/');
        JsonNode? node = _root;
        foreach (var seg in segments)
        {
            if (node is JsonObject obj && obj.ContainsKey(seg))
                node = obj[seg];
            else
                return null;
        }
        return node?.GetValue<string>();
    }

    private static void ApplyContent(JsonObject target, DataModelContent content)
    {
        if (content.ValueString is not null) { target[content.Key] = content.ValueString; return; }
        if (content.ValueInt    is not null) { target[content.Key] = content.ValueInt;    return; }
        if (content.ValueBool   is not null) { target[content.Key] = content.ValueBool;   return; }
        if (content.ValueMap    is not null)
        {
            var child = new JsonObject();
            foreach (var c in content.ValueMap) ApplyContent(child, c);
            target[content.Key] = child;
        }
    }
}
```

### Step B5 — Surface manager

Create `src/A2Ui.Core/SurfaceManager.cs`:

```csharp
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Manages the lifecycle of A2UI surfaces and their component trees.
/// Thread-safe via locking; call from dispatcher thread in Avalonia.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, Surface> _surfaces = new();
    private readonly object _lock = new();

    public event EventHandler<SurfaceCreatedEventArgs>?   SurfaceCreated;
    public event EventHandler<SurfaceDeletedEventArgs>?   SurfaceDeleted;
    public event EventHandler<ComponentsUpdatedEventArgs>? ComponentsUpdated;
    public event EventHandler<DataModelUpdatedEventArgs>?  DataModelUpdated;

    public void Process(A2UiMessage message)
    {
        lock (_lock)
        {
            if (message.CreateSurface is { } cs)    HandleCreate(cs);
            if (message.DeleteSurface is { } ds)    HandleDelete(ds);
            if (message.UpdateComponents is { } uc) HandleUpdateComponents(uc);
            if (message.UpdateDataModel  is { } ud) HandleUpdateDataModel(ud);
            if (message.DataModelUpdate  is { } dm) HandleDataModelUpdate(dm);
        }
    }

    public Surface? GetSurface(string surfaceId)
    {
        lock (_lock) { return _surfaces.GetValueOrDefault(surfaceId); }
    }

    private void HandleCreate(CreateSurface cs)
    {
        if (_surfaces.ContainsKey(cs.SurfaceId)) return;
        var surface = new Surface(cs.SurfaceId, cs.CatalogId);
        _surfaces[cs.SurfaceId] = surface;
        SurfaceCreated?.Invoke(this, new(surface));
    }

    private void HandleDelete(DeleteSurface ds)
    {
        if (_surfaces.Remove(ds.SurfaceId, out var surface))
            SurfaceDeleted?.Invoke(this, new(surface));
    }

    private void HandleUpdateComponents(UpdateComponents uc)
    {
        if (!_surfaces.TryGetValue(uc.SurfaceId, out var surface)) return;
        surface.UpdateComponents(uc.Components);
        ComponentsUpdated?.Invoke(this, new(surface, uc.Components));
    }

    private void HandleUpdateDataModel(UpdateDataModel ud)
    {
        if (!_surfaces.TryGetValue(ud.SurfaceId, out var surface)) return;
        surface.DataModel.Apply(ud);
        DataModelUpdated?.Invoke(this, new(surface));
    }

    private void HandleDataModelUpdate(DataModelUpdate dm)
    {
        if (!_surfaces.TryGetValue(dm.SurfaceId, out var surface)) return;
        surface.DataModel.Apply(dm);
        DataModelUpdated?.Invoke(this, new(surface));
    }
}

public sealed class Surface(string surfaceId, string catalogId)
{
    public string    SurfaceId { get; } = surfaceId;
    public string    CatalogId { get; } = catalogId;
    public DataModel DataModel { get; } = new();

    private readonly Dictionary<string, A2UiComponent> _components = new();

    public IReadOnlyDictionary<string, A2UiComponent> Components => _components;

    public void UpdateComponents(A2UiComponent[] components)
    {
        foreach (var c in components)
            _components[c.Id] = c;
    }

    public IEnumerable<A2UiComponent> GetRootComponents() =>
        _components.Values.Where(c => c.Parent is null);
}

public sealed record SurfaceCreatedEventArgs(Surface Surface);
public sealed record SurfaceDeletedEventArgs(Surface Surface);
public sealed record ComponentsUpdatedEventArgs(Surface Surface, A2UiComponent[] Updated);
public sealed record DataModelUpdatedEventArgs(Surface Surface);
```

---

## Build Verification

```bash
cd /sandbox/develop/A2Ui
dotnet build src/AgUi.Protocol/AgUi.Protocol.csproj
dotnet build src/A2Ui.Core/A2Ui.Core.csproj
# Expected: 0 errors, 0 warnings (TreatWarningsAsErrors=true)
```

Then commit:

```bash
git add src/AgUi.Protocol/ src/A2Ui.Core/
git commit -m "feat(protocol): implement AG-UI 26-event model and A2UI message types

AgUi.Protocol:
- All 26 event types as C# records with JSON polymorphism
- RunAgentInput request contract
- SseEventParser: resilient async SSE stream reader
- ToolCallArgsAccumulator: delta concatenation per toolCallId

A2Ui.Core:
- A2UiMessage discriminated union (v0.8 + v0.9)
- A2UiComponent with BoundOrLiteral converter
- DataModel: JSON Pointer BoundValue resolver
- SurfaceManager: surface lifecycle + event dispatch

Refs:
- AG-UI spec @ag-ui/core v0.0.47 (MIT)
- A2UI spec v0.8 stable (Apache 2.0)
- RFC 6902 JSON Patch"
```