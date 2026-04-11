# Windows setup notes (fork-specific)

This file documents one-time preparatory steps that **every Windows contributor
to this fork** must run before the lit samples will start. It exists because
this fork is the first attempt to run the A2UI web samples on Windows, and
the npm dependency graph has at least one known Windows-hostile interaction
that the upstream `samples/client/lit/package-lock.json` does not address.

If you are on Linux or macOS, **none of this applies to you** — `npm run
demo:restaurant` (and the other `demo:*` scripts) will work without any of
the steps below.

---

## Fork intent: lit samples are reference material, not a maintenance target

Before the procedure, one important framing point that affects how you
should treat any lockfile divergence this workaround produces:

**This fork (`peterblazejewicz/A2UI`) exists to build the first .NET/C#
implementation of the A2UI protocol.** The lit samples live at
`samples/client/lit/` only as **reference implementations** — we read
them to understand how a real A2UI client is shaped, so we can port
their structure to Avalonia and the .NET SDK. We do **not** maintain
the lit samples, contribute fixes to them, or push changes under
`samples/client/lit/` back upstream. That workspace is effectively
read-only for this fork's purposes.

The practical implication: when the Windows workaround below regenerates
`samples/client/lit/package-lock.json`, **that regenerated file stays on
your local machine**. Do not commit it, do not push it. If `git status`
shows it as modified after you run the procedure, that is the expected
and permanent state for a Windows working copy of this fork. Treat it
the same way you would treat `.vscode/launch.json` tweaks or other
local-only workstation artifacts.

This is different from items 1 and 3 of [`TODO_UPSTREAM_PATCHES.md`](TODO_UPSTREAM_PATCHES.md),
which are genuine portability fixes we do intend to upstream because
they affect files we actively care about (the Python SDK and the
lit-samples build script that we run ourselves).

---

## Preparatory step — rebuild the lit samples `node_modules` on first clone

**Symptom you will see without this step:**

```
[SHELL] D:\develop\a2-ui\samples\client\lit\node_modules\rollup\dist\native.js:115
[SHELL]   throw new Error(
[SHELL]   ^
[SHELL]
[SHELL] Error: Cannot find module @rollup/rollup-win32-x64-msvc. npm has a bug related
[SHELL] to optional dependencies (https://github.com/npm/cli/issues/4828). Please try
[SHELL] `npm i` again after removing both package-lock.json and node_modules directory.
[SHELL] ❌ [serve] Service exited unexpectedly
```

The Python agent side (`[REST]`) is unaffected — this is purely a
Node/npm problem.

**What is happening:**

Rollup (pulled in transitively by Vite, used by `samples/client/lit/shell`
for its dev server) ships one native binary per platform as
`optionalDependencies` in its own `package.json`. The rollup versions
relevant to this repo (4.x) require the platform-specific binary
(`@rollup/rollup-win32-x64-msvc` on Windows x64) to be present at
`require` time — if it is missing, the Vite dev server process crashes
on startup before it can print anything.

