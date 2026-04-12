---
name: build-and-test
description: Build and test the .NET solution, report pass/fail summary
---

Read `.github/skills/build-and-test/SKILL.md` for build, test, format, and MTP commands.

Then run the default build-and-test sequence from the repo root.
If build succeeds, run tests.
Report a concise summary: project count, warning count, error count, test pass/fail/skip counts per test assembly.
If anything fails, show the relevant error output.
