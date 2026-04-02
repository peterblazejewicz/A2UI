Verify the DGX Spark development environment is correctly configured for this project.

Run each check in sequence and report pass/fail for each item:

1. **Ollama + model**
   ```bash
   curl -s http://localhost:11434/api/tags | grep -o 'nemotron-3-super:120b' || echo "MODEL NOT FOUND"
   ```

2. **.NET SDK**
   ```bash
   DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
   DOTNET_ROOT=/home/spark/.dotnet \
   /home/spark/.dotnet/dotnet --version
   ```
   Expected: `10.0.201` or later 10.x

3. **Git identity**
   ```bash
   git config user.name
   git config user.email
   git branch --show-current
   ```

4. **Solution builds**
   ```bash
   DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
   DOTNET_ROOT=/home/spark/.dotnet \
   /home/spark/.dotnet/dotnet build A2Ui.sln --configuration Release --no-restore 2>&1 | tail -5
   ```

5. **Node (for A2UI tooling)**
   ```bash
   node --version
   ```

6. **A2UI reference repo**
   ```bash
   ls docs/a2ui-spec/ 2>/dev/null && echo "spec docs present" || echo "MISSING — run /scan-repo"
   ```

Report a summary table with ✓ / ✗ for each item. If any check fails, print the exact fix command.