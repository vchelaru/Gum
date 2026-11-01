#!/bin/bash
# PreToolUse(Bash|PowerShell): refuses `python -` / `python3 -` (a script read from stdin, usually a heredoc).
# A heredoc that loses its terminator, or a script piped from a command that never closes, leaves
# python waiting on stdin forever: an orphan process the app lists as a background task for hours.
# Write the script to a file (Write tool, in the scratchpad) and run `python3 <file>` instead.
# A plain regex over the raw hook JSON, so text that only mentions the pattern in a quoted string
# is refused too; reword it or write it to a file. Tests: block-python-stdin.test.sh.
input=$(cat)

case "$input" in
  *python*) ;;
  *) exit 0 ;;
esac

# `python -`, `python3 -`, `python3.14 -`, `python.exe -`, also `python3 -I -` style flags before the dash,
# and a heredoc straight after the interpreter (`python3 <<EOF`).
if printf '%s' "$input" | grep -Eq '(^|[^[:alnum:]_./-])python([0-9.]*)(\.exe)?( +-[A-Za-z]+)* +(-( |\n|\r|<|"|$)|<<)'; then
  echo "Refused: python reading its script from stdin (python -, or a heredoc) can hang forever waiting on stdin. Write the script to a file with the Write tool and run python3 <file>." >&2
  exit 2
fi
exit 0
