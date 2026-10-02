# Unity

## Introduction

{% hint style="warning" %}
The Unity package is experimental. It currently supports Windows x64 only. Other platforms are in progress.
{% endhint %}

Gum for Unity draws Gum UI (layout, Forms controls, and `.gumx` projects) with SkiaSharp. It has two Unity packages: Gum, and SkiaGameRendering, which supplies SkiaSharp.

## Installing the Packages

Unity cannot resolve a git dependency from inside a package, so add both packages to your project's `Packages/manifest.json`:

```json
"com.vchelaru.skiagamerendering": "https://github.com/vchelaru/SkiaGameRendering.git#upm",
"com.vchelaru.gum": "https://github.com/vchelaru/Gum.git#upm"
```

Each Gum release publishes the package to the `upm` branch. To pin a version, use `#upm/v<version>` (for example `#upm/v2026.10.1`) instead of `#upm`.

{% hint style="info" %}
The `#upm` URL works once the first Gum release that includes the package has shipped. Until then, install Gum from a Gum checkout by pointing a `file:` path at the `Unity/com.vchelaru.gum` folder.
{% endhint %}

## Input System

The Gum package depends on `com.unity.inputsystem`. In **Player Settings**, set **Active Input Handling** to **Input System Package** or **Both**.

## Adding Gum to a Scene

Add the `GumRenderer` and `GumInput` components to a GameObject. `GumRenderer` initializes `GumService.Default` in `Awake` and draws Gum over the screen, so create your UI from `Start`:

```csharp
// Initialize
var button = new Gum.Forms.Controls.Button();
button.Text = "Click me";
button.AddToRoot();
```

`GumInput` reads Unity's Input System (mouse, touches, and keyboard) and passes it to Gum each frame, so add it to the same GameObject as `GumRenderer`.

## Loading a Gum Project

Place the project folder under `Assets/StreamingAssets`, then set `GumRenderer.ProjectFile` to the project's path inside that folder, for example `GumProject/GumProject.gumx`.

If you add the components from code, add them to an inactive GameObject, set `ProjectFile`, then activate the GameObject.

## Rendering

On Direct3D 11, Gum draws on the GPU through SkiaGameRendering. On any other graphics API, Gum draws on the CPU and uploads a texture each frame. `GumRenderer.Texture` holds the result either way. Turn off `GumRenderer.DrawToScreen` to display the texture yourself.

## IL2CPP

A package cannot contain a `link.xml`, so add these to your project when building with IL2CPP:

* An `Assets/link.xml` that preserves `netstandard`. Stripped SkiaSharp still references the `netstandard` facade, and IL2CPP fails to resolve it otherwise:

```xml
<linker>
  <assembly fullname="netstandard" preserve="all" />
</linker>
```

* A **Managed Stripping Level** of at least **Low**. IL2CPP at **Minimal** has not been verified.

The `Samples/UnityGum` project in the Gum repository has both.
