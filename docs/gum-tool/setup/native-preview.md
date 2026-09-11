# Native Preview (Windows, macOS, Linux)

The native preview is a build of the Gum tool that runs directly on Windows, macOS, and Linux, with no WINE or other compatibility layer. It opens the same projects, saves the same files, and generates the same code as the Windows tool.

{% hint style="info" %}
The native preview is available in October 2026, or now if building Gum from source. Until then, releases include only the Windows tool.
{% endhint %}

## Download

Each release that includes the preview lists these files next to `Gum.zip`:

| File | For |
|---|---|
| `Gum-Avalonia-preview-win-x64.zip` | Windows (64-bit) |
| `Gum-Avalonia-preview-osx-arm64.tar.gz` | macOS on Apple Silicon (M1 and later) |
| `Gum-Avalonia-preview-osx-x64.tar.gz` | macOS on Intel |
| `Gum-Avalonia-preview-linux-x64.tar.gz` | Linux (64-bit) |

Each download includes everything it needs, so you do not need to install .NET first. A matching `.sha256` file lists the checksum of each download.

{% tabs %}
{% tab title="Windows" %}
1. Unzip `Gum-Avalonia-preview-win-x64.zip` into a folder of your choice.
2. Run `Gum.Avalonia.exe`.

The preview is not signed, so Windows shows the "Windows protected your PC" popup the first time. Click **More info**, then **Run anyway**.
{% endtab %}

{% tab title="macOS" %}
1. Double-click the `.tar.gz` file for your Mac to extract `Gum.app`.
2. Move `Gum.app` into your **Applications** folder.
3. Open a terminal and allow the app to run. The preview is not notarized by Apple, so macOS blocks it until you do this once:

    ```sh
    xattr -dr com.apple.quarantine /Applications/Gum.app
    ```
4. Open Gum from **Applications** or Launchpad.
{% endtab %}

{% tab title="Linux" %}
1. Extract the download into a folder of your choice:

    ```sh
    mkdir -p ~/gum && tar -xzf Gum-Avalonia-preview-linux-x64.tar.gz -C ~/gum
    ```
2. Run Gum:

    ```sh
    ~/gum/Gum.Avalonia
    ```

Gum needs a desktop session (X11 or Wayland), a graphics driver with OpenGL 3.0 or newer, and the fontconfig library (`libfontconfig1` on Debian and Ubuntu), which desktop installs normally include.
{% endtab %}
{% endtabs %}

## Known issues in the preview

* The preview keeps its own settings, so recent projects and layout from the Windows tool do not carry over.
* Plugins built only for the Windows tool do not load. The **Plugins** dialog lists them as not supported.
* The command-line tool (`gumcli`) is included in the `GumCli` folder beside the tool, but only the tool's own **Export as SVG** uses it so far.
* The macOS app has no icon yet.
* The preview is not signed on Windows or notarized on macOS, which is why the steps above are needed.

Please report anything else you find on the [Gum GitHub issues page](https://github.com/vchelaru/Gum/issues) and mention the native preview and your operating system.
