# Windows setup notes (fork-specific)

One-time steps for Windows contributors before the lit samples will start.
Linux/macOS users can ignore this file — `npm run demo:restaurant` works
out of the box.

---

## Fork intent: lit samples are reference material

This fork exists to build the .NET/C# A2UI implementation. The lit samples
at `samples/client/lit/` are **read-only reference** — we read them to
understand client structure, but do not maintain or push changes to them.
When the workaround below regenerates `package-lock.json`, **do not commit
that file**. Treat it as a local workstation artifact.

---

## Preparatory step — rebuild `node_modules` on first clone

**Symptom without this step:**

```
Error: Cannot find module @rollup/rollup-win32-x64-msvc. npm has a bug
related to optional dependencies (https://github.com/npm/cli/issues/4828).
```

The upstream lockfile was generated on Linux and omits Windows-specific
optional binaries. npm honors the lockfile strictly and skips them.

**Fix — delete and reinstall:**

```bash
cd samples/client/lit
rm -rf node_modules package-lock.json
npm install
```

This regenerates the lockfile with Windows-native stanzas. Subsequent
`npm install` runs (including those inside `demo:*` scripts) will keep
the Windows binary in place.

> **Note:** `npm install --no-save @rollup/rollup-win32-x64-msvc` does
> NOT work — the binary is pruned as "extraneous" on the next `npm install`
> that the demo scripts run automatically.

**Do NOT commit the regenerated `package-lock.json`.** If you accidentally
stage it: `git restore --staged samples/client/lit/package-lock.json`.

---

## Verify it worked

```bash
cd samples/client/lit
npm run demo:restaurant
```

Both `[SHELL]` (Vite) and `[REST]` (Uvicorn) should report ready-state
URLs. If `[SHELL]` crashes with the `@rollup/rollup-win32-x64-msvc`
error, verify that both `node_modules/` and `package-lock.json` were
actually removed before reinstalling.
