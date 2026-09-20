#!/bin/bash
# Installs the .NET SDK so builds and tests work in Claude Code on the web.
# Gum pins 10.0.100 (rollForward latestFeature) in global.json; Ubuntu noble
# ships 10.0.1xx in its own archive, so no Microsoft CDN is involved - the
# agent proxy blocks builds.dotnet.microsoft.com.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

if command -v dotnet >/dev/null 2>&1; then
  exit 0
fi

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq || true
# net8.0 test projects (RaylibGum.Tests) need the 8.0 runtime alongside the SDK.
apt-get install -y -qq dotnet-sdk-10.0 dotnet-runtime-8.0

# Keep telemetry and the first-run banner out of build output.
{
  echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
  echo 'export DOTNET_NOLOGO=1'
} >> "${CLAUDE_ENV_FILE:-/dev/null}"
