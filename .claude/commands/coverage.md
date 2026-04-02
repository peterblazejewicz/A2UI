Generate a code coverage report for the solution.

```bash
bash scripts/test-coverage.sh
```

If `scripts/test-coverage.sh` does not exist yet, create it first with this content:

```bash
#!/usr/bin/env bash
set -euo pipefail
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_ROOT=/home/spark/.dotnet
export PATH="$DOTNET_ROOT:$PATH"

REPO=$(git rev-parse --show-toplevel)
COVERAGE=$REPO/coverage
rm -rf "$COVERAGE" && mkdir -p "$COVERAGE"

dotnet test "$REPO/A2Ui.sln" \
  --configuration Release \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE/raw" \
  --logger "trx;LogFileName=results.trx" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

dotnet reportgenerator \
  -reports:"$COVERAGE/raw/**/*.xml" \
  -targetdir:"$COVERAGE/report" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"-*.Tests.*"

cat "$COVERAGE/report/Summary.txt"
```

Then `chmod +x scripts/test-coverage.sh` and run it.

After completion, show the coverage percentage per project and flag any file below 70%.