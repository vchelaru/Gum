#!/usr/bin/env bash
# Installs .NET workloads for CI, capping each attempt at 10 minutes and retrying once. An attempt
# can hang with no output (seen on macos-15 for 50 min). Each attempt's detailed log is written to
# workload-install-<attempt>.log for the upload step that runs when both fail.
#
# Usage: install-workloads.sh <workload>...
set -o pipefail

for attempt in 1 2; do
  if perl -e 'alarm shift; exec @ARGV' 600 dotnet workload install "$@" --verbosity detailed 2>&1 | tee "workload-install-$attempt.log"; then
    exit 0
  fi
  echo "::warning::workload install attempt $attempt failed or timed out"

  # Killing dotnet on Windows leaves the MSI it started running in Windows Installer, and a retry
  # that starts before it ends fails at once with 0x652 (another installation is in progress).
  if [ "$attempt" = 1 ] && [ "$RUNNER_OS" = "Windows" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File "$(dirname "$0")/wait-for-msi-idle.ps1" -TimeoutSeconds 240
  fi
done
exit 1
