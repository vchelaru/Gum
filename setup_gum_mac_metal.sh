#!/bin/bash
###############################################################################
### setup_gum_mac_metal.sh
###
### EXPERIMENTAL companion to setup_gum_mac.sh. Sets up the Gum tool on macOS
### using Wine + Apple's D3DMetal (Direct3D 11 -> Metal) via the Game Porting
### Toolkit (GPTK).
###
### WHY: Gum's embedded KNI renderer needs D3D feature level 10 (device + Shader
### Model 4.0 shaders). WineD3D (D3D->OpenGL) caps at FL 9_3 and DXVK (D3D->Vulkan
### ->MoltenVK) can't create a device, so both crash Gum on launch. D3DMetal
### translates D3D11 -> Metal directly at FL11, which is what Gum needs.
###
### *** APPLE SILICON ONLY ***
###   Apple's D3DMetal does NOT run on Intel Macs (GPTK dropped Intel). The
###   open-source DXMT layer used to cover Intel, but its Homebrew wine
###   ("wine-crossover") was removed from the gcenx/wine tap, so there is no
###   longer a brew-installable Metal path for Intel. Intel needs a separate
###   manual setup (out of scope here). This script targets M1-M5 Macs.
###
### STATUS: still being validated. The Homebrew/GPTK install steps are applied
### from real on-machine feedback (Homebrew 6.x); the D3DMetal *launch* specifics
### are the least-certain part and may need adjusting per the installed GPTK.
###############################################################################
set -u

SCRIPT_VERSION="2026.07.01-gptk-d3dmetal-2"
PREFIX="${1:-$HOME/.wine_gum_metal}"
WINE_TAP="gcenx/wine"
WINE_CASK="gcenx/wine/game-porting-toolkit"
GUM_ZIP_URL="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"
LOG="/tmp/gum_metal_setup_$(date +%Y%m%d_%H%M%S).log"

echo "=== Gum macOS (GPTK / D3DMetal) setup  v$SCRIPT_VERSION ==="
echo "prefix : $PREFIX"
echo "log    : $LOG"
echo

log() { echo "$@"; echo "$@" >> "$LOG" 2>&1; }
run() { echo "+ $*" >> "$LOG" 2>&1; "$@" >> "$LOG" 2>&1; }

###############################################################################
### 0) Architecture gate - D3DMetal is Apple Silicon only
###############################################################################
ARCH="$(uname -m)"
log "Architecture: $ARCH"
if [ "$ARCH" != "arm64" ]; then
    log "ERROR: this script uses Apple's D3DMetal, which only runs on Apple Silicon"
    log "       (M1-M5). On an Intel Mac it cannot work. Intel needs a different"
    log "       (manual DXMT) setup that is not yet automated. Stopping."
    exit 1
fi
# The x86-64 Gum.exe runs under Rosetta 2.
if ! /usr/bin/pgrep -q oahd 2>/dev/null; then
    log "Installing Rosetta 2 (needed to run the x86-64 Gum.exe)..."
    softwareupdate --install-rosetta --agree-to-license || \
        log "WARN: Rosetta install returned non-zero (may already be installed)."
fi

###############################################################################
### 1) Homebrew
###############################################################################
if ! command -v brew >/dev/null 2>&1; then
    log "Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" || {
        log "ERROR: Homebrew install failed."; exit 1; }
    if [ -x /opt/homebrew/bin/brew ]; then eval "$(/opt/homebrew/bin/brew shellenv)";
    elif [ -x /usr/local/bin/brew ]; then eval "$(/usr/local/bin/brew shellenv)"; fi
fi
log "Homebrew: $(command -v brew)  ($(brew --version | head -1))"

###############################################################################
### 2) Game Porting Toolkit (provides the Metal-capable Wine + D3DMetal)
###     NOTE: Homebrew 6.x removed the --no-quarantine flag from `brew install`,
###     so we install normally and clear the quarantine attribute afterward.
###############################################################################
log "Tapping $WINE_TAP and installing $WINE_CASK ..."
run brew tap "$WINE_TAP"
if ! brew install --cask "$WINE_CASK" >> "$LOG" 2>&1; then
    log "ERROR: '$WINE_CASK' failed to install. See $LOG."
    log "       (If brew says the cask is unavailable, the tap may have changed -"
    log "        check 'brew search gcenx/wine/' for the current GPTK cask name.)"
    exit 1
fi
command -v winetricks >/dev/null 2>&1 || run brew install winetricks

