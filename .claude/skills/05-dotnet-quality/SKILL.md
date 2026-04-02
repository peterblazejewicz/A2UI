---
name: a2ui_dotnet_quality
description: Configure .editorconfig, Roslyn analyzers, xUnit 2.9, Avalonia.Headless.XUnit tests, Coverlet coverage, CSharpier formatter.
---


---

## Purpose

Configure the full .NET code quality pipeline:
- `.editorconfig` with C# style enforcement
- Roslyn analyzers (errors not warnings)
- xUnit 2.9 unit tests with FluentAssertions
- Avalonia.Headless.XUnit for UI component tests
- Coverlet + ReportGenerator for coverage
- CSharpier for opinionated formatting

All tools are MIT or Apache 2.0 licensed as of March 2026.

---

## Phase 1 — .editorconfig

Create `/sandbox/develop/A2Ui/.editorconfig`:

```ini
# .editorconfig — C# code style for A2Ui project
# Enforced at build time via Directory.Build.props:
#   <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

root = true

[*]
charset                  = utf-8
end_of_line              = lf
indent_style             = space
indent_size              = 4
trim_trailing_whitespace = true
insert_final_newline     = true
max_line_length          = 120

[*.md]
indent_size              = 2
trim_trailing_whitespace = false

[*.json]
indent_size              = 2

[*.{yaml,yml}]
indent_size              = 2

[*.{csproj,props,targets}]
indent_size              = 2

# ── C# Specific ───────────────────────────────────────────────────────────
[*.cs]

# Braces: always (Allman style)
csharp_new_line_before_open_brace                    = all
csharp_new_line_before_else                          = true
csharp_new_line_before_catch                         = true
csharp_new_line_before_finally                       = true
csharp_new_line_before_members_in_anonymous_types    = true
csharp_new_line_between_query_expression_clauses     = true

# Indentation
csharp_indent_case_contents                          = true
csharp_indent_switch_labels                          = true
csharp_indent_labels                                 = no_change

# Spacing
csharp_space_after_cast                              = false
csharp_space_before_colon_in_inheritance_clause      = true
csharp_space_after_colon_in_inheritance_clause       = true
csharp_space_around_binary_operators                 = before_and_after
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_method_call_parameter_list_parentheses        = false

# Wrapping
csharp_preserve_single_line_statements               = false
csharp_preserve_single_line_blocks                   = true

# var usage — use var only when type is obvious
csharp_style_var_for_built_in_types                  = false:warning
csharp_style_var_when_type_is_apparent               = true:suggestion
csharp_style_var_elsewhere                           = false:warning

# Expression body — use for simple single-expression members
csharp_style_expression_bodied_methods               = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors          = false:none
csharp_style_expression_bodied_operators             = when_on_single_line:suggestion
csharp_style_expression_bodied_properties            = true:suggestion
csharp_style_expression_bodied_indexers              = true:suggestion
csharp_style_expression_bodied_accessors             = when_on_single_line:suggestion

# Pattern matching
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_pattern_matching_over_as_with_null_check  = true:warning
csharp_style_prefer_switch_expression                  = true:suggestion
csharp_style_prefer_pattern_matching                   = true:suggestion
csharp_style_prefer_not_pattern                        = true:suggestion

# Null checks
csharp_style_prefer_null_check_over_type_check         = true:suggestion
dotnet_style_null_propagation                          = true:warning
dotnet_style_coalesce_expression                       = true:warning
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:warning

# Throw expressions
csharp_style_throw_expression                          = true:suggestion

# Using directives — always at the top, System first
csharp_using_directive_placement                       = outside_namespace:warning
dotnet_sort_system_directives_first                    = true
dotnet_separate_import_directive_groups                = true

# Namespace style — file-scoped
csharp_style_namespace_declarations                    = file_scoped:warning

# Primary constructors (C# 12+)
csharp_style_prefer_primary_constructors               = false:none

# Collections
dotnet_style_prefer_collection_expression              = true:suggestion

# Naming conventions
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols  = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style    = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds          = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, internal

dotnet_naming_style.begins_with_i.required_prefix        = I
dotnet_naming_style.begins_with_i.capitalization         = pascal_case

# Private fields: _camelCase
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case.symbols  = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style    = camel_case_underscore

dotnet_naming_symbols.private_fields.applicable_kinds          = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private, private_protected

dotnet_naming_style.camel_case_underscore.required_prefix      = _
dotnet_naming_style.camel_case_underscore.capitalization       = camel_case

# Constants: SCREAMING_SNAKE or PascalCase (both are fine — enforce PascalCase)
dotnet_naming_rule.constants_should_be_pascal.severity = warning
dotnet_naming_rule.constants_should_be_pascal.symbols  = constants
dotnet_naming_rule.constants_should_be_pascal.style    = pascal_case_style

dotnet_naming_symbols.constants.applicable_kinds          = field
dotnet_naming_symbols.constants.required_modifiers        = const

dotnet_naming_style.pascal_case_style.capitalization = pascal_case

# Diagnostic severities — promote common issues to errors
dotnet_diagnostic.CA1822.severity = warning    # Mark members as static
dotnet_diagnostic.CA1812.severity = warning    # Avoid uninstantiated internal classes
dotnet_diagnostic.CS8600.severity = error      # Converting null literal
dotnet_diagnostic.CS8602.severity = error      # Dereference of possibly null
dotnet_diagnostic.CS8603.severity = error      # Possible null reference return
dotnet_diagnostic.CS8604.severity = error      # Possible null reference argument
dotnet_diagnostic.CS8618.severity = error      # Non-nullable field uninitialized
dotnet_diagnostic.CS8625.severity = error      # Cannot convert null literal
dotnet_diagnostic.IDE0055.severity = warning   # Fix formatting
dotnet_diagnostic.IDE0161.severity = warning   # Use file-scoped namespace
```

