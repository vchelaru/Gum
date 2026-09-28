#!/usr/bin/env bash
# Launch-tests one release package (#5465) the way a user would use it: extract it, open a copy
# of the Forms sample in the packaged Gum unattended, and run the packaged gumcli on that copy.
#
# Usage: smoke-test-package.sh <rid> <dist-dir> <sample-project-dir> <out-dir> [--launch-preview]
#   dist-dir holds Gum-<rid>.zip or Gum-<rid>.tar.xz and its .sha256.
#   out-dir receives the screenshots and logs.
#   --launch-preview also starts Preview-Aot/GumPreview on a JSON copy of the project and checks it
#   is still running after a few seconds. GumPreview has no unattended exit flag, so this opens a
#   window until it is killed; only pass it where no one is looking (CI, Xvfb).
#
# Extract-only mode, so CI can drop software GL next to the executables before launching:
#   smoke-test-package.sh --extract <rid> <dist-dir> <extract-dir>
set -euo pipefail

fail() { echo "::error::$*" >&2; exit 1; }

extract() {
  local rid="$1" dist="$2" dest="$3"
  rm -rf "$dest"
  mkdir -p "$dest"
  case "$rid" in
    win-*)
      local zip="$dist/Gum-$rid.zip"
      [ -f "$zip" ] || fail "missing $zip"
      powershell -NoProfile -Command "Expand-Archive -Path '$(cygpath -w "$zip")' -DestinationPath '$(cygpath -w "$dest")' -Force"
      ;;
    *)
      local tarball="$dist/Gum-$rid.tar.xz"
      [ -f "$tarball" ] || fail "missing $tarball"
      tar -xJf "$tarball" -C "$dest"
      ;;
  esac
}

# The folder holding Gum, Plugins/, GumCli/ and Preview-Aot/ inside an extracted package.
app_dir() {
  local rid="$1" root="$2"
  case "$rid" in
    osx-*) echo "$root/Gum.app/Contents/MacOS" ;;
    *) echo "$root" ;;
  esac
}

# RUNNER_TEMP is a Windows path under Git Bash; make it POSIX so the paths below compose.
temp_root="${RUNNER_TEMP:-}"
if [ -n "$temp_root" ] && command -v cygpath >/dev/null; then
  temp_root=$(cygpath -u "$temp_root")
fi

if [ "${1:-}" = "--extract" ]; then
  extract "$2" "$3" "$4"
  echo "extracted to $(app_dir "$2" "$4")"
  exit 0
fi

rid="$1"; dist="$2"; sample="$3"; out="$4"
launch_preview=false
[ "${5:-}" = "--launch-preview" ] && launch_preview=true

mkdir -p "$out"
out=$(cd "$out" && pwd)
work="${temp_root:-$(mktemp -d)}/gum-smoke-$rid"
pkg="$work/package"

exe_suffix=""
case "$rid" in win-*) exe_suffix=".exe" ;; esac

case "$rid" in
  *-x64) arch_pattern='x86[-_]64|x86-64|PE32\+ executable.*x86-64' ;;
  *-arm64) arch_pattern='arm64|aarch64' ;;
  *) fail "unknown rid $rid" ;;
esac

echo "== checksum"
case "$rid" in
  win-*) archive="Gum-$rid.zip" ;;
  *) archive="Gum-$rid.tar.xz" ;;
esac
expected=$(awk '{print tolower($1)}' "$dist/Gum-$rid.sha256")
if command -v shasum >/dev/null; then
  actual=$(shasum -a 256 "$dist/$archive" | awk '{print $1}')
else
  actual=$(sha256sum "$dist/$archive" | awk '{print $1}')
fi
[ "$expected" = "$actual" ] || fail "checksum mismatch: $expected vs $actual"

# Reuse an earlier --extract (CI copies Mesa into it on Windows); otherwise extract now.
if [ ! -d "$pkg" ]; then
  extract "$rid" "$dist" "$pkg"
fi
app=$(app_dir "$rid" "$pkg")
gum="$app/Gum$exe_suffix"
preview="$app/Preview-Aot/GumPreview$exe_suffix"
gumcli="$app/GumCli/gumcli$exe_suffix"

echo "== package layout"
for f in "$gum" "$preview" "$gumcli"; do
  [ -f "$f" ] || fail "missing $f"
done
plugin_count=$(find "$app/Plugins" -name '*.dll' | wc -l | tr -d ' ')
[ "$plugin_count" -gt 0 ] || fail "no plugins in $app/Plugins"
echo "plugins: $plugin_count dlls"

