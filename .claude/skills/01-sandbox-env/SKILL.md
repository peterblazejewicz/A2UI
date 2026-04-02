---
name: a2ui_sandbox_env
description: Verify and configure the DGX Spark environment for .NET 10 development: PATH, NuGet, global tools, solution scaffold.
---


---

## Purpose

Verify and configure the `spark-dev` sandbox for .NET 10 development.  
The sandbox runs on DGX Spark (aarch64, Ubuntu 24.04, `$HOME=/sandbox`).  
All commands use `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` because the sandbox
lacks full ICU libraries.

---

## Phase 1 — Baseline Verification

Run each command and confirm output before proceeding.

```bash
# .NET SDK
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT=/sandbox/.dotnet
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
# Expected: 10.0.201 (or later 10.x)

dotnet --list-sdks
dotnet --list-runtimes

# Git
git --version
git config --global user.name "Volta DGX Agent"
git config --global user.email "volta@spark-one.local"
git config --global init.defaultBranch main
git config --global core.autocrlf false

# Node / npm (for A2UI tooling, spec generation)
node --version          # Expected: v24.x
npm --version

# Python (for uv/A2UI reference agent)
python3 --version       # 3.10+

# Docker GPU passthrough
docker run --rm --gpus all ubuntu:24.04 nvidia-smi | head -5
```

---

## Phase 2 — Persistent Environment Profile

Create `/sandbox/.profile.d/dotnet.sh` for reproducible sessions:

```bash
cat > /sandbox/.profile.d/dotnet.sh << 'EOF'
# .NET 10 — required for all dotnet commands in this sandbox
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT=/sandbox/.dotnet
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
# nuget cache inside sandbox (not /root)
export NUGET_PACKAGES=/sandbox/.nuget/packages
export NUGET_HTTP_CACHE_PATH=/sandbox/.nuget/http-cache
EOF
chmod +x /sandbox/.profile.d/dotnet.sh
source /sandbox/.profile.d/dotnet.sh
```

---

## Phase 3 — Global .NET Tools

Install tools required by the project. Each tool must be MIT or Apache 2.0 licensed.

```bash
# Restore tools from manifest (after project setup) — preferred
dotnet tool restore

# Or install globally now
dotnet tool install --global dotnet-script        # MIT — C# scripting
dotnet tool install --global dotnet-format        # MIT — code formatting
dotnet tool install --global coverlet.console     # MIT — code coverage
dotnet tool install --global dotnet-reportgenerator  # Apache 2.0 — coverage reports
dotnet tool install --global dotnet-outdated      # MIT — NuGet audit
dotnet tool install --global csharpier            # Apache 2.0 — opinionated formatter

# Verify
dotnet tool list --global
```

---

## Phase 4 — NuGet Feed Configuration

```bash
mkdir -p /sandbox/.nuget
cat > /sandbox/.nuget/NuGet.Config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json"
         protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
  <config>
    <add key="globalPackagesFolder" value="/sandbox/.nuget/packages" />
    <add key="repositoryPath" value="/sandbox/.nuget/packages" />
  </config>
</configuration>
EOF
```

---

## Phase 5 — Workspace Directory Structure

```bash
mkdir -p /sandbox/develop/{A2Ui,scratch,tools}
mkdir -p /sandbox/develop/A2Ui/{src,tests,docs,scripts}

# Verify structure
find /sandbox/develop -type d | head -20
```

Expected layout after full project init:

```
/sandbox/develop/A2Ui/
├── src/
│   ├── AgUi.Protocol/          # AG-UI event types + SSE/gRPC transport
│   ├── A2Ui.Core/              # A2UI message model + catalog
│   ├── A2Ui.Avalonia/          # Avalonia renderer + control catalog
│   └── A2Ui.Avalonia.App/      # Composer port — MVVM desktop app
├── tests/
│   ├── AgUi.Protocol.Tests/
│   ├── A2Ui.Core.Tests/
│   ├── A2Ui.Avalonia.Tests/    # headless Avalonia tests
│   └── A2Ui.Integration.Tests/
├── docs/
│   ├── architecture.md
│   ├── a2ui-spec-notes.md
│   └── progress.md
├── scripts/
│   ├── build.sh
│   └── test-coverage.sh
├── A2Ui.sln
├── .editorconfig
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
└── .gitignore
```

---

## Phase 6 — Solution Scaffold

