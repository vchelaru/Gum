# Setup

Latest release and release history (including older releases):\
[https://github.com/vchelaru/Gum/releases](https://github.com/vchelaru/Gum/releases)

Gum Source Code:\
[https://www.github.com/vchelaru/Gum](https://www.github.com/vchelaru/Gum)

{% tabs %}
{% tab title="Windows" %}
Download and unzip the .zip file and run the Gum.exe file.

Since Gum is a prebuilt file in a .zip, Windows _blocks_ the file which results in the "Windows protected your PC" popup:

<figure><img src="../../.gitbook/assets/image (77).png" alt=""><figcaption><p>Windows protected your PC popup</p></figcaption></figure>

You can click **More info,** then **Run anyway**. Alternatively, you can right-click on the .zip file and select the option to unblock:

<figure><img src="../../.gitbook/assets/image (78).png" alt=""><figcaption><p>Unblocking the .zip file removes the "Windows protected your PC" popup</p></figcaption></figure>


{% endtab %}

{% tab title="Linux (Bottles, recommended)" %}

{% endtab %}

{% tab title="Linux (script)" %}
#### Prerequisites

You will need these following prerequisites installed on your system, you can run the automated install script to have them installed automatically or you can install them yourself manually if you run into issues (see Manual Setup Steps section):

1. WINE
2. Winetricks

#### Automated Setup Linux

These following commands will go through the steps of downloading and running the `setup_gum_linux.sh` script. This script goes through the steps for you with minimal interaction required to setup your Linux environment to run the GUM tool using WINE. If you would prefer to do this setup manually, please refer to the Manual Setup Steps section below.

1. Download the setup\_gum.linux.sh script\
   [https://raw.githubusercontent.com/vchelaru/Gum/master/setup\_gum\_linux.sh](https://raw.githubusercontent.com/vchelaru/Gum/master/setup_gum_linux.sh)
2. Open a terminal and `cd` to the directory that the script was downloaded to.
3. Make the script executable and run the setup.

```sh
chmod +x ./setup_gum_linux.sh && ./setup_gum_linux.sh
```

**Install WINE and Winetricks Manually**

If the auto script fails to install the prerequisites try this.

If your distro is not listed bellow please use your preferred search engine to find out how to install\
wine and winetricks properly on your system.

You can install WINE and Winetricks using your package manager. Open a terminal and run the following commands:

**Ubuntu 22.04**

```sh
sudo dpkg --add-architecture i386 
sudo mkdir -pm755 /etc/apt/keyrings
wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/jammy/winehq-jammy.sources
sudo apt update && sudo apt install --install-recommends winehq-stable
sudo apt-get -y install winetricks
```

**Ubuntu 24.04**

```sh
sudo dpkg --add-architecture i386 
sudo mkdir -pm755 /etc/apt/keyrings
wget -O - https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor -o /etc/apt/keyrings/winehq-archive.key -
sudo wget -NP /etc/apt/sources.list.d/ https://dl.winehq.org/wine-builds/ubuntu/dists/noble/winehq-noble.sources
sudo apt update && sudo apt install --install-recommends winehq-stable
sudo apt-get -y install winetricks
```

**Fedora & Nobara (All Versions)**

```sh
sudo dnf install wine
sudo dnf install winetricks
```

**Linux Mint 20**

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ focal main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

**Linux Mint 21**

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ jammy main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

**Linux Mint 22**

```sh
sudo apt install dirmngr ca-certificates software-properties-common apt-transport-https curl -y
sudo dpkg --add-architecture i386
curl -s https://dl.winehq.org/wine-builds/winehq.key | sudo gpg --dearmor | sudo tee /usr/share/keyrings/winehq.gpg > /dev/null
echo deb [signed-by=/usr/share/keyrings/winehq.gpg] http://dl.winehq.org/wine-builds/ubuntu/ noble main | sudo tee /etc/apt/sources.list.d/winehq.list
sudo apt-get install winetricks
```

#### Manual Setup Steps

These following commands will go through the steps setting up your macOS or Linux environment to run the GUM tool using WINE.

1. Open a new terminal and set our wine prefix for Gum with the following command:

```sh
GUM_WINE_PREFIX_PATH=$HOME/.wine_gum_prefix/
```

2. Install Windows All Fonts using `winetricks` with the following command:

```sh
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks allfonts
```

3. Install .NET Framework 4.8 using `winetricks` with the following command:

```sh
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks dotnet48
```

This command initiates the installation of .NET Framework 4.8. A total of two installer dialogs appear one after another. Follow the steps for both to complete the installation. **This process may take a several minutes, so please be patient**.

4. Download the Gum Tool ZIP file from the FlatRedBall website and save it to your preferred location. You can download it using the following command:

```sh
curl -o Gum.zip https://files.flatredball.com/content/Tools/Gum/Gum.zip
```

5. Unzip the downloaded Gum Tool ZIP file into the Program Files directory of the WINE folder, this will also remove any existing Gum Tool installed and the zip to clean up. Run the following command in the terminal:

```sh
rm -rf $GUM_WINE_PREFIX_PATH/drive_c/Program\ Files/Gum && \
unzip Gum.zip -d $GUM_WINE_PREFIX_PATH/drive_c/Program\ Files/Gum && \
rm -f Gum.zip
```

6. Create a script to run the Gum Tool. Run the following command in the terminal:

```sh
GUM_EXE_PATH=$(find "$GUM_WINE_PREFIX_PATH" -name "Gum.exe" -type f) && \
mkdir -p $HOME/bin && \
printf '#!%s\nWINEPREFIX=%s wine "%s"\n' "/bin/bash" "$GUM_WINE_PREFIX_PATH" "$GUM_EXE_PATH" > $HOME/bin/gum && \
chmod +x ~/bin/gum
```

7. To ensure you can run the Gum Tool from any directory, add the script directory to your PATH. Most Linux and macOS users using macOS 10.14 or lower, will be using the BASH shell.

**For Bash SHELL**

```sh
if ! grep -q 'export PATH="\$HOME/bin:\$PATH"' ~/.bashrc 2>/dev/null; then
    echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bashrc
fi
```

**For ZSH Shell**

```sh
if ! grep -q 'export PATH="\$HOME/bin:\$PATH"' ~/.zshrc 2>/dev/null; then
    echo 'export PATH="$HOME/bin:$PATH"' >> ~/.zshrc
fi
```

8. Reload the shell configuration to apply the changes. Run the following command:

**For BASH Shell**

```sh
source ~/.bashrc
```

**For ZSH Shell**

```sh
source ~/.zshrc
```

#### Running the Gum Tool

Congratulations! You have now successfully set up the Gum Tool on Linux using WINE. You can open the Gum Tool by simply typing the following command in the terminal:

```sh
gum
```

If the command doesn't work immediately, try closing and reopening the terminal.
{% endtab %}

{% tab title="Mac OS" %}
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
{% endtab %}
{% endtabs %}
