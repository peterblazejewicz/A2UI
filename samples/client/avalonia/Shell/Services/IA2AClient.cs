using A2Ui.Core.Messages;

namespace A2Ui.Avalonia.Shell.Services;

/// <summary>
/// Client interface for communicating with an A2A agent that supports A2UI extensions.
/// </summary>
public interface IA2AClient
{
    /// <summary>Gets the agent's display name from its agent card.</summary>
    Task<string> GetAgentNameAsync(CancellationToken ct = default);

    /// <summary>Sends a text query and returns the A2UI messages from the response.</summary>
    Task<IReadOnlyList<A2UiMessage>> SendTextAsync(string text, CancellationToken ct = default);

    /// <summary>Sends a user action (v0.8 envelope) and returns the A2UI messages from the response.</summary>
    Task<IReadOnlyList<A2UiMessage>> SendActionAsync(object userAction, CancellationToken ct = default);
}
