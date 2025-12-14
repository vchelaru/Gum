#!/bin/bash

################################################################################
### Check if wine-stable is installed
################################################################################

set -e

GUM_WINE_PREFIX_PATH=$HOME/.wine_gum_prefix/

echo "This is an experimental script."
echo "Script last updated on the 4th of October 2025!"

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

echo "Winetricks is installed\n"

################################################################################
### Check if winetricks is newer than version 2024
###   The 2025 version has the new dotnetdesktop8 verb
################################################################################
## Commented out for now until we figure out the menu issues in dotnet8 and linux and wine
# echo "Verifying winetricks version..."
# WINETRICKS_YEAR=$(winetricks --version 2>/dev/null | grep -Eo '[0-9]{4}' | head -n1)
# if [[ ! "${WINETRICKS_YEAR}" || "${WINETRICKS_YEAR}" -le 2024 ]]; then
#     echo "Winetricks version is older than 2024 or could not be determined."
#     echo " - A newer version is required for dotnet 8. Attempting to update..."
#     echo " - Attempting to self update winetricks..."

#     case "$DISTRO" in
#         ubuntu|linuxmint)
#             sudo winetricks --self-update
#             ;;
#         fedora|nobara)
#             sudo winetricks --self-update
#             ;;
#         darwin)
#             brew winetricks --self-update
#             ;;
#         *)
#             echo "Unsupported distribution [${DISTRO}] for automated winetricks update."
#             exit 1
#             ;;
#     esac
# else
#     echo "Winetricks version is new enough ${WINETRICKS_YEAR}."
# fi


################################################################################
### Install two fonts with winetricks.
################################################################################
echo "Checking and Installing Some fonts using winetricks"
echo " - They may take a few minutes to install, please be patient"
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks arial &> /dev/null
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks tahoma &> /dev/null
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks courier &> /dev/null
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks calibri &> /dev/null
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks micross &> /dev/null
echo " - Fonts installed"

################################################################################
### Install dotnet48 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################
echo "Installing .NET Framework 4.8 using winetricks"
echo " - Two installer dialogs will appear, follow the steps for both to install"
echo " - They may take a few minutes to install, please be patient"
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks dotnet48 &> /dev/null

################################################################################
### Install dotnetdeskop8 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################

# TODO: Commented out for now until we figure out the menu issues in dotnet8 and linux and wine

# echo "Installing .NET 8 using winetricks"
# echo " - Two installer dialogs will appear, follow the steps for both to install"
# echo " - They may take a few minutes to install, please be patient"
# WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks dotnetdesktop8 &> /dev/null


################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."
GUM_ZIP_FILE="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
# Temporarily pointing directly at the october release until we can fix the dotnet8+linux+wine issues
GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/Gum/releases/download/Release_October_31_2025/Gum.zip"
#GUM_ZIP_DOWNLOAD="https://github.com/vchelaru/gum/releases/latest/download/gum.zip"

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
### Define the script content
################################################################################
echo "Creating gum script and adding to path"
GUM_EXE_PATH=$(find "$GUM_WINE_EXTRACT_DIR" -name "Gum.exe" -type f)
SCRIPT_CONTENT="#!/bin/bash
WINEPREFIX=\"$GUM_WINE_PREFIX_PATH\" wine \"$GUM_EXE_PATH\""

################################################################################
### Create the ~/bin directory if it doesn't exist
################################################################################
mkdir -p ~/bin &> /dev/null

################################################################################
### Create the Gum script in the ~/bin directory
################################################################################
printf "%s\n" "$SCRIPT_CONTENT" > ~/bin/gum

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
        echo source ~/.bashrc
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
    echo "Reloading config.fish, please wait..."
    source ~/.config/fish/config.fish &> /dev/null
else
    echo "WARNING: Unable to determine shell type. Please ensure ~/bin is in your PATH manually."
fi

################################################################################
### Finished
################################################################################
echo "SUCCESS: Gum setup on Linux using WINE is now complete. You can open the GUM Tool by using the command 'gum'."
echo "TIP: To start Gum: in a terminal type ~/bin/gum"
echo "TIP: You may need to restart the terminal or your computer if it doesn't work at first"
echo "-------------------"
echo "OPTIONAL: Install dxvk with the command winetricks dxvk, if you can use Vulkan on your system! (It handles better than OpenGL)."
echo "-------------------"
echo "Enjoy using GUM!"
