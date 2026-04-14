using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CA1812 // DTOs instantiated by System.Text.Json deserialization

namespace A2Ui.Avalonia.Shell.Models;

// Minimal A2A wire format DTOs — only what the Shell client needs to send/receive.
// These model the A2A transport envelope, not A2UI content.

// ── JSON-RPC 2.0 envelope ─────────────────────

/// <summary>
/// JSON-RPC 2.0 request envelope. A2A wraps <c>message/send</c> params in this
/// envelope; posting the bare params object causes the Python <c>a2a</c> SDK
/// to return an empty result.
/// </summary>
internal sealed record JsonRpcRequest<TParams>
    where TParams : class
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("params")]
    public required TParams Params { get; init; }
}

// ── Request ────────────────────────────────────

/// <summary>Params payload for the A2A <c>message/send</c> method.</summary>
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
