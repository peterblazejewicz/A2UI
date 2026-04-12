using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Text display component.</summary>
public sealed record TextComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Text";
}

/// <summary>Clickable button component.</summary>
public sealed record ButtonComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Button";
}

/// <summary>Vertical layout container.</summary>
public sealed record ColumnComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Column";
}

/// <summary>Horizontal layout container.</summary>
public sealed record RowComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Row";
}

/// <summary>Card container with rounded borders.</summary>
public sealed record CardComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Card";
}

/// <summary>Icon display component.</summary>
public sealed record IconComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Icon";
}

/// <summary>Visual divider (horizontal or vertical).</summary>
public sealed record DividerComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Divider";
}

/// <summary>Image display component.</summary>
public sealed record ImageComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Image";
}

/// <summary>Video placeholder component.</summary>
public sealed record VideoComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Video";
}

/// <summary>Audio player placeholder component.</summary>
public sealed record AudioPlayerComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "AudioPlayer";
}

/// <summary>Scrollable list container.</summary>
public sealed record ListComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "List";
}

/// <summary>Tabbed container component.</summary>
public sealed record TabsComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Tabs";
}

/// <summary>Modal dialog component.</summary>
public sealed record ModalComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Modal";
}

/// <summary>Text input field component.</summary>
public sealed record TextFieldComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "TextField";
}

/// <summary>Date/time input component.</summary>
public sealed record DateTimeInputComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "DateTimeInput";
}

/// <summary>Choice picker (dropdown/radio/checkbox list).</summary>
public sealed record ChoicePickerComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "ChoicePicker";
}

/// <summary>Checkbox input component.</summary>
public sealed record CheckBoxComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "CheckBox";
}

/// <summary>Slider input component.</summary>
public sealed record SliderComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Slider";
}

/// <summary>Data table component (extension).</summary>
public sealed record TableComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Table";
}

/// <summary>Root surface container (extension).</summary>
public sealed record SurfaceComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Surface";
}
