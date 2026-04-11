# TODO: Upstream patches to contribute back to `google/A2UI`

Collected issues found while working in this fork that should be filed as PRs /
issues against the upstream repo. Keep this file updated as more are found.

**Quick status (2026-04-11):**
- Item 1 — needs an upstream PR. Fixed locally in `agent_sdks/python/src/...`
  and in the restaurant-finder venv.
- Item 2 — already fixed upstream in commit `0b4352eb` (PR #1084), just not
  yet released to PyPI. Needs a new `a2ui-agent-sdk` version cut. Venv was
  patched in-place to backport the fix.
- Item 3 — needs an upstream PR. Fixed locally in
  `samples/client/lit/package.json`. Bash-only `for` loop in `build:renderer`
  broke every Windows contributor trying to run the lit demos.

---

## 1. Python SDK — `os.path.join` used to build URIs (Windows-breaking)

**Status:** Fixed locally in this fork at `agent_sdks/python/src/a2ui/core/schema/validator.py`.
Installed copy at `.venv/Lib/site-packages/a2ui/core/schema/validator.py` was
patched in-place as a temporary unblock; a clean reinstall from source or a
published fix on PyPI will supersede it.

**Package affected:** `a2ui-agent-sdk` (PyPI 0.1.1) — same source lives at
`agent_sdks/python/src/a2ui/core/schema/validator.py`.

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
- File: `agent_sdks/python/src/a2ui/core/schema/validator.py`
- Lines touched: ~173–178 (v0.8) and ~215–222 (v0.9)
- Also add a Windows CI job or at least a unit test that builds a v0.9
  validator and resolves the `catalog.json#/$defs/theme` ref — the test
  would have caught this on any platform, since the broken URI can be
  observed directly in the registry keys.

---

## 2. Python SDK — strict JSON-Pointer check rejects relative `path` fields

**Status:** ALREADY FIXED UPSTREAM in this fork's git history at commit
`0b4352eb` (PR #1084, "A2UI v0.9 Path Resolution and Agent Updates", merged
2026-04-08). The vendored source at
`agent_sdks/python/src/a2ui/core/schema/validator.py` carries the fix. The
**PyPI release `a2ui-agent-sdk==0.1.1` pre-dates this commit**, so the
pip/uv-installed copy is stale and still hits the bug. The installed copy
inside `samples/agent/adk/.venv` was patched in-place with the same 5-line
delta as a temporary unblock; a clean venv rebuild pulling from PyPI would
re-introduce the bug until a new release is cut.

**Therefore the upstream ask for item 2 is not a code PR but a release:**
cut a new `a2ui-agent-sdk` PyPI version that includes commit `0b4352eb`.

**Package affected:** `a2ui-agent-sdk` (PyPI 0.1.1), file
`a2ui/core/schema/validator.py`, function `_validate_recursion_and_paths`
and module-level `JSON_POINTER_PATTERN`.

**Symptom:** With item 1 fixed, validating
`samples/agent/adk/restaurant_finder/examples/0.9/single_column_list.json`
raises:

```
ValueError: Failed to validate example examples/0.9\single_column_list.json:
Invalid JSON Pointer syntax: 'imageUrl'
```

**Root cause:** the 0.1.1 validator walks the entire A2UI message tree and,
for every dict with a `"path"` key whose value is a string, asserts the
string matches the RFC 6901 absolute-JSON-Pointer regex
(`^(?:\/(?:[^~\/]|~[01])*)*$` — must start with `/` or be empty).

But v0.9 `List` components use *relative* data paths inside their `List.data`
template (e.g. `"path": "imageUrl"`, `"path": "name"`, `"path": "rating"`),
because inside a template binding the root is each item of the bound array,
not the surface data model root. A leading `/` would actually be semantically
incorrect there — it would re-root the lookup.

**Fix shipped upstream:** commit `0b4352eb` replaces `JSON_POINTER_PATTERN`
with a new `RELAXED_PATH_PATTERN` that alternates between the old absolute
form and a new relative form:

```python
RELAXED_PATH_PATTERN = re.compile(
    r"^(?:(?:\/(?:[^~\/]|~[01])*)*|(?:[^~\/]|~[01])+(?:\/(?:[^~\/]|~[01])*)*)$"
)
```

and updates the error message from `"Invalid JSON Pointer syntax"` to
`"Invalid path syntax"`. The check in `_validate_recursion_and_paths` is
updated to use the new pattern. Total delta: ~5 lines.

**What was applied to the installed copy:** the same 5-line delta, with a
comment block naming commit `0b4352eb` as the origin. No other changes.

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

## Notes

- **Items 1 and 3 are real PR targets for upstream.** Item 1 is a Python SDK
  Windows path-separator bug; item 3 is a lit-samples Windows shell-syntax
  bug. Both are pure portability fixes that unblock Windows contributors with
  no behavior change on POSIX systems.
- **Item 2 is a release-hygiene follow-up.** Once a new `a2ui-agent-sdk` is
  published to PyPI, the in-place patch in `.venv/Lib/site-packages/a2ui/core/
  schema/validator.py` should be removed and the dependency pin in
  `samples/agent/adk/restaurant_finder/pyproject.toml` bumped.
- **A single upstream PR could bundle item 1 with a CI addition** — e.g., a
  Windows job in the Python SDK workflow, or a unit test that constructs the
  v0.9 validator and inspects the registered URI keys for backslashes. Such a
  test would have caught item 1 on any platform because the broken URI can be
  observed directly in the registry.
- The .NET SDK/Avalonia renderer in this fork is unaffected; items 1 and 2 are
  Python reference SDK bugs, item 3 is a lit samples build-script bug.
- Long-term cleanup: consider installing `agent_sdks/python/` as an editable
  dependency (`uv pip install -e ../../agent_sdks/python`) in the restaurant
  finder venv so the Python samples and the vendored source stay in lockstep
  automatically. Requires confirming that the custom `pack_specs_hook.py`
  build hook works in editable mode and populates `src/a2ui/assets/` correctly.
