#!/usr/bin/env bash
# Usage: apt-install.sh <package>...
# apt-get update + install with bounded retries, so a brief mirror/DNS failure does not fail the job
# while a sustained outage still fails in a few minutes (each apt call is killed after 150s).
set -u

apt_opts=(-o Acquire::Retries=2 -o Acquire::http::Timeout=30 -o Acquire::https::Timeout=30)
max_attempts=3
delays=(10 30)

for attempt in $(seq 1 "$max_attempts"); do
  echo "apt-install: attempt $attempt of $max_attempts"
  timeout 150 sudo apt-get "${apt_opts[@]}" update -q &&
    timeout 150 sudo apt-get "${apt_opts[@]}" install -y -q "$@"
  rc=$?
  if [ "$rc" -eq 0 ]; then
    exit 0
  fi
  echo "apt-install: attempt $attempt of $max_attempts failed with exit code $rc" >&2
  if [ "$attempt" -lt "$max_attempts" ]; then
    delay=${delays[$((attempt - 1))]}
    echo "apt-install: retrying in ${delay}s" >&2
    sleep "$delay"
  fi
done

echo "apt-install: all $max_attempts attempts failed; likely a DNS/network outage reaching the Ubuntu mirrors" >&2
exit 1
