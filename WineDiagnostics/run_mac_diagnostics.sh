#!/bin/bash
################################################################################
### Gum macOS Wine diagnostic runner
###
### Runs each probe under Wine with crash diagnostics enabled and collects the
### logs. The probes are ordered from "most basic" to "most likely to fail", so
### the FIRST probe that fails localizes the broken layer of the Gum tool's
### startup stack.
###
### Usage:
###   ./run_mac_diagnostics.sh [WINE_PREFIX] [DIST_DIR]
###
###   WINE_PREFIX  Wine prefix to run in (default: $HOME/.wine_gum_dotnet8,
###                the prefix created by setup_gum_mac.sh)
###   DIST_DIR     Folder containing the published probe folders
###                (default: the folder this script lives in)
###
### Environment overrides:
###   PROBE_HOLD_SECONDS  How long windowed probes stay open (default 2)
###   WINEDEBUG           Wine debug channels (default +seh,+loaddll)
################################################################################
set -u

GUM_WINE_PREFIX_PATH="${1:-$HOME/.wine_gum_dotnet8}"
DIST_DIR="${2:-$(cd "$(dirname "$0")" && pwd)}"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_DIR="$DIST_DIR/results_$TIMESTAMP"
mkdir -p "$RESULTS_DIR"

echo "Wine prefix : $GUM_WINE_PREFIX_PATH"
echo "Dist dir    : $DIST_DIR"
echo "Results dir : $RESULTS_DIR"
echo

if [ ! -d "$GUM_WINE_PREFIX_PATH" ]; then
    echo "ERROR: Wine prefix not found at $GUM_WINE_PREFIX_PATH"
    echo "Run setup_gum_mac.sh first, or pass the prefix path as the first argument."
    exit 1
fi

if ! command -v wine >/dev/null 2>&1; then
    echo "ERROR: 'wine' is not on the PATH. Install it (see setup_gum_mac.sh) and retry."
    exit 1
fi

# Probes in increasing order of likelihood-to-fail.
PROBES=(
    "Probe0.Runtime"
    "Probe1.Wpf"
    "Probe2.WinForms"
    "Probe3.WindowsFormsHost"
    "Probe4.SkiaCpu"
    "Probe5.Direct3D11"
    "Probe6.KniDx11"
    "Probe7.MonoGameDesktopGL"
    "Probe8.MonoGameWindowsDX"
)

# Diagnostics: first-chance exceptions + DLL load tracing, and a .NET managed crash dump.
export WINEPREFIX="$GUM_WINE_PREFIX_PATH"
export WINEDEBUG="${WINEDEBUG:-+seh,+loaddll}"
export PROBE_LOG_DIR="$RESULTS_DIR"
export PROBE_HOLD_SECONDS="${PROBE_HOLD_SECONDS:-2}"
export WINE_NO_WM_DECORATION=1
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpType=4

# DOTNET_ROOT* vars, if set, break dotnet apps under Wine. See Gum issue #1957.
unset DOTNET_ROOT
unset DOTNET_ROOT_X64

SUMMARY=()
for probe in "${PROBES[@]}"; do
    EXE=$(find "$DIST_DIR/$probe" -maxdepth 1 -iname "$probe.exe" -type f 2>/dev/null | head -n1)
    if [ -z "$EXE" ]; then
        echo "[$probe] SKIP - exe not found under $DIST_DIR/$probe"
        SUMMARY+=("$probe : SKIP (exe missing)")
        continue
    fi

    export DOTNET_DbgMiniDumpName="$RESULTS_DIR/$probe.%d.dmp"
    CONSOLE_LOG="$RESULTS_DIR/$probe.console.log"

    echo "[$probe] running ..."
    wine "$EXE" > "$CONSOLE_LOG" 2>&1
    CODE=$?

    PROBE_LOG="$RESULTS_DIR/$probe.log"
    if grep -q "RESULT: PASS" "$PROBE_LOG" 2>/dev/null; then
        RESULT="PASS"
    elif grep -q "RESULT: FAIL" "$PROBE_LOG" 2>/dev/null; then
        RESULT="FAIL"
    else
        RESULT="CRASH (no result line)"
    fi
    echo "[$probe] -> $RESULT (exit=$CODE)"
    SUMMARY+=("$probe : $RESULT (exit=$CODE)")
done

echo
echo "================ SUMMARY ================"
for line in "${SUMMARY[@]}"; do
    echo "  $line"
done
echo "========================================"
echo "The first non-PASS probe localizes the broken layer (see README.md)."
echo

# Bundle results for attaching to a GitHub issue / Discord message.
BUNDLE="$DIST_DIR/gum-mac-diag-results-$TIMESTAMP.tar.gz"
tar -czf "$BUNDLE" -C "$DIST_DIR" "results_$TIMESTAMP"
echo "Results bundled: $BUNDLE"
echo "Please attach this file when reporting (it contains all probe + Wine + crash-dump logs)."
