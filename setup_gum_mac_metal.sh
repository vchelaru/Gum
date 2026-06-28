#!/bin/bash
###############################################################################
### setup_gum_mac_metal.sh
###
### EXPERIMENTAL companion to setup_gum_mac.sh. Sets up the Gum tool on macOS
### using Wine + DXMT (a free, open-source Direct3D 11 -> Metal translator).
###
### WHY THIS EXISTS
###   The old recipe (wine-stable + WineD3D, or DXVK) cannot launch Gum on macOS:
###     - WineD3D translates D3D11 -> OpenGL, which Apple froze at 4.1, so it caps
###       at D3D feature level 9_3. Gum's embedded KNI renderer needs FL10 (device
###       + Shader Model 4.0 shaders), so it crashes on launch.
###     - DXVK translates D3D11 -> Vulkan -> MoltenVK, but MoltenVK is too
###       incomplete for DXVK to create a device at all.
###   DXMT translates D3D11 -> Metal DIRECTLY, exposing FL11, which is what Gum
###   needs. It is free (LGPL) and works on BOTH Apple Silicon and Intel.
###
### Wine is STILL the engine. This only changes the graphics-translation DLLs
### inside the Wine prefix (DXMT instead of WineD3D/DXVK), and uses a Metal-capable
### Wine build (Gcenx's CrossOver-derived "wine-crossover") that DXMT requires.
###
### HARDWARE / STATUS
###   - Apple Silicon (M1-M5): DXMT is mature here. Strongest chance of working.
###   - Intel Macs: DXMT support is EXPERIMENTAL upstream and UNTESTED for Gum.
###   - Requires a recent macOS with a Metal 3 GPU.
###
### IMPORTANT: This script is UNTESTED (written without a Mac to run it on). The
###   first person to run it is validating it. It logs verbosely and never hides
###   errors, so if a step fails we can see exactly where.
###############################################################################
set -u

SCRIPT_VERSION="2026.07.01-metal-dxmt-1"
PREFIX="${1:-$HOME/.wine_gum_metal}"
WINE_CASK="gcenx/wine/wine-crossover"
DXMT_API="https://api.github.com/repos/3Shain/dxmt/releases/latest"
GUM_ZIP_URL="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"
LOG="/tmp/gum_metal_setup_$(date +%Y%m%d_%H%M%S).log"

echo "=== Gum macOS (Metal/DXMT) setup  v$SCRIPT_VERSION ==="
echo "prefix : $PREFIX"
echo "log    : $LOG"
echo

log() { echo "$@"; echo "$@" >> "$LOG" 2>&1; }
run() { echo "+ $*" >> "$LOG" 2>&1; "$@" >> "$LOG" 2>&1; }

###############################################################################
### 0) Architecture + Rosetta (Apple Silicon runs the x86-64 Wine via Rosetta)
###############################################################################
ARCH="$(uname -m)"
log "Detected architecture: $ARCH"
if [ "$ARCH" = "arm64" ]; then
    log "Apple Silicon detected - ensuring Rosetta 2 is installed (needed to run the x86-64 Wine)."
    if ! /usr/bin/pgrep -q oahd 2>/dev/null; then
        softwareupdate --install-rosetta --agree-to-license || \
            log "WARN: Rosetta install returned non-zero (may already be installed)."
    fi
fi

###############################################################################
### 1) Homebrew
###############################################################################
if ! command -v brew >/dev/null 2>&1; then
    log "Homebrew not found. Installing it..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" || {
        log "ERROR: Homebrew install failed."; exit 1; }
    if [ -x /opt/homebrew/bin/brew ]; then eval "$(/opt/homebrew/bin/brew shellenv)";
    elif [ -x /usr/local/bin/brew ]; then eval "$(/usr/local/bin/brew shellenv)"; fi
fi
log "Homebrew: $(command -v brew)"

###############################################################################
### 2) Metal-capable Wine (Gcenx wine-crossover) + winetricks
###     DXMT needs a CrossOver-derived Wine that exposes Metal symbols in
###     winemac.drv; stock wine-stable does NOT work.
###############################################################################
log "Installing Wine (Metal-capable, CrossOver-derived) + winetricks ..."
run brew tap gcenx/wine
run brew install --cask --no-quarantine "$WINE_CASK"
command -v winetricks >/dev/null 2>&1 || run brew install winetricks