# Locate the GPTK app + its Wine, then clear Gatekeeper quarantine on it.
GPTK_APP="$(find /Applications -maxdepth 1 -iname 'Game Porting Toolkit*.app' 2>/dev/null | head -1)"
[ -n "$GPTK_APP" ] && run xattr -dr com.apple.quarantine "$GPTK_APP"
WINE_BIN="$(find "${GPTK_APP:-/Applications}" -type f \( -name wine64 -o -name wine \) -path '*wine/bin/*' 2>/dev/null | head -1)"
if [ -z "$WINE_BIN" ]; then
    log "ERROR: could not find the GPTK Wine binary under $GPTK_APP"
    log "       Expected something like .../Game Porting Toolkit.app/Contents/Resources/wine/bin/wine64"
    exit 1
fi
GPTK_WRAPPER="$(find "${GPTK_APP:-/Applications}" -type f -name 'gameportingtoolkit*' 2>/dev/null | head -1)"
log "GPTK app   : ${GPTK_APP:-(?)}"
log "Wine binary: $WINE_BIN"
log "GPTK wrapper: ${GPTK_WRAPPER:-(none found - will run wine directly)}"

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
    log "ERROR: prefix '$PREFIX' already exists. Remove it (rm -rf '$PREFIX') or pass"
    log "       another path: ./setup_gum_mac_metal.sh ~/.other_prefix"
    exit 1
fi
log "Creating Wine prefix at $PREFIX ..."
run "$WINE_BIN" wineboot --init
run "$WINESERVER" -w

###############################################################################
### 4) Fonts + .NET 8 desktop runtime
###############################################################################
log "Installing fonts (arial tahoma courier calibri) ..."
for f in arial tahoma courier calibri; do run winetricks -q "$f"; done
log "Installing .NET 8 desktop runtime (this takes a few minutes) ..."
run winetricks -q dotnetdesktop8

###############################################################################
### 5) Download + extract the stock Gum release (no code changes needed)
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
### 6) ~/bin/gum launcher (run / upgrade / uninstall / prefix)
###
### D3DMetal is GPTK's built-in Direct3D implementation; running Gum.exe through
### the GPTK Wine should use it. If a 'gameportingtoolkit' wrapper is present we
### prefer it (it sets the D3DMetal env Apple expects).
### *** Vic's on-machine AI: please verify this launch actually engages D3DMetal
###     (e.g. set MTL_HUD_ENABLED=1 to see the Metal HUD), and adjust if needed. ***
###############################################################################
mkdir -p ~/bin
cat > ~/bin/gum <<EOF
#!/bin/bash
# Gum launcher (Wine + GPTK/D3DMetal). Generated by setup_gum_mac_metal.sh v$SCRIPT_VERSION
export WINEPREFIX="$PREFIX"
export WINE_NO_WM_DECORATION=1
export WINEESYNC=1
unset DOTNET_ROOT DOTNET_ROOT_X64
WINE_BIN="$WINE_BIN"
GPTK_WRAPPER="$GPTK_WRAPPER"
GUM_EXE="$GUM_EXE"

case "\${1:-}" in
  ""|run)
      if [ -n "\$GPTK_WRAPPER" ] && [ -x "\$GPTK_WRAPPER" ]; then
          exec "\$GPTK_WRAPPER" "$PREFIX" "\$GUM_EXE"
      else
          exec "\$WINE_BIN" "\$GUM_EXE"
      fi ;;
  upgrade)
      ZIP="$PREFIX/drive_c/Program Files/Gum.zip"
      curl -fL -o "\$ZIP" "$GUM_ZIP_URL" || { echo "download failed"; exit 1; }
      rm -rf "$GUM_DIR"; unzip -q "\$ZIP" -d "$GUM_DIR"; rm -f "\$ZIP"; echo "Gum updated." ;;
  prefix)   echo "$PREFIX" ;;
  uninstall)
      read -p "Remove the Gum Wine prefix at $PREFIX and ~/bin/gum? (y/N) " a
      case "\$a" in y|Y) rm -rf "$PREFIX"; rm -f ~/bin/gum; echo "Removed.";; *) echo "Cancelled.";; esac ;;
  *) echo "Usage: gum [run|upgrade|uninstall|prefix]" ;;
esac
EOF
chmod +x ~/bin/gum

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
echo "Run Gum:      ~/bin/gum"
echo "Update:       ~/bin/gum upgrade"
echo "Remove:       ~/bin/gum uninstall"
echo
echo "If Gum does not open, capture the log and send it back:"
echo "    ~/bin/gum > ~/gum-metal-output.log 2>&1   (plus $LOG)"
echo
echo "Watch for: does the editor window open AND the design canvas render?"
