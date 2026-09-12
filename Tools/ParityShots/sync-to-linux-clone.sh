#!/bin/bash
# Copy the Windows working tree's changes (modified, added, renamed, deleted) into a Linux clone.
# Usage: sync-to-linux-clone.sh [clone dir, default ~/Gum] [Windows checkout, default /mnt/c/git/Gum]
# Runs entirely inside the clone; never builds or writes under /mnt/c (a build there from Linux
# pollutes the Windows obj folders).
clone="${1:-$HOME/Gum}"
src="${2:-/mnt/c/git/Gum}"
cd "$clone" || { echo "no clone at $clone"; exit 1; }
git -C "$src" status --porcelain --untracked-files=all | grep -vE '^\?\? (WineDiagnostics/|docs-broken-links)' | while IFS= read -r line; do
  status="${line:0:2}"
  rest="${line:3}"
  case "$status" in
    R*|RM)
      old="${rest%% -> *}"; new="${rest##* -> }"
      rm -f "$old"
      mkdir -p "$(dirname "$new")"; cp "$src/$new" "$new" ;;
    " D"|"D ")
      rm -f "$rest" ;;
    *)
      mkdir -p "$(dirname "$rest")"; cp "$src/$rest" "$rest" ;;
  esac
done
echo "synced into $PWD"
