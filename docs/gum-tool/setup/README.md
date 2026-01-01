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

{% tab title="Linux (script)" %}
#### 1.) Prerequisites

Gum is a windows application (executable) that uses dotnet 8.  To run on Linux you need something that can emulate/interpret windows like WINE.

You will need these following prerequisites installed on your system, you can run the automated install script to have them installed automatically or you can install them yourself manually if you run into issues (see Manual Setup Steps section):

1. WINE
2. Winetricks
   1. fonts (allfonts or the specific ones suggested)
   2. dotnet8 (dotnetdesktop8)
3. unzip gum.zip into wine prefix and launch with wine prefix

#### 2.) Automated Setup Linux

The following commands will go through the steps of downloading and running the `setup_gum_linux.sh` script. This script goes through the steps for you with minimal interaction required to setup your Linux environment to run the GUM tool using WINE. If you would prefer to do this setup manually, please refer to the Manual Setup Steps section below.

1. Download the setup\_gum.linux.sh script\
   [https://raw.githubusercontent.com/vchelaru/Gum/master/setup\_gum\_linux.sh](https://raw.githubusercontent.com/vchelaru/Gum/master/setup_gum_linux.sh)
2. Open a terminal and `cd` to the directory that the script was downloaded to.
3.  Make the script executable and run the installation.

    ```shellscript
    chmod +x ./setup_gum_linux.sh && ./setup_gum_linux.sh
    ```
4. The script should download and install wine, winetricks, fonts, dotnet8, and gum.
5. It will also create a executable file at `~/bin/gum` that sets up wine automatically for you.
6. When done, you should be able to type `gum` at the command line to start Gum.

#### **3.) Manually Install WINE and Winetricks**

If the auto script fails to install try these steps.&#x20;

If your distro is not listed bellow please use your preferred search engine to find out how to install `wine` and `winetricks` properly on your system.

You should be able to install WINE and Winetricks using your package manager. Open a terminal and run the following commands:

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

#### 4.) Manually install required winetricks verbs

These following commands will go through the steps setting up your Linux environment to run the GUM tool using WINE.

1. Open a new terminal and set our wine prefix for Gum with the following command:

```sh
GUM_WINE_PREFIX_PATH=$HOME/.wine_gum_prefix/
```

2.  Update winetricks if it is not using version 2025 or newer

    ```bash
    winetricks --version
    sudo winetricks --self-update
    ```
3. Install Windows fonts using `winetricks` with the following command:

```bash
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks arial
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks tahoma
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks courier
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks calibri
```

3. Install .NET Framework 8 using `winetricks` with the following command:

```sh
WINEPREFIX=$GUM_WINE_PREFIX_PATH winetricks dotnetdesktop8
```

This command initiates the installation of .NET Framework 8. A total of two installer dialogs appear one after another. Follow the steps for both to complete the installation. **This process may take a several minutes, so please be patient**.

#### 5.) Download and install Gum

1.  **Download** the latest Gum Tool ZIP file from Github and save it to your preferred location. You can download it using the following command:

    ```bash
    curl -o Gum.zip https://github.com/vchelaru/gum/releases/latest/download/Gum.zip
    ```
2.  **Unzip** the downloaded Gum Tool ZIP file into the Program Files directory of the WINE folder, this will also remove any existing Gum Tool installed and the zip to clean up. Run the following command in the terminal:

    ```bash
    rm -rf $GUM_WINE_PREFIX_PATH/drive_c/Program\ Files/Gum && \
    unzip Gum.zip -d $GUM_WINE_PREFIX_PATH/drive_c/Program\ Files/Gum && \
    rm -f Gum.zip
    ```
3.  Create a script to run the Gum Tool. Run the following command in the terminal:

    ```bash
    GUM_EXE_PATH=$(find "$GUM_WINE_PREFIX_PATH" -name "Gum.exe" -type f) && \
    mkdir -p $HOME/bin && \
    touch $HOME/bin/gum
    chmod +x $HOME/bin/gum
    ```
4.  Paste the following into the empty script created above called `gum.`&#x20;

    ```bash
    #!/bin/bash

    # Setup Env vars (harmless if unsupported)
    export WINE_NO_WM_DECORATION=1
    export PROTON_NO_WM_DECORATION=1

    # Update these with the correct paths
    GUM_WINE_PREFIX_PATH="$HOME/.wine_gum_dotnet8/"
    GUM_EXE_PATH="$GUM_WINE_PREFIX_PATH/drive_c/Program Files/Gum/gum.exe"

    # Attempt to add registry keys
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine reg add "HKCU\\Software\\Wine\\X11 Driver" /v Decorated /t REG_SZ /d N /f

    # Launch gum through wine using the wineprefix
    WINEPREFIX="$GUM_WINE_PREFIX_PATH" wine "$GUM_EXE_PATH"
    ```

    1. Update `GUM_WINE_PREFIX_PATH` and `GUM_EXE_PATH` with the correct values for you
5. To ensure you can run the Gum Tool from any directory, add the script directory to your PATH. Most Linux and macOS users using macOS 10.14 or lower, will be using the BASH shell.

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

## Common errors and resolutions for Linux

### Most issues are caused by a misconfigured wine installation.

High level requirements

1. Install `wine` freshly into a new directory "wine prefix".
2. Install `winetricks`.
3. Install the `required fonts`, or use the allfonts verb into the correct wine prefix.
4. Install dotnet8 using the new verb added in 2025 winetricks `dotnetdesktop8` into the correct wine prefix.
5. Configure winecfg to disable the "Allow the window manager to decorate the window" graphics setting for the correct wine prefix.
   1. This fixes an issue where the _**menu is missing**_ and you can't see/select FILE/EDIT/etc
6. launch gum.exe using wine from the correct wine prefix.
{% endtab %}

{% tab title="Linux (Bottles)" %}
[Bottles](https://usebottles.com/) is an application that makes it easy to configure custom Wine prefixes and application specific configuration.&#x20;

Official releases of bottles are released as [Flatpak packages on the flathub repository](https://flathub.org/en/apps/com.usebottles.bottles). Use the install button if it's not already installed.

<figure><img src="../../.gitbook/assets/image (21).png" alt=""><figcaption></figcaption></figure>

Next run the bottles application.  The program uses the term "bottle" to refer to an isolated wine configuration. We'll need to create a new bottle specifically for Gum.

If this is your first time running bottles you will have a blank screen with a `Create New Bottle` button in the center.  If you have used bottles before, then the top left corner will have a button to create a new bottle. Click on either of these.

<figure><img src="../../.gitbook/assets/image (23).png" alt=""><figcaption></figcaption></figure>

This will open the `Create New Bottle` window. Give it a name (such as `Gum`), select `Application` and set the runner to `sys-wine-10.0`. The click the `Create` button.

<figure><img src="../../.gitbook/assets/image (25).png" alt=""><figcaption></figcaption></figure>

This will set up wine and configure an initial wine configuration for that bottle. Since we used the `Application` option, it will install the required fonts we need.

Next we need to install the .net 8 desktop runtime. When the bottle is selected, click `Options->Dependencies` and click `dotnetcoredesktop8`.  This will install the runtime directly into the bottle.

<figure><img src="../../.gitbook/assets/image (32).png" alt=""><figcaption></figcaption></figure>

![](<../../.gitbook/assets/image (43).png>)

<figure><img src="../../.gitbook/assets/image (48).png" alt=""><figcaption></figcaption></figure>

Now use the back button in the top left corner to go back to the main details page for your Gum bottle. Scroll to the bottom and click `Tools -> Legacy Wine Tools` to expand the options, and then click `Configuration`&#x20;

<figure><img src="../../.gitbook/assets/image (50).png" alt=""><figcaption></figcaption></figure>

This opens a wine configuration window. Under the `Graphics` tab, uncheck `Allow the window manager to decorate the windows`.  This will make it so that it won't use native decorations on the window. If this is not done then it is likely that window decorations may cover the menu bar of the gum application.

![](<../../.gitbook/assets/image (52).png>)

Now click `Ok` to save the configuration and scroll back up to the top of the detail page of the Gum bottle. Click the `Browse` button for `Browse C:/ drive`. &#x20;

<figure><img src="../../.gitbook/assets/image (67).png" alt=""><figcaption></figcaption></figure>

This will open the file browser for where your bottle will have its `C:/` file system. Create a `Gum` folder and [unzip the latest gum release](https://github.com/vchelaru/Gum/releases) into this folder. Once done you can close this folder.

Next we need to add a shortcut to the Gum executable. In the bottle detail page, select the `Add Shortcut` button.

<figure><img src="../../.gitbook/assets/image (68).png" alt=""><figcaption></figcaption></figure>

A file selection window will appear. Select the `drive_c/Gum/Gum.exe` file. That will add the program to the Gum bottle.

<figure><img src="../../.gitbook/assets/image (80).png" alt=""><figcaption></figcaption></figure>

Pressing the play button should now successfully launch gum! Clicking the kebab menu next to the shortcut will give you the options to add it to your "Bottles" library to make it quicker to run through the bottles application, or to add a desktop entry to make it runnable without the bottles application in the future.&#x20;

<figure><img src="../../.gitbook/assets/image (84).png" alt=""><figcaption></figcaption></figure>

If you need to upgrade the Gum release in the future, you can use the `Browse C:/ drive` option again to find the location of the gum folder and extract the new release in there.
{% endtab %}

{% tab title="Mac OS" %}
#### 1) Prerequisites

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

Configure and install Gum

1. Now you need to basically follow the LINUX script steps, but convert each command to mac specific commands like brew, etc.

_**NOTE:** We have yet to get GUM working on Mac since the November 2025 release that utilizes dotnet8._

#### Automated Setup MacOS

The following goes through the steps do download and run the `setup_gum_mac.sh` automation script. This script goes through the steps for you with minimal interaction to setup your environment on macOS to run the GUM tool using WINE. If you would prefer to do this setup manually, please see the Manual Setup Steps section below. &#x20;

_**WARNING:**_ After this runs, gum fails to load and crashes.  _We are unable to determine why, if you have information to help please join the discord or submit a PR._



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









