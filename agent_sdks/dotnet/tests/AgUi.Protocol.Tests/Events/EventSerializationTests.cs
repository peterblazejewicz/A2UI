using System.Text.Json;
using AgUi.Protocol.Events;
using FluentAssertions;

namespace AgUi.Protocol.Tests.Events;

public sealed class EventSerializationTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RunStartedEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"RUN_STARTED","threadId":"t1","runId":"r1","parentRunId":"p1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<RunStartedEvent>();
        var run = (RunStartedEvent)evt!;
        run.ThreadId.Should().Be("t1");
        run.RunId.Should().Be("r1");
        run.ParentRunId.Should().Be("p1");
    }

    [Fact]
    public void RunFinishedEvent_RoundTrip_PreservesResult()
    {
        var json = """{"type":"RUN_FINISHED","threadId":"t1","runId":"r1","result":{"status":"ok"}}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<RunFinishedEvent>();
        var fin = (RunFinishedEvent)evt!;
        fin.ThreadId.Should().Be("t1");
        fin.Result.Should().NotBeNull();
    }

    [Fact]
    public void RunErrorEvent_RoundTrip_PreservesMessageAndCode()
    {
        var json = """{"type":"RUN_ERROR","message":"something failed","code":"E001"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<RunErrorEvent>();
        var err = (RunErrorEvent)evt!;
        err.Message.Should().Be("something failed");
        err.Code.Should().Be("E001");
    }

    [Fact]
    public void StepStartedEvent_RoundTrip_PreservesStepName()
    {
        var json = """{"type":"STEP_STARTED","stepName":"planning"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<StepStartedEvent>();
        ((StepStartedEvent)evt!).StepName.Should().Be("planning");
    }

    [Fact]
    public void TextMessageStartEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"TEXT_MESSAGE_START","messageId":"m1","role":"assistant"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<TextMessageStartEvent>();
        var msg = (TextMessageStartEvent)evt!;
        msg.MessageId.Should().Be("m1");
        msg.Role.Should().Be("assistant");
    }

    [Fact]
    public void TextMessageContentEvent_RoundTrip_PreservesDelta()
    {
        var json = """{"type":"TEXT_MESSAGE_CONTENT","messageId":"m1","delta":"Hello "}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<TextMessageContentEvent>();
        ((TextMessageContentEvent)evt!).Delta.Should().Be("Hello ");
    }

    [Fact]
    public void ToolCallStartEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"TOOL_CALL_START","toolCallId":"tc1","toolCallName":"render_ui","parentMessageId":"m1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ToolCallStartEvent>();
        var tc = (ToolCallStartEvent)evt!;
        tc.ToolCallId.Should().Be("tc1");
        tc.ToolCallName.Should().Be("render_ui");
        tc.ParentMessageId.Should().Be("m1");
    }

    [Fact]
    public void ToolCallResultEvent_RoundTrip_SupportsStructuredContent()
    {
        var json = """{"type":"TOOL_CALL_RESULT","messageId":"m1","toolCallId":"tc1","content":{"output":"done"},"role":"tool"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ToolCallResultEvent>();
        var res = (ToolCallResultEvent)evt!;
        res.ToolCallId.Should().Be("tc1");
        res.Content.ValueKind.Should().Be(JsonValueKind.Object);
        res.Role.Should().Be("tool");
    }

    [Fact]
    public void ToolCallArgsAccumulator_ConcatenatesDeltas()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "{\"ci" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "ty\":\"San" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = " Francisco\"}" });

        string result = acc.Complete("tc1");

        result.Should().Be("""{"city":"San Francisco"}""");
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

        evt.Should().BeOfType<StateDeltaEvent>();
        var delta = (StateDeltaEvent)evt!;
        delta.Delta.Should().HaveCount(2);
    }

    [Fact]
    public void ActivitySnapshotEvent_RoundTrip_PreservesAllFields()
    {
        var json = """{"type":"ACTIVITY_SNAPSHOT","messageId":"m1","activityType":"thinking","activity":{"step":"plan"},"replace":true}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ActivitySnapshotEvent>();
        var snap = (ActivitySnapshotEvent)evt!;
        snap.MessageId.Should().Be("m1");
        snap.ActivityType.Should().Be("thinking");
        snap.Replace.Should().BeTrue();
    }

    [Fact]
    public void ActivityDeltaEvent_RoundTrip_PreservesFields()
    {
        var json = """{"type":"ACTIVITY_DELTA","messageId":"m1","activityType":"thinking","patch":[{"op":"replace","path":"/step","value":"execute"}]}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ActivityDeltaEvent>();
        var delta = (ActivityDeltaEvent)evt!;
        delta.MessageId.Should().Be("m1");
        delta.ActivityType.Should().Be("thinking");
        delta.Patch.Should().HaveCount(1);
    }

    [Fact]
    public void ReasoningStartEvent_RoundTrip_PreservesMessageId()
    {
        var json = """{"type":"REASONING_START","messageId":"m1"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ReasoningStartEvent>();
        ((ReasoningStartEvent)evt!).MessageId.Should().Be("m1");
    }

    [Fact]
    public void ReasoningMessageStartEvent_RoundTrip_PreservesRole()
    {
        var json = """{"type":"REASONING_MESSAGE_START","messageId":"m1","role":"reasoning"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ReasoningMessageStartEvent>();
        var msg = (ReasoningMessageStartEvent)evt!;
        msg.MessageId.Should().Be("m1");
        msg.Role.Should().Be("reasoning");
    }

    [Fact]
    public void ReasoningMessageChunkEvent_Deserializes()
    {
        var json = """{"type":"REASONING_MESSAGE_CHUNK","messageId":"m1","delta":"thinking..."}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ReasoningMessageChunkEvent>();
        var chunk = (ReasoningMessageChunkEvent)evt!;
        chunk.MessageId.Should().Be("m1");
        chunk.Delta.Should().Be("thinking...");
    }

    [Fact]
    public void ReasoningEncryptedValueEvent_RoundTrip_CorrectEnumCasing()
    {
        var json = """{"type":"REASONING_ENCRYPTED_VALUE","subtype":"tool-call","entityId":"e1","encryptedValue":"abc123"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        evt.Should().BeOfType<ReasoningEncryptedValueEvent>();
        var enc = (ReasoningEncryptedValueEvent)evt!;
        enc.Subtype.Should().Be(ReasoningSubtype.ToolCall);
        enc.EntityId.Should().Be("e1");
        enc.EncryptedValue.Should().Be("abc123");
    }

    [Fact]
    public void ReasoningSubtype_Message_SerializesAsLowercase()
    {
        var json = """{"type":"REASONING_ENCRYPTED_VALUE","subtype":"message","entityId":"e2","encryptedValue":"xyz"}""";

        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        ((ReasoningEncryptedValueEvent)evt!).Subtype.Should().Be(ReasoningSubtype.Message);
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
            "TEXT_MESSAGE_CONTENT" or "REASONING_MESSAGE_CONTENT"
                or "TEXT_MESSAGE_CHUNK" or "REASONING_MESSAGE_CHUNK"
                or "TOOL_CALL_ARGS" or "TOOL_CALL_CHUNK" => "\"test delta\"",
            "STATE_DELTA" or "ACTIVITY_DELTA" => "[{\"op\":\"replace\",\"path\":\"/test\",\"value\":42}]",
            _ => "[]"
        };

        string extraFields = eventType switch
        {
            "REASONING_ENCRYPTED_VALUE" => ",\"subtype\":\"message\",\"entityId\":\"e1\",\"encryptedValue\":\"enc\"",
            "TOOL_CALL_RESULT" => ",\"content\":\"result\"",
            "TOOL_CALL_START" => ",\"toolCallName\":\"render_ui\"",
            "MESSAGES_SNAPSHOT" => ",\"messages\":[]",
            _ => ""
        };

        var json = $$"""{"type":"{{eventType}}","message":"test","threadId":"t","runId":"r","messageId":"m","toolCallId":"tc","snapshot":{},"name":"n","value":"v","delta":{{deltaValue}},"stepName":"s","activity":{},"patch":{{deltaValue}},"event":{},"source":"src"{{extraFields}}}""";

        var act = () => JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        act.Should().NotThrow();
    }
}
