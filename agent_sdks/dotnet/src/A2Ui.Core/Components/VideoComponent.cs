using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Video placeholder component.</summary>
public sealed record VideoComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Video";

    /// <summary>Video source URL.</summary>
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }
}