---

## Phase 2 — Analyzer Configuration

Update `Directory.Build.props` to add analyzers:

```xml
<!-- Add to existing Directory.Build.props ItemGroup -->
<ItemGroup Label="Analyzers" Condition="$(MSBuildProjectName) != ''">
  <PackageReference Include="Roslynator.Analyzers"               PrivateAssets="all" />
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" PrivateAssets="all" />
</ItemGroup>
```

Create `/sandbox/develop/A2Ui/.globalconfig`:

```ini
# Global analyzer config — augments .editorconfig for analyzer rules
is_global = true

# Roslynator
dotnet_diagnostic.RCS1021.severity = suggestion  # Simplify lambda
dotnet_diagnostic.RCS1090.severity = warning     # Call ConfigureAwait
dotnet_diagnostic.RCS1194.severity = warning     # Implement exception constructors
dotnet_diagnostic.RCS1229.severity = warning     # Use async/await

# .NET analyzers
dotnet_diagnostic.CA2007.severity = warning      # ConfigureAwait
dotnet_diagnostic.CA1031.severity = none         # Allow catch Exception (we log)
dotnet_diagnostic.CA1062.severity = none         # Validate params (nullable handles this)
```

---

## Phase 3 — xUnit Test Projects Setup

### Protocol Tests

Edit `tests/AgUi.Protocol.Tests/AgUi.Protocol.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <!-- Disable doc warnings for test projects -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Bogus" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/AgUi.Protocol/AgUi.Protocol.csproj" />
    <ProjectReference Include="../../src/A2Ui.Core/A2Ui.Core.csproj" />
  </ItemGroup>
</Project>
```

### Sample Tests — Event Serialization

Create `tests/AgUi.Protocol.Tests/Events/EventSerializationTests.cs`:

