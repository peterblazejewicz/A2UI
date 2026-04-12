namespace A2Ui.Avalonia.Shell.Services;

/// <summary>
/// Serializes <see cref="UserActionEventArgs"/> into the v0.8 <c>userAction</c> envelope
/// expected by the Python restaurant agent.
/// <para>
/// Wire format: <c>{ "userAction": { "name": "...", "surfaceId": "...",
/// "sourceComponentId": "...", "timestamp": "...", "context": { ... } } }</c>
/// </para>
/// <para>
/// Uses <c>name</c> (not <c>actionName</c>) to match the Lit shell convention
/// (<c>samples/client/lit/shell/app.ts:494</c>).
/// </para>
/// </summary>
internal static class UserActionSerializer
{
    public static object Serialize(UserActionEventArgs e) =>
        new
        {
            userAction = new
            {
                name = e.EventName,
                surfaceId = e.SurfaceId,
                sourceComponentId = e.ComponentId ?? string.Empty,
                timestamp = DateTimeOffset.UtcNow.ToString("o"),
                context = e.Payload,
            },
        };
}
