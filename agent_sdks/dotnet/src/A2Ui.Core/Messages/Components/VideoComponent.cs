using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Video placeholder component.</summary>
public sealed record VideoComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Video";

    /// <summary>Video source URL.</summary>
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }
}