```csharp
using System.Text.Json;
using AgUi.Protocol.Events;
using FluentAssertions;

namespace AgUi.Protocol.Tests.Events;

public sealed class EventSerializationTests
{
    private static readonly JsonSerializerOptions s_opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RunStartedEvent_RoundTrip_PreservesFields()
    {
        // Arrange
        var json = """{"type":"RUN_STARTED","threadId":"t1","runId":"r1","parentRunId":"p1"}""";

        // Act
        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        // Assert
        evt.Should().BeOfType<RunStartedEvent>();
        var run = (RunStartedEvent)evt!;
        run.ThreadId.Should().Be("t1");
        run.RunId.Should().Be("r1");
        run.ParentRunId.Should().Be("p1");
    }

    [Fact]
    public void ToolCallArgsAccumulator_ConcatenatesDeltas()
    {
        // Arrange
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "{\"ci" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "ty\":\"San" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = " Francisco\"}" });

        // Act
        string result = acc.Complete("tc1");

        // Assert
        result.Should().Be("""{"city":"San Francisco"}""");
    }

    [Fact]
    public void StateDeltaEvent_ContainsRfc6902Patches()
    {
        // Arrange
        var json = """
            {
              "type":"STATE_DELTA",
              "delta":[
                {"op":"replace","path":"/user/status","value":"active"},
                {"op":"add","path":"/items/-","value":{"id":42}}
              ]
            }
            """;

        // Act
        var evt = JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        // Assert
        evt.Should().BeOfType<StateDeltaEvent>();
        var delta = (StateDeltaEvent)evt!;
        delta.Delta.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("RUN_STARTED")]
    [InlineData("RUN_FINISHED")]
    [InlineData("RUN_ERROR")]
    [InlineData("TEXT_MESSAGE_CONTENT")]
    [InlineData("TOOL_CALL_ARGS")]
    [InlineData("STATE_SNAPSHOT")]
    [InlineData("CUSTOM")]
    public void AllEventTypes_DeserializeWithoutException(string eventType)
    {
        // Arrange
        var json = $$"""{"type":"{{eventType}}","message":"test","threadId":"t","runId":"r","messageId":"m","toolCallId":"tc","content":"c","snapshot":{},"name":"n","value":"v","delta":[]}""";

        // Act
        var act = () => JsonSerializer.Deserialize<BaseEvent>(json, s_opts);

        // Assert
        act.Should().NotThrow();
    }
}
```

### Sample Tests — A2UI Core

Create `tests/AgUi.Protocol.Tests/A2Ui/DataModelTests.cs`:

```csharp
using System.Text.Json;
using A2Ui.Core;
using A2Ui.Core.Messages;
using FluentAssertions;

namespace AgUi.Protocol.Tests.A2Ui;

public sealed class DataModelTests
{
    [Fact]
    public void DataModel_Apply_UpdateDataModel_SetsNestedPath()
    {
        // Arrange
        var dm = new DataModel();
        var update = new UpdateDataModel
        {
            SurfaceId = "main",
            Path      = "/reservation/date",
            Value     = JsonSerializer.SerializeToElement("2025-12-15"),
        };

        // Act
        dm.Apply(update);

        // Assert
        dm.Resolve(BoundOrLiteral.Bound_("/reservation/date"))
          .Should().Be("2025-12-15");
    }

    [Fact]
    public void DataModel_Resolve_LiteralValue_ReturnsLiteral()
    {
        // Arrange
        var dm = new DataModel();

        // Act + Assert
        dm.Resolve(BoundOrLiteral.Literal_("Hello")).Should().Be("Hello");
    }

    [Fact]
    public void DataModel_Resolve_UnknownPath_ReturnsNull()
    {
        // Arrange
        var dm = new DataModel();

        // Act + Assert
        dm.Resolve(BoundOrLiteral.Bound_("/nonexistent/path")).Should().BeNull();
    }

    [Fact]
    public void SurfaceManager_Process_CreateSurface_RaisesEvent()
    {
        // Arrange
        var sm = new SurfaceManager();
        Surface? created = null;
        sm.SurfaceCreated += (_, e) => created = e.Surface;

        var msg = new A2UiMessage
        {
            CreateSurface = new CreateSurface
            {
                SurfaceId = "main",
                CatalogId = "https://a2ui.org/specification/v0_9/basic_catalog.json",
            }
        };

        // Act
        sm.Process(msg);

        // Assert
        created.Should().NotBeNull();
        created!.SurfaceId.Should().Be("main");
    }
}
```

---

## Phase 4 — Avalonia Headless UI Tests

### Setup

Edit `tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Avalonia.Headless.XUnit" />
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/A2Ui.Avalonia/A2Ui.Avalonia.csproj" />
    <ProjectReference Include="../../src/A2Ui.Core/A2Ui.Core.csproj" />
  </ItemGroup>
</Project>
```

Create `tests/A2Ui.Avalonia.Tests/TestApp.cs`:

```csharp
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(A2Ui.Avalonia.Tests.TestApp))]

namespace A2Ui.Avalonia.Tests;

/// <summary>
/// Headless Avalonia application for UI tests.
/// Required by Avalonia.Headless.XUnit.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
                  .UseHeadless(new AvaloniaHeadlessOptions
                  {
                      UseHeadlessDrawing = true,
                  });
}
```

Create `tests/A2Ui.Avalonia.Tests/Catalog/TextCatalogEntryTests.cs`:

