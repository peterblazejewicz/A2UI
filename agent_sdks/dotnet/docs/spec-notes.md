# A2UI v0.9 Specification Notes for .NET Implementation

## Component Type Inventory
From `specification/v0_9/json/basic_catalog.json`:
- AudioPlayer
- Button
- Card
- CheckBox
- ChoicePicker
- Column
- DateTimeInput
- Divider
- Icon
- Image
- List
- Modal
- Row
- Slider
- Tabs
- Text
- TextField
- Video

## Angular/Lit → Avalonia Control Mapping Table
Based on analysis of existing renderers:

| A2UI Component | Angular Control | Lit Control | Suggested Avalonia Control |
|----------------|----------------|-------------|----------------------------|
| Text | a2ui-text | A2uiTextElement | TextBlock |
| Button | a2ui-button | (inherits from A2uiLitElement) | Button |
| Column | a2ui-column | (inherits from A2uiLitElement) | StackPanel (Orientation="Horizontal") or Panel |
| Row | a2ui-row | (inherits from A2uiLitElement) | StackPanel (Orientation="Vertical") or Panel |
| Card | a2ui-card | (inherits from A2uiLitElement) | Border or Expander |
| TextField | a2ui-text-field | (inherits from A2uiLitElement) | TextBox |
| CheckBox | a2ui-checkbox | (inherits from A2uiLitElement) | CheckBox |
| Slider | a2ui-slider | (inherits from A2uiLitElement) | Slider |
| DateTimeInput | a2ui-date-time-input | (inherits from A2uiLitElement) | DatePicker or TimePicker |
| Image | a2ui-image | (inherits from A2uiLitElement) | Image |
| Icon | a2ui-icon | (inherits from A2uiLitElement) | Image (with icon source) |
| List | a2ui-list | (inherits from A2uiLitElement) | ItemsControl or ListBox |
| Select | (inferred) | (inferred) | ComboBox |
| Tabs | a2ui-tabs | (inherits from A2uiLitElement) | TabControl |
| Modal | a2ui-modal | (inherits from A2uiLitElement) | Window or ContentDialog |
| Divider | a2ui-divider | (inherits from A2uiLitElement) | Rectangle or Line (as separator) |
| Video | a2ui-video | (inherits from A2uiLitElement) | MediaElement |
| AudioPlayer | a2ui-audio-player | (inherits from A2uiLitElement) | MediaElement (audio only) |
| ChoicePicker | a2ui-choice-picker | (inherits from A2uiLitElement) | ComboBox or ListBox |
| Surface | a2ui-surface | (inherits from A2uiLitElement) | ContentControl or UserControl |

## Wire Format Notes
From `specification/v0_9/json/server_to_client.json`:
- Message types: CreateSurfaceMessage, UpdateComponentsMessage, UpdateDataModelMessage
- JSONL format (one JSON object per line)
- Each message has a `type` field indicating the message kind

From `specification/v0_9/json/client_to_server.json`:
- Client-to-server messages contain `version: "v0.9"` and a `userAction` field
- UserAction includes action type, componentId, and optional formData

## Composer Feature List for Port
From scanning `tools/composer/`:
- Widget creation interface with form-based configuration
- Preview pane showing live A2UI rendering
- Component property editing panels
- JSON import/export functionality
- Template gallery with sample widgets
- Copy-to-clipboard for generated code
- Responsive layout suitable for desktop application
- Integration with CopilotKit for AI-assisted widget creation

## Implementation Targets
Based on CLAUDE.md deliverables:
1. `agent_sdks/dotnet/` - C# SDK: AG-UI event types + A2UI message model
2. `renderers/avalonia/` - Avalonia renderer: catalog registry + control implementations  
3. `samples/client/avalonia/composer/` - Composer port: native MVVM desktop app

## Forward Compatibility Hints (v0.10)
Reviewed `specification/v0_10/docs/`:
- Evolution guide indicates additive changes
- Custom functions and extension mechanisms added
- Core message types remain compatible
- Focus on v0.9 implementation with awareness of v0.10 extensions

