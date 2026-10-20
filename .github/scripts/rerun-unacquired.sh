#!/usr/bin/env bash
# Re-runs a workflow run's failed jobs once when GitHub never gave a job a runner (#6282). Such a job
# sits in the queue for about 15 minutes and is then cancelled with an empty runner name
# ("The job was not acquired by Runner of type hosted"); it is a hosted-runner outage, not a test
# result. A job that ran and failed, a run cancelled by a newer push, and any attempt after the first
# are left alone.
#
# Usage: rerun-unacquired.sh <run-id> <attempt> [--dry-run]
# Env: MIN_QUEUE_SECONDS (default 840): a job counts as unacquired only if it was cancelled without a
# runner after waiting at least this long, so cancelling a run by hand never matches.
set -uo pipefail

run="$1"; attempt="$2"; dry="${3:-}"
min="${MIN_QUEUE_SECONDS:-840}"
repo="${GITHUB_REPOSITORY:-vchelaru/Gum}"

unacquired="$(gh api "repos/$repo/actions/runs/$run/attempts/$attempt/jobs?per_page=100" --paginate \
  --jq ".jobs[] | select(.conclusion == \"cancelled\" and ((.runner_name // \"\") == \"\") and ((.completed_at | fromdate) - (.created_at | fromdate) >= $min)) | .name")" || {
  echo "Could not read the jobs of run $run attempt $attempt."; exit 1; }

if [ -z "$unacquired" ]; then echo "Run $run attempt $attempt: no unacquired jobs."; exit 0; fi
echo "Run $run attempt $attempt has jobs that never got a runner:"; echo "$unacquired" | sed 's/^/  /'

if [ "$attempt" != "1" ]; then echo "Already a re-run (attempt $attempt); not re-running again."; exit 0; fi
if [ "$dry" = "--dry-run" ]; then echo "Dry run: would re-run the failed jobs."; exit 0; fi
gh run rerun "$run" --failed --repo "$repo"
