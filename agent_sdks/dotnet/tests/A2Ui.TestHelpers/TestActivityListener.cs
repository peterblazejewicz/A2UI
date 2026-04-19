using System.Collections.Concurrent;
using System.Diagnostics;

namespace A2Ui.TestHelpers;

/// <summary>
/// In-memory <see cref="ActivityListener"/> that captures every started
/// and stopped <see cref="Activity"/> from the specified sources. Use
/// <see cref="StoppedActivities"/> to assert on span names, tags, and
/// status codes in tests.
/// </summary>
/// <remarks>
/// Wire into a test like this:
/// <code>
/// using var listener = new TestActivityListener("A2Ui.Core", "A2Ui.Avalonia.Renderer");
/// sut.DoWork();
/// var span = listener.StoppedActivities
///     .First(a => a.OperationName == "Surface.createSurface");
/// Assert.Equal("default", span.GetTagItem("a2ui.surface_id"));
/// </code>
/// Disposal unregisters the listener from <see cref="ActivitySource"/>
/// globally so test isolation is preserved.
/// </remarks>
public sealed class TestActivityListener : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentQueue<Activity> _started = new();
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly HashSet<string> _sourceNames;

    /// <summary>
    /// Create a listener that subscribes to the specified source names.
    /// Pass zero arguments to subscribe to all sources (useful when you
    /// don't know which sources the code under test will use).
    /// </summary>
    public TestActivityListener(params string[] sourceNames)
    {
        _sourceNames = new HashSet<string>(sourceNames, StringComparer.Ordinal);
        _listener = new ActivityListener
        {
            ShouldListenTo = source => _sourceNames.Count == 0 || _sourceNames.Contains(source.Name),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _started.Enqueue(activity),
            ActivityStopped = activity => _stopped.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>All activities started so far (in enqueue order).</summary>
    public IReadOnlyCollection<Activity> StartedActivities => _started;

    /// <summary>All activities that have been stopped (disposed) so far.</summary>
    public IReadOnlyCollection<Activity> StoppedActivities => _stopped;

    public void Dispose() => _listener.Dispose();
}
