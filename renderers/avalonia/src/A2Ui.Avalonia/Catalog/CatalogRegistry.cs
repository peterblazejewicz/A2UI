using System;
using System.Collections.Generic;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Registry of A2UI type → Avalonia control factory mappings.
/// The agent may ONLY reference types registered here — security boundary.
/// </summary>
public sealed class CatalogRegistry
{
    private readonly Dictionary<string, ICatalogEntry> _entries = new();

    /// <summary>Register a catalog entry. Throws if already registered.</summary>
    public CatalogRegistry Register(ICatalogEntry entry)
    {
        _entries.Add(entry.ComponentType, entry);
        return this;
    }

    /// <summary>Register a simple factory function without implementing ICatalogEntry.</summary>
    public CatalogRegistry Register(string componentType,
        Func<A2UiComponent, DataModel, IRenderContext, Control> factory)
    {
        return Register(new DelegateCatalogEntry(componentType, factory));
    }

    public bool TryGetEntry(string componentType, out ICatalogEntry? entry) =>
        _entries.TryGetValue(componentType, out entry);

    public IReadOnlyCollection<string> RegisteredTypes => _entries.Keys;

    /// <summary>Build the default catalog with all built-in component types.</summary>
    public static CatalogRegistry CreateDefault() => new CatalogRegistry()
        .Register(new TextCatalogEntry())
        .Register(new ButtonCatalogEntry())
        .Register(new ColumnCatalogEntry())
        .Register(new RowCatalogEntry())
        .Register(new TextFieldCatalogEntry())
        .Register(new DateTimeInputCatalogEntry())
        .Register(new CardCatalogEntry())
        .Register(new ImageCatalogEntry())
        .Register(new SelectCatalogEntry())
        .Register(new CheckboxCatalogEntry())
        .Register(new SliderCatalogEntry())
        .Register(new TableCatalogEntry())
        .Register(new SurfaceCatalogEntry());
}

internal sealed class DelegateCatalogEntry(
    string componentType,
    Func<A2UiComponent, DataModel, IRenderContext, Control> factory) : ICatalogEntry
{
    public string ComponentType => componentType;

    public Control Create(A2UiComponent component, DataModel dataModel,
                          IRenderContext context) =>
        factory(component, dataModel, context);

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel,
                       IRenderContext context) => false; // recreate by default
}