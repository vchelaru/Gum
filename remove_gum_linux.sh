#!/bin/bash

################################################################################
### Check if wine-stable is installed
################################################################################

SCRIPT_VERSION="2026.02.16"

# Determine the wine prefix: CLI arg > read from gum launcher > default
GUM_WINE_PREFIX_PATH="$HOME/.wine_gum_dotnet8"
if [ -n "$1" ]; then
    GUM_WINE_PREFIX_PATH="$1"
elif [ -f ~/bin/gum ]; then
    FOUND=$(grep 'WINEPREFIX=' ~/bin/gum | head -1 | sed 's/.*WINEPREFIX="//' | sed 's/".*//')
    if [ -n "$FOUND" ]; then
        GUM_WINE_PREFIX_PATH="$FOUND"
    fi
fi

echo "GUM removal script (v$SCRIPT_VERSION)"
echo "This will attempt to remove the automated install of wine, winetricks, and GUM on Linux systems."
echo "If you did not install GUM using the setup_gum_linux.sh script, please do not run this script !!!!"
echo "Using WINE PREFIX: $GUM_WINE_PREFIX_PATH"
echo ""

read -p "Do you wish to continue? (y/n): " choice
case "$choice" in
  y|Y ) echo "Continuing...";;
  n|N ) echo "Exiting."; exit 0;;
  * ) echo "Invalid option. Exiting."; exit 1;;
esac


if ! rm -rf "$GUM_WINE_PREFIX_PATH"; then
    echo "ERROR: Failed to remove wine folder $GUM_WINE_PREFIX_PATH"
    exit 1
fi
echo "Removed wine folder $GUM_WINE_PREFIX_PATH"

################################################################################
### Remove the gum launcher script
################################################################################
if [ -f ~/bin/gum ]; then
    if ! rm -f ~/bin/gum; then
        echo "ERROR: Failed to remove ~/bin/gum launcher script"
        exit 1
    fi
    echo "Removed ~/bin/gum launcher script"
fi

# Uninstall depending on the OS
DISTRO=$(( lsb_release -si 2>/dev/null || grep '^ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[0-9\.]/}" 2>/dev/null || name ) | cut -d= -f2 | tr -d '"' | tr '[:upper:]'  '[:lower:]')
VERSION=$(( lsb_release -sr 2>/dev/null || grep '^VERSION_ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[A-Za-z]/}" 2>/dev/null | cut -d '.' -f1 || sw_vers --productVersion ) | cut -d= -f2 | tr -d '"' | cut -c1-2 )


case "$DISTRO" in
    ubuntu|linuxmint)

        sudo apt remove --purge winetricks -y || echo "ERROR: Failed to uninstall winetricks"
        sudo apt remove --purge winehq-* wine-* -y || echo "ERROR: Failed to uninstall wine"
        sudo rm -f /etc/apt/keyrings/winehq-archive.key 2>/dev/null || true
        sudo rm -f /etc/apt/sources.list.d/winehq-*.sources 2>/dev/null || true

        ;;

    fedora|nobara)

        sudo dnf remove winetricks || echo "ERROR: Failed to uninstall winetricks"
        sudo dnf remove winehq-* wine-*  || echo "ERROR: Failed to uninstall wine"
        ;;


    darwin)
        brew uninstall winetricks || echo "ERROR: Failed to uninstall winetricks"
        brew uninstall --cask wine-stable || echo "ERROR: Failed to uninstall wine-stable"
        echo "BREW will not be uninstalled, if you do no want that, you can uninstall that manually"
        ;;
        
    *)
        echo "Unsupported or unknown distribution: $DISTRO"
        echo "Please uninstall wine manually!"
        echo "https://duckduckgo.com/?t=h_&q=Insert+Your+Linux+Distro+Here+How+To+Install+Wine"
        exit 1
        ;;
esac


################################################################################
### Clean up PATH entries added by setup script (safe: only removes exact lines)
################################################################################
if [ -f ~/.bashrc ] && grep -q 'export PATH="$HOME/bin:$PATH"' ~/.bashrc 2>/dev/null; then
    if ! sed -i '\|export PATH="$HOME/bin:$PATH"|d' ~/.bashrc; then
        echo "ERROR: Failed to update ~/.bashrc"
        exit 1
    fi
    echo "Removed PATH entry from ~/.bashrc"
fi
if [ -f ~/.zshrc ] && grep -q 'export PATH="$HOME/bin:$PATH"' ~/.zshrc 2>/dev/null; then
    if ! sed -i '\|export PATH="$HOME/bin:$PATH"|d' ~/.zshrc; then
        echo "ERROR: Failed to update ~/.zshrc"
        exit 1
    fi
    echo "Removed PATH entry from ~/.zshrc"
fi
if [ -f ~/.config/fish/config.fish ] && grep -q 'set -x PATH $HOME/bin $PATH' ~/.config/fish/config.fish 2>/dev/null; then
    if ! sed -i '\|set -x PATH $HOME/bin $PATH|d' ~/.config/fish/config.fish; then
        echo "ERROR: Failed to update config.fish"
        exit 1
    fi
    echo "Removed PATH entry from config.fish"
fi

################################################################################
### Finished
################################################################################
echo -e "\nFinished!  Wine and GUM have been removed from your system."
echo "Leftover installers may be purged with sudo apt autoremove && sudo apt clean."
echo "If you wish to reinstall GUM, please run setup_gum_linux.sh again."

