#!/usr/bin/env bash
# Files or updates the open `macos-nightly` issue when the nightly macOS Build-and-Test leg fails
# (#8164). Pull requests never run that leg, so the nightly run is where a Mac-only break first shows;
# this puts it in the issue list for the next morning. One open issue at a time: a later failure
# comments on it instead of opening another. Closing it is left to a person.
#
# Env: GH_TOKEN (issues: write, actions: read), GITHUB_REPOSITORY, GITHUB_RUN_ID, GITHUB_RUN_ATTEMPT,
# GITHUB_SERVER_URL, GITHUB_API_URL, GITHUB_SHA (all set by Actions). JOB_NAME overrides the job to look at.
set -euo pipefail

job_name="${JOB_NAME:-Build-and-Test (macos-15)}"
label="macos-nightly"
export GH_REPO="$GITHUB_REPOSITORY"
run_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID"

job="$(gh api "repos/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID/attempts/$GITHUB_RUN_ATTEMPT/jobs?per_page=100" \
  | jq --arg name "$job_name" '[.jobs[] | select(.name == $name)][0]')"
conclusion="$(jq -r '.conclusion // "missing"' <<<"$job")"
case "$conclusion" in
  failure|timed_out|cancelled) ;;
  *) echo "$job_name concluded '$conclusion'; nothing to report."; exit 0 ;;
esac

job_id="$(jq -r '.id' <<<"$job")"
job_url="$(jq -r '.html_url' <<<"$job")"
steps="$(jq -r '[.steps[] | select(.conclusion == "failure") | .name] | join(", ")' <<<"$job")"
# dotnet test prints "  Failed Namespace.Class.Method [12 ms]" for each failed test.
tests="$(curl -sSL -H "Authorization: Bearer $GH_TOKEN" "$GITHUB_API_URL/repos/$GITHUB_REPOSITORY/actions/jobs/$job_id/logs" \
  | grep -aoE ' Failed [A-Za-z_][A-Za-z0-9_.]+' | sed 's/^ Failed //' | sort -u | head -40 || true)"

body="$(mktemp)"
{
  echo "The nightly \`$job_name\` leg ended \`$conclusion\` on $(date -u +%Y-%m-%d) at ${GITHUB_SHA:0:9}."
  echo
  echo "- Run: $run_url"
  echo "- Job: $job_url"
  echo "- Failed step(s): ${steps:-none reported}"
  if [ -n "$tests" ]; then
    echo "- Failed tests:"
    sed 's/^/  - `/; s/$/`/' <<<"$tests"
  fi
} > "$body"

gh label create "$label" --color D93F0B --description "Nightly macOS Build-and-Test failure (#8164)" 2>/dev/null || true
existing="$(gh issue list --label "$label" --state open --limit 1 --json number --jq '.[0].number // empty')"
if [ -n "$existing" ]; then
  gh issue comment "$existing" --body-file "$body"
  echo "Commented on #$existing."
else
  gh issue create --title "Nightly macOS Build-and-Test is failing" --label "$label" --body-file "$body"
fi
