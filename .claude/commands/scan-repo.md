All spec files are already in this repo under `specification/`. No external clone needed.

**1. Read the authoritative spec**
```bash
cat specification/v0_9/docs/a2ui_protocol.md
```

**2. Read the component catalog**
```bash
cat specification/v0_9/json/basic_catalog.json
```
Extract all component type names — these are the exact strings the C# catalog must support.

**3. Read both wire format schemas**
```bash
cat specification/v0_9/json/server_to_client.json
cat specification/v0_9/json/client_to_server.json
```

**4. Scan existing renderers for catalog patterns**
```bash
# Angular renderer — most structurally similar to C#
find renderers/angular/src -name "*.ts" | head -20

# Lit renderer — simplest implementation
find renderers/lit/src -name "*.ts" | head -20
```

**5. Scan the Python SDK for message model reference**
```bash
find agent_sdks/python/src -name "*.py" | head -20
```

**6. Scan the Composer tool (source for Avalonia port)**
```bash
find tools/composer -not -path "*/node_modules/*" | head -30
```

**7. Check v0.10 for forward-compatibility hints**
```bash
ls specification/v0_10/docs/ 2>/dev/null || echo "v0_10 not present yet"
```

After all scans, write a summary to `agent_sdks/dotnet/docs/spec-notes.md` with:
- Component type inventory (from basic_catalog.json)
- Angular/Lit → Avalonia control mapping table
- Wire format notes
- Composer feature list for the port