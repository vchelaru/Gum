#!/bin/bash

################################################################################
### Builds GumFull.sln and runs the result through Wine, for developers
### iterating on Gum tool source from Linux. One command: build + run any
### commit, no Rider/Visual Studio/other IDE required.
###
### This is NOT the same as setup_gum_linux.sh, which downloads and installs
### the official released build. This script instead builds and launches your
### own checkout's source through an existing Wine prefix, so you can test
### source changes without a Windows machine.
###
### A plain `dotnet build` does not produce a native Gum.exe apphost - only a
### framework-dependent Gum.dll, launched via `dotnet Gum.dll`. That works for
### everything except EditorTabPlugin_XNA's KNI-based render viewport, which
### silently fails to initialize under Wine when launched that way (no
### exception anywhere - AppDomain/WinForms/WPF/TaskScheduler unhandled-
### exception hooks all stay silent, it just never renders). Launched as a
### real Gum.exe, the same build renders correctly. So this script builds a
### lean native apphost (not a full self-contained publish - that also works
### but is far slower and produces a ~100MB+ single-file bundle for no benefit
### here) and launches that instead.
###
### Prerequisite: a Wine prefix with the .NET 8 desktop runtime installed. The
### prefix created by setup_gum_linux.sh (default: ~/.wine_gum_dotnet8)
### already has this; run that script first if you haven't.
###
### Usage:
###   ./run_gum_linux.sh [Debug|Release]
###   GUM_WINE_PREFIX_PATH=/custom/prefix ./run_gum_linux.sh
################################################################################

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${1:-Debug}"
GUM_WINE_PREFIX_PATH="${GUM_WINE_PREFIX_PATH:-$HOME/.wine_gum_dotnet8}"
GUM_OUTPUT_DIR="$SCRIPT_DIR/Gum/bin/$CONFIGURATION"

if [ ! -d "$GUM_WINE_PREFIX_PATH" ]; then
    echo "ERROR: No Wine prefix found at $GUM_WINE_PREFIX_PATH."
    echo "Run setup_gum_linux.sh first to install Wine and the .NET 8 desktop runtime into a prefix."
    exit 1
fi

# Full solution build - not just Gum.csproj - since plugin projects deploy
# themselves into Gum/bin/.../Plugins/ via their own post-build steps, which
# only run when those projects build (see GumFull.sln builds, not individual
# csproj builds, in CLAUDE.md).
echo "Building GumFull.sln ($CONFIGURATION)..."
dotnet build "$SCRIPT_DIR/GumFull.sln" -c "$CONFIGURATION" --nologo -v quiet

echo "Building a native Gum.exe apphost for $CONFIGURATION..."
dotnet build "$SCRIPT_DIR/Gum/Gum.csproj" -c "$CONFIGURATION" -r win-x64 --self-contained false \
    -p:UseAppHost=true -p:AppendRuntimeIdentifierToOutputPath=false --nologo -v quiet

# Overwrite DOTNET environment variables that if set will break dotnet apps under Wine
# https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_root-dotnet_rootx86-dotnet_root_x86-dotnet_root_x64
# https://github.com/vchelaru/Gum/issues/1957
unset DOTNET_ROOT
unset DOTNET_ROOT_X64

export WINE_NO_WM_DECORATION=1
export PROTON_NO_WM_DECORATION=1

echo "Launching $GUM_OUTPUT_DIR/Gum.exe via Wine prefix $GUM_WINE_PREFIX_PATH..."
cd "$GUM_OUTPUT_DIR"
WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine Gum.exe
