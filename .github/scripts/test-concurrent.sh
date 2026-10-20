#!/usr/bin/env bash
# Runs the Windows CI test projects as several streams at once (#6282). The manifest
# (windows-test-groups.txt) assigns every project to a stream; the streams run concurrently and the
# projects inside a stream run one after another. Each project is still its own `dotnet test` with
# its own trx, so Publish Test Results reports them as before, and a failure in any project fails
# the step after every stream has finished.
#
# Why not one process per project: the headless projects use little GL, the Mesa llvmpipe ones are
# slow and mostly wait on software rendering, so running both kinds side by side fills the runner's
# cores that a one-after-another run leaves idle. Only "mesa*" streams get GALLIUM_DRIVER, and each
# stream gets its own temp folder so fixed-name temp files cannot collide across streams.
#
# Usage: test-concurrent.sh [manifest] [slnf]
# Expects every project already built in Release (it passes --no-build).
set -uo pipefail

manifest_file="${1:-.github/scripts/windows-test-groups.txt}"
# A Windows checkout turns the manifest's LF into CRLF (core.autocrlf), so read a copy without the CRs.
manifest="$(mktemp)"; tr -d '\015' < "$manifest_file" > "$manifest"
slnf="${2:-Tests/CiTestProjects.slnf}"
results="TestResults"

# The manifest must list exactly the projects the build step compiles (the slnf), each once.
slnf_projects="$(grep -o '"[^"]*\.csproj"' "$slnf" | tr -d '"' | sed -E 's#\\+#/#g; s#/[^/]*$##' | sort)"
manifest_projects="$(grep -vE '^\s*(#|$)' "$manifest" | awk '{print $2}' | sort)"
if [ "$slnf_projects" != "$manifest_projects" ]; then
  echo "::error::$manifest_file and $slnf list different test projects."
  diff <(echo "$slnf_projects") <(echo "$manifest_projects") | sed 's/^/  /'
  echo "  (< only in the slnf, so it is built but never run; > only in the manifest, so it is run but never built)"
  exit 1
fi

mkdir -p "$results"
streams="$(grep -vE '^\s*(#|$)' "$manifest" | awk '{print $1}' | sort -u)"
# STREAMS (space separated) limits the run to those streams; the manifest check above still covers all.
if [ -n "${STREAMS:-}" ]; then
  wanted=""
  for want in $STREAMS; do
    echo "$streams" | grep -qFx "$want" || { echo "::error::Unknown stream $want"; exit 1; }
    wanted="$wanted $want"
  done
  streams="$wanted"
fi
# SPLIT="<stream>=<n>" deals one stream's projects round-robin into n streams named <stream>-1..n
# (manifest order), for runs where the other streams are skipped and the cores would sit idle.
if [ -n "${SPLIT:-}" ]; then
  split_stream="${SPLIT%%=*}"; split_n="${SPLIT##*=}"
  if echo $streams | tr ' ' '\n' | grep -qFx "$split_stream"; then
    awk -v s="$split_stream" -v n="$split_n" '$1 == s { k++; sub(/^[^ \t]+/, s "-" ((k - 1) % n + 1)) } { print }' "$manifest" > "$manifest.split"
    mv "$manifest.split" "$manifest"
    parts=""; for ((i = 1; i <= split_n; i++)); do parts="$parts $split_stream-$i"; done
    streams="$(echo $streams | tr ' ' '\n' | grep -vFx "$split_stream" | tr '\n' ' ')$parts"
  fi
fi
echo "Streams:" $streams
pids=(); names=()

run_stream() {
  local stream="$1" log="$results/stream-$1.log" tmp
  tmp="$(mktemp -d)"; if command -v cygpath >/dev/null 2>&1; then tmp="$(cygpath -w "$tmp")"; fi
  export TMPDIR="$tmp" TMP="$tmp" TEMP="$tmp"
  case "$stream" in mesa*) export GALLIUM_DRIVER=llvmpipe ;; esac
  local failed=0 line project minutes
  while read -r line; do
    set -- $line   # stream project [minutes] [extra...] (word-split on purpose; quotes are kept for eval)
    [ "$1" = "$stream" ] || continue
    project="$2"; minutes="${3:-10}"
    shift 3 2>/dev/null || shift $#
    local start=$SECONDS rc=0
    echo "=== $project (stream $stream, limit ${minutes} min) ===" >> "$log"
    eval timeout "$((minutes * 60))" dotnet test "\"$project\"" --configuration Release --no-build \
      --logger '"trx"' --results-directory "$results" "$@" >> "$log" 2>&1 < /dev/null || rc=$?
    echo "=== $project finished in $((SECONDS - start)) s, exit $rc ===" >> "$log"
    [ "$rc" -eq 0 ] || failed=1
  done < <(grep -vE '^\s*(#|$)' "$manifest")
  return $failed
}

total_start=$SECONDS
for s in $streams; do
  run_stream "$s" &
  pids+=($!); names+=("$s")
done

failed=0
for i in "${!pids[@]}"; do
  rc=0; wait "${pids[$i]}" || rc=$?
  [ "$rc" -eq 0 ] || failed=1
  s="${names[$i]}"
  echo "::group::Stream $s (exit $rc)"
  cat "$results/stream-$s.log"
  echo "::endgroup::"
  [ "$rc" -eq 0 ] || echo "::error::Stream $s has failing test projects; see its log group above."
done

summary="$(grep -h '^=== .* finished in ' "$results"/stream-*.log | sed -E 's/^=== (.*) finished in ([0-9]+) s, exit ([0-9]+) ===$/\2 s\t\1\texit \3/' | sort -rn)"
echo "Wall time $((SECONDS - total_start)) s across $(echo $streams | wc -w) streams."
echo "$summary"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### Windows test streams (wall $((SECONDS - total_start)) s)"; echo '```'; echo "$summary"; echo '```'; } >> "$GITHUB_STEP_SUMMARY"
fi
exit $failed
