#!/bin/bash

################################################################################
### macOS setup script for Gum (Wine-based)
### Works on both Intel and Apple Silicon Macs
################################################################################

set -e

SCRIPT_VERSION="2026.02.16"

if [ -z "$1" ]; then
    GUM_WINE_PREFIX_PATH="$HOME/.wine_gum_dotnet8"
else
    GUM_WINE_PREFIX_PATH="$1"
fi

INSTALL_LOG_FILE="/tmp/gum_install_$(date +%Y%m%d_%H%M%S).log"

write_log_section_header() {
    echo "#############################################" >> "$INSTALL_LOG_FILE" 2>&1
    echo "$@" >> "$INSTALL_LOG_FILE" 2>&1
    echo "#############################################" >> "$INSTALL_LOG_FILE" 2>&1
    echo "" >> "$INSTALL_LOG_FILE" 2>&1
}

run_and_write_log() {
    eval "$@" >> "$INSTALL_LOG_FILE" 2>&1
    echo "" >> "$INSTALL_LOG_FILE" 2>&1
}

run_winetricks() {
    write_log_section_header "Running WINEPREFIX=\"$GUM_WINE_PREFIX_PATH\" winetricks \"$@\""
    run_and_write_log WINEPREFIX="$GUM_WINE_PREFIX_PATH" winetricks "$@"
}


echo "" > "$INSTALL_LOG_FILE" # clear the log file
write_log_section_header "Starting installation process..."

echo "This is an experimental script. (v$SCRIPT_VERSION)"
echo "This will set up a new Wine prefix for gum in $GUM_WINE_PREFIX_PATH"
echo "Install logs will be written to $INSTALL_LOG_FILE"

read -p "Do you wish to continue? (y/n): " choice
case "$choice" in
  y|Y ) echo "Continuing...";;
  n|N ) echo "Exiting."; exit 0;;
  * ) echo "Invalid option. Exiting."; exit 1;;
esac

################################################################################
### Check if wine-stable is installed
################################################################################
echo -e "\nVerifying that WINE is installed..."
WINE_VERSION=$(wine --version 2>/dev/null | grep -Eo '[0-9]+' | head -n1)
INSTALL_OR_UPGRADE_NEEDED="N"
if [[ ! "${WINE_VERSION}" ]]; then
    echo "Wine is not installed!"
    INSTALL_OR_UPGRADE_NEEDED="Y"
elif [[ "${WINE_VERSION}" -lt 10 ]]; then
    echo "Wine is version [${WINE_VERSION}] and must be at least 10!"
    INSTALL_OR_UPGRADE_NEEDED="Y"
else
    echo "Wine version [${WINE_VERSION}] found!"
fi

VERSION=$(sw_vers --productVersion 2>/dev/null | cut -d '.' -f1)

# Install or update wine
if [[ "${INSTALL_OR_UPGRADE_NEEDED}" == "Y" ]]; then
    echo -e "\nDetected macOS Version ${VERSION}"

    echo -e "\nVerifying that BREW is installed..."
    BREW_VERSION=$(brew --version 2>/dev/null | grep -Eo '[0-9]+' | head -n1)
    BREW_INSTALL_REQUIRED="N"
    if [[ ! "${BREW_VERSION}" ]]; then
        echo "Brew is not installed!"
        BREW_INSTALL_REQUIRED="Y"
    else
        echo "Brew version [${BREW_VERSION}] found!"
    fi

    if [[ "${BREW_INSTALL_REQUIRED}" == "Y" ]]; then
        read -p "Do you wish to install brew (home brew)? (Y/n): " choice
        case "$choice" in
            ""|y|Y ) echo "INFO: Installing brew";;
            n|N ) echo "WARN: Unable to continue, GUM requires Wine on Mac which is installed with brew!"; exit 0;;
            * ) echo "ERROR: Invalid option. Exiting."; exit 1;;
        esac

        echo -e "\nAttempting to install brew..."

        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

        # Load brew into PATH for this session (not in PATH after fresh install)
        if [ -x /opt/homebrew/bin/brew ]; then
            eval "$(/opt/homebrew/bin/brew shellenv)"
        elif [ -x /usr/local/bin/brew ]; then
            eval "$(/usr/local/bin/brew shellenv)"
        else
            echo "ERROR: Homebrew installed but 'brew' not found in expected locations."
            exit 1
        fi
    fi

    echo -e "\nAttempting to install Wine-stable"
    brew install --cask --no-quarantine wine-stable
fi

################################################################################
### Check if winetricks is installed
################################################################################
if ! winetricks --version &> /dev/null; then
    echo -e "\nWinetricks is not installed. Attempting to install..."
    brew install winetricks