if command -v file >/dev/null; then
  for f in "$gum" "$preview"; do
    desc=$(file -b "$f")
    echo "$(basename "$f"): $desc"
    echo "$desc" | grep -Eq "$arch_pattern" || fail "$f is not built for $rid"
  done
else
  echo "file not available, skipping architecture check"
fi

case "$rid" in
  osx-*)
    bundle="$pkg/Gum.app"
    codesign -v "$bundle" || fail "Gum.app fails codesign -v"
    # Finder double-click needs a GUI session; the automated part is that the bundle declares
    # the document types Finder routes to it.
    plist="$bundle/Contents/Info.plist"
    plutil -lint "$plist"
    exts=$(plutil -extract CFBundleDocumentTypes xml1 -o - "$plist")
    for ext in gumx gumj; do
      echo "$exts" | grep -q "<string>$ext</string>" || fail "Info.plist does not register .$ext"
    done
    echo "Info.plist registers .gumx and .gumj"
    ls "$app/Preview-Aot" | grep -Eq 'libSDL2.*\.dylib' || fail "Preview-Aot is missing the SDL2 dylib"
    ;;
esac

# osx-x64 is built and tested on an Apple Silicon runner, so its launches need Rosetta.
can_launch=true
if [ "$rid" = "osx-x64" ] && [ "$(uname -m)" = "arm64" ] && ! /usr/bin/arch -x86_64 /usr/bin/true 2>/dev/null; then
  echo "::warning::Rosetta is not installed on this runner; osx-x64 gets the package checks only, no launch"
  can_launch=false
fi

# On Linux run GUI programs on a virtual display unless one already exists.
gui() {
  if [ "$(uname -s)" = "Linux" ] && [ -z "${DISPLAY:-}" ]; then
    xvfb-run -a -s "-screen 0 1600x1000x24" "$@"
  else
    "$@"
  fi
}

echo "== copy sample project"
project_dir="$work/project"
rm -rf "$project_dir" "$work/userdata"
mkdir -p "$project_dir" "$work/userdata"
cp -R "$sample/." "$project_dir/"
gumx=$(find "$project_dir" -maxdepth 1 -name '*.gumx' | head -n 1)
[ -n "$gumx" ] || fail "no .gumx in $sample"

if ! $can_launch; then
  echo "== $rid package checks passed (launch skipped)"
  exit 0
fi

echo "== launch packaged Gum unattended"
shot="$out/gum-$rid.png"
rm -f "$shot"
set +e
gui "$gum" "$gumx" --exit-after 120 --select DemoScreenGum --screenshot "$shot" \
  --user-data "$work/userdata" > "$out/gum-$rid.log" 2>&1
code=$?
set -e
cat "$out/gum-$rid.log"
[ "$code" -eq 0 ] || fail "Gum exited $code"
[ -s "$shot" ] || fail "Gum exited 0 but wrote no screenshot"
echo "screenshot: $shot"

echo "== gumcli check"
"$gumcli" check "$gumx" || fail "gumcli check failed"

echo "== gumcli fonts (from an empty FontCache)"
rm -rf "$project_dir/FontCache"
"$gumcli" fonts "$gumx" || fail "gumcli fonts failed"
fnt_count=$(find "$project_dir/FontCache" -name '*.fnt' 2>/dev/null | wc -l | tr -d ' ')
[ "$fnt_count" -gt 0 ] || fail "gumcli fonts generated no .fnt files"
echo "generated $fnt_count fonts"

if $launch_preview; then
  echo "== launch GumPreview (Native AOT)"
  # The AOT build loads JSON projects only; the tool converts before launching it, so do the same.
  "$gumcli" convert-to-json "$gumx" || fail "gumcli convert-to-json failed"
  gumj="${gumx%.gumx}.gumj"
  [ -f "$gumj" ] || fail "convert-to-json wrote no $gumj"
  # GumPreview runs until closed. Alive after 15 s means it loaded the project and entered its
  # game loop; an early exit is a crash.
  gui "$preview" --project "$gumj" --element DemoScreenGum > "$out/preview-$rid.log" 2>&1 &
  pid=$!
  sleep 15
  if kill -0 "$pid" 2>/dev/null; then
    # Kill the whole group: under xvfb-run the pid is the wrapper, not GumPreview.
    pkill -P "$pid" 2>/dev/null || true
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
    echo "GumPreview was still running after 15 s"
  else
    set +e; wait "$pid"; pcode=$?; set -e
    cat "$out/preview-$rid.log"
    fail "GumPreview exited early with $pcode"
  fi
fi

echo "== $rid package smoke test passed"
