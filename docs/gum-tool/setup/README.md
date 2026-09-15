# Setup

Gum runs natively on Windows, macOS, and Linux — no WINE or other compatibility layer needed. It opens the same projects, saves the same files, and generates the same code on every OS.

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
| `Gum-osx-arm64.tar.gz` | macOS on Apple Silicon (M1 and later) |
| `Gum-osx-x64.tar.gz` | macOS on Intel |
| `Gum-linux-x64.tar.gz` | Linux (64-bit) |

Each download includes everything it needs, so you do not need to install .NET first. A matching `.sha256` file lists the checksum of each download.

{% tabs %}
{% tab title="Windows" %}
1. Unzip `Gum-win-x64.zip` into a folder of your choice.
2. Run `Gum.Avalonia.exe`.

Gum is not signed, so Windows shows the "Windows protected your PC" popup the first time. Click **More info**, then **Run anyway**. Alternatively, right-click the `.zip` file and select the option to unblock it before extracting.
{% endtab %}

{% tab title="macOS" %}
1. Double-click the `.tar.gz` file for your Mac to extract `Gum.app`.
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
    mkdir -p ~/gum && tar -xzf Gum-linux-x64.tar.gz -C ~/gum
    ```
2. Run Gum:

    ```sh
    ~/gum/Gum.Avalonia
    ```

Gum needs a desktop session (X11 or Wayland), a graphics driver with OpenGL 3.0 or newer, and the fontconfig library (`libfontconfig1` on Debian and Ubuntu), which desktop installs normally include.
{% endtab %}
{% endtabs %}

## Older releases (the WPF tool)

Releases up to and including [September 2, 2026](https://github.com/vchelaru/Gum/releases/tag/Release_September_02_2026) are the older Windows-only WPF tool, distributed as `Gum.zip` with `Gum.exe` inside. That release stays downloadable if you need it, for example for a plugin that has not been migrated yet (see [Plugins](../plugins/README.md)), but it does not receive new features. Its WINE setup scripts for Linux and macOS are kept at that release's tag ([setup\_gum\_linux.sh](https://github.com/vchelaru/Gum/blob/Release_September_02_2026/setup_gum_linux.sh), [setup\_gum\_mac.sh](https://github.com/vchelaru/Gum/blob/Release_September_02_2026/setup_gum_mac.sh)); the native downloads above replace them.

## Known issues

* Plugins built only for the older WPF tool do not load. The **Plugins** dialog lists them as not supported. See [Plugins](../plugins/README.md) for how to migrate one.
* The command-line tool (`gumcli`) is included in the `GumCli` folder beside the tool, but only the tool's own **Export as SVG** uses it so far.
* The macOS app has no icon yet.
* Gum is not signed on Windows or notarized on macOS, which is why the steps above are needed.

Please report anything else you find on the [Gum GitHub issues page](https://github.com/vchelaru/Gum/issues), along with your operating system.
