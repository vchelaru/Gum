#!/bin/bash

################################################################################
### Check if wine-stable is installed
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

DISTRO=$(( lsb_release -si 2>/dev/null || grep '^ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[0-9\.]/}" 2>/dev/null || name ) | cut -d= -f2 | tr -d '"' | tr '[:upper:]'  '[:lower:]')
VERSION=$(( lsb_release -sr 2>/dev/null || grep '^VERSION_ID=' /etc/os-release 2>/dev/null || echo "${OSTYPE//[A-Za-z]/}" 2>/dev/null | cut -d '.' -f1 || sw_vers --productVersion ) | cut -d= -f2 | tr -d '"' | cut -c1-2 )

# Install or update wine
if [[ "${INSTALL_OR_UPGRADE_NEEDED}" == "Y" ]]; then
    echo ""
    read -p "Do you wish to install the latest version of wine? (Y/n): " choice
    case "$choice" in
        ""|y|Y ) echo "INFO: Installing latest Wine";;
        n|N ) echo "WARN: Unable to continue, GUM requires Wine on Linux!"; exit 0;;
          * ) echo "ERROR: Invalid option. Exiting."; exit 1;;
    esac

    case "$DISTRO" in
        ubuntu)
            if [[ "$VERSION" == "22" ]]; then
                echo "Installing Wine for Ubuntu 22.xx"
                sudo dpkg --add-architecture i386
                sudo mkdir -pm755 /etc/apt/keyrings
                wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
                sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources
                sudo apt update
                sudo apt install --install-recommends winehq-stable -y
            elif [[ "$VERSION" == "24" ]]; then
                echo "Installing Wine for Ubuntu 24.xx"
                sudo dpkg --add-architecture i386
                sudo mkdir -pm755 /etc/apt/keyrings
                wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
                sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/noble/winehq-noble.sources
                sudo apt update
                sudo apt install --install-recommends winehq-stable -y
            fi
            ;;

        linuxmint)
            if [[ "$VERSION" == "20" ]]; then
                BASE="focal"
            elif [[ "$VERSION" == "21" ]]; then
                BASE="jammy"
            elif [[ "$VERSION" == "22" ]]; then
                BASE="noble"
            else
                echo "Unsupported Linux Mint version: $VERSION"
                exit 1
            fi
            echo "Installing Wine for Linux Mint $VERSION ($BASE)"
            sudo apt install -y dirmngr ca-certificates software-properties-common apt-transport-https curl
            sudo dpkg --add-architecture i386
            curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
            echo "deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ $BASE main" | sudo tee /etc/apt/sources.list.d/winehq.list
            sudo apt update
            sudo apt install --install-recommends winehq-stable -y
            ;;

        fedora|nobara)
            echo "Installing Wine for Fedora/Nobara"
            sudo dnf install -y wine
            ;;

        darwin)
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
            fi


            echo -e "\nAttempting to install Wine-stable"
            brew install --cask --no-quarantine wine-stable
            ;;

        *)
            echo "ERROR: Unsupported or unknown distribution: $DISTRO"
  			echo "ERROR: Please install wine manually!"
			echo "ERROR: https://duckduckgo.com/?t=h_&q=Insert+Your+Linux+Distro+Here+How+To+Install+Wine"
            exit 1
            ;;
    esac
fi

################################################################################
### Check if winetricks is installed
################################################################################
if ! winetricks --version &> /dev/null; then
    echo -e "\nWinetricks is not installed. Attempting to install..."

    case "$DISTRO" in
        ubuntu|linuxmint)
            sudo apt install -y winetricks
            ;;
        fedora|nobara)
            sudo dnf install -y winetricks
            ;;
        darwin)
            brew install winetricks
            ;;
        *)
            echo "ERROR: Unsupported distribution [${DISTRO}] for automated winetricks install."
            exit 1
            ;;
    esac
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
    echo " - Attempting to self update winetricks..."

    case "$DISTRO" in
        ubuntu|linuxmint)
            sudo winetricks --self-update
            ;;
        fedora|nobara)
            sudo winetricks --self-update
            ;;
        darwin)
            brew upgrade winetricks
            ;;
        *)
            echo "Unsupported distribution [${DISTRO}] for automated winetricks update."
            exit 1
            ;;
    esac
else
    echo "Winetricks version is new enough ${WINETRICKS_YEAR}."
fi


