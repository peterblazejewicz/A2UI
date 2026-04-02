---
name: a2ui_git_workflow
description: Clone google/A2UI reference repo, scan structure, extract spec docs, init implementation git repo and feature branches.
---


---

## Purpose

Clone Google's A2UI repository into sandbox, create the implementation feature branch,
map the repository structure, and extract the spec documents and existing renderer
implementations as reference material for the .NET/C# port.

---

## Phase 1 — Clone A2UI Reference Repository

```bash
cd /sandbox/develop

# Clone A2UI (Apache 2.0 licensed)
git clone https://github.com/google/A2UI.git a2ui-reference
cd a2ui-reference

# Inspect top-level structure
ls -la
find . -name "*.md" | head -30
find . -name "*.json" | grep -i schema | head -20
find . -name "*.ts" | head -20
find . -name "*.proto" | head -10
```

---

## Phase 2 — Map the Repository Structure

Run this full scan and save output as project documentation:

```bash
cd /sandbox/develop/a2ui-reference

# Full tree (exclude node_modules, dist, build artifacts)
find . -not -path "*/node_modules/*" \
       -not -path "*/.git/*" \
       -not -path "*/dist/*" \
       -not -path "*/build/*" \
       -not -path "*/__pycache__/*" \
       | sort | head -200

# Save to project docs
find . -not -path "*/node_modules/*" -not -path "*/.git/*" \
       -not -path "*/dist/*" -not -path "*/build/*" \
       | sort > /sandbox/develop/A2Ui/docs/a2ui-reference-structure.txt
```

### Critical files to locate and read:

```bash
# 1. The A2UI v0.8 stable specification
find . -name "*.md" | xargs grep -l "v0.8" | head -5
find . -path "*/specification/*" | head -20

# 2. The JSON schema (component definitions)
find . -name "*.json" | xargs grep -l '"component"' 2>/dev/null | head -5
find . -name "basic_catalog.json" 2>/dev/null
find . -name "*.schema.json" 2>/dev/null | head -10

# 3. Existing renderers (Angular, Flutter, Lit, Markdown)
ls docs/renderers/ 2>/dev/null || find . -name "renderers.md"
find . -path "*/renderers/*" -not -path "*/node_modules/*" | head -30

# 4. Message reference (wire format details)
find . -name "messages.md" | head -5
find . -path "*/reference/*" | head -20

# 5. The Composer application (source we will port)
find . -name "composer" -type d
find . -path "*/composer/*" -name "*.ts" | head -20
find . -path "*/composer/*" -name "*.html" | head -10
```

---

## Phase 3 — Extract Specification Documents

```bash
mkdir -p /sandbox/develop/A2Ui/docs/a2ui-spec

# Copy spec docs to project reference
SPEC_SRC=/sandbox/develop/a2ui-reference/docs

# Try various known paths
for dir in specification spec; do
  if [ -d "$SPEC_SRC/$dir" ]; then
    cp -r "$SPEC_SRC/$dir/"*.md /sandbox/develop/A2Ui/docs/a2ui-spec/ 2>/dev/null && \
    echo "Copied from $dir"
  fi
done

# Copy component reference
find /sandbox/develop/a2ui-reference \
  -name "components.md" -o -name "messages.md" -o -name "*.schema.json" \
  | xargs -I{} cp {} /sandbox/develop/A2Ui/docs/a2ui-spec/ 2>/dev/null

ls /sandbox/develop/A2Ui/docs/a2ui-spec/
```

---

## Phase 4 — Study Existing Renderer Implementation

Read each existing renderer to understand catalog patterns before writing .NET code.

```bash
# Read Lit renderer (simplest — web components)
find /sandbox/develop/a2ui-reference -path "*/lit/*" \
  -name "*.ts" | head -10
# Read the key renderer file
RENDERER=$(find /sandbox/develop/a2ui-reference -path "*/lit/*" \
  -name "*renderer*" -o -name "*catalog*" 2>/dev/null | head -1)
[ -n "$RENDERER" ] && cat "$RENDERER"

# Read Flutter renderer (closest to Avalonia — native widget mapping)
find /sandbox/develop/a2ui-reference -path "*/flutter/*" \
  -name "*.dart" | head -10
FLUTTER_CATALOG=$(find /sandbox/develop/a2ui-reference -path "*/flutter/*" \
  -name "*catalog*" 2>/dev/null | head -1)
[ -n "$FLUTTER_CATALOG" ] && cat "$FLUTTER_CATALOG"

# Extract component type names from any TypeScript definitions
find /sandbox/develop/a2ui-reference -name "*.ts" \
  | xargs grep -h "component.*:" 2>/dev/null \
  | grep -v "//" | sort -u | head -50
```

Document findings in:

```bash
cat > /sandbox/develop/A2Ui/docs/a2ui-spec-notes.md << 'EOF'
# A2UI Spec Notes — Renderer Analysis

## Component Types (from reference implementation scan)
<!-- Fill in after running grep above -->

## Flutter → Avalonia Mapping Plan
| A2UI Type | Flutter Widget | Avalonia Control |
|-----------|---------------|-----------------|
| Text | Text | TextBlock |
| Button | ElevatedButton | Button |
| Column | Column | StackPanel (Vertical) |
| Row | Row | StackPanel (Horizontal) |
| TextField | TextField | TextBox |
| DateTimeInput | DatePicker | DatePicker |
| Card | Card | Border + BoxShadow |
| Image | Image | Image |
| Select | DropdownButton | ComboBox |
| Checkbox | Checkbox | CheckBox |
| Slider | Slider | Slider |
| Table | DataTable | DataGrid |
| Surface | Scaffold | UserControl |

## Wire Format Notes
- All messages are JSONL (one JSON object per line)
- `updateComponents` → flat array, string IDs, parent refs
- `updateDataModel` → RFC 6902 JSON Pointer paths for BoundValue
- `createSurface` → catalogId URL, surfaceId string
- `deleteSurface` → surfaceId string
- Events from client: `userAction` with `name` and `payload`

## Catalog Pattern
- Client registers typed factory functions keyed by component type string
- Agent can ONLY reference registered types (security boundary)
- Smart Wrappers pattern: register decorator that adds auth/sandbox
EOF
```

---

## Phase 5 — Initialize Implementation Repository

```bash
cd /sandbox/develop/A2Ui

# Initialize git for implementation
git init
git remote add a2ui-reference file:///sandbox/develop/a2ui-reference

# Create .gitignore
cat > .gitignore << 'EOF'
# .NET artifacts
bin/
obj/
*.user
*.suo
.vs/
.vscode/settings.json
TestResults/
coverage/
*.coverage
*.coveragexml

# NuGet
.nuget/
packages/
*.nupkg

# Build
publish/
artifacts/

# IDE
.idea/
*.DotSettings.user
.DS_Store
Thumbs.db

# Secrets
*.pfx
*.p12
appsettings.*.json
!appsettings.Development.json

# Generated protobuf
**/Protos/generated/
EOF

# Initial commit of scaffolding
git add .
git commit -m "chore: initial project scaffolding

- Solution structure with 4 library projects + 3 test projects
- Directory.Build.props: shared MSBuild properties
- Directory.Packages.props: central package management (CPM)
- .editorconfig: code style enforcement
- global.json: SDK version pinning (10.0.201)
- .gitignore

Projects:
  - AgUi.Protocol: AG-UI event types and transport
  - A2Ui.Core: A2UI message model and catalog
  - A2Ui.Avalonia: Avalonia renderer implementation
  - A2Ui.Avalonia.App: Composer port (MVVM desktop)

Refs: google/A2UI (Apache 2.0), AG-UI spec v0.0.47"
```

---

## Phase 6 — Feature Branch Strategy

```bash
# Main branches
git checkout -b develop          # integration branch
git checkout -b feature/agui-protocol          # AG-UI event types
git checkout develop
git checkout -b feature/a2ui-core-types        # A2UI message model
git checkout develop
git checkout -b feature/avalonia-catalog       # renderer catalog
git checkout develop
git checkout -b feature/composer-port          # Composer app port

# Working branch for first task
git checkout feature/agui-protocol

# Confirm
git branch -a
```

Branch flow:
```
main ← develop ← feature/agui-protocol
                ← feature/a2ui-core-types
                ← feature/avalonia-catalog
                ← feature/composer-port
```

---

## Phase 7 — Repository Scan Summary Report

After all scans, generate a summary:

```bash
cat > /sandbox/develop/A2Ui/docs/progress.md << 'EOF'
# Implementation Progress

## Status: BOOTSTRAPPING

### Completed
- [ ] Env check (skill 01)
- [ ] A2UI repo cloned and mapped (skill 02)
- [ ] AG-UI event types implemented (skill 03)
- [ ] A2UI core types implemented (skill 03)
- [ ] Avalonia catalog skeleton (skill 04)
- [ ] Code quality configured (skill 05)
- [ ] Composer port started (skill 06)

### A2UI Reference Findings
<!-- Fill after scan -->
- Spec version: v0.8 stable / v0.9 draft
- Catalog URL: https://a2ui.org/specification/v0_9/basic_catalog.json
- Component count: <!-- fill -->
- Existing renderers: Angular, Flutter, Lit, Markdown

### Key Decisions
- Transport: gRPC over Unix Domain Socket (primary), SSE (compat)
- State: In-process Channel<AgentEvent> for same-process agent
- MVVM: CommunityToolkit.Mvvm (Source Generators, no reflection)
- Testing: xUnit 2.9 + Avalonia.Headless.XUnit for UI tests
EOF
```