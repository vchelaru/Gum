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

rm -rf $GUM_WINE_PREFIX_PATH
sudo apt remove --purge winetricks
sudo apt remove --purge winehq-* wine-* 

################################################################################
### Finished
################################################################################
echo "Finished!  Wine and GUM have been removed from your system."
echo "Leftover installers may be purged with sudo apt autoremove && sudo apt clean."
echo "If you wish to reinstall GUM, please run setup_gum_linux.sh again."
