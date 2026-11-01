#!/bin/bash
# Tests for block-python-stdin.sh. Run: bash .claude/hooks/block-python-stdin.test.sh
hook="$(dirname "$0")/block-python-stdin.sh"
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

check $blocked "python3 - heredoc" "python3 - <<'EOF'\nprint(1)\nEOF"
check $blocked "python - heredoc" "python - <<EOF\nprint(1)\nEOF"
check $blocked "cd then python3 -" 'cd x && python3 - 2>/dev/null || echo nopy'
check $blocked "piped into python3 -" 'cat a.py | python3 -'
check $blocked "python3 -I -" 'python3 -I - <<EOF\nx\nEOF'
check $blocked "heredoc straight after interpreter" "python3 <<'EOF'\nprint(1)\nEOF"
check $blocked "python.exe -" 'python.exe - < a.py'
check $blocked "python3.14 -" 'python3.14 -'
check $allowed "script file" 'python3 tools/x.py --flag'
check $allowed "python -c" 'python3 -c \"print(1)\"'
check $allowed "python -m" 'python3 -m json.tool a.json'
check $allowed "no python" 'dotnet build Gum.slnx'
check $allowed "a flag named like python" 'echo --python-flag - x'
check $allowed "pip install" 'pip install -r requirements.txt'
check $blocked "bare cat > file after cd" 'cd x && cat > /tmp/null 2>&1; node -v'
check $blocked "bare cat > file at start" 'cat > out.txt'
check $blocked "bare cat >> file" 'cd x; cat >> out.txt'
check $blocked "bare cat > var path with stderr redirect, a < later in the command" 'cd x && cat > $TMP/fix.txt 2>/dev/null; sed -i \"64a <Compile Include=a />\" b.csproj'
check $allowed "cat with heredoc" "cat > out.txt <<'EOF'\nhello\nEOF"
check $allowed "cat file to file" 'cat a.txt > out.txt'
check $allowed "piped into cat" 'echo hi | cat > out.txt'
check $allowed "cat with input redirect" 'cat > out.txt < in.txt'
check $allowed "cat only" 'cat a.txt b.txt'

if [ "$failures" -ne 0 ]; then
  echo "$failures failure(s)"
  exit 1
fi
echo "all passed"
