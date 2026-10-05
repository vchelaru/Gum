# Unity

{% hint style="warning" %}
The Unity package is experimental. It supports Windows x64 and macOS (Intel and Apple silicon). Linux, Android, iOS, and WebGL are not supported yet.
{% endhint %}

Gum for Unity draws Gum UI (layout, Forms controls, and `.gumx` projects) with SkiaSharp. It needs two Unity packages: Gum, and SkiaGameRendering, which supplies SkiaSharp.

## Quick Start

1. Install the two packages (see **Install the Packages** below). Gum requires Unity 6 or newer.
2. In **Edit > Project Settings > Player**, set **Active Input Handling** to **Input System Package (New)** or **Both**.
3. In your scene, create an empty GameObject (**GameObject > Create Empty**) and name it `Gum`.
4. Select the `Gum` GameObject. In the Inspector, click **Add Component** and add `GumRenderer`, then add `GumInput`.
5. With the `Gum` GameObject still selected, click **Add Component > New script**, name it `GumExample`, and replace its contents with:

```csharp
using Gum.Forms.Controls;
using UnityEngine;

public class GumExample : MonoBehaviour
{
    void Start()
    {
        var button = new Button();
        button.Text = "Click me";
        button.AddToRoot();
    }
}
```

6. Press **Play**. A button labeled "Click me" appears in the Game view.

`GumRenderer` sets up Gum in `Awake`, so your own code can create UI from `Start` or later.

## Install the Packages

1. Open **Window > Package Manager**.
2. Click the **+** button, then **Install package from git URL**.
3. Paste this URL and click **Install**:

   ```
   https://github.com/vchelaru/SkiaGameRendering.git#upm
   ```

4. Repeat with this URL:

   ```
   https://github.com/vchelaru/Gum.git#upm
   ```

You can also add both lines to `Packages/manifest.json` instead:

```json
"com.vchelaru.skiagamerendering": "https://github.com/vchelaru/SkiaGameRendering.git#upm",
"com.vchelaru.gum": "https://github.com/vchelaru/Gum.git#upm"
```

The `#upm` at the end of each URL tells Unity to use the ready-to-install copy of the package that Gum publishes with each release. It always points at the newest one. Unity does not update on its own, so to upgrade, reinstall the package or use the update button in Package Manager.

To stay on a specific Gum version, end the Gum URL with `#upm/v<version>` instead, for example `#upm/v2026.10.2-preview.2.2`. Each version is listed under [Gum's tags on GitHub](https://github.com/vchelaru/Gum/tags) as `upm/v<version>`.

## What the Components Do

* `GumRenderer` sets up Gum and draws the UI over the screen.
* `GumInput` passes Unity's mouse, touch, keyboard, and gamepad input to Gum each frame. It belongs on the same GameObject as `GumRenderer`.

Mouse and touch work right away. To move between Forms controls with the keyboard or a gamepad, call `GumService.Default.UseKeyboardDefaults()` or `GumService.Default.UseGamepadDefaults()` in your script.

## Using the Code Samples in These Docs

Most code samples on other pages are written for a `Game` class. In Unity, adapt them like this:

* Code under a `// Initialize` comment goes in your script's `Start` method.
* Code under a `// Update` comment goes in your script's `Update` method. Skip any `GumUI.Update` call, since `GumRenderer` already updates Gum.
* Skip any `GumUI.Draw` call, since `GumRenderer` already draws Gum.
* `GumUI` in the samples is `GumService.Default`.

Samples that use MonoGame types such as `GameTime`, `GraphicsDevice`, or `Microsoft.Xna.Framework.Color` do not apply to Unity.

## Loading a Gum Project

1. Copy your Gum project folder into `Assets/StreamingAssets`.
2. Set `GumRenderer.ProjectFile` to the `.gumx` path inside that folder, for example `GumProject/GumProject.gumx`.

If you add the components from code, add them to an inactive GameObject, set `ProjectFile`, then activate the GameObject.

## Rendering

Gum draws on the GPU through SkiaGameRendering on every graphics API it supports, including Direct3D 11 and Metal. On any other API, Gum draws on the CPU and uploads a texture each frame, and the console logs a warning explaining why. `GumRenderer.Texture` holds the result either way. Turn off `GumRenderer.DrawToScreen` to display the texture yourself.

## IL2CPP

If you build with IL2CPP, add both of these to your project:

* An `Assets/link.xml` file that keeps `netstandard`:

```xml
<linker>
  <assembly fullname="netstandard" preserve="all" />
</linker>
```

* A **Managed Stripping Level** of at least **Low** in **Player Settings**.

The `Samples/UnityGum` project in the Gum repository has both.
