#!/bin/bash
# Tests for block-full-test-runs.sh. Pipes Bash-tool hook payloads into the hook and checks
# the exit code (0 = allowed, 2 = blocked). Run: bash .claude/hooks/block-full-test-runs.test.sh
# Payloads are JSON as Claude Code sends them, so \n and \" are JSON escapes.
hook="$(dirname "$0")/block-full-test-runs.sh"
failures=0

check() {
  expected=$1
  name=$2
  command_json=$3
  printf '{"session_id":"s","hook_event_name":"PreToolUse","tool_name":"Bash","tool_input":{"command":"%s","description":"d"}}' "$command_json" \
    | bash "$hook" 2>/dev/null
  actual=$?
  if [ "$actual" -eq "$expected" ]; then
    echo "PASS  $name"
  else
    echo "FAIL  $name (expected $expected, got $actual)"
    failures=$((failures + 1))
  fi
}

allowed=0
blocked=2

check $blocked "plain unfiltered run" 'dotnet test Tests/Foo/Foo.csproj'
check $allowed "--filter" 'dotnet test Tests/Foo/Foo.csproj --filter \"FooTests\"'
check $allowed "--filter=value" 'dotnet test Tests/Foo/Foo.csproj --filter=FooTests'
check $allowed "verify.ps1" 'pwsh Tools/verify.ps1'
check $allowed "dotnet build" 'dotnet build Gum.slnx'
check $allowed "GUM_ALLOW_FULL_TESTS=1 prefix" 'GUM_ALLOW_FULL_TESTS=1 dotnet test Tests/Foo/Foo.csproj'
check $blocked "other env prefix" 'FOO=1 dotnet test Tests/Foo/Foo.csproj'
check $blocked "after cd &&" 'cd x && dotnet test Tests/Foo/Foo.csproj'
check $blocked "after ;" 'echo hi; dotnet test'
check $blocked "after ||" 'false || dotnet test'
check $blocked "after newline" 'echo hi\ndotnet test'
check $blocked "allow flag on a different command" 'GUM_ALLOW_FULL_TESTS=1 echo hi && dotnet test'
check $blocked "filter on a different command" 'dotnet test && dotnet test x --filter Y'
check $allowed "double-quoted string" 'git commit -m \"blocks dotnet test without filter\"'
check $allowed "single-quoted string" "echo 'run dotnet test here'"
check $allowed "multi-line quoted string" 'git commit -m \"line one\ndotnet test\nline three\"'
check $allowed "heredoc body" 'cat > f.md <<'"'"'EOF'"'"'\nUse dotnet test with a filter.\ndotnet test\nEOF'
check $blocked "command after heredoc" 'cat > f.md <<EOF\nnotes\nEOF\ndotnet test'
check $allowed "dotnet test-like word" 'dotnet testx'

if [ "$failures" -ne 0 ]; then
  echo "$failures failure(s)"
  exit 1
fi
echo "All passed"
