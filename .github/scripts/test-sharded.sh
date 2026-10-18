#!/usr/bin/env bash
# Runs one xunit test project as N concurrent `dotnet test` processes, each over a share of the test
# classes. The Avalonia head tests set DisableTestParallelization (one headless Application and one
# graphics device per process), so a single process runs them one at a time and leaves the runner's
# other cores idle. Separate processes share no statics, so this is safe; each shard gets its own
# temp folder so the few fixed-name temp files (saved frames, screenshots) cannot collide either.
#
# Usage: test-sharded.sh <csproj> <shards> <trx-name-prefix> [extra dotnet test args...]
#
# Every class the build lists lands in exactly one shard, so a new class needs no edit here.
# Expects the project already built in Release (it passes --no-build).
set -uo pipefail

project="$1"; shards="$2"; trx_prefix="$3"; shift 3

list="$(dotnet test "$project" --configuration Release --no-build --list-tests)" || {
  echo "$list"; echo "Could not list tests."; exit 1; }

# Listed tests are indented lines "Ns.Class.Method" or "Ns.Class.Method(args)"; keep the class part.
classes="$(echo "$list" | grep -E '^    [A-Za-z_]' | sed -E 's/^ +//; s/\(.*$//; s/\.[^.]+$//' | sort -u)"
count="$(echo "$classes" | grep -c . || true)"
if [ "$count" -eq 0 ]; then echo "No test classes were listed."; exit 1; fi
echo "Sharding $count test classes across $shards processes."

results="TestResults"
mkdir -p "$results"
pids=(); logs=()

for ((i = 0; i < shards; i++)); do
  # Round-robin over the sorted names spreads a namespace's heavy classes across the shards.
  # The trailing dot makes each term match exactly one class, nested ("Outer+Inner.") included.
  filter="$(echo "$classes" | awk -v n="$shards" -v k="$i" 'NR % n == k { printf "%sFullyQualifiedName~%s.", (c++ ? "|" : ""), $0 }')"
  [ -z "$filter" ] && continue

  tmp="$(mktemp -d)"
  if command -v cygpath >/dev/null 2>&1; then tmp="$(cygpath -w "$tmp")"; fi
  log="$results/shard-$i.log"
  logs+=("$log")
  TMPDIR="$tmp" TMP="$tmp" TEMP="$tmp" dotnet test "$project" --configuration Release --no-build \
    --logger "trx;LogFileName=$trx_prefix-$i.trx" --results-directory "$results" \
    --filter "$filter" "$@" > "$log" 2>&1 &
  pids+=($!)
done

failed=0
for idx in "${!pids[@]}"; do
  wait "${pids[$idx]}" || failed=1
  echo "::group::Shard $idx"
  cat "${logs[$idx]}"
  echo "::endgroup::"
done

exit $failed
