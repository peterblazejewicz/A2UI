namespace A2Ui.Avalonia.Shell.Models;

/// <summary>
/// Configuration for connecting to an A2A agent.
/// </summary>
public sealed record AgentConfig(string ServerUrl, string Title, string Placeholder);
