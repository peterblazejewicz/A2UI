#!/bin/bash
set -euo pipefail

source /sandbox/.profile.d/dotnet.sh

REPO=/sandbox/develop/A2Ui
COVERAGE=$REPO/coverage

rm -rf "$COVERAGE"
mkdir -p "$COVERAGE"

echo "=== Running tests with coverage ==="
dotnet test "$REPO/A2Ui.sln" \
  --configuration Release \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE/raw" \
  --logger "trx;LogFileName=results.trx" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

echo "=== Generating coverage report ==="
reportgenerator \
  -reports:"$COVERAGE/raw/**/*.xml" \
  -targetdir:"$COVERAGE/report" \
  -reporttypes:"Html;Badges;TextSummary" \
  -classfilters:"-*.Tests.*"

echo "=== Coverage Summary ==="
cat "$COVERAGE/report/Summary.txt"
echo ""
echo "Full report: $COVERAGE/report/index.html"