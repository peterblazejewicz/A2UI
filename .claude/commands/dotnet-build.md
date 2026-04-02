Build the entire A2Ui solution and report a clean error/warning summary.

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
DOTNET_ROOT=/home/spark/.dotnet \
/home/spark/.dotnet/dotnet build A2Ui.sln \
  --configuration Release \
  2>&1 | grep -E "^Build|error|warning|Error|Warning" | head -40
```

If there are errors, show the full error with file path and line number.
If build succeeds with zero errors, confirm and show the binary output paths.