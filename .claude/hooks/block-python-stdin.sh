#!/bin/bash
# PreToolUse(Bash|PowerShell): refuses commands that read their script or input from a stdin nothing feeds.
#  1. `python -` / `python3 -` (a script read from stdin, usually a heredoc): a heredoc that loses its
#     terminator, or a script piped from a command that never closes, leaves python waiting forever.
#  2. A bare `cat > file` / `cat >> file` that starts a command with no file argument, no heredoc and no
#     input redirect: cat waits on stdin forever.
# Both leave an orphan process the app lists as a background task for hours. Write the file with the Write
# tool (in the scratchpad) and run interpreters on a file path instead.
# A plain regex over the raw hook JSON, so text that only mentions a pattern in a quoted string is refused
# too; reword it or write it to a file. Tests: block-python-stdin.test.sh.
input=$(cat)

case "$input" in
  *python*|*cat*) ;;
  *) exit 0 ;;
esac

# `python -`, `python3 -`, `python3.14 -`, `python.exe -`, also `python3 -I -` style flags before the dash,
# and a heredoc straight after the interpreter (`python3 <<EOF`).
if printf '%s' "$input" | grep -Eq '(^|[^[:alnum:]_./-])python([0-9.]*)(\.exe)?( +-[A-Za-z]+)* +(-( |\n|\r|<|"|$)|<<)'; then
  echo "Refused: python reading its script from stdin (python -, or a heredoc) can hang forever waiting on stdin. Write the script to a file with the Write tool and run python3 <file>." >&2
  exit 2
fi

# `cat > file` or `cat >> file` where cat starts a command (start of the command, after ; & ( or a newline,
# not after a pipe) and nothing in the command feeds it: no heredoc (<<) and no input redirect (< file).
if printf '%s' "$input" | grep -Eq '(^|[;&(]|\\n|")[ ]*cat +>{1,2} *[^ <]' \
  && ! printf '%s' "$input" | grep -Eq '<<|[^2>&]< *[^ <]'; then
  echo "Refused: a bare 'cat > file' reads stdin and hangs forever when nothing feeds it. Write the file with the Write tool (or use a heredoc, then 'cat > file <<EOF')." >&2
  exit 2
fi
exit 0
