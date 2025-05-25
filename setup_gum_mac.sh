#!/bin/bash

# Default WINEPREFIX location, but needs to be set for later use in here.
# Probably a better way to get the default WINEPREFIX, though.
export WINEPREFIX="${HOME}/.wine"

# Parse debug flag
DEBUG=false
INSTALL_PRERELEASE=false
while [[ "$1" != "" ]]; do
    case $1 in
        --debug ) DEBUG=true ;;
        --install-prerelease ) INSTALL_PRERELEASE=true ;;
        --custom-wine-prefix ) 
            shift
            if [[ -n "$1" ]]; then
                export WINEPREFIX="$1"
            else
                echo "Error: --custom-wine-prefix requires an argument."
                exit 1
            fi ;;
        --help )
            echo "Usage: $0 [--debug] [--install-prerelease] [--custom-wine-prefix <prefix>]"
            echo "  --debug: Enable debug mode"
            echo "  --install-prerelease: Install the prerelease version of GUM"
            echo "  --custom-wine-prefix <prefix>: Use a custom WINEPREFIX (default is '$WINEPREFIX')"
            echo "  --help: Show this help message"
            exit 0 ;;
    esac
    shift
done

echo "Debug mode: $DEBUG"
echo "Install prerelease: $INSTALL_PRERELEASE"
echo "Using WINEPREFIX: $WINEPREFIX"

# Helper to run commands with or without output
run_cmd() {
    if [ "$DEBUG" = true ]; then
        "$@"
    else
        "$@" &> /dev/null
    fi
}

################################################################################
### Check if wine-stable is installed
################################################################################
echo "Verifying that WINE is installed..."
if ! command -v wine &> /dev/null
then
    echo "Wine-stable is not installed."
    echo "Please install Wine-stable using the following command:"
    echo "brew install --cask --no-quarantine wine-stable"
    exit 1
fi

################################################################################
### Check if winetricks is installed
################################################################################
echo "Verifying that winetricks is installed..."
if ! command -v winetricks &> /dev/null
then
    echo "Winetricks is not installed."
    echo "Please install Winetricks using the following command:"
    echo "brew install winetricks"
    exit 1
fi

################################################################################
### Install dotnet48 with winetricks. This will cause two installation prompts
### to appear.  They can take a few minutes to finish, please be patient
################################################################################
echo "Installing .NET Framework 4.8 using winetricks"
echo "Two installer dialogs will appear, follow the steps for both to install"
echo "They may take a few minutes to install, please be patient"

# Check if the .NET 4.8 is already installed
if winetricks list-installed | grep -q "dotnet48"; then
    echo ".NET Framework 4.8 is already installed."
else
    echo ".NET Framework 4.8 is not installed, installing now..."
    run_cmd winetricks dotnet48
fi

################################################################################
### The current main release requires the XNA 4.0 redistributable to be installed
################################################################################
if [ "$INSTALL_PRERELEASE" = false ]; then
    ################################################################################
    ### Download the xna redistributable msi file from Microsoft
    ################################################################################
    echo "Installing XNA 4.0 Redistributable, please follow the installation prompts"
    echo "At the end of the installation it may say it has an error launching DirectX, this is normal, just click close on the error dialog"
    run_cmd curl -O https://download.microsoft.com/download/A/C/2/AC2C903B-E6E8-42C2-9FD7-BEBAC362A930/xnafx40_redist.msi &> /dev/null

    ################################################################################
    ### Execute the xna msi file using wine
    ################################################################################
    run_cmd wine msiexec /i xnafx40_redist.msi

    ################################################################################
    ### Clean up the file we downloaded.
    ################################################################################
    run_cmd rm -f ./xnafx40_redist.msi
fi

################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."

# WARNING: If debug mode isn't enabled, and you are reinstalling on top of existing files, the replace command will not display for the (yes/no/all/none/rename) prompt.
if [ "$INSTALL_PRERELEASE" = true ]; then
    echo "Installing the latest pre-release version of GUM Tool..."
    # Get the latest release/prerelease download URL from the GitHub API. Requires at least one release to be available.
    PRERELEASE_URL=$(curl -s https://api.github.com/repos/vchelaru/Gum/releases | \
    grep -o '"browser_download_url": *"[^"]*Gum.zip"' | head -n 1 | sed 's/.*: *"//;s/"$//')

    run_cmd curl --output "$WINEPREFIX/drive_c/Program Files/Gum.zip" --location "$PRERELEASE_URL"
else
    echo "Installing the latest release version of GUM Tool..."
    run_cmd curl --output "$WINEPREFIX/drive_c/Program Files/Gum.zip" --location "https://files.flatredball.com/content/Tools/Gum/Gum.zip"
fi

################################################################################
### Unzip the gum.zip file into Program Files/Gum
################################################################################
run_cmd unzip $WINEPREFIX/drive_c/Program\ Files/Gum.zip -d $WINEPREFIX/drive_c/Program\ Files/Gum

################################################################################
### Clean up the zip file we downloaded
################################################################################
run_cmd rm -f $WINEPREFIX/drive_c/Program\ Files/Gum.zip

################################################################################
### Define the script content
################################################################################
echo "Adding Gum to path"
SCRIPT_CONTENT="#!/bin/bash
wine $WINEPREFIX/drive_c/Program\\ Files/Gum/Data/Debug/Gum.exe"

################################################################################
### Create the ~/bin directory if it doesn't exist
################################################################################
run_cmd mkdir -p ~/bin

################################################################################
### Create the Gum script in the ~/bin directory
################################################################################
run_cmd echo "$SCRIPT_CONTENT" > ~/bin/Gum

################################################################################
### Make the Gum script executable
################################################################################
run_cmd chmod +x ~/bin/Gum

################################################################################
### Check if the bin directory is in PATH based on the shell being used
### If not, add it to PATh and reload the shell configuration.
################################################################################
if [[ $SHELL == *"bash"* ]]; then
    if ! grep -q 'export PATH="$HOME/bin:$PATH"' ~/.bash_profile 2>/dev/null; then
        echo "Adding ~/bin to PATH in ~/.bash_profile, please wait..."
        echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bash_profile
    fi
    echo "Reloading ~/.bash_profile, please wait..."
    run_cmd source ~/.bash_profile
elif [[ $SHELL == *"zsh"* ]]; then
    if ! grep -q 'export PATH="$HOME/bin:$PATH"' ~/.zshrc 2>/dev/null; then
        echo "Adding ~/bin to PATH in ~/.zshrc, please wait..."
        echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc
    fi
    echo "Reloading ~/.zshrc, please wait..."
    run_cmd source ~/.zshrc
fi

################################################################################
### Finished
################################################################################
echo "Gum setup on macOS using WINE is now complete. You can open the GUM Tool by using the command 'Gum'."
echo "You may need to close and reopen the terminal if it doesn't work at first."
