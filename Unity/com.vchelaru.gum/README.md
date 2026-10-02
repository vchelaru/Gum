# Gum for Unity

Gum UI (layout, Forms controls, `.gumx` projects) drawn with SkiaSharp. Windows x64 and macOS only so far.

## Platforms

Windows x64 and macOS (Intel and Apple silicon), in the Editor and in players. Linux, Android,
iOS and WebGL aren't supported yet: SkiaGameRendering ships Unity natives only for Windows x64 and
macOS, and Gum reads project files synchronously, which a browser can't do.

## Install

Gum needs SkiaGameRendering, which supplies SkiaSharp. Unity can't resolve a git dependency from a
package, so add both to `Packages/manifest.json`:

```json
"com.vchelaru.skiagamerendering": "https://github.com/vchelaru/SkiaGameRendering.git#upm",
"com.vchelaru.gum": "https://github.com/vchelaru/Gum.git#upm"
```

Each Gum release publishes the package to the `upm` branch, so `#upm` works once the first release
with it has shipped; until then, install from a Gum checkout with a `file:` path to this folder. To
pin a version, use `#upm/v<version>` (for example `#upm/v2026.10.1`) instead of `#upm`.

Gum's package brings in `com.unity.inputsystem`; set Player Settings > Active Input Handling to
Input System Package (or Both).

## Use

Add `GumRenderer` and `GumInput` to a GameObject. `GumRenderer` initializes `GumService.Default` in
`Awake` and draws Gum over the screen, so build UI from `Start`:

```csharp
var button = new Button();
button.Text = "Click me";
button.AddToRoot();
```

To load a project, put it under `Assets/StreamingAssets` and set `GumRenderer.ProjectFile` to its path
there (for example `GumProject/GumProject.gumx`). When adding the components from code, add them to an
inactive GameObject and set `ProjectFile` before activating it.

`GumInput` pushes the mouse, touches, keyboard and gamepads. Keyboard and gamepad navigation of Forms
controls is off until you call `GumService.Default.UseKeyboardDefaults()` or `UseGamepadDefaults()`.

## Rendering

Gum draws on the GPU through SkiaGameRendering wherever SkiaGameRendering supports the graphics API.
On any other API it draws on the CPU and uploads a texture each frame, and the console logs a warning
saying why. `GumRenderer.Texture` holds the result either way; turn off `DrawToScreen` to show it yourself.

## IL2CPP

`link.xml` can't live in a package, so add these to your project:

- An `Assets/link.xml` that preserves `netstandard` (stripped SkiaSharp still references the facade,
  and IL2CPP fails to resolve it otherwise):
  `<linker><assembly fullname="netstandard" preserve="all" /></linker>`
- Managed Stripping Level of at least Low. RichTextKit is still built against SkiaSharp 2.88, and
  IL2CPP at Minimal is not verified with it yet.

`Samples/UnityGum` in the Gum repo has both.
