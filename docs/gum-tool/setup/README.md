# Setup

Gum Application (binaries):

* Latest release direct link: [files.flatredball.com/content/Tools/Gum/Gum.zip](https://files.flatredball.com/content/Tools/Gum/Gum.zip)
* Release history (including older releases): [https://github.com/vchelaru/Gum/releases](https://github.com/vchelaru/Gum/releases)

Gum Source Code: [https://www.github.com/vchelaru/Gum](https://www.github.com/vchelaru/Gum)

### Windows

Currently the Gum tool requires XNA runtimes. Download and install the runtime prior to running Gum:

{% embed url="https://www.microsoft.com/en-us/download/details.aspx?id=20914" %}

Since Gum is a prebuilt file in a .zip, Windows _blocks_ the file which results in the "Windows protected your PC" popup:

<figure><img src="../../.gitbook/assets/image (77).png" alt=""><figcaption><p>Windows protected your PC popup</p></figcaption></figure>

You can click **More info,** then **Run anyway**. Alternatively, you can right-click on the .zip file and select the option to unblock:

<figure><img src="../../.gitbook/assets/image (78).png" alt=""><figcaption><p>Unblocking the .zip file removes the "Windows protected your PC" popup</p></figcaption></figure>

### MacOS

#### Prerequisites

Before proceeding, ensure that you have the following prerequisites installed on your system:

1. Homebrew
2. WINE
3. Winetricks

**Install Homebrew**

Homebrew is a package manager for macOS. If you haven't installed Homebrew yet, you can install it by following the instructions on the [official Homebrew website](https://brew.sh/).

**Install WINE and Winetricks**

You can install WINE and Winetricks using Homebrew. Open a terminal and run the following commands:

```sh
brew install --cask --no-quarantine wine-stable
brew install winetricks
```

#### Automated Setup MacOS

The following goes through the steps do download and run the `setup_gum_mac.sh` automation script. This script goes through the steps for you with minimal interaction to setup your environment on macOS to run the GUM tool using WINE. If you would prefer to do this setup manually, please see the Manual Setup Steps section below.

1. Download the setup\_gum.mac.sh script\
   [https://raw.githubusercontent.com/vchelaru/Gum/master/setup\_gum\_mac.sh](https://raw.githubusercontent.com/vchelaru/Gum/master/setup_gum_mac.sh)
2. Open a terminal and `cd` to the directory that the script was downloaded to
3. Make the script executable

```sh
chmod +x ./setup_gum_mac.sh
```

4. Execute the script

```sh
./setup_gum.mac.sh
```

### Linux

#### Prerequisites

Before proceeding, ensure that you have the following prerequisites installed on your system:

1. WINE
2. Winetricks

**Install WINE and Winetricks**

You can install WINE and Winetricks using Homebrew. Open a terminal and run the following commands:

If your distro is not listed bellow please use your prefered search engine to find out how to install\
wine and winetricks properly on your system.

Ubuntu 22.04

```sh
sudo dpkg --add-architecture i386 
sudo mkdir -pm755 /etc/apt/keyrings
wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources
sudo apt update && sudo apt install --install-recommends winehq-stable
sudo apt-get -y install winetricks
```

Ubuntu 24.04

```sh
sudo dpkg --add-architecture i386 
sudo mkdir -pm755 /etc/apt/keyrings
wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/noble/winehq-noble.sources
sudo apt update && sudo apt install --install-recommends winehq-stable
sudo apt-get -y install winetricks
```

Fedora & Nobara (All Versions)

```sh
sudo dnf install wine
sudo dnf install winetricks
```

Linux Mint 20

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ focal main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

Linux Mint 21

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ jammy main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

Linux Mint 22

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ noble main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

#### Automated Setup Linux

The following goes through the steps do download and run the `setup_gum_linux.sh` automation script. This script goes through the steps for you with minimal interaction to setup your Linux environment to run the GUM tool using WINE. If you would prefer to do this setup manually, please see the Manual Setup Steps section below.

1. Download the setup\_gum.linux.sh script\
   [https://raw.githubusercontent.com/vchelaru/Gum/master/setup\_gum\_linux.sh](https://raw.githubusercontent.com/vchelaru/Gum/master/setup_gum_linux.sh)
2. Open a terminal and `cd` to the directory that the script was downloaded to
3. Make the script executable

```sh
chmod +x ./setup_gum_linux.sh
```

4. Execute the script

```sh
./setup_gum_linux.sh
```

#### Manual Setup Steps

The following goes through the steps setup your environment on macOS to run the GUM tool using WINE.

1. Open a new terminal
2. Install .NET Framework 4.8 using `winetricks` with the following command:

```sh
winetricks dotnet48
```

This command initiates the installation of .NET Framework 4.8. A total of two installer dialogs appear one after another. Follow the steps for both to complete the installation. **This process may take a several minutes, so please be patient**.

1. Download the XNA 4.0 Redistributable MSI file from Microsoft. You can download it using the following command:

```sh
curl -O https://download.microsoft.com/download/A/C/2/AC2C903B-E6E8-42C2-9FD7-BEBAC362A930/xnafx40_redist.msi
```

4. After downloading the MSI file, install it using the following command:

```sh
wine msiexec /i xnafx40_redist.msi
```

Follow the installation prompts. At the end of the installation, it may display an error related to launching DirectX. **This is normal; just click "Close" on the error dialog.**

5. Download the Gum Tool ZIP file from the FlatRedBall website and save it to your preferred location. You can download it using the following command:

```sh
curl -o Gum.zip https://files.flatredball.com/content/Tools/Gum/Gum.zip
```

6. Unzip the downloaded Gum Tool ZIP file into the Program Files directory of the WINE folder. Run the following command in the terminal:

```sh
unzip Gum.zip -d ~/.wine/drive_c/Program\ Files/Gum
```

7. After unzipping the Gum Tool, remove the downloaded ZIP file using the following command:

```sh
rm -f Gum.zip
```

8. Create a script to run the Gum Tool. Run the following command in the terminal:

```sh
echo '#!/bin/bash
wine ~/.wine/drive_c/Program\\ Files/Gum/Data/Gum.exe' > ~/bin/Gum
```

9. Make the script executable by running:

```sh
chmod +x ~/bin/Gum
```

10. To ensure you can run the Gum Tool from any directory, add the script directory to your PATH.

**For Bash SHELL**

```sh
if ! grep -q 'export PATH="\$HOME/bin:\$PATH"' ~/.bash_profile 2>/dev/null; then
    echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bash_profile
fi
```

**For ZSH Shell**

```sh
if ! grep -q 'export PATH="\$HOME/bin:\$PATH"' ~/.zshrc 2>/dev/null; then
    echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc
fi
```

11. Reload the shell configuration to apply the changes. Run the following command:

**For BASH Shell**

```sh
source ~/.bash_profile
```

**For ZSH Shell**

```sh
source ~/.zshrc
```

#### Running the Gum Tool

Congratulations! You have now successfully set up the Gum Tool on macOS using WINE. You can open the Gum Tool by simply typing the following command in the terminal:

```sh
Gum
```

If the command doesn't work immediately, try closing and reopening the terminal.
