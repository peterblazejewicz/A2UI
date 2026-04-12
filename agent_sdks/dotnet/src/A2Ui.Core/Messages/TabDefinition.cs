using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Tab definition for Tabs component.</summary>
public sealed record TabDefinition
{
    /// <summary>Display title for the tab.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>Component ID of the tab's content.</summary>
    [JsonPropertyName("child")]
    public required string Child { get; init; }
}
