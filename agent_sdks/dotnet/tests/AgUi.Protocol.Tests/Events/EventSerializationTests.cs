using System.Text.Json;
using AgUi.Protocol.Events;

namespace AgUi.Protocol.Tests.Events;

public sealed class EventSerializationTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RunStartedEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"RUN_STARTED","threadId":"t1","runId":"r1","parentRunId":"p1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var run = Assert.IsType<RunStartedEvent>(evt);
        Assert.Equal("t1", run.ThreadId);
        Assert.Equal("r1", run.RunId);
        Assert.Equal("p1", run.ParentRunId);
    }

    [Fact]
    public void RunFinishedEvent_RoundTrip_PreservesResult()
    {
        var json = """{"type":"RUN_FINISHED","threadId":"t1","runId":"r1","result":{"status":"ok"}}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var fin = Assert.IsType<RunFinishedEvent>(evt);
        Assert.Equal("t1", fin.ThreadId);
        Assert.NotNull(fin.Result);
    }

    [Fact]
    public void RunErrorEvent_RoundTrip_PreservesMessageAndCode()
    {
        var json = """{"type":"RUN_ERROR","message":"something failed","code":"E001"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var err = Assert.IsType<RunErrorEvent>(evt);
        Assert.Equal("something failed", err.Message);
        Assert.Equal("E001", err.Code);
    }

    [Fact]
    public void StepStartedEvent_RoundTrip_PreservesStepName()
    {
        var json = """{"type":"STEP_STARTED","stepName":"planning"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var step = Assert.IsType<StepStartedEvent>(evt);
        Assert.Equal("planning", step.StepName);
    }

    [Fact]
    public void TextMessageStartEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"TEXT_MESSAGE_START","messageId":"m1","role":"assistant"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var msg = Assert.IsType<TextMessageStartEvent>(evt);
        Assert.Equal("m1", msg.MessageId);
        Assert.Equal("assistant", msg.Role);
    }

    [Fact]
    public void TextMessageContentEvent_RoundTrip_PreservesDelta()
    {
        var json = """{"type":"TEXT_MESSAGE_CONTENT","messageId":"m1","delta":"Hello "}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var content = Assert.IsType<TextMessageContentEvent>(evt);
        Assert.Equal("Hello ", content.Delta);
    }

    [Fact]
    public void ToolCallStartEvent_RoundTrip_PreservesFields()
    {
        var json =
            """{"type":"TOOL_CALL_START","toolCallId":"tc1","toolCallName":"render_ui","parentMessageId":"m1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var tc = Assert.IsType<ToolCallStartEvent>(evt);
        Assert.Equal("tc1", tc.ToolCallId);
        Assert.Equal("render_ui", tc.ToolCallName);
        Assert.Equal("m1", tc.ParentMessageId);
    }

    [Fact]
    public void ToolCallResultEvent_RoundTrip_SupportsStructuredContent()
    {
        var json =
            """{"type":"TOOL_CALL_RESULT","messageId":"m1","toolCallId":"tc1","content":{"output":"done"},"role":"tool"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var res = Assert.IsType<ToolCallResultEvent>(evt);
        Assert.Equal("tc1", res.ToolCallId);
        Assert.Equal(JsonValueKind.Object, res.Content.ValueKind);
        Assert.Equal("tool", res.Role);
    }

    [Fact]
    public void ToolCallArgsAccumulator_ConcatenatesDeltas()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "{\"ci" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "ty\":\"San" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = " Francisco\"}" });

        string result = acc.Complete("tc1");

        Assert.Equal("""{"city":"San Francisco"}""", result);
    }

    [Fact]
    public void StateDeltaEvent_ContainsRfc6902Patches()
    {
        var json = """
            {
              "type":"STATE_DELTA",
              "delta":[
                {"op":"replace","path":"/user/status","value":"active"},
                {"op":"add","path":"/items/-","value":{"id":42}}
              ]
            }
            """;

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var delta = Assert.IsType<StateDeltaEvent>(evt);
        Assert.Equal(2, delta.Delta.Length);
    }

    [Fact]
    public void ActivitySnapshotEvent_RoundTrip_PreservesAllFields()
    {
        var json =
            """{"type":"ACTIVITY_SNAPSHOT","messageId":"m1","activityType":"thinking","activity":{"step":"plan"},"replace":true}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var snap = Assert.IsType<ActivitySnapshotEvent>(evt);
        Assert.Equal("m1", snap.MessageId);
        Assert.Equal("thinking", snap.ActivityType);
        Assert.True(snap.Replace);
    }

    [Fact]
    public void ActivityDeltaEvent_RoundTrip_PreservesFields()
    {
        var json =
            """{"type":"ACTIVITY_DELTA","messageId":"m1","activityType":"thinking","patch":[{"op":"replace","path":"/step","value":"execute"}]}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var delta = Assert.IsType<ActivityDeltaEvent>(evt);
        Assert.Equal("m1", delta.MessageId);
        Assert.Equal("thinking", delta.ActivityType);
        Assert.Single(delta.Patch);
    }

    [Fact]
    public void ReasoningStartEvent_RoundTrip_PreservesMessageId()
    {
        var json = """{"type":"REASONING_START","messageId":"m1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var reasoning = Assert.IsType<ReasoningStartEvent>(evt);
        Assert.Equal("m1", reasoning.MessageId);
    }

    [Fact]
    public void ReasoningMessageStartEvent_RoundTrip_PreservesRole()
    {
        var json = """{"type":"REASONING_MESSAGE_START","messageId":"m1","role":"reasoning"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var msg = Assert.IsType<ReasoningMessageStartEvent>(evt);
        Assert.Equal("m1", msg.MessageId);
        Assert.Equal("reasoning", msg.Role);
    }

    [Fact]
    public void ReasoningMessageChunkEvent_Deserializes()
    {
        var json = """{"type":"REASONING_MESSAGE_CHUNK","messageId":"m1","delta":"thinking..."}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var chunk = Assert.IsType<ReasoningMessageChunkEvent>(evt);
        Assert.Equal("m1", chunk.MessageId);
        Assert.Equal("thinking...", chunk.Delta);
    }

    [Fact]
    public void ReasoningEncryptedValueEvent_RoundTrip_CorrectEnumCasing()
    {
        var json =
            """{"type":"REASONING_ENCRYPTED_VALUE","subtype":"tool-call","entityId":"e1","encryptedValue":"abc123"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var enc = Assert.IsType<ReasoningEncryptedValueEvent>(evt);
        Assert.Equal(ReasoningSubtype.ToolCall, enc.Subtype);
        Assert.Equal("e1", enc.EntityId);
        Assert.Equal("abc123", enc.EncryptedValue);
    }

    [Fact]
    public void ReasoningSubtype_Message_SerializesAsLowercase()
    {
        var json =
            """{"type":"REASONING_ENCRYPTED_VALUE","subtype":"message","entityId":"e2","encryptedValue":"xyz"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        var enc = Assert.IsType<ReasoningEncryptedValueEvent>(evt);
        Assert.Equal(ReasoningSubtype.Message, enc.Subtype);
    }

    [Theory]
    [InlineData("RUN_STARTED")]
    [InlineData("RUN_FINISHED")]
    [InlineData("RUN_ERROR")]
    [InlineData("STEP_STARTED")]
    [InlineData("STEP_FINISHED")]
    [InlineData("TEXT_MESSAGE_START")]
    [InlineData("TEXT_MESSAGE_CONTENT")]
    [InlineData("TEXT_MESSAGE_END")]
    [InlineData("TEXT_MESSAGE_CHUNK")]
    [InlineData("TOOL_CALL_START")]
    [InlineData("TOOL_CALL_ARGS")]
    [InlineData("TOOL_CALL_END")]
    [InlineData("TOOL_CALL_RESULT")]
    [InlineData("TOOL_CALL_CHUNK")]
    [InlineData("STATE_SNAPSHOT")]
    [InlineData("STATE_DELTA")]
    [InlineData("MESSAGES_SNAPSHOT")]
    [InlineData("ACTIVITY_SNAPSHOT")]
    [InlineData("ACTIVITY_DELTA")]
    [InlineData("REASONING_START")]
    [InlineData("REASONING_MESSAGE_START")]
    [InlineData("REASONING_MESSAGE_CONTENT")]
    [InlineData("REASONING_MESSAGE_END")]
    [InlineData("REASONING_MESSAGE_CHUNK")]
    [InlineData("REASONING_END")]
    [InlineData("REASONING_ENCRYPTED_VALUE")]
    [InlineData("RAW")]
    [InlineData("CUSTOM")]
    public void AllEventTypes_DeserializeWithoutException(string eventType)
    {
        string deltaValue = eventType switch
        {
            "TEXT_MESSAGE_CONTENT"
            or "REASONING_MESSAGE_CONTENT"
            or "TEXT_MESSAGE_CHUNK"
            or "REASONING_MESSAGE_CHUNK"
            or "TOOL_CALL_ARGS"
            or "TOOL_CALL_CHUNK" => "\"test delta\"",
            "STATE_DELTA" or "ACTIVITY_DELTA" => "[{\"op\":\"replace\",\"path\":\"/test\",\"value\":42}]",
            _ => "[]",
        };

        string extraFields = eventType switch
        {
            "REASONING_ENCRYPTED_VALUE" => ",\"subtype\":\"message\",\"entityId\":\"e1\",\"encryptedValue\":\"enc\"",
            "TOOL_CALL_RESULT" => ",\"content\":\"result\"",
            "TOOL_CALL_START" => ",\"toolCallName\":\"render_ui\"",
            "MESSAGES_SNAPSHOT" => ",\"messages\":[]",
            _ => "",
        };

        var json =
            $$"""{"type":"{{eventType}}","message":"test","threadId":"t","runId":"r","messageId":"m","toolCallId":"tc","snapshot":{},"name":"n","value":"v","delta":{{deltaValue}},"stepName":"s","activity":{},"patch":{{deltaValue}},"event":{},"source":"src"{{extraFields}}}""";

        // xUnit will report failure if deserialization throws
        JsonSerializer.Deserialize<BaseEvent>(json, s_opts);
    }
}
