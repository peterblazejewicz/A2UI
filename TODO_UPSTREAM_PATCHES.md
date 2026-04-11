# TODO: Upstream patches to contribute back to `google/A2UI`

Collected issues found while working in this fork that should be filed as PRs /
issues against the upstream repo. Keep this file updated as more are found.

**Quick status (re-evaluated 2026-04-11 after rebase and verification via
GitHub/PyPI):**

- **Item 1 — NEEDS UPSTREAM PR.** Python SDK validator still uses
  `os.path.join` for sibling URI construction, breaking Windows. Verified
  against upstream `main`; PRs #1084 and #1091 did not address it. My fix
  is rebased onto the post-#1091 flattened layout and is the only source
  of the `urljoin` correction.
- **Item 2 — RESOLVED.** PR #1084 (`RELAXED_PATH_PATTERN`) shipped in
  `a2ui-agent-sdk` 0.2.0/0.2.1 on PyPI, and `restaurant_finder` also
  switched to an editable install of the vendored source. Archived below.
- **Item 3 — NEEDS UPSTREAM PR.** `samples/client/lit/package.json`
  `build:renderer` script uses a bash-only `for` loop that breaks every
  Windows contributor. Fixed locally; upstream untouched.
- **Item 4 — KNOWN LIMITATION, NOT UPSTREAM-FIXABLE.** Windows
  `@rollup/rollup-win32-x64-msvc` native binary missing due to npm bug
  [`npm/cli#4828`](https://github.com/npm/cli/issues/4828). Workaround
  documented in [`WINDOWS_SETUP.md`](WINDOWS_SETUP.md) — the regenerated
  lockfile **stays local** since this fork treats `samples/client/lit/`
  as read-only reference material for the .NET/C# port.

---

## 1. Python SDK — `os.path.join` used to build URIs (Windows-breaking)

**Status:** Fixed locally in this fork at `agent_sdks/python/src/a2ui/schema/validator.py`.
**Upstream main still has the bug** (verified 2026-04-11 against
`https://raw.githubusercontent.com/google/A2UI/main/agent_sdks/python/src/a2ui/schema/validator.py`
— both `_build_0_8_validator` and `_build_0_9_validator` still use
`os.path.join` for sibling URI construction). So this remains a real
upstream PR target; PRs #1084 and #1091 did not address it.

The old `.venv/Lib/site-packages/a2ui/core/schema/validator.py` in-place
patch that was previously listed here is now obsolete:
`samples/agent/adk/restaurant_finder/pyproject.toml` pins
`a2ui-agent-sdk` as `editable = true` pointing at
`../../../../agent_sdks/python`, so `uv sync` rebuilds the venv directly
against the vendored source where my fix lives. No manual patching needed
in the venv going forward — just `uv sync` after any source change.

**Note on path:** upstream commit `f9b732af` (PR #1091, "refactor: flatten core
directory structure and relocate a2a parts logic") moved the source from
`a2ui/core/schema/` to `a2ui/schema/`. The fix commit in this fork was rebased
onto the new layout. The installed PyPI 0.1.1 copy still uses the old
`a2ui/core/schema/` layout because it pre-dates #1091, so the venv-patch path
intentionally retains `core/`.

**Package affected:** `a2ui-agent-sdk` (PyPI 0.1.1) — same source lives at
`agent_sdks/python/src/a2ui/schema/validator.py`.

**Symptom:** On Windows, starting the Python restaurant agent
(`samples/agent/adk/restaurant_finder`) fails during example validation with:

```
ERROR:__main__:An error occurred during server startup:
Failed to validate example examples/0.9\booking_form.json:
Unresolvable: catalog.json#/$defs/theme
```

Linux/macOS users don't hit this — it is purely a Windows path-separator bug.

**Root cause:** `_build_0_8_validator` and `_build_0_9_validator` use
`os.path.join` to build sibling **URIs** for the `referencing` registry:

```python
def get_sibling_uri(uri, filename):
    return os.path.join(os.path.dirname(uri), filename)

catalog_uri = get_sibling_uri(base_uri, "catalog.json")
```

`os.path.join` uses the platform separator. On Windows it produces
`https://a2ui.org/specification/v0_9\catalog.json` (backslash). The
`referencing` library then resolves relative `$ref`s like
`"catalog.json#/$defs/theme"` against the parent schema's `$id` using
forward slashes per RFC 3986, getting
`https://a2ui.org/specification/v0_9/catalog.json` — which never matches the
backslash-containing key registered in the registry, so lookup fails with
`Unresolvable: catalog.json#/$defs/theme`.

The plain-relative fallback key `"catalog.json"` registered on
validator.py:241 does not help, because `referencing` resolves the relative
`$ref` against the absolute base URI *before* looking up the registry — it
only ever queries the fully-resolved absolute URI.

**Fix (applied to this fork):** replace `os.path.join`-based URI construction
with `urllib.parse.urljoin`, which correctly handles URIs with forward slashes
on every platform. Both the v0.8 and v0.9 builders are affected and both were
patched. The unused `import os` inside those methods was removed in the
process.

**Verification:** After the fix, `A2uiSchemaManager.generate_system_prompt(
include_examples=True, validate_examples=True)` no longer raises on
`booking_form.json`. (A separate, unrelated bug surfaces next — see item 2
below.)

**Upstream PR sketch:**
- File: `agent_sdks/python/src/a2ui/schema/validator.py` (post-#1091 layout)
- Lines touched: ~171–176 (v0.8) and ~212–220 (v0.9)
- Also add a Windows CI job or at least a unit test that builds a v0.9
  validator and resolves the `catalog.json#/$defs/theme` ref — the test
  would have caught this on any platform, since the broken URI can be
  observed directly in the registry keys.

---

## 2. Python SDK — strict JSON-Pointer check rejects relative `path` fields ✅ RESOLVED

**Status:** RESOLVED 2026-04-11. No further upstream action needed; kept
in this file only so the item number does not renumber.

**Summary of closure:**
- Upstream PR #1084 introduced `RELAXED_PATH_PATTERN` (accepts both absolute
  RFC 6901 pointers AND relative paths inside `List` template bindings).
  Released as `a2ui-agent-sdk` 0.2.0 (2026-04-08) and 0.2.1 (2026-04-10) on PyPI.
- `samples/agent/adk/restaurant_finder/pyproject.toml` independently
  migrated to an editable install of the vendored source, so the
  restaurant_finder venv now consumes `agent_sdks/python/` directly
  and doesn't depend on a PyPI release at all.

**Cleanup still owed** per workspace: discard any old in-place patch at
`.venv/Lib/site-packages/a2ui/core/schema/validator.py` and rebuild the
venv with `uv sync`. The installed package now lives at
`a2ui/schema/validator.py` (post-#1091 flat layout).

Historical context (root cause, symptom, regex diff) is preserved in
git history — see the commit that introduced this entry, plus
`0b4352eb` in `google/A2UI` for the upstream fix.

---

## 3. Lit samples — `build:renderer` script uses bash-only loop (Windows-breaking)

**Status:** Fixed locally in this fork at `samples/client/lit/package.json`.
Needs an upstream PR; affects any Windows contributor who tries to run the
lit demos (`npm run demo:restaurant`, `demo:contact`, `demo:orchestrator`,
`demo:gallery09`), which is the documented entry point for the reference
samples.

**Package affected:** `@a2ui/lit-samples` at `samples/client/lit/package.json`,
the `build:renderer` script.

**Symptom:** On Windows (where `npm run` uses `cmd.exe` by default), running
any of the `demo:*` scripts fails almost immediately with:

```
> @a2ui/lit-samples@0.8.1 build:renderer
> cd ../../../renderers && for dir in 'web_core' 'markdown/markdown-it' 'lit'; do (cd "$dir" && npm install && npm run build); done

dir was unexpected at this time.
```

No renderer is built, so the subsequent `concurrently` call launching the
Shell fails at module-resolution time even if the user tries to run it
manually.

**Root cause:** The original script relies on three bash-only constructs
that `cmd.exe` cannot parse:

1. `for dir in 'a' 'b' 'c'; do ... done` — cmd.exe's `for` grammar is
   `for %%i in (...) do ...`; it chokes on the unquoted `dir`, which is
   also the name of a built-in, hence the `"dir was unexpected at this
   time."` error.
2. Subshell grouping `(cd "$dir" && ...)` — cmd.exe treats parentheses
   as command grouping with different semantics, and does not fork a
   sub-process for `cd`.
3. The `$dir` variable reference — cmd.exe uses `%dir%`.

Linux/macOS users don't hit this; they're only running the script because
`npm run` picks `sh` on POSIX systems.

**Fix (applied to this fork):** unroll the loop into six `&&`-chained
commands using `npm --prefix <path>` instead of `cd`. The new script runs
identically under `cmd.exe`, PowerShell, bash, and zsh because `&&`
short-circuit sequencing is the one operator that has matching semantics
across all four shells, and `npm --prefix` is interpreted by Node itself
rather than by any shell:

```json
"build:renderer": "npm --prefix ../../../renderers/web_core install && npm --prefix ../../../renderers/web_core run build && npm --prefix ../../../renderers/markdown/markdown-it install && npm --prefix ../../../renderers/markdown/markdown-it run build && npm --prefix ../../../renderers/lit install && npm --prefix ../../../renderers/lit run build"
```

The build order (`web_core` → `markdown-it` → `lit`) is preserved, which
matters because `markdown-it` depends on `@a2ui/web_core` via a `file:`
protocol dev dep, and `lit` depends on both.

**Verification:** After the fix, `cd samples/client/lit && npm run
build:renderer` on Windows 11 with `cmd.exe` as the default npm script shell
parses correctly and builds `web_core` via wireit with
`✅ Ran 2 scripts and skipped 0`, then proceeds to `markdown-it` and `lit`.
The preceding shell parse error no longer occurs.

**Upstream PR sketch:**
- File: `samples/client/lit/package.json`
- Line touched: the `build:renderer` script (one line).
- Optional but ideal follow-up: audit the other scripts in the same file —
  `serve:agent:*`, `serve:shell`, `serve:gallery09` — they use bare `cd`
  which happens to work in both shells, so those are fine. No other bash-
  specific syntax in the file.
- Also worth auditing sibling sample package manifests
  (`samples/client/angular/**/package.json`, any other `samples/**`) for the
  same anti-pattern before opening the PR, so the fix covers the whole repo
  in one pass.

---

## 4. Lit samples — npm optional-dependency bug breaks Windows rollup install

**Status:** KNOWN LIMITATION, NOT UPSTREAM-FIXABLE in this repo. Workaround
documented in [`WINDOWS_SETUP.md`](WINDOWS_SETUP.md) as a one-time manual
step for every Windows contributor. This fork will NOT patch `package.json`
or `package-lock.json` to work around it, because the root cause is a bug
in npm itself, not in the A2UI dependency declarations.

**Package affected:** `@a2ui/lit-samples` workspace, specifically the
transitive path `@a2ui/custom-components-example` → `vite@7.3.1` →
`rollup@4.60.0` → `@rollup/rollup-win32-x64-msvc@4.60.0` (platform binary
listed as `optionalDependency` in rollup's own `package.json`, not ours).

**Symptom:** On Windows, running `npm run demo:restaurant` (or any
`demo:*` script) progresses successfully through `npm install`,
`build:renderer`, and the start of the `concurrently`-launched shell
process, then crashes at `wireit → vite → rollup/dist/native.js` on its
very first `require`:

```
[SHELL] D:\develop\a2-ui\samples\client\lit\node_modules\rollup\dist\native.js:115
[SHELL]   throw new Error(
[SHELL]
[SHELL] Error: Cannot find module @rollup/rollup-win32-x64-msvc. npm has a bug
[SHELL] related to optional dependencies (https://github.com/npm/cli/issues/4828).
[SHELL] Please try `npm i` again after removing both package-lock.json and
[SHELL] node_modules directory.
[SHELL] ❌ [serve] Service exited unexpectedly
```

The Python agent side (`[REST]`) is unaffected and starts fine — this is
purely an npm/node side-effect.

**Root cause:** Rollup 4.x distributes its native bindings as one
platform-specific npm package per OS/arch combination
(`@rollup/rollup-win32-x64-msvc`, `@rollup/rollup-linux-x64-gnu`,
`@rollup/rollup-darwin-arm64`, etc.) listed as `optionalDependencies` in
rollup's own `package.json`. At runtime, `rollup/dist/native.js` does a
`require('@rollup/rollup-' + platform + '-' + arch + variant)` and throws
if the matching package is missing.

`samples/client/lit/package-lock.json` was generated on a Linux machine
by upstream CI. Inspection of the lockfile confirms:

- Line 7641–7644: all platform-specific rollup binaries ARE declared as
  optional deps of the `rollup` package — including
  `@rollup/rollup-win32-x64-msvc@4.60.0`.
- Line 6035 and 6047: installation stanzas exist ONLY for
  `node_modules/@rollup/rollup-linux-x64-gnu` and
  `node_modules/@rollup/rollup-linux-x64-musl`. The Windows, macOS, and
  BSD stanzas are missing entirely.

This is the exact fingerprint of [`npm/cli#4828`](https://github.com/npm/cli/issues/4828):
npm's lockfile generator, when it encounters an `optionalDependency`
whose platform does not match the generating machine's platform, records
the dep under the parent package's `optionalDependencies` block but
does NOT create a `node_modules/` installation entry for it. When `npm
install` later runs on a different platform, it honors the lockfile
strictly and skips the install, leaving `rollup/dist/native.js` unable
to `require` its own binary.

**Why this is NOT fixable in this fork's `package.json` or lockfile
(and why we do not care):**

The deeper reason is scope, not philosophy: **this fork treats
`samples/client/lit/` as reference material for the .NET/C# port, not
as a workspace we maintain.** We read those samples to understand how
a real A2UI client is shaped so we can port their structure to
Avalonia and the .NET SDK; we do not ship fixes back to them, nor do
we track their dep graph over time. That workspace is effectively
read-only for this fork's purposes.

That framing changes how we evaluate candidate fixes. Every option
below was considered and rejected:

1. **Pin `rollup` or `@rollup/rollup-win32-x64-msvc` in our own
   `devDependencies`.** Would work for the current version but defers
   the problem to every rollup bump. Also adds a maintenance burden on
   every upstream sync. Worst: would conflict with vite's own rollup
   version pin. And since we do not maintain the lit workspace,
   committing to maintain a dep override here is exactly backwards.
2. **Commit a Windows-regenerated `package-lock.json` back to the fork
   branch.** Would fix Windows globally but break Linux CI for the
   same reason in reverse — the bug is symmetric. And even if we only
   committed it to the feature branch, any upstream sync would conflict
   on that file indefinitely.
3. **Switch from npm to pnpm.** pnpm handles platform-specific optional
   deps correctly. But this is a cross-cutting change affecting every
   other workspace in the repo and is not justifiable to push upstream
   just for Windows support in one sample directory we do not maintain.
4. **Platform-scoped `ensure:native-rollup` npm script prepended to
   each `demo:*` script.** We considered an inline `node -e "if
   (process.platform==='win32') require('child_process').execSync(...)"`
   that would self-heal the binary after every `npm install`. It would
   work, but it means shipping platform-detection logic in the same
   `package.json` that items 1 and 3 are trying to keep minimal and
   upstreamable. Adds surface area to a file we only want to touch for
   the shell portability fix.
5. **Pre-commit hook that warns Windows users to run a manual step.**
   Adds friction on the wrong side — the hook would fire on every
   commit, not just first-time clones.
6. **`--no-save` ad-hoc install of the missing binary.** This was our
   first documented approach and it **does not work** for this
   workflow. The `demo:*` scripts all begin with `npm install`, which
   reconciles `node_modules/` against `package-lock.json` and prunes
   anything "extraneous". A `--no-save` binary has no lockfile entry,
   so it is pruned on the very next demo invocation before Vite ever
   gets to import it. Verified empirically: the crash came back with
   `removed 1 package, and audited 715 packages` at the top of the
   subsequent `demo:restaurant` run. Keeping this as a cautionary tale
   in the workaround section — do not trust a `--no-save` workaround
   for anything downstream of an `npm install`.

**Workaround (from `WINDOWS_SETUP.md`) — delete and reinstall:**

Exactly what the rollup error message tells you to do:

```bash
cd samples/client/lit
rm -rf node_modules package-lock.json
npm install
```

(cmd.exe: `rmdir /s /q node_modules && del package-lock.json && npm install`.)

This regenerates `package-lock.json` on your Windows machine with
installation stanzas for `@rollup/rollup-win32-x64-msvc` and the rest
of the Windows-specific optional dep graph. The new lockfile is
**self-consistent** with your Windows `node_modules/`, so subsequent
`npm install` runs (including the ones `demo:*` scripts invoke) no
longer prune the Windows binary as extraneous. Verified working
locally with the expected `[SHELL] VITE v7.3.2 ready in 214 ms`
output and `Uvicorn running on http://localhost:10002` on the agent
side.

**The regenerated `samples/client/lit/package-lock.json` stays on your
local machine.** Do not commit it, do not push it, do not include it in
upstream PRs. This is acceptable precisely because the fork does not
maintain the lit workspace — the divergence is intentional,
client-side, and has zero cost to us. If `git status` shows the file
as modified after the workaround, that is the expected state for a
Windows working copy; leave it modified or revert with
`git checkout -- samples/client/lit/package-lock.json` and re-run the
workaround next time you need the demo.

**Upstream escape hatches to watch for:**

- npm may eventually fix [`npm/cli#4828`](https://github.com/npm/cli/issues/4828)
  — then this section can be deleted and Windows users can clone and
  run without any preparation.
- Rollup may drop the native-binary dispatch in favor of pure-JS (it has
  been discussed but is a major architectural shift) — same outcome.
- Vite may switch away from rollup (also discussed for their Rolldown
  migration, tracked at `vitejs/vite` Rolldown RFC) — same outcome.

Until one of those lands, every new Windows contributor who wants to
run the lit demos must do the delete+reinstall once per clone (and
again any time they pull upstream changes that re-introduce the
upstream-sourced lockfile over their local regeneration).

---

## Notes

- **Items 1 and 3 are real PR targets for upstream.** Item 1 is a Python SDK
  Windows path-separator bug; item 3 is a lit-samples Windows shell-syntax
  bug. Both are pure portability fixes that unblock Windows contributors with
  no behavior change on POSIX systems.
- **Item 2 is RESOLVED** (see item 2 status block above). The PyPI release
  0.2.0/0.2.1 and the editable-install migration in
  `restaurant_finder/pyproject.toml` independently both close it. Only local
  cleanup remains: discard any stale `.venv/Lib/site-packages/a2ui/core/schema/`
  patch and `uv sync`.
- **A single upstream PR could bundle item 1 with a CI addition** — e.g., a
  Windows job in the Python SDK workflow, or a unit test that constructs the
  v0.9 validator and inspects the registered URI keys for backslashes. Such a
  test would have caught item 1 on any platform because the broken URI can be
  observed directly in the registry.
- The .NET SDK/Avalonia renderer in this fork is unaffected; items 1 and 2 are
  Python reference SDK bugs, item 3 is a lit samples build-script bug.