# Discover the wine-crossover binary and its lib dirs (paths vary by version).
WINE_BIN="$(find /Applications -maxdepth 4 -type f -name wine -path '*Wine*Crossover*' 2>/dev/null | head -1)"
[ -z "$WINE_BIN" ] && WINE_BIN="$(find /Applications -maxdepth 5 -type f -name wine64 -path '*Crossover*' 2>/dev/null | head -1)"
if [ -z "$WINE_BIN" ]; then
    log "ERROR: could not locate the wine-crossover binary under /Applications."
    log "       Check that the '$WINE_CASK' cask installed (see $LOG)."
    exit 1
fi
WINE_DIR="$(cd "$(dirname "$WINE_BIN")/.." && pwd)"   # .../Resources/wine
WINE_LIB_UNIX="$(find "$WINE_DIR" -type d -path '*lib/wine/x86_64-unix' 2>/dev/null | head -1)"
WINE_LIB_WIN="$(find "$WINE_DIR" -type d -path '*lib/wine/x86_64-windows' 2>/dev/null | head -1)"
log "Wine binary  : $WINE_BIN"
log "Wine unix lib: ${WINE_LIB_UNIX:-(not found)}"
log "Wine win lib : ${WINE_LIB_WIN:-(not found)}"

export WINE="$WINE_BIN"
export WINE64="$WINE_BIN"
export WINESERVER="$(dirname "$WINE_BIN")/wineserver"
export PATH="$(dirname "$WINE_BIN"):$PATH"
export WINEPREFIX="$PREFIX"
unset DOTNET_ROOT DOTNET_ROOT_X64

###############################################################################
### 3) Create a fresh prefix
###############################################################################
if [ -d "$PREFIX" ]; then
    log "ERROR: prefix '$PREFIX' already exists. Remove it first (see uninstall notes"
    log "       in the docs) or pass a different path: ./setup_gum_mac_metal.sh ~/.other_prefix"
    exit 1
fi
log "Creating Wine prefix at $PREFIX ..."
run "$WINE_BIN" wineboot --init
run "$WINESERVER" -w

###############################################################################
### 4) Fonts + .NET 8 desktop runtime (winetricks targets the wine above)
###############################################################################
log "Installing fonts (arial tahoma courier calibri) ..."
for f in arial tahoma courier calibri; do run winetricks -q "$f"; done
log "Installing .NET 8 desktop runtime (this takes a few minutes) ..."
run winetricks -q dotnetdesktop8

###############################################################################
### 5) DXMT (the free D3D11 -> Metal translator)
###############################################################################
log "Fetching the latest DXMT release ..."
DXMT_URL="$(curl -fsSL "$DXMT_API" | grep -oE 'https://[^"]+\.(tar\.gz|zip)' | head -1)"
if [ -z "$DXMT_URL" ]; then
    log "ERROR: could not find a DXMT release asset from $DXMT_API"
    exit 1
fi
log "DXMT asset: $DXMT_URL"
DXMT_TMP="/tmp/dxmt_$$"; rm -rf "$DXMT_TMP"; mkdir -p "$DXMT_TMP"
DXMT_FILE="$DXMT_TMP/$(basename "$DXMT_URL")"
run curl -fsSL -o "$DXMT_FILE" "$DXMT_URL"
case "$DXMT_FILE" in
    *.tar.gz) run tar -xzf "$DXMT_FILE" -C "$DXMT_TMP" ;;
    *.zip)    run unzip -q "$DXMT_FILE" -d "$DXMT_TMP" ;;
esac

SYS32="$PREFIX/drive_c/windows/system32"
place() {  # place <filename> <dest-dir>
    local src; src="$(find "$DXMT_TMP" -type f -name "$1" 2>/dev/null | head -1)"
    if [ -n "$src" ] && [ -n "$2" ]; then
        cp "$src" "$2/" 2>>"$LOG" && log "  placed $1 -> $2" || log "  WARN: could not place $1 -> $2"
    else
        log "  WARN: $1 not found in DXMT archive (or dest missing): dest=$2"
    fi
}
log "Installing DXMT files ..."
place winemetal.so  "$WINE_LIB_UNIX"
place winemetal.dll "$WINE_LIB_WIN"
place winemetal.dll "$SYS32"
place d3d11.dll     "$SYS32"
place dxgi.dll      "$SYS32"
place d3d10core.dll "$SYS32"
# Make Wine load the DXMT DLLs (native) instead of its built-ins.
for dll in d3d11 dxgi d3d10core; do
    run "$WINE_BIN" reg add "HKCU\\Software\\Wine\\DllOverrides" /v "$dll" /t REG_SZ /d native /f
