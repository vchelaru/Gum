# Gum for Unity

Gum UI (layout, Forms controls, `.gumx` projects) drawn with SkiaSharp. Windows x64 only so far.

## Install

Gum needs SkiaGameRendering, which supplies SkiaSharp. Unity can't resolve a git dependency from a
package, so add both to `Packages/manifest.json`:

```json
"com.vchelaru.skiagamerendering": "https://github.com/vchelaru/SkiaGameRendering.git#upm",
"com.vchelaru.gum": "https://github.com/vchelaru/Gum.git#upm"
```

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

## Rendering

On Direct3D 11, Gum draws on the GPU through SkiaGameRendering. On any other graphics API it draws on
the CPU and uploads a texture each frame. `GumRenderer.Texture` holds the result either way; turn off
`DrawToScreen` to show it yourself.

## IL2CPP

`link.xml` can't live in a package, so add these to your project:

- An `Assets/link.xml` that preserves `netstandard` (stripped SkiaSharp still references the facade,
  and IL2CPP fails to resolve it otherwise):
  `<linker><assembly fullname="netstandard" preserve="all" /></linker>`
- Managed Stripping Level of at least Low. At Minimal, Svg.Skia (built against SkiaSharp 2.88) keeps
  calls that don't compile to C++ against SkiaSharp 3.x.

`Samples/UnityGum` in the Gum repo has both.