```csharp
using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class TextCatalogEntryTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithLiteralText_SetsTextBlockText()
    {
        // Arrange
        var entry  = new TextCatalogEntry();
        var dm     = new DataModel();
        var ctx    = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id        = "t1",
            Component = "Text",
            Text      = BoundOrLiteral.Literal_("Hello World"),
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        control.Should().BeOfType<TextBlock>();
        ((TextBlock)control).Text.Should().Be("Hello World");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithH1Variant_HasHeading1Class()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm    = new DataModel();
        var ctx   = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id        = "h1",
            Component = "Text",
            Text      = BoundOrLiteral.Literal_("Title"),
            Variant   = "h1",
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        control.Classes.Should().Contain("Heading1");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Update_ChangesTextInPlace()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm    = new DataModel();
        var ctx   = new MockRenderContext(dm);
        var comp1 = new A2UiComponent { Id = "t1", Component = "Text",
                                         Text = BoundOrLiteral.Literal_("Before") };
        var comp2 = new A2UiComponent { Id = "t1", Component = "Text",
                                         Text = BoundOrLiteral.Literal_("After") };

        var control = entry.Create(comp1, dm, ctx);

        // Act
        bool updated = entry.Update(control, comp2, dm, ctx);

        // Assert
        updated.Should().BeTrue();
        ((TextBlock)control).Text.Should().Be("After");
    }
}
```

Create `tests/A2Ui.Avalonia.Tests/MockRenderContext.cs`:

```csharp
using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Tests;

internal sealed class MockRenderContext(DataModel dataModel) : IRenderContext
{
    public Control? RenderChild(string? childId) => null;
    public IEnumerable<Control> RenderChildren(string parentId) => [];
    public void FireUserAction(string surfaceId, string eventName, object? payload = null) { }
    public string? Resolve(BoundOrLiteral? bound) => dataModel.Resolve(bound);
}
```

---

## Phase 5 — Coverage Scripts

Create `scripts/test-coverage.sh`:

```bash
#!/bin/bash
set -euo pipefail

source /sandbox/.profile.d/dotnet.sh

REPO=/sandbox/develop/A2Ui
COVERAGE=$REPO/coverage

rm -rf "$COVERAGE"
mkdir -p "$COVERAGE"

echo "=== Running tests with coverage ==="
dotnet test "$REPO/A2Ui.sln" \
  --configuration Release \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE/raw" \
  --logger "trx;LogFileName=results.trx" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

echo "=== Generating coverage report ==="
reportgenerator \
  -reports:"$COVERAGE/raw/**/*.xml" \
  -targetdir:"$COVERAGE/report" \
  -reporttypes:"Html;Badges;TextSummary" \
  -classfilters:"-*.Tests.*"

echo "=== Coverage Summary ==="
cat "$COVERAGE/report/Summary.txt"
echo ""
echo "Full report: $COVERAGE/report/index.html"
```

```bash
chmod +x /sandbox/develop/A2Ui/scripts/test-coverage.sh
```

---

## Phase 6 — CSharpier Formatting

```bash
# Check formatting (CI mode)
dotnet csharpier --check /sandbox/develop/A2Ui/src/

# Auto-fix formatting
dotnet csharpier /sandbox/develop/A2Ui/src/
```

Add `.csharpierrc.json` to project root:

```json
{
  "printWidth": 120,
  "tabWidth": 4,
  "useTabs": false
}
```

---

## Phase 7 — .NET Tool Manifest

Create `dotnet-tools.json` for reproducible tool restore:

```bash
cd /sandbox/develop/A2Ui
dotnet new tool-manifest
dotnet tool install dotnet-format
dotnet tool install csharpier
dotnet tool install coverlet.console
dotnet tool install dotnet-reportgenerator-globaltool
```

This generates `.config/dotnet-tools.json` — commit it:

```bash
git add .config/dotnet-tools.json .csharpierrc.json scripts/test-coverage.sh \
        tests/ .editorconfig .globalconfig
git commit -m "chore(quality): configure code quality pipeline

- .editorconfig: C# style rules (file-scoped namespaces, var policy,
  null safety, naming conventions, IDE diagnostics)
- .globalconfig: Roslynator + .NET analyzer severities
- xUnit 2.9 tests: EventSerializationTests, DataModelTests
- Avalonia.Headless.XUnit: TextCatalogEntryTests with MockRenderContext
- scripts/test-coverage.sh: Coverlet + ReportGenerator
- .config/dotnet-tools.json: dotnet tool manifest
- .csharpierrc.json: CSharpier formatter config

All packages: MIT or Apache 2.0 licensed (March 2026)"
```