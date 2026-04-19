using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace A2Ui.TestHelpers;

/// <summary>
/// In-memory <see cref="ILoggerProvider"/> that captures every log entry
/// emitted during a test as a record. Use <see cref="Entries"/> to query
/// captured logs via LINQ, asserting on <c>EventId</c> and structured
/// property values rather than formatted message strings.
/// </summary>
/// <remarks>
/// Wire into a test like this:
/// <code>
/// using var provider = new TestLoggerProvider();
/// var sut = new SurfaceManager(provider.CreateFactory());
/// sut.Process(message);
/// var entries = provider.Entries.Where(e => e.EventId.Id == 9).ToList();
/// </code>
/// Thread-safe for concurrent writes via <see cref="ConcurrentQueue{T}"/>.
/// <para>
/// <see cref="CreateFactory"/> returns a minimal <see cref="ILoggerFactory"/>
/// that forwards to this provider. We expose our own factory rather than
/// depending on <c>LoggerFactory.Create</c> because that API lives in the
/// <c>Microsoft.Extensions.Logging</c> NuGet package, while TestHelpers
/// only references <c>Microsoft.Extensions.Logging.Abstractions</c>.
/// </para>
/// </remarks>
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<TestLogEntry> _entries = new();

    /// <summary>All log entries captured so far, in the order they were emitted.</summary>
    public IReadOnlyCollection<TestLogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _entries);

    /// <summary>
    /// Create an <see cref="ILoggerFactory"/> that routes every logger to
    /// this provider's entry queue. The returned factory is owned by the
    /// caller and should be disposed together with the test.
    /// </summary>
    public ILoggerFactory CreateFactory() => new TestLoggerFactory(this);

    public void Dispose()
    {
        /* nothing to release — entries snapshot survives */
    }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestLoggerProvider _provider;

        public TestLoggerFactory(TestLoggerProvider provider) => _provider = provider;

        public void AddProvider(ILoggerProvider provider)
        {
            // No-op: tests intentionally route to a single provider.
        }

        public ILogger CreateLogger(string categoryName) => _provider.CreateLogger(categoryName);

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConcurrentQueue<TestLogEntry> _sink;

        public TestLogger(string categoryName, ConcurrentQueue<TestLogEntry> sink)
        {
            _categoryName = categoryName;
            _sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            // Extract structured properties if state is IEnumerable<KeyValuePair<string, object?>>
            // which is how LoggerMessage source-generated state appears.
            Dictionary<string, object?> properties = [];
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                foreach (var kvp in kvps)
                    properties[kvp.Key] = kvp.Value;
            }

            string message = formatter(state, exception);

            _sink.Enqueue(
                new TestLogEntry(
                    CategoryName: _categoryName,
                    Level: logLevel,
                    EventId: eventId,
                    Message: message,
                    Properties: properties,
                    Exception: exception
                )
            );
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
