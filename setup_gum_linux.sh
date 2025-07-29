#!/bin/bash

################################################################################
### Check if wine-stable is installed
################################################################################

set -e

GUM_WINE_PREFIX_PATH=$HOME/.wine_gum_prefix/

echo "This is an experimental script."
echo "Script last updated on the 8th of May 2025!"

read -p "Do you wish to continue? (y/n): " choice
case "$choice" in
  y|Y ) echo "Continuing...";;
  n|N ) echo "Exiting."; exit 0;;
  * ) echo "Invalid option. Exiting."; exit 1;;
esac

echo "Verifying that WINE is installed..."

if ! command -v wine &> /dev/null; then
    echo "Wine is not installed. Attempting to install..."

    DISTRO=$(lsb_release -si 2>/dev/null || grep '^ID=' /etc/os-release | cut -d= -f2 | tr -d '"')
    VERSION=$(lsb_release -sr 2>/dev/null || grep '^VERSION_ID=' /etc/os-release | cut -d= -f2 | tr -d '"')

    case "$DISTRO" in
        Ubuntu)
            if [[ "$VERSION" == "22.04" ]]; then
                echo "Installing Wine for Ubuntu 22.04"
                sudo dpkg --add-architecture i386 
                sudo mkdir -pm755 /etc/apt/keyrings
                wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
                sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources
                sudo apt update
                sudo apt install --install-recommends winehq-stable -y
            elif [[ "$VERSION" == "24.04" ]]; then
                echo "Installing Wine for Ubuntu 24.04"
                sudo dpkg --add-architecture i386 
                sudo mkdir -pm755 /etc/apt/keyrings
                wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
                sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/noble/winehq-noble.sources
                sudo apt update
                sudo apt install --install-recommends winehq-stable -y
            fi
            ;;

        LinuxMint)
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

        Fedora|Nobara)
            echo "Installing Wine for Fedora/Nobara"
            sudo dnf install -y wine
            ;;

        Darwin)
            echo "Detected macOS"
            echo "Please install Wine-stable manually:"
            echo "brew install --cask --no-quarantine wine-stable"
            exit 1
            ;;

        *)
            echo "Unsupported or unknown distribution: $DISTRO"
  			echo "Please install wine manually!"
			echo "https://duckduckgo.com/?t=h_&q=Insert+Your+Linux+Distro+Here+How+To+Install+Wine"
            exit 1
            ;;
    esac
fi

################################################################################
### Check if winetricks is installed
################################################################################
if ! command -v winetricks &> /dev/null; then
    echo "Winetricks is not installed. Attempting to install..."

    case "$DISTRO" in
        Ubuntu|LinuxMint)
            sudo apt install -y winetricks
            ;;
        Fedora|Nobara)
            sudo dnf install -y winetricks
            ;;
        Darwin)
            echo "Please install Winetricks manually:"
            echo "brew install winetricks"
            exit 1
            ;;
        *)
            echo "Unsupported distribution for automated winetricks install."
            exit 1
            ;;
    esac
fi

echo "Winetricks is installed"

################################################################################
### Install windows allfonts with winetricks.  They can take a few minutes to finish, please be patient
################################################################################
echo "Installing all fonts using winetricks"
echo "An installer dialog may appear, follow the steps to install"
echo "They may take a few minutes to install, please be patient"
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks allfonts &> /dev/null

################################################################################
### Install dotnet48 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################
echo "Installing .NET Framework 4.8 using winetricks"
echo "Two installer dialogs will appear, follow the steps for both to install"
echo "They may take a few minutes to install, please be patient"
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks dotnet48 &> /dev/null

################################################################################
### Download the xna redistributable msi file from Microsoft
################################################################################
echo "Installing XNA 4.0 Redistributable, please follow the installation prompts"
echo "At the end of the installation it may say it has an error launching DirectX, this is normal, just click close on the error dialog"
curl -O https://download.microsoft.com/download/A/C/2/AC2C903B-E6E8-42C2-9FD7-BEBAC362A930/xnafx40_redist.msi &> /dev/null

################################################################################
### Execute the XNA MSI file using WINE 
################################################################################
WINEPREFIX=$GUM_WINE_PREFIX_PATH wine msiexec /i xnafx40_redist.msi &> /dev/null || true 
## We must return true, so when you click cancel (if for example you must rerun the script it wont't close the script!

################################################################################
### Clean up the file we downloaded.
################################################################################
rm -f ./xnafx40_redist.msi &> /dev/null

################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."
GUM_ZIP_FILE="$WINE_PREFIX_PATH/drive_c/Program Files/Gum.zip"
curl -L -o "$GUM_ZIP_FILE" "https://files.flatredball.com/content/Tools/Gum/Gum.zip" \
    && echo "Download completed." || { echo "Download failed."; exit 1; }

################################################################################
### Unzip the gum.zip file into Program Files/Gum
################################################################################
echo "Extracting GUM Tool..."
GUM_WINE_EXTRACT_DIR="$WINE_PREFIX_PATH/drive_c/Program Files/Gum"
rm -rf "$GUM_WINE_EXTRACT_DIR"
unzip -q "$GUM_ZIP_FILE" -d "$GUM_WINE_EXTRACT_DIR" \
    && echo "Extraction completed." || { echo "Extraction failed."; exit 1; }
echo "Cleaning up..."
rm -f "$GUM_ZIP_FILE" \
    && echo "Cleanup completed." || { echo "Cleanup failed."; exit 1; }

echo "Adding Gum to path"

################################################################################
### Define the script content
################################################################################
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
fi

################################################################################
### Finished
################################################################################
echo "Gum setup on Linux using WINE is now complete. You can open the GUM Tool by using the command 'gum'."
echo "Pro Tip: Install dxvk with the command winetricks dxvk, if you can use Vulkan on your system! (It handles better than OpenGL)."
echo "You may need to close and reopen the terminal if it doesn't work at first."
