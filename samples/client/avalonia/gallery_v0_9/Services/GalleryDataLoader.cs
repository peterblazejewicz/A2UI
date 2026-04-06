using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using A2Ui.Avalonia.Gallery.Models;
using A2Ui.Core.Messages;

namespace A2Ui.Avalonia.Gallery.Services;

/// <summary>
/// Loads A2UI spec examples from the Specs/ directory relative to the app executable.
/// Handles both envelope format ({ name, description, messages[] }) and bare array format.
/// Auto-injects a createSurface message if the example doesn't include one.
/// </summary>
public sealed class GalleryDataLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public static async Task<IReadOnlyList<DemoItem>> LoadAsync(CancellationToken ct = default)
    {
        var items = new List<DemoItem>();
        string specsDir = Path.Combine(AppContext.BaseDirectory, "Specs");

        await LoadFromDirectoryAsync(
                Path.Combine(specsDir, "minimal"),
                isBasic: false,
                items,
                ct)
            .ConfigureAwait(false);
        await LoadFromDirectoryAsync(
                Path.Combine(specsDir, "basic"),
                isBasic: true,
                items,
                ct)
            .ConfigureAwait(false);

        // Assign continuous display indices so the sidebar shows 1..N
        // instead of the per-directory numbering from filenames.
        for (int i = 0; i < items.Count; i++)
            items[i] = items[i] with { Title = $"{i + 1}. {items[i].Title}" };

        return items;
    }

    private static async Task LoadFromDirectoryAsync(
        string directory,
        bool isBasic,
        List<DemoItem> items,
        CancellationToken ct)
    {
        if (!Directory.Exists(directory))
        {
            Debug.WriteLine($"[GalleryDataLoader] Specs directory not found: {directory}");
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                DemoItem? item = await LoadFileAsync(filePath, isBasic, ct)
                    .ConfigureAwait(false);
                if (item is not null)
                    items.Add(item);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(
                    $"[GalleryDataLoader] Skipping malformed JSON '{filePath}': {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine(
                    $"[GalleryDataLoader] Cannot read file '{filePath}': {ex.Message}");
            }
        }
    }

    private static async Task<DemoItem?> LoadFileAsync(
        string filePath,
        bool isBasic,
        CancellationToken ct)
    {
        string json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        // Extract messages — handle both envelope and bare array
        A2UiMessage[] messages;
        string? name = null;
        string? description = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            messages =
                JsonSerializer.Deserialize<A2UiMessage[]>(root.GetRawText(), s_jsonOptions) ?? [];
        }
        else
        {
            if (root.TryGetProperty("name", out JsonElement nameEl))
                name = nameEl.GetString();
            if (root.TryGetProperty("description", out JsonElement descEl))
                description = descEl.GetString();

            if (root.TryGetProperty("messages", out JsonElement messagesEl))
                messages =
                    JsonSerializer.Deserialize<A2UiMessage[]>(
                        messagesEl.GetRawText(),
                        s_jsonOptions) ?? [];
            else
                return null;
        }

        if (messages.Length == 0)
            return null;

        string filename = Path.GetFileName(filePath);
        string surfaceId = Path.GetFileNameWithoutExtension(filePath);

        // Auto-inject createSurface if missing
        bool hasCreate = messages.Any(m => m.CreateSurface is not null);
        if (!hasCreate)
        {
            string catalogId = isBasic
                ? "https://a2ui.org/specification/v0_9/basic_catalog.json"
                : "https://a2ui.org/specification/v0_9/catalogs/minimal/minimal_catalog.json";

            var createMsg = new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface
                {
                    SurfaceId = surfaceId,
                    CatalogId = catalogId,
                },
            };
            messages = [createMsg, .. messages];
        }
        else
        {
            // Use the surfaceId from the createSurface message
            A2UiMessage? createMessage = messages.FirstOrDefault(m =>
                m.CreateSurface is not null);
            if (createMessage?.CreateSurface is not null)
                surfaceId = createMessage.CreateSurface.SurfaceId;
        }

        // Always derive title from filename (includes number prefix like Lit gallery)
        string title = DeriveTitleFromFilename(filename);

        return new DemoItem(
            Id: surfaceId,
            Title: title,
            Filename: filename,
            Description: description ?? $"Source: {filename}",
            Messages: messages,
            IsBasic: isBasic);
    }

    private static string DeriveTitleFromFilename(string filename)
    {
        string stem = Path.GetFileNameWithoutExtension(filename);
        string[] words = stem.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);

        // Skip the leading numeric token (e.g., "1", "01", "33") so that the
        // caller can assign continuous display indices without duplication.
        if (words.Length > 1 && words[0].All(char.IsDigit))
            words = words[1..];

        return string.Join(
            ' ',
            words.Select(w =>
                string.IsNullOrEmpty(w)
                    ? w
                    : char.ToUpper(w[0], CultureInfo.InvariantCulture) + w[1..]));
    }
}
