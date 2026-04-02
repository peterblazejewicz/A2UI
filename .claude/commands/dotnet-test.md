Run all tests in the solution and report a clean pass/fail summary.

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
DOTNET_ROOT=/home/spark/.dotnet \
/home/spark/.dotnet/dotnet test A2Ui.sln \
  --configuration Release \
  --no-build \
  --logger "console;verbosity=minimal" \
  2>&1
```

Report:
- Total tests / passed / failed / skipped
- For any failures: test name, failure message, and the minimal fix needed
- If all pass: confirm and suggest running `/coverage` next