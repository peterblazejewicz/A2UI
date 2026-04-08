---
name: dotnet-build
description: Build and test the .NET solution, report pass/fail summary
---

Run from the repo root:

```bash
dotnet build A2Ui.slnx --configuration Release
```

If build succeeds, run tests:

```bash
dotnet test A2Ui.slnx --configuration Release --no-build
```

Report a concise summary: project count, warning count, error count, test pass/fail/skip counts.
If anything fails, show the relevant error output.
