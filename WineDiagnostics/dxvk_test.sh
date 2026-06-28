#!/bin/bash
###############################################################################
# dxvk_test.sh (v2) - one-shot macOS test using DXVK 1.10.3.
#
# DXVK 2.x/3.x require Vulkan 1.3, which MoltenVK does not fully expose, so they
# fail with "No adapters found". DXVK 1.10.3 only needs Vulkan 1.1, which MoltenVK
# supports - so it can actually lift Wine's Direct3D ceiling to FL11 via Metal.
#
# Steps (no manual typing on the Mac beyond running this):
#   1. Download DXVK 1.10.3 and install its d3d11/dxgi into the Gum wine prefix.
#   2. Run Probe5 (console, quick) to check the Direct3D feature level + verdict.
#   3. Launch the STOCK, UNMODIFIED Gum.exe to see if it now starts.
#   4. Bundle all logs into ~/dxvk_test_results.tar.gz to send back.
#
# Usage:  chmod +x dxvk_test.sh && ./dxvk_test.sh [PROBE_DIR]
###############################################################################
set -u

PREFIX="${WINEPREFIX:-$HOME/.wine_gum_dotnet8}"
PROBE_DIR="${1:-$HOME/gum-diag}"
DXVK_VER="1.10.3"
RESULTS="$HOME/dxvk_test_results_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$RESULTS"

echo "=== Gum macOS DXVK $DXVK_VER test ==="
echo "prefix  : $PREFIX"
echo "results : $RESULTS"
echo

# --- preflight ---------------------------------------------------------------
if [ ! -d "$PREFIX" ]; then echo "ERROR: wine prefix not found: $PREFIX"; exit 1; fi
if ! command -v wine >/dev/null 2>&1; then echo "ERROR: 'wine' not on PATH"; exit 1; fi

export WINEPREFIX="$PREFIX"
unset DOTNET_ROOT DOTNET_ROOT_X64

# Point Wine's Vulkan at Homebrew's MoltenVK if present.
if command -v brew >/dev/null 2>&1; then
    MVK_ICD="$(brew --prefix molten-vk 2>/dev/null)/share/vulkan/icd.d/MoltenVK_icd.json"
    [ -f "$MVK_ICD" ] && export VK_ICD_FILENAMES="$MVK_ICD" && echo "MoltenVK ICD: $MVK_ICD" && echo
fi

# --- 1) install DXVK 1.10.3 --------------------------------------------------
TARBALL="/tmp/dxvk-$DXVK_VER.tar.gz"
URL="https://github.com/doitsujin/dxvk/releases/download/v$DXVK_VER/dxvk-$DXVK_VER.tar.gz"
SYS32="$PREFIX/drive_c/windows/system32"

echo "[1/3] Downloading DXVK $DXVK_VER..."
if ! curl -L -o "$TARBALL" "$URL" > "$RESULTS/dxvk_download.log" 2>&1; then
    echo "ERROR: download failed (see $RESULTS/dxvk_download.log)"; exit 1
fi
rm -rf "/tmp/dxvk-$DXVK_VER"
tar -xzf "$TARBALL" -C /tmp

echo "      Installing DXVK $DXVK_VER d3d11/dxgi/d3d10core/d3d9 (x64) into the prefix..."
: > "$RESULTS/dxvk_install.log"
for dll in d3d11 dxgi d3d10core d3d9; do
    cp "/tmp/dxvk-$DXVK_VER/x64/$dll.dll" "$SYS32/" 2>> "$RESULTS/dxvk_install.log" \
        && echo "        copied $dll.dll" \
        || echo "        WARN: could not copy $dll.dll"
    wine reg add "HKCU\\Software\\Wine\\DllOverrides" /v "$dll" /t REG_SZ /d native /f >> "$RESULTS/dxvk_install.log" 2>&1
done
echo "      done."
echo

# --- 2) confirm the feature level (Probe5 - console, no window to hang) -------
P5=$(find "$PROBE_DIR" -iname "Probe5.Direct3D11.exe" -type f 2>/dev/null | head -1)
if [ -n "$P5" ]; then
    echo "[2/3] Checking the Direct3D feature level (Probe5)..."
    export PROBE_LOG_DIR="$RESULTS"
    WINEDEBUG=-all wine "$P5" > "$RESULTS/Probe5.console.log" 2>&1
    grep -aE "FeatureLevel|Result|RESULT" "$RESULTS/Probe5.Direct3D11.log" 2>/dev/null | sed 's/^/        /'
    if grep -aqE "FeatureLevel = (1[012]_)" "$RESULTS/Probe5.Direct3D11.log" 2>/dev/null; then
        echo "      VERDICT: ceiling LIFTED to FL10/11 via DXVK+MoltenVK - the stock tool should run."
    else
        echo "      VERDICT: still no FL10+ - DXVK init likely failed; see Probe5.console.log."
    fi
    echo
else
    echo "[2/3] Probe5 not found under $PROBE_DIR - skipping ceiling check."
    echo
fi

# --- 3) launch the stock (unmodified) Gum ------------------------------------
GUM_EXE=$(find "$PREFIX/drive_c" -iname "Gum.exe" -type f 2>/dev/null | head -1)
if [ -z "$GUM_EXE" ]; then
    echo "ERROR: stock Gum.exe not found under $PREFIX/drive_c"; exit 1
fi
echo "[3/3] Launching STOCK Gum.exe: $GUM_EXE"
echo "      If it works, the editor window opens - CLOSE it when done and the script finishes."
echo "      (output + any backtrace -> $RESULTS/gum-stock-output.log)"
WINEDEBUG=+seh wine "$GUM_EXE" > "$RESULTS/gum-stock-output.log" 2>&1

# --- bundle ------------------------------------------------------------------
echo
BUNDLE="$HOME/dxvk_test_results.tar.gz"
tar -czf "$BUNDLE" -C "$(dirname "$RESULTS")" "$(basename "$RESULTS")"
echo "=== done ==="
echo "Send back: $BUNDLE"
