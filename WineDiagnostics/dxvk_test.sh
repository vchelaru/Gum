#!/bin/bash
###############################################################################
# dxvk_test.sh - one-shot macOS test of the "lift the D3D ceiling with DXVK" fix.
#
# Does everything so you don't have to type on the second machine:
#   1. Installs DXVK into the Gum wine prefix (D3D11 -> Vulkan -> MoltenVK -> Metal).
#   2. Re-runs Probe5 (raw D3D11) + Probe6 (KNI FL10_0) to confirm the Direct3D
#      feature level lifted from 9_3, and prints a verdict.
#   3. Launches the STOCK, UNMODIFIED Gum.exe to see if it now starts.
#   4. Bundles every log into ~/dxvk_test_results.tar.gz to send back.
#
# Usage:  chmod +x dxvk_test.sh && ./dxvk_test.sh [PROBE_DIR]
#   PROBE_DIR  folder holding the probe exes (default: ~/gum-diag). Optional; if the
#              probes aren't found, step 2 is skipped and it still tests the tool.
###############################################################################
set -u

PREFIX="${WINEPREFIX:-$HOME/.wine_gum_dotnet8}"
PROBE_DIR="${1:-$HOME/gum-diag}"
RESULTS="$HOME/dxvk_test_results_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$RESULTS"

echo "=== Gum macOS DXVK test ==="
echo "prefix  : $PREFIX"
echo "probes  : $PROBE_DIR"
echo "results : $RESULTS"
echo

# --- preflight ---------------------------------------------------------------
if [ ! -d "$PREFIX" ]; then echo "ERROR: wine prefix not found: $PREFIX"; exit 1; fi
if ! command -v wine >/dev/null 2>&1; then echo "ERROR: 'wine' not on PATH"; exit 1; fi
if ! command -v winetricks >/dev/null 2>&1; then echo "ERROR: 'winetricks' not on PATH"; exit 1; fi

export WINEPREFIX="$PREFIX"
# DOTNET_ROOT* break dotnet apps under wine (Gum issue #1957).
unset DOTNET_ROOT DOTNET_ROOT_X64

# Help wine/DXVK find MoltenVK's Vulkan driver if Homebrew installed it.
if command -v brew >/dev/null 2>&1; then
    MVK_ICD="$(brew --prefix molten-vk 2>/dev/null)/share/vulkan/icd.d/MoltenVK_icd.json"
    if [ -f "$MVK_ICD" ]; then
        export VK_ICD_FILENAMES="$MVK_ICD"
        echo "MoltenVK ICD: $MVK_ICD"
        echo
    fi
fi

# --- 1) install DXVK ---------------------------------------------------------
echo "[1/3] Installing DXVK into the prefix (this can take a minute)..."
winetricks -q dxvk > "$RESULTS/dxvk_install.log" 2>&1
echo "      done  ->  $RESULTS/dxvk_install.log"
echo

# --- 2) confirm the ceiling lifted via the probes ----------------------------
P5=$(find "$PROBE_DIR" -iname "Probe5.Direct3D11.exe" -type f 2>/dev/null | head -1)
P6=$(find "$PROBE_DIR" -iname "Probe6.KniDx11.exe" -type f 2>/dev/null | head -1)
if [ -n "$P5" ]; then
    echo "[2/3] Re-checking the Direct3D feature level..."
    export PROBE_LOG_DIR="$RESULTS"
    export PROBE_HOLD_SECONDS=1
    WINEDEBUG=-all wine "$P5" > "$RESULTS/Probe5.console.log" 2>&1
    [ -n "$P6" ] && WINEDEBUG=-all wine "$P6" > "$RESULTS/Probe6.console.log" 2>&1

    echo "      --- Probe5 (raw D3D11) ---"
    grep -aE "FeatureLevel|Result" "$RESULTS/Probe5.Direct3D11.log" 2>/dev/null | sed 's/^/        /'
    echo "      --- Probe6 (KNI FL10_0) ---"
    grep -aE "RESULT:|Reach|FL10_0|Adapter" "$RESULTS/Probe6.KniDx11.log" 2>/dev/null | sed 's/^/        /'

    if grep -aqE "FeatureLevel = (1[012]_)" "$RESULTS/Probe5.Direct3D11.log" 2>/dev/null; then
        echo "      VERDICT: ceiling LIFTED above 9_3 - DXVK is working; the stock tool should run."
    else
        echo "      VERDICT: still capped (no FL10/11) - DXVK likely not active. Check dxvk_install.log"
        echo "               and that MoltenVK is installed (brew install molten-vk)."
    fi
    echo
else
    echo "[2/3] Probe exes not found under $PROBE_DIR - skipping the ceiling check."
    echo "      (re-run as './dxvk_test.sh /path/to/probe/folder' to include it)"
    echo
fi

# --- 3) launch the stock (unmodified) Gum ------------------------------------
GUM_EXE=$(find "$PREFIX/drive_c" -iname "Gum.exe" -type f 2>/dev/null | head -1)
if [ -z "$GUM_EXE" ]; then
    echo "ERROR: stock Gum.exe not found under $PREFIX/drive_c (run setup_gum_mac.sh first)."
    exit 1
fi
echo "[3/3] Launching STOCK Gum.exe: $GUM_EXE"
echo "      If DXVK lifted the ceiling, the editor window should open."
echo "      -> Close the Gum window when you're done looking; this script then finishes."
echo "      (output + any crash backtrace -> $RESULTS/gum-stock-output.log)"
WINEDEBUG=+seh wine "$GUM_EXE" > "$RESULTS/gum-stock-output.log" 2>&1

# --- bundle ------------------------------------------------------------------
echo
BUNDLE="$HOME/dxvk_test_results.tar.gz"
tar -czf "$BUNDLE" -C "$(dirname "$RESULTS")" "$(basename "$RESULTS")"
echo "=== done ==="
echo "Send this back: $BUNDLE"
