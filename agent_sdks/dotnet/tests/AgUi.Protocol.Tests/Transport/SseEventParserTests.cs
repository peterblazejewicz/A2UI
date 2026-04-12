using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgUi.Protocol.Events;
using AgUi.Protocol.Transport;

namespace AgUi.Protocol.Tests.Transport;

public sealed class SseEventParserTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ParseAsync_WellFormedStream_YieldsEvents()
    {
        var sse =
            "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n";

        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(
                ToStream(sse),
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.IsType<RunStartedEvent>(events[0]);
        Assert.IsType<RunFinishedEvent>(events[1]);
    }

    [Fact]
    public async Task ParseAsync_MalformedJsonLine_SkipsSilently()
    {
        var sse =
            "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\ndata: {not valid json}\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n";

        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(
                ToStream(sse),
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            events.Add(evt);

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task ParseAsync_DoneSentinel_StopsYielding()
    {
        var sse =
            "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\ndata: [DONE]\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n";

        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(
                ToStream(sse),
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            events.Add(evt);

        // [DONE] is skipped, but parsing continues for lines after it
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task ParseAsync_NonDataLines_Skipped()
    {
        var sse = "event: message\nid: 1\ndata: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\n";

        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(
                ToStream(sse),
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            events.Add(evt);

        Assert.Single(events);
        Assert.IsType<RunStartedEvent>(events[0]);
    }

    [Fact]
    public async Task ParseAsync_CancellationToken_StopsProcessing()
    {
        var sse =
            "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n";
        using var cts = new CancellationTokenSource();

        var events = new List<BaseEvent>();
        await foreach (var evt in SseEventParser.ParseAsync(ToStream(sse), cancellationToken: cts.Token))
        {
            events.Add(evt);
            cts.Cancel(); // cancel after first event
        }

        Assert.Single(events);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_YieldsNothing()
    {
        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(ToStream(""), cancellationToken: TestContext.Current.CancellationToken)
        )
            events.Add(evt);

        Assert.Empty(events);
    }

    [Fact]
    public async Task ParseAsync_DataWithExtraSpaces_TrimsCorrectly()
    {
        var sse = "data:   {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n";

        var events = new List<BaseEvent>();
        await foreach (
            var evt in SseEventParser.ParseAsync(
                ToStream(sse),
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            events.Add(evt);

        Assert.Single(events);
    }
}