```bash
cd /sandbox/develop/A2Ui

# Pin SDK version
cat > global.json << 'EOF'
{
  "sdk": {
    "version": "10.0.201",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
EOF

# Create solution
dotnet new sln -n A2Ui

# Create library projects
dotnet new classlib -n AgUi.Protocol      -o src/AgUi.Protocol      --framework net10.0
dotnet new classlib -n A2Ui.Core          -o src/A2Ui.Core          --framework net10.0
dotnet new classlib -n A2Ui.Avalonia      -o src/A2Ui.Avalonia      --framework net10.0

# Create test projects
dotnet new xunit -n AgUi.Protocol.Tests     -o tests/AgUi.Protocol.Tests     --framework net10.0
dotnet new xunit -n A2Ui.Core.Tests         -o tests/A2Ui.Core.Tests         --framework net10.0
dotnet new xunit -n A2Ui.Avalonia.Tests     -o tests/A2Ui.Avalonia.Tests     --framework net10.0

# Add all to solution
dotnet sln add src/AgUi.Protocol/AgUi.Protocol.csproj
dotnet sln add src/A2Ui.Core/A2Ui.Core.csproj
dotnet sln add src/A2Ui.Avalonia/A2Ui.Avalonia.csproj
dotnet sln add tests/AgUi.Protocol.Tests/AgUi.Protocol.Tests.csproj
dotnet sln add tests/A2Ui.Core.Tests/A2Ui.Core.Tests.csproj
dotnet sln add tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj

# Verify
dotnet sln list
dotnet build
```

---

## Phase 7 — Directory.Build.props (shared MSBuild properties)

```bash
cat > /sandbox/develop/A2Ui/Directory.Build.props << 'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <WarningLevel>9999</WarningLevel>
    <!-- Suppress doc warnings for test projects (overridden per project) -->
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <!-- Deterministic builds -->
    <Deterministic>true</Deterministic>
    <DotnetSystemGlobalizationInvariant>true</DotnetSystemGlobalizationInvariant>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <Optimize>false</Optimize>
    <DebugType>full</DebugType>
    <DebugSymbols>true</DebugSymbols>
  </PropertyGroup>
</Project>
EOF
```

---

## Phase 8 — Directory.Packages.props (Central Package Management)

```bash
cat > /sandbox/develop/A2Ui/Directory.Packages.props << 'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup Label="Core">
    <PackageVersion Include="System.Text.Json"                  Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.AI"           Version="10.2.0" />
    <PackageVersion Include="Microsoft.Extensions.AI.OpenAI"    Version="10.2.0" />
    <PackageVersion Include="Grpc.AspNetCore"                   Version="2.67.0" />
    <PackageVersion Include="Google.Protobuf"                   Version="3.29.3" />
    <PackageVersion Include="Grpc.Tools"                        Version="2.67.0" />
    <PackageVersion Include="Microsoft.Json.Patch"              Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Avalonia">
    <PackageVersion Include="Avalonia"                          Version="11.2.5" />
    <PackageVersion Include="Avalonia.Desktop"                  Version="11.2.5" />
    <PackageVersion Include="Avalonia.Themes.Fluent"            Version="11.2.5" />
    <PackageVersion Include="Avalonia.ReactiveUI"               Version="11.2.5" />
    <PackageVersion Include="Avalonia.Headless.XUnit"           Version="11.2.5" />
    <PackageVersion Include="CommunityToolkit.Mvvm"             Version="8.4.0" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="xunit"                             Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio"         Version="2.8.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk"            Version="17.12.0" />
    <PackageVersion Include="coverlet.collector"                Version="6.0.4" />
    <PackageVersion Include="FluentAssertions"                  Version="6.12.2" />
    <PackageVersion Include="NSubstitute"                       Version="5.3.0" />
    <PackageVersion Include="Bogus"                             Version="35.6.1" />
  </ItemGroup>
  <ItemGroup Label="Analyzers">
    <PackageVersion Include="Roslynator.Analyzers"              Version="4.12.10" />
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="10.0.0" />
  </ItemGroup>
</Project>
EOF
```

> All packages above are MIT or Apache 2.0 licensed as of March 2026.  
> Run `dotnet outdated` periodically to audit for updates.

---

## Verification Checklist

After completing all phases:

```bash
cd /sandbox/develop/A2Ui
dotnet build --configuration Release
# Expected: Build succeeded. 0 Error(s)

dotnet test --no-build --configuration Release
# Expected: All tests pass (initial scaffolding has 0 tests — that's fine)

echo "ENV CHECK COMPLETE ✅"
```