fi

echo "Winetricks is installed"

################################################################################
### Check if winetricks is newer than version 2024
###   The 2025 version has the new dotnetdesktop8 verb
################################################################################
echo "Verifying winetricks version..."
WINETRICKS_YEAR=$(winetricks --version 2>/dev/null | grep -Eo '[0-9]{4}' | head -n1)
if [[ ! "${WINETRICKS_YEAR}" || "${WINETRICKS_YEAR}" -le 2024 ]]; then
    echo "Winetricks version is older than 2024 or could not be determined."
    echo " - A newer version is required for dotnet 8. Attempting to update..."
    echo " - Attempting to upgrade winetricks via brew..."
    brew upgrade winetricks
else
    echo "Winetricks version is new enough ${WINETRICKS_YEAR}."
fi


################################################################################
### Install MoltenVK (Vulkan-to-Metal translation layer)
### macOS caps OpenGL at 4.1 and has deprecated it. MoltenVK provides Vulkan
### support so WineD3D can use its Vulkan backend as an alternative to OpenGL.
################################################################################
echo -e "\nInstalling MoltenVK (Vulkan support for macOS)..."
brew install molten-vk || echo "WARN: Failed to install MoltenVK - Vulkan renderer will not be available"

################################################################################
### Make sure gum prefix is clear
################################################################################
if [ -d "$GUM_WINE_PREFIX_PATH" ]; then
    echo "Error: The gum wine prefix directory '$GUM_WINE_PREFIX_PATH' already exists."
    echo "This script can only be used on the initial creation of the gum wine prefix."
    echo "Call '~/bin/gum upgrade' if upgrading the gum version, or call this script with a different directory to setup gum in (e.g. './setup_gum_mac.sh ~/.other_gum_wine_prefix')"
    exit 1
fi

################################################################################
### Install fonts with winetricks
################################################################################
echo "Checking and Installing Some fonts using winetricks"
echo " - They may take a few minutes to install, please be patient"
run_winetricks arial
run_winetricks tahoma
run_winetricks courier
run_winetricks calibri
echo " - Fonts installed"

################################################################################
### Install dotnetdesktop8 with winetricks
################################################################################
echo "Installing .NET 8 using winetricks (this may take a few minutes)..."
run_winetricks -q dotnetdesktop8

################################################################################
### Configure WineD3D renderer (after .NET install to avoid registry reset)
### macOS OpenGL is capped at 4.1 (deprecated by Apple). If MoltenVK is
### available, we default to the Vulkan renderer. Otherwise, fall back to
### OpenGL which still works for many apps including Gum.
### Users can switch with: ~/bin/gum vulkan  or  ~/bin/gum opengl
################################################################################
if brew list molten-vk &> /dev/null; then
    echo "MoltenVK found. Setting WineD3D renderer to Vulkan..."
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\Direct3D" /v renderer /t REG_SZ /d vulkan /f >> "$INSTALL_LOG_FILE" 2>&1
    echo " - Vulkan renderer set as default (via MoltenVK -> Metal)"
else
    echo "MoltenVK not found. Using OpenGL renderer (capped at 4.1 on macOS)."
    echo " - If you experience rendering issues, install MoltenVK: brew install molten-vk"
    echo " - Then switch renderer: ~/bin/gum vulkan"
fi

################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."
GUM_ZIP_FILE="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"

curl -L -o "$GUM_ZIP_FILE" "$GUM_ZIP_DOWNLOAD" \
    && echo " - Download completed." || { echo "Download failed using CURL."; exit 1; }

################################################################################
### Unzip the gum.zip file into Program Files/Gum
################################################################################
echo "Extracting GUM Tool..."
GUM_WINE_EXTRACT_DIR="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum"
rm -rf "$GUM_WINE_EXTRACT_DIR"
unzip -q "$GUM_ZIP_FILE" -d "$GUM_WINE_EXTRACT_DIR" \
    && echo "Extraction completed." || { echo "Extraction failed."; exit 1; }
echo " - Cleaning up..."
rm -f "$GUM_ZIP_FILE" \
    && echo "Cleanup completed." || { echo "Cleanup failed."; exit 1; }

################################################################################
### Define the script variables
################################################################################
echo "Creating gum script and adding to path"
GUM_EXE_PATH=$(find "$GUM_WINE_EXTRACT_DIR" -name "Gum.exe" -type f | head -n1)

################################################################################
### Create the ~/bin directory if it doesn't exist
################################################################################
mkdir -p ~/bin &> /dev/null

################################################################################
### Create the Gum script in the ~/bin directory using a HEREDOC
### Some variables are escaped so the variable is expanded at runtime
################################################################################

