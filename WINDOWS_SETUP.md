# Windows setup notes (fork-specific)

This file documents one-time preparatory steps that **every Windows contributor
to this fork** must run before the lit samples will start. It exists because
this fork is the first attempt to run the A2UI web samples on Windows, and
the npm dependency graph has at least one known Windows-hostile interaction
that the upstream `package.json` does not address.

If you are on Linux or macOS, **none of this applies to you** — `npm run
demo:restaurant` (and the other `demo:*` scripts) will work without any of
the steps below.

---

## Preparatory step 1 — install the Windows rollup binary for the lit samples

**Symptom you will see without this step:**

```
[SHELL] D:\develop\a2-ui\samples\client\lit\node_modules\rollup\dist\native.js:115
[SHELL]   throw new Error(
[SHELL]   ^
[SHELL]
[SHELL] Error: Cannot find module @rollup/rollup-win32-x64-msvc. npm has a bug related
[SHELL] to optional dependencies (https://github.com/npm/cli/issues/4828).
```

**What is happening:**

Rollup (pulled in transitively by Vite, used by `samples/client/lit/shell` for
its dev server) ships one native binary per platform as `optionalDependencies`
in its `package.json`. The rollup versions relevant to this repo (4.x) require
the platform-specific binary to be present at `require` time — if it is
missing, the dev server process crashes on startup.

npm has a long-standing bug ([`npm/cli#4828`](https://github.com/npm/cli/issues/4828))
where `package-lock.json` files generated on one platform silently omit
installation entries for other platforms' optional native binaries. The
lockfile committed in `samples/client/lit/` was generated on Linux, so it
contains installation stanzas for `@rollup/rollup-linux-x64-gnu` and
`@rollup/rollup-linux-x64-musl`, but **not** for `@rollup/rollup-win32-x64-msvc`.
When `npm install` runs on Windows, it honors the lockfile strictly and skips
the Windows binary entirely, even though it is listed as an optional dep.

**Why this fork does not patch `package.json`:**

The fix has to happen in npm itself, not in this repository. Adding
`@rollup/rollup-win32-x64-msvc` as a platform-specific `optionalDependency` in
our own `package.json` would either a) duplicate what rollup already declares,
which is a maintenance burden, or b) conflict with upstream when syncing.
Pinning a specific rollup version in our `package.json` only defers the
problem to the next rollup bump. The pragmatic solution accepted by the
wider JS ecosystem is: document the manual step and run it once per clone.

**The manual step:**

After cloning this fork and running `npm install` once in
`samples/client/lit/`, run:

```bash
cd samples/client/lit
npm install --no-save @rollup/rollup-win32-x64-msvc@4.60.0
```

- `--no-save` means the install does **not** modify `package.json` or
  `package-lock.json`. This is deliberate — we do not want to drift the
  upstream-sourced lockfile on every Windows developer machine.
- The version (`4.60.0`) must exactly match the rollup version resolved in
  `samples/client/lit/node_modules/rollup/package.json`. If you have pulled
  new changes from upstream and npm has bumped rollup, check the current
  installed version with:

  ```bash
  cd samples/client/lit
  node -p "require('./node_modules/rollup/package.json').version"
  ```

  and substitute that into the `npm install --no-save @rollup/rollup-win32-x64-msvc@<version>`
  command.

You only need to do this once per clone, or after any change that wipes
`samples/client/lit/node_modules/` (for example if you ran `npm ci` or
deleted `node_modules/` to troubleshoot a different issue). The installed
binary lives at `samples/client/lit/node_modules/@rollup/rollup-win32-x64-msvc/`
and does not affect any other workspace.

**How to verify it worked:**

```bash
cd samples/client/lit
npm run demo:restaurant
```

should now progress past the `[SHELL] Error: Cannot find module` crash and
the Vite dev server should come up (look for `[SHELL] Local: http://...`
or similar in the interleaved output).

---

## Related Windows-specific fixes already landed in this fork

Items 1 and 3 of [`TODO_UPSTREAM_PATCHES.md`](TODO_UPSTREAM_PATCHES.md)
document two other Windows portability bugs that have been fixed in this
fork's commits but are still open upstream:

- **Item 1** — `os.path.join` used to build URIs in
  `agent_sdks/python/src/a2ui/schema/validator.py`. Affects the Python
  restaurant agent on Windows. Fixed in commit `e889f287`. Still needs an
  upstream PR against `google/A2UI`.
- **Item 3** — bash-only `for` loop in `samples/client/lit/package.json`'s
  `build:renderer` script. Affects anyone running `npm run demo:*` on
  Windows with `cmd.exe` as the default npm script shell. Fixed in commit
  `cc467cdf`. Still needs an upstream PR.

Both fixes are already applied in this fork, so you do not need to do
anything manually — they are automatic as long as you are on the
`feature/restaurant-demo-shell` branch (or any branch containing those
commits). This file only documents the rollup workaround because that is
the one Windows-specific issue that cannot be committed into the repo.