done

###############################################################################
### 6) Download + extract Gum (stock release - no code changes needed)
###############################################################################
log "Downloading the latest Gum release ..."
GUM_DIR="$PREFIX/drive_c/Program Files/Gum"
ZIP="$PREFIX/drive_c/Program Files/Gum.zip"
mkdir -p "$PREFIX/drive_c/Program Files"
run curl -fL -o "$ZIP" "$GUM_ZIP_URL"
rm -rf "$GUM_DIR"
run unzip -q "$ZIP" -d "$GUM_DIR"
rm -f "$ZIP"
GUM_EXE="$(find "$GUM_DIR" -name Gum.exe -type f | head -1)"
log "Gum.exe: ${GUM_EXE:-(NOT FOUND)}"

###############################################################################
### 7) Create the ~/bin/gum launcher (run / upgrade / uninstall / prefix)
###############################################################################
mkdir -p ~/bin
cat > ~/bin/gum <<EOF
#!/bin/bash
# Gum launcher (Wine + DXMT / Metal). Generated by setup_gum_mac_metal.sh v$SCRIPT_VERSION
export WINEPREFIX="$PREFIX"
export WINE_NO_WM_DECORATION=1
# Tell Wine to use the DXMT DLLs (native) for Direct3D.
export WINEDLLOVERRIDES="dxgi,d3d11,d3d10core=n,b;\${WINEDLLOVERRIDES:-}"
# DOTNET_ROOT* break dotnet apps under Wine (Gum issue #1957).
unset DOTNET_ROOT DOTNET_ROOT_X64
WINE_BIN="$WINE_BIN"
GUM_EXE="$GUM_EXE"

case "\${1:-}" in
  ""|run)
      exec "\$WINE_BIN" "\$GUM_EXE" ;;
  upgrade)
      ZIP="$PREFIX/drive_c/Program Files/Gum.zip"
      curl -fL -o "\$ZIP" "$GUM_ZIP_URL" || { echo "download failed"; exit 1; }
      rm -rf "$GUM_DIR"; unzip -q "\$ZIP" -d "$GUM_DIR"; rm -f "\$ZIP"
      echo "Gum updated." ;;
  prefix)
      echo "$PREFIX" ;;
  uninstall)
      read -p "Remove the Gum Wine prefix at $PREFIX and ~/bin/gum? (y/N) " a
      case "\$a" in y|Y) rm -rf "$PREFIX"; rm -f ~/bin/gum; echo "Removed.";; *) echo "Cancelled.";; esac ;;
  *)
      echo "Usage: gum [run|upgrade|uninstall|prefix]" ;;
esac
EOF
chmod +x ~/bin/gum

# Ensure ~/bin is on PATH for future shells.
case ":$PATH:" in *":$HOME/bin:"*) : ;; *)
    SHELL_RC="$HOME/.zshrc"; [ -n "${BASH_VERSION:-}" ] && SHELL_RC="$HOME/.bashrc"
    echo 'export PATH="$HOME/bin:$PATH"' >> "$SHELL_RC"
    log "Added ~/bin to PATH in $SHELL_RC (open a new terminal to pick it up)." ;;
esac

###############################################################################
### Done
###############################################################################
echo
echo "=== setup finished ==="
echo "Run Gum with:   ~/bin/gum        (or just 'gum' in a new terminal)"
echo "Update later:   ~/bin/gum upgrade"
echo "Remove it:      ~/bin/gum uninstall"
echo
echo "If Gum does NOT open, capture the log and send it back:"
echo "    ~/bin/gum > ~/gum-metal-output.log 2>&1"
echo "    (also include $LOG)"
echo
echo "Watch for: does the editor window open AND the design canvas render?"
echo "That canvas is exactly where the old (WineD3D/DXVK) setup crashed."
