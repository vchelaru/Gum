#!/bin/bash

################################################################################
### Check if wine-stable is installed
################################################################################

set -e

GUM_WINE_PREFIX_PATH=$HOME/.wine_gum_prefix/

echo "This is an experimental script to remove the automated install of wine, winetricks, and GUM on Linux systems."
echo "If you did not install GUM using the setup_gum_linux.sh script, please do not run this script !!!!"
echo "Script last updated on the 15th of November 2025!"

read -p "Do you wish to continue? (y/n): " choice
case "$choice" in
  y|Y ) echo "Continuing...";;
  n|N ) echo "Exiting."; exit 0;;
  * ) echo "Invalid option. Exiting."; exit 1;;
esac


rm -rf $GUM_WINE_PREFIX_PATH && echo "Removed wine folder $GUM_WINE_PREFIX_PATH"

# Uninstall depending on the OS
DISTRO=$(( lsb_release -si 2>/dev/null || grep '^ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[0-9\.]/}" 2>/dev/null || name ) | cut -d= -f2 | tr -d '"' | tr '[:upper:]'  '[:lower:]')
VERSION=$(( lsb_release -sr 2>/dev/null || grep '^VERSION_ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[A-Za-z]/}" 2>/dev/null | cut -d '.' -f1 || sw_vers --productVersion ) | cut -d= -f2 | tr -d '"' | cut -c1-2 )


case "$DISTRO" in
    ubuntu|linuxmint)

        sudo apt remove --purge winetricks || echo "ERROR: Failed to uninstall winetricks"
        sudo apt remove --purge winehq-* wine-* || echo "ERROR: Failed to uninstall wine"
        sudo rm /etc/apt/keyrings/winehq-archive.key

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
### Finished
################################################################################
echo -e "\nFinished!  Wine and GUM have been removed from your system."
echo "Leftover installers may be purged with sudo apt autoremove && sudo apt clean."
echo "If you wish to reinstall GUM, please run setup_gum_linux.sh again."