npm has a long-standing bug ([`npm/cli#4828`](https://github.com/npm/cli/issues/4828))
where `package-lock.json` files generated on one platform silently omit
installation entries for other platforms' optional native binaries. The
lockfile committed in upstream `google/A2UI`'s `samples/client/lit/`
was generated on Linux, so it contains installation stanzas for
`@rollup/rollup-linux-x64-gnu` and `@rollup/rollup-linux-x64-musl` but
**not** for `@rollup/rollup-win32-x64-msvc`. When `npm install` runs on
Windows, it honors the lockfile strictly and skips the Windows binary
entirely, even though the binary is listed as an optional dep.

**The fix — exactly what the error message tells you to do:**

Delete `node_modules/` and `package-lock.json` inside
`samples/client/lit/`, then run `npm install`. This causes npm to
regenerate the lockfile on your Windows machine with installation
stanzas for `@rollup/rollup-win32-x64-msvc` and the rest of the
Windows-specific optional dep graph. The new lockfile is self-consistent
with your Windows `node_modules/`, so subsequent `npm install` runs
(including the one `demo:restaurant` invokes on every execution) no
longer prune the Windows binary as "extraneous".

In Git Bash / WSL / PowerShell:

```bash
cd samples/client/lit
rm -rf node_modules package-lock.json
npm install
```

In `cmd.exe`:

```cmd
cd samples\client\lit
rmdir /s /q node_modules
del package-lock.json
npm install
```

After the `npm install` completes, `npm run demo:restaurant` (and the
other `demo:*` scripts) will work correctly. The Vite dev server
starts, rollup's Windows native binary loads, and the crash does not
recur.

**Why the `--no-save` ad-hoc approach does NOT work:**

You may see web search results or AI assistants suggesting
`npm install --no-save @rollup/rollup-win32-x64-msvc@<version>`
as a lighter alternative. **Do not use this approach for this
workflow.** It installs the binary into `node_modules/` without
updating `package-lock.json`, so the binary is immediately pruned as
"extraneous" the next time npm reconciles `node_modules/` against the
lockfile. The `demo:*` scripts in `samples/client/lit/package.json`
all begin with `npm install` specifically so you get fresh deps on
every invocation, which means the `--no-save` binary is destroyed
*before* Vite ever gets to import it. The only approach that survives
the demo script's own `npm install` is the delete+reinstall above,
because it produces a self-consistent lockfile.

## IMPORTANT: do NOT commit the regenerated `package-lock.json`

The regenerated `samples/client/lit/package-lock.json` is a **local
client-side artifact**. As explained in the "Fork intent" section at
the top of this file, this fork does not maintain the lit samples, so
lockfile divergence from upstream is acceptable as long as it stays
on your machine.

- Never `git add` this file.
- Never include it in a commit intended to be pushed.
- If you accidentally stage it, unstage with
  `git restore --staged samples/client/lit/package-lock.json`.
- If you want a clean `git status`, you can either leave it modified
  indefinitely, or revert it with `git checkout -- samples/client/lit/package-lock.json`
  and re-run the delete+reinstall next time you need to use the demo
  (the regeneration is cheap — it is what `npm install` does anyway).

If you pull upstream changes that touch the lit samples and things
break again after a sync, repeat the delete+reinstall. That is the
sustainable Windows workflow for this workspace.

## How to verify it worked

```bash
cd samples/client/lit
npm run demo:restaurant
```

Look for output like:

```
[SHELL]   VITE v7.3.2  ready in 214 ms
[SHELL]   ➜  Local:   http://localhost:5173/
[REST] INFO:     Uvicorn running on http://localhost:10002 (Press CTRL+C to quit)
```

Both `[SHELL]` and `[REST]` should report ready-state URLs. If you only
see `[REST]` and then a `[SHELL] Error: Cannot find module
@rollup/rollup-win32-x64-msvc` stack trace, the delete+reinstall did
not take effect — verify that both `samples/client/lit/node_modules/`
and `samples/client/lit/package-lock.json` were actually removed
before the `npm install`.

---

## Related Windows-specific fixes already landed in this fork

Items 1 and 3 of [`TODO_UPSTREAM_PATCHES.md`](TODO_UPSTREAM_PATCHES.md)
document two other Windows portability bugs that have been fixed in this
fork's commits. Unlike the rollup binary workaround in this file,
those two are **real bugs in files we care about** and are intended to
be upstreamed:

- **Item 1** — `os.path.join` used to build URIs in
  `agent_sdks/python/src/a2ui/schema/validator.py`. Affects the Python
  restaurant agent on Windows. Fixed in commit `e889f287`. Still needs
  an upstream PR against `google/A2UI`.
- **Item 3** — bash-only `for` loop in `samples/client/lit/package.json`'s
  `build:renderer` script. Affects anyone running `npm run demo:*` on
  Windows with `cmd.exe` as the default npm script shell. Fixed in
  commit `cc467cdf`. Still needs an upstream PR.

Both fixes are already applied in this fork, so you do not need to do
anything manually — they are automatic as long as you are on the
`feature/restaurant-demo-shell` branch (or any branch containing those
commits). This file only documents the rollup regeneration procedure
because it is the one Windows-specific workaround whose result
intentionally stays in your local working copy.