cat > ~/bin/gum <<EOF
#!/bin/bash

# Setup Env vars (harmless if unsupported)
export WINE_NO_WM_DECORATION=1
export PROTON_NO_WM_DECORATION=1

# Overwrite DOTNET environment variables that if set will break dotnet apps
# https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_root-dotnet_rootx86-dotnet_root_x86-dotnet_root_x64
# https://github.com/vchelaru/Gum/issues/1957
unset DOTNET_ROOT
unset DOTNET_ROOT_X64

# If no arguments were passed in, then just run gum
if [ \$# -eq 0 ]; then

    # Attempt to add registry keys
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\Mac Driver" /v Decorated /t REG_SZ /d N /f

    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine "$GUM_EXE_PATH"
    exit 0
fi

# If the first argument was 'upgrade', then upgrade gum
if [ "\$1" = "upgrade" ]; then
    GUM_ZIP_FILE="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
    GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"

    curl -L -o "\$GUM_ZIP_FILE" "\$GUM_ZIP_DOWNLOAD" \
        && echo " - Download completed." || { echo "Download failed using CURL."; exit 1; }

    rm -rf "$GUM_WINE_EXTRACT_DIR"
    unzip -q "\$GUM_ZIP_FILE" -d "$GUM_WINE_EXTRACT_DIR" \
        && echo "Extraction completed." || { echo "Extraction failed."; exit 1; }
    echo " - Cleaning up..."
    rm -f "\$GUM_ZIP_FILE" \
        && echo "Cleanup completed." || { echo "Cleanup failed."; exit 1; }

    echo "Latest version of gum extracted successfully."
    exit 0
fi

# Print the wine prefix path
if [ "\$1" = "prefix" ]; then
    echo "$GUM_WINE_PREFIX_PATH"
    exit 0
fi

# Switch to Vulkan renderer (WineD3D -> Vulkan -> MoltenVK -> Metal)
if [ "\$1" = "vulkan" ]; then
    echo "Switching WineD3D to Vulkan renderer (requires MoltenVK)..."
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\Direct3D" /v renderer /t REG_SZ /d vulkan /f
    echo "Done. To revert: ~/bin/gum opengl"
    exit 0
fi

# Switch to OpenGL renderer (WineD3D -> OpenGL, capped at 4.1 on macOS)
if [ "\$1" = "opengl" ]; then
    echo "Switching WineD3D to OpenGL renderer..."
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\Direct3D" /v renderer /t REG_SZ /d gl /f
    echo "Done. To switch back: ~/bin/gum vulkan"
    exit 0
fi

# Unknown argument
echo "Unknown argument: \$1"
echo "Usage: gum [upgrade|vulkan|opengl|prefix]"
exit 1
EOF

################################################################################
### Make the Gum script executable
################################################################################
chmod +x ~/bin/gum &> /dev/null

################################################################################
### Check if the bin directory is in PATH based on the shell being used
### If not, add it to PATH and reload the shell configuration.
################################################################################
if [[ $SHELL == *"bash"* ]]; then
    if ! grep -q 'export PATH="$HOME/bin:$PATH"' ~/.bashrc 2>/dev/null; then
        echo "Adding ~/bin to PATH in ~/.bashrc, please wait..."
        echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bashrc
    fi
    echo "Reloading ~/.bashrc, please wait..."
    source ~/.bashrc &> /dev/null
elif [[ $SHELL == *"zsh"* ]]; then
    if ! grep -q 'export PATH="$HOME/bin:$PATH"' ~/.zshrc 2>/dev/null; then
        echo "Adding ~/bin to PATH in ~/.zshrc, please wait..."
        echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc
    fi
    echo "Reloading ~/.zshrc, please wait..."
    source ~/.zshrc &> /dev/null
elif [[ $SHELL == *"fish"* ]]; then
    if ! grep -q 'set -x PATH $HOME/bin $PATH' ~/.config/fish/config.fish 2>/dev/null; then
        echo "Adding ~/bin to PATH in config.fish, please wait..."
        echo 'set -x PATH $HOME/bin $PATH' >> ~/.config/fish/config.fish
    fi
    echo "Please restart your terminal for PATH changes to take effect."
else
    echo "WARNING: Unable to determine shell type. Please ensure ~/bin is in your PATH manually."
fi

################################################################################
### Finished
################################################################################
echo "SUCCESS: Gum setup on macOS using WINE is now complete. You can open the GUM Tool by using the command 'gum'."
echo "TIP: To start Gum: in a terminal type ~/bin/gum"
echo "TIP: You may need to restart the terminal or your computer if it doesn't work at first"
echo "Enjoy using GUM!"
