#!/usr/bin/env bash
# Usage: apt-install.sh <package>...
# apt-get update + install with retries, so a transient mirror/DNS failure does not fail the job.
# Exits non-zero when every attempt fails, so a real outage still fails loudly.
set -u

apt_opts=(-o Acquire::Retries=5 -o Acquire::http::Timeout=30 -o Acquire::https::Timeout=30)
max_attempts=3

for attempt in $(seq 1 "$max_attempts"); do
  echo "apt-install: attempt $attempt of $max_attempts"
  if sudo apt-get "${apt_opts[@]}" update -q && sudo apt-get "${apt_opts[@]}" install -y -q "$@"; then
    exit 0
  fi
  echo "apt-install: attempt $attempt of $max_attempts failed" >&2
  if [ "$attempt" -lt "$max_attempts" ]; then
    delay=$((attempt * 10))
    echo "apt-install: retrying in ${delay}s" >&2
    sleep "$delay"
  fi
done

echo "apt-install: all $max_attempts attempts failed" >&2
exit 1
