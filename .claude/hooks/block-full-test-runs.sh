#!/bin/bash
# PreToolUse(Bash): refuses `dotnet test` without --filter. A whole test project can take 10-20
# minutes, and CI already runs the full matrix; local verification is `pwsh Tools/verify.ps1`
# plus focused --filter runs (CLAUDE.md "Building and Testing"). Commands that start with
# GUM_ALLOW_FULL_TESTS=1 pass, for the rare case the user asks for a full run.
# Reads the raw hook JSON with grep so it needs no jq.
input=$(cat)

if ! printf '%s' "$input" | grep -q 'dotnet test'; then
  exit 0
fi

if printf '%s' "$input" | grep -q -e '--filter' -e 'GUM_ALLOW_FULL_TESTS=1'; then
  exit 0
fi

echo "Blocked: 'dotnet test' without --filter runs a whole test project (10-20 min). Use 'pwsh Tools/verify.ps1' or 'dotnet test <csproj> --filter <Class>'. CI runs the full suites. If the user asked for a full run, prefix the command with GUM_ALLOW_FULL_TESTS=1." >&2
exit 2