################################################################################
### Make sure gum prefix is clear
################################################################################
if [ -d "$GUM_WINE_PREFIX_PATH" ]; then
    echo "Error: The gum wine prefix directory '$GUM_WINE_PREFIX_PATH' already exists."
    echo "This script can only be used on the initial creation of the gum wine prefix."
    echo "Call '~/bin/gum.sh upgrade' if upgrading the gum version, or call this script with a different directory to setup gum in (e.g. './setup_gum_linux.sh ~/home/.other_gum_wine_prefix')"
    exit 1
fi

################################################################################
### Install two fonts with winetricks.
################################################################################
echo "Checking and Installing Some fonts using winetricks"
echo " - They may take a few minutes to install, please be patient"
run_winetricks arial
run_winetricks tahoma
run_winetricks courier
run_winetricks calibri
#run_winetricks micross # not available in 2024 winetricks
echo " - Fonts installed"

################################################################################
### Install dotnet48 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################
#echo "Installing .NET Framework 4.8 using winetricks"
#echo " - Two installer dialogs will appear, follow the steps for both to install"
#echo " - They may take a few minutes to install, please be patient"
#$RUN_WINETRICKS dotnet48

################################################################################
### Install dotnetdeskop8 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################

echo "Installing .NET 8 using winetricks (this may take a few minutes)..."
run_winetricks -q dotnetdesktop8

################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."
GUM_ZIP_FILE="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"

if ! curl --version &> /dev/null; then
    wget -O "$GUM_ZIP_FILE" "$GUM_ZIP_DOWNLOAD" \
        && echo " - Download completed." || { echo "Download failed using WGET."; exit 1; }
else
    curl -L -o "$GUM_ZIP_FILE" "$GUM_ZIP_DOWNLOAD" \
        && echo " - Download completed." || { echo "Download failed using CURL."; exit 1; }
fi

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
### Install DXVK (for optional Vulkan support) but default to WineD3D
################################################################################
echo -e "\nInstalling DXVK (for optional Vulkan support)..."
run_winetricks dxvk
# Default to WineD3D — user can switch with: ~/bin/gum dxvk
echo "Reverting to WineD3D as the default renderer..."
run_and_write_log WINEPREFIX="$GUM_WINE_PREFIX_PATH" winetricks d3d8=builtin d3d9=builtin d3d10=builtin d3d10_1=builtin d3d10core=builtin d3d11=builtin dxgi=builtin
run_and_write_log WINEPREFIX="$GUM_WINE_PREFIX_PATH" wineboot -u
echo " - DXVK installed. WineD3D is the default. To switch: ~/bin/gum dxvk"

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
### Some variables are escaped lke GUM_ZIP_FILE so the variable is expanded at runtime
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
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\X11 Driver" /v Decorated /t REG_SZ /d N /f
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\Mac Driver" /v Decorated /t REG_SZ /d N /f

    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine "$GUM_EXE_PATH"
    exit 0
fi

# If the first argument was 'upgrade', then upgrade gum
if [ "\$1" = "upgrade" ]; then
    GUM_ZIP_FILE="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
    GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"

    if ! curl --version &> /dev/null; then
        wget -O "\$GUM_ZIP_FILE" "\$GUM_ZIP_DOWNLOAD" \
            && echo " - Download completed." || { echo "Download failed using WGET."; exit 1; }
    else
        curl -L -o "\$GUM_ZIP_FILE" "\$GUM_ZIP_DOWNLOAD" \
            && echo " - Download completed." || { echo "Download failed using CURL."; exit 1; }
    fi

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

# Switch to DXVK (Vulkan-based Direct3D)
if [ "\$1" = "dxvk" ]; then
    echo "Switching to DXVK..."
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" winetricks dxvk
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wineboot -u
    echo "Done. To revert: ~/bin/gum d3d"
    exit 0
fi

# Revert to WineD3D (Wine's built-in Direct3D)
if [ "\$1" = "d3d" ]; then
    echo "Switching to WineD3D..."
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" winetricks d3d8=builtin d3d9=builtin d3d10=builtin d3d10_1=builtin d3d10core=builtin d3d11=builtin dxgi=builtin
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wineboot -u
    echo "Done. To switch back: ~/bin/gum dxvk"
    exit 0
fi

# Unknown argument
echo "Unknown argument: \$1"
echo "Usage: gum [upgrade|dxvk|d3d|prefix]"
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
    source ~/.bash_profile &> /dev/null
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
echo "SUCCESS: Gum setup on Linux using WINE is now complete. You can open the GUM Tool by using the command 'gum'."
echo "TIP: To start Gum: in a terminal type ~/bin/gum"
echo "TIP: You may need to restart the terminal or your computer if it doesn't work at first"
echo "Enjoy using GUM!"
