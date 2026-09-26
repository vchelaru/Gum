# Setup

Gum runs natively on Windows, macOS, and Linux, with no WINE or other compatibility layer. It opens the same projects, saves the same files, and generates the same code on every OS.

Download the latest release directly:\
[https://github.com/vchelaru/Gum/releases/latest](https://github.com/vchelaru/Gum/releases/latest)

Release history (including older releases):\
[https://github.com/vchelaru/Gum/releases](https://github.com/vchelaru/Gum/releases)

Gum Source Code:\
[https://www.github.com/vchelaru/Gum](https://www.github.com/vchelaru/Gum)

Each release lists these files:

| File | For |
|---|---|
| `Gum-win-x64.zip` | Windows (64-bit) |
| `Gum-osx-arm64.tar.xz` | macOS on Apple Silicon (M1 and later) |
| `Gum-osx-x64.tar.xz` | macOS on Intel |
| `Gum-linux-x64.tar.xz` | Linux (64-bit) |

Each download includes everything it needs, so you do not need to install .NET first. A matching `.sha256` file lists the checksum of each download.

{% tabs %}
{% tab title="Windows" %}
1. Unzip `Gum-win-x64.zip` into a folder of your choice.
2. Run `Gum.exe`.

Gum is not signed, so Windows shows the "Windows protected your PC" popup the first time. Click **More info**, then **Run anyway**. Alternatively, right-click the `.zip` file and select the option to unblock it before extracting.
{% endtab %}

{% tab title="macOS" %}
1. Double-click the `.tar.xz` file for your Mac to extract `Gum.app`.
2. Move `Gum.app` into your **Applications** folder.
3. Open a terminal and allow the app to run. Gum is not notarized by Apple, so macOS blocks it until you do this once:

    ```sh
    xattr -dr com.apple.quarantine /Applications/Gum.app
    ```
4. Open Gum from **Applications** or Launchpad.
{% endtab %}

{% tab title="Linux" %}
1. Extract the download into a folder of your choice:

    ```sh
    mkdir -p ~/gum && tar -xJf Gum-linux-x64.tar.xz -C ~/gum
    ```
2. Run Gum:

    ```sh
    ~/gum/Gum
    ```

Gum needs a desktop session (X11 or Wayland), a graphics driver with OpenGL 3.0 or newer, and the fontconfig library (`libfontconfig1` on Debian and Ubuntu), which desktop installs normally include.
{% endtab %}
{% endtabs %}

## System requirements

* **Windows:** Windows 10 or later, 64-bit.
* **macOS:** an Apple Silicon or Intel Mac. Download the file that matches your Mac's processor.
* **Linux:** 64-bit (x64), with a desktop session and graphics driver as described in the Linux steps above.

## Opening a project

Gum reopens the project you had open last time. To open a different one, select **File** > **Load Project...**. Gum also opens a project whose path you pass when you start it, which is how a file association or a script can open a specific project:

{% tabs %}
{% tab title="Windows" %}
```
Gum.exe C:\Path\To\MyProject.gumx
```

To open `.gumx` files by double-clicking them, right-click a `.gumx` file, select **Open with** > **Choose another app**, and pick `Gum.exe`.
{% endtab %}

{% tab title="macOS" %}
```sh
/Applications/Gum.app/Contents/MacOS/Gum ~/Path/To/MyProject.gumx
```
{% endtab %}

{% tab title="Linux" %}
```sh
~/gum/Gum ~/Path/To/MyProject.gumx
```
{% endtab %}
{% endtabs %}

## Differences on macOS

* Gum's menus (**File**, **Edit**, **View**, and so on) appear in the macOS menu bar at the top of the screen rather than inside the Gum window. **About Gum** is in the **Gum** application menu.
* Shortcuts that use Ctrl on Windows and Linux use Cmd on macOS. For example, undo is Cmd+Z.

## Fonts on macOS and Linux

Gum generates bitmap fonts with KernSmith on every operating system. A project whose **Font Generator** is set to **BMFont** still opens and generates fonts on macOS and Linux, but Gum uses KernSmith there, because BMFont only runs on Windows. The Font Generator property is read-only in **Project Properties** on those systems. See [Font Generator](../project-properties.md#font-generator) for how the two generators differ.

## Settings

Gum keeps its settings, including the last project and the recent project list, in a per-user folder. Select **Help** > **Open Settings Folder...** to open it. On Windows the folder is `%APPDATA%\Gum`, the same folder the older WPF tool used, so your recent projects carry over when you upgrade.

## Older releases (the WPF tool)

Releases up to and including [September 2, 2026](https://github.com/vchelaru/Gum/releases/tag/Release_September_02_2026) are the older Windows-only WPF tool, distributed as `Gum.zip` with `Gum.exe` inside. That release stays downloadable if you need it, for example for a plugin that has not been migrated yet (see [Plugins](../plugins/README.md)), but it does not receive new features. Its WINE setup scripts for Linux and macOS are kept at that release's tag ([setup\_gum\_linux.sh](https://github.com/vchelaru/Gum/blob/Release_September_02_2026/setup_gum_linux.sh), [setup\_gum\_mac.sh](https://github.com/vchelaru/Gum/blob/Release_September_02_2026/setup_gum_mac.sh)); the native downloads above replace them.

## Known issues

* Plugins built only for the older WPF tool do not load. The **Plugins** dialog lists them as not supported. See [Plugins](../plugins/README.md) for how to migrate one.
* The command-line tool (`gumcli`) is included in the `GumCli` folder beside the tool, but only the tool's own **Export as SVG** uses it so far.
* Gum is not signed on Windows or notarized on macOS, which is why the steps above are needed.
* Dropping a `.gumx` or `.gumj` project file onto the Gum window does not open it. Use **File** > **Load Project...** instead.
* The back and forward buttons on a mouse do not step through selection history. Use Alt+Left and Alt+Right instead.

Please report anything else you find on the [Gum GitHub issues page](https://github.com/vchelaru/Gum/issues), along with your operating system.
