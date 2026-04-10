using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CA1812 // DTOs instantiated by System.Text.Json deserialization

namespace A2Ui.Avalonia.Shell.Models;

/// <summary>
/// Minimal A2A wire format DTOs — only what the Shell client needs to send/receive.
/// These model the A2A transport envelope, not A2UI content.
/// </summary>
// ── Request ────────────────────────────────────

internal sealed record A2ASendMessageRequest
{
    [JsonPropertyName("message")]
    public required A2AMessage Message { get; init; }
}

internal sealed record A2AMessage
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    [JsonPropertyName("role")]
    public string Role { get; init; } = "user";

    [JsonPropertyName("parts")]
    public required A2APart[] Parts { get; init; }

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "message";
}

// A2APart uses [JsonDerivedType] polymorphism — kept as a class since it's
// a non-sealed base type. Derived types are sealed records.
[JsonDerivedType(typeof(A2ATextPart))]
[JsonDerivedType(typeof(A2ADataPart))]
internal class A2APart
{
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }
}

internal sealed class A2ATextPart : A2APart
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

internal sealed class A2ADataPart : A2APart
{
    [JsonPropertyName("data")]
    public required JsonElement Data { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
}

// ── Response ───────────────────────────────────

internal sealed record A2ATaskResponse
{
    [JsonPropertyName("result")]
    public A2ATask? Result { get; init; }

    [JsonPropertyName("error")]
    public A2AError? Error { get; init; }
}

internal sealed record A2ATask
{
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("status")]
    public A2ATaskStatus? Status { get; init; }
}

internal sealed record A2ATaskStatus
{
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("message")]
    public A2AResponseMessage? Message { get; init; }
}

internal sealed record A2AResponseMessage
{
    [JsonPropertyName("parts")]
    public A2AResponsePart[]? Parts { get; init; }
}

internal sealed record A2AResponsePart
{
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

internal sealed record A2AError
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

// ── Agent Card (minimal) ───────────────────────

internal sealed record A2AAgentCard
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
