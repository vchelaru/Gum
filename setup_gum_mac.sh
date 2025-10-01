#!/bin/bash

################################################################################
### Check if wine-stable is installed
################################################################################
echo "Verifying that WINE is installed..."
if ! wine --version &> /dev/null
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
if ! winetricks --version &> /dev/null
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
winetricks dotnet48 &> /dev/null

################################################################################
### Download the xna redistributable msi file from Microsoft
################################################################################
echo "Installing XNA 4.0 Redistributable, please follow the installation prompts"
echo "At the end of the installation it may say it has an error launching DirectX, this is normal, just click close on the error dialog"
curl -O https://download.microsoft.com/download/A/C/2/AC2C903B-E6E8-42C2-9FD7-BEBAC362A930/xnafx40_redist.msi &> /dev/null

################################################################################
### Execute the xna msi file using wine
################################################################################
wine msiexec /i xnafx40_redist.msi &> /dev/null

################################################################################
### Clean up the file we downloaded.
################################################################################
rm -f ./xnafx40_redist.msi &> /dev/null

################################################################################
### Download the gum.zip file from the FRB site into the Program Files directory
### of the wine folder
################################################################################
echo "Installing GUM Tool..."
curl -o ~/.wine/drive_c/Program\ Files/Gum.zip https://files.flatredball.com/content/Tools/Gum/Gum.zip &> /dev/null

################################################################################
### Unzip the gum.zip file into Program Files/Gum
################################################################################
unzip ~/.wine/drive_c/Program\ Files/Gum.zip -d ~/.wine/drive_c/Program\ Files/Gum &> /dev/null

################################################################################
### Clean up the zip file we downloaded
################################################################################
rm -f ~/.wine/drive_c/Program\ Files/Gum.zip &> /dev/null

################################################################################
### Define the script content
################################################################################
echo "Adding Gum to path"
SCRIPT_CONTENT="#!/bin/bash
wine ~/.wine/drive_c/Program\\ Files/Gum/Data/Debug/Gum.exe"

################################################################################
### Create the ~/bin directory if it doesn't exist
################################################################################
mkdir -p ~/bin &> /dev/null

################################################################################
### Create the Gum script in the ~/bin directory
################################################################################
echo "$SCRIPT_CONTENT" > ~/bin/Gum

################################################################################
### Make the Gum script executable
################################################################################
chmod +x ~/bin/Gum &> /dev/null

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
    source ~/.bash_profile &> /dev/null
elif [[ $SHELL == *"zsh"* ]]; then
    if ! grep -q 'export PATH="$HOME/bin:$PATH"' ~/.zshrc 2>/dev/null; then
        echo "Adding ~/bin to PATH in ~/.zshrc, please wait..."
        echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc
    fi
    echo "Reloading ~/.zshrc, please wait..."
    source ~/.zshrc &> /dev/null
fi

################################################################################
### Finished
################################################################################
echo "Gum setup on macOS using WINE is now complete. You can open the GUM Tool by using the command 'Gum'."
echo "You may need to close and reopen the terminal if it doesn't work at first."
