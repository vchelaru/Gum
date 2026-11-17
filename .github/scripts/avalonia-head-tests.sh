#!/usr/bin/env bash
# Runs the Build-Avalonia-Head job's tests as two concurrent streams (#8169):
#   1. Gum.Avalonia.Tests headless, split across 3 processes by test-sharded.sh.
#   2. One after another: the head and device tests under Xvfb (Linux), then Gum.Presentation.Tests
#      and Gum.ProjectServices.Tests (not on Windows, whose main job already runs them).
# The ubuntu runner has 4 vCPUs and the 3 shards keep about 3 of them busy, so stream 2 used to run
# after the shards on an otherwise idle core. Each project still writes its own trx into TestResults,
# so Publish Test Results reports them as before, and any failure fails the step after both streams end.
#
# Usage: avalonia-head-tests.sh  (expects Gum.slnx built in Release; Linux expects Xvfb and Mesa)
set -uo pipefail

results="TestResults"
mkdir -p "$results"
has_timeout=""; if command -v timeout >/dev/null 2>&1; then has_timeout=1; fi
echo "vCPUs: $(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo unknown)"
cpu_busy() { awk '/^cpu /{ print $2+$3+$4+$7+$8, $2+$3+$4+$5+$6+$7+$8 }' /proc/stat 2>/dev/null; }
cpu_start="$(cpu_busy)"
total_start=$SECONDS

# Stream 1: the headless suite. test-sharded.sh prints each shard's log once it finishes.
shard_log="$results/stream-sharded.log"
bash .github/scripts/test-sharded.sh Tests/Gum.Avalonia.Tests/Gum.Avalonia.Tests.csproj 3 avalonia-head \
  --blame --blame-hang-timeout 8m --blame-hang-dump-type mini > "$shard_log" 2>&1 < /dev/null &
shard_pid=$!

# Stream 2. Own temp folder, so fixed-name temp files cannot collide with the shards'.
run_extra() {
  local tmp failed=0
  tmp="$(mktemp -d)"; if command -v cygpath >/dev/null 2>&1; then tmp="$(cygpath -w "$tmp")"; fi
  export TMPDIR="$tmp" TMP="$tmp" TEMP="$tmp"
  step() {  # step <name> <timeout-minutes> <command...>
    local name="$1" minutes="$2" start=$SECONDS rc=0; shift 2
    echo "=== $name ==="
    if [ -n "$has_timeout" ]; then timeout $((minutes * 60)) "$@" < /dev/null || rc=$?
    else "$@" < /dev/null || rc=$?; fi
    echo "=== $name finished in $((SECONDS - start)) s, exit $rc ==="
    [ "$rc" -eq 0 ] || failed=1
  }
  # The full-startup layer (phase 100): the real head executable on a fixture project, plus the KNI
  # device tests and the canvas end-to-end scenarios. Only Linux has a cheap virtual display plus
  # software GL on the runner (Xvfb + Mesa llvmpipe), so it is the one OS where CI proves the startup
  # sequence and the device path; the other two run them locally.
  if [ "${RUNNER_OS:-}" = "Linux" ]; then
    step "Gum.Avalonia head and device tests under Xvfb" 20 \
      env GUM_RUN_HEAD_PROCESS_TEST=1 GUM_RUN_CANVAS_DEVICE_TESTS=1 LIBGL_ALWAYS_SOFTWARE=1 \
      xvfb-run -a -s "-screen 0 1600x1000x24" dotnet test Tests/Gum.Avalonia.Tests/Gum.Avalonia.Tests.csproj \
      --configuration Release --no-build --blame --blame-hang-timeout 8m --blame-hang-dump-type mini \
      --logger "trx;LogFileName=head-process.trx" --results-directory "$results" \
      --filter "FullyQualifiedName~HeadProcessTests|FullyQualifiedName~CanvasHostTests|FullyQualifiedName~TextureCoordinateTab|FullyQualifiedName~CanvasScenarioTests|FullyQualifiedName~CanvasMenuAndToolbarScenarioTests|FullyQualifiedName~DragDropScenarioTests|FullyQualifiedName~ExportAsImageAlphaTests"
  fi
  # The logic shared by both heads and save byte-parity (ProjectSaveParityTests) must hold on macOS and
  # Linux too; Windows runs both in Build-and-Test, so they are not repeated there.
  if [ "${RUNNER_OS:-}" != "Windows" ]; then
    step "Gum.Presentation.Tests" 15 dotnet test Tests/Gum.Presentation.Tests/Gum.Presentation.Tests.csproj \
      --configuration Release --logger "trx" --results-directory "$results" \
      --blame-hang --blame-hang-timeout 5m --blame-hang-dump-type none
    step "Gum.ProjectServices.Tests" 15 dotnet test Tests/Gum.ProjectServices.Tests/Gum.ProjectServices.Tests.csproj \
      --configuration Release --logger "trx" --results-directory "$results"
  fi
  return $failed
}
extra_log="$results/stream-extra.log"
run_extra > "$extra_log" 2>&1 &
extra_pid=$!

trap 'cat "$shard_log" "$extra_log"; exit 143' INT TERM

failed=0
shard_rc=0; wait "$shard_pid" || shard_rc=$?
echo "Sharded headless stream finished at $((SECONDS - total_start)) s, exit $shard_rc"
extra_rc=0; wait "$extra_pid" || extra_rc=$?
echo "Xvfb and shared-logic stream finished at $((SECONDS - total_start)) s, exit $extra_rc"

echo "::group::Stream: Gum.Avalonia.Tests headless shards (exit $shard_rc)"; cat "$shard_log"; echo "::endgroup::"
echo "::group::Stream: Xvfb and shared-logic tests (exit $extra_rc)"; cat "$extra_log"; echo "::endgroup::"
[ "$shard_rc" -eq 0 ] || { failed=1; echo "::error::Gum.Avalonia.Tests headless shards failed; see their log group above."; }
[ "$extra_rc" -eq 0 ] || { failed=1; echo "::error::Xvfb or shared-logic tests failed; see their log group above."; }

summary="$(grep -hE '(Passed|Failed)!' "$shard_log" | sed -E 's/^.*Duration: ([^-]+) - .*$/shard: \1/'
grep -h '^=== .* finished in ' "$extra_log" | sed -E 's/^=== (.*) finished in ([0-9]+) s, (exit [0-9]+) ===$/\2 s  \1  \3/')"
cpu_end="$(cpu_busy)"
usage=""
if [ -n "$cpu_start" ] && [ -n "$cpu_end" ]; then
  usage="$(echo "$cpu_start $cpu_end" | awk '{ t = $4 - $2; if (t > 0) printf ", CPU busy %.0f%% on average", 100 * ($3 - $1) / t }')"
fi
echo "Wall time $((SECONDS - total_start)) s$usage."
echo "$summary"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### Avalonia head test streams, ${RUNNER_OS:-$(uname -s)} (wall $((SECONDS - total_start)) s$usage)"
    echo '```'; echo "shards stream exit $shard_rc"; echo "$summary"; echo '```'; } >> "$GITHUB_STEP_SUMMARY"
fi
exit $failed
