using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Registry of A2UI type → control factory mappings.
/// The agent may ONLY reference types registered here — security boundary.
/// </summary>
public sealed class CatalogRegistry
{
    private readonly Dictionary<string, ICatalogEntry> _entries = new();
    private readonly ILogger<CatalogRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for registration and lookup diagnostics.</param>
    public CatalogRegistry(ILogger<CatalogRegistry>? logger = null)
    {
        this._logger = logger ?? NullLogger<CatalogRegistry>.Instance;
    }

    /// <summary>Register a catalog entry. Throws if already registered.</summary>
    public CatalogRegistry Register(ICatalogEntry entry)
    {
        this._entries.Add(entry.ComponentType, entry);
        CatalogRegistryLog.CatalogEntryRegistered(this._logger, entry.ComponentType, entry.GetType().Name);
        return this;
    }

    /// <summary>Register a simple factory function without implementing ICatalogEntry.</summary>
    public CatalogRegistry Register(
        string componentType,
        Func<A2UiComponent, DataModel, IRenderContext, Control> factory
    )
    {
        return this.Register(new DelegateCatalogEntry(componentType, factory));
    }

    /// <summary>Looks up a catalog entry by component type string.</summary>
    /// <param name="componentType">The A2UI component type to look up.</param>
    /// <param name="entry">The entry if found, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the component type is registered.</returns>
    public bool TryGetEntry(string componentType, out ICatalogEntry? entry)
    {
        if (this._entries.TryGetValue(componentType, out entry))
        {
            return true;
        }

        CatalogRegistryLog.CatalogLookupMiss(this._logger, componentType, string.Join(",", this._entries.Keys));
        entry = null;
        return false;
    }

    /// <summary>Gets the set of registered component type strings.</summary>
    public IReadOnlyCollection<string> RegisteredTypes => this._entries.Keys;

    /// <summary>
    /// Build the default catalog with all 18 v0.9 basic catalog component types.
    /// </summary>
    public static CatalogRegistry CreateDefault(ILoggerFactory? loggerFactory = null) =>
        new CatalogRegistry(loggerFactory?.CreateLogger<CatalogRegistry>())
            // Display
            .Register(new TextCatalogEntry())
            .Register(new ImageCatalogEntry(loggerFactory?.CreateLogger<ImageCatalogEntry>()))
            .Register(new IconCatalogEntry())
            .Register(new VideoCatalogEntry())
            .Register(new AudioPlayerCatalogEntry())
            .Register(new DividerCatalogEntry())
            // Layout
            .Register(new RowCatalogEntry())
            .Register(new ColumnCatalogEntry())
            .Register(new ListCatalogEntry())
            .Register(new CardCatalogEntry())
            .Register(new TabsCatalogEntry())
            .Register(new ModalCatalogEntry())
            // Interactive
            .Register(new ButtonCatalogEntry())
            .Register(new TextFieldCatalogEntry())
            .Register(new CheckBoxCatalogEntry())
            .Register(new ChoicePickerCatalogEntry())
            .Register(new DateTimeInputCatalogEntry())
            .Register(new SliderCatalogEntry());
}

internal sealed class DelegateCatalogEntry(
    string componentType,
    Func<A2UiComponent, DataModel, IRenderContext, Control> factory
) : ICatalogEntry
{
    public string ComponentType => componentType;

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context) =>
        factory(component, dataModel, context);

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false;
}

internal static partial class CatalogRegistryLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Catalog entry registered: {ComponentType} → {EntryTypeName}"
    )]
    public static partial void CatalogEntryRegistered(ILogger logger, string componentType, string entryTypeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Catalog lookup miss for component type '{ComponentType}'. Known types: [{KnownTypes}]"
    )]
    public static partial void CatalogLookupMiss(ILogger logger, string componentType, string knownTypes);
}
