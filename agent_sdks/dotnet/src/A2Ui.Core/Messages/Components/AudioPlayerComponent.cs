using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Audio player placeholder component.</summary>
public sealed record AudioPlayerComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "AudioPlayer";

    /// <summary>Audio source URL.</summary>
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }

    /// <summary>Descriptive text for the audio content.</summary>
    [JsonPropertyName("description")]
    public DynamicValue? Description { get; init; }
}
