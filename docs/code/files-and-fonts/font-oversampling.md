# Font Oversampling

## Introduction

When a camera or layer zooms in, text rasterized at its normal `FontSize` gets stretched along with everything else, so it looks soft or blocky compared to art rendered at native resolution. Font oversampling fixes this by regenerating the font atlas at a higher raster size while zoomed in, then scaling the drawn glyphs back down — so the text occupies the same layout space but its glyphs are rasterized at a higher pixel density, keeping edges crisp.

{% hint style="info" %}
Available in September 2026, or now if building Gum from source.
{% endhint %}

{% hint style="info" %}
Available on MonoGame, KNI, FNA, and Raylib. Not needed on SkiaGum or Silk.NET, since SkiaSharp rasterizes text natively at whatever size it's drawn -- it's crisp under zoom without any oversampling step.
{% endhint %}

## Enabling Oversampling

Two things are required:

1. Set the global flag: `TextRuntime.UseFontOversampling = true`. This is a project-wide `static` switch, not a per-instance property — pixel-art games that want blocky text at any zoom level should leave it off.
2. Register an in-memory font creator (`IInMemoryFontCreator` on MonoGame/KNI/FNA, `IRaylibFontCreator` on Raylib -- KernSmith ships both, see [Dynamic KernSmith Generation](font-strategies.md#dynamic-kernsmith-generation)). Oversampling only makes sense with dynamic font generation; a disk-based `FontCache` holds a fixed set of pre-baked sizes and can't rasterize a new one on demand.

```csharp
// Initialize
TextRuntime.UseFontOversampling = true;
CustomSetPropertyOnRenderable.InMemoryFontCreator =
    new KernSmithFontCreator(GraphicsDevice);
```

Once both are set, oversampling runs automatically: every frame, each visible `TextRuntime` checks the effective zoom of the `Layer` it's on and regenerates its font when that zoom has moved by at least a full raster pixel. No manual per-frame code is needed for the common case.

That per-`Layer` scoping matters: a layer with `LayerCameraSettings.IsInScreenSpace = true` (the normal setup for a HUD) is pinned to its own `Zoom` (default 1) regardless of the world camera's zoom, so text on a screen-space layer never regenerates — correctly, since it's never visually zoomed. Only text on a layer that actually tracks the world camera's zoom oversamples. See [Layer — LayerCameraSettings](../gum-code-reference/layer.md#layercamerasettings).

## Manual Control

`RegenerateOversampledFont(oversampleRatio)` is the method the automatic path calls internally; call it directly to force a specific raster ratio outside of camera-driven zoom (for example, a scripted zoom-in cutscene that doesn't go through `Camera.Zoom`). It returns `false` (and does nothing) if `UseFontOversampling` is off, no `IInMemoryFontCreator` is registered, or `oversampleRatio` isn't positive.

## Limitation: System Fonts vs. Registered `.ttf`

Measurement-stable oversampling — where the box a `TextRuntime` measures against doesn't shift as the font is regenerated at different raster sizes — requires `Font` (or `CustomFontFile`) to resolve to an explicit `.ttf` file, not a bare system font family name. Oversampling still runs with a system font name like `"Arial"`, but width/wrap measurement isn't guaranteed stable across regenerations. See [Font Strategies — System Fonts vs Registered Fonts](font-strategies.md#system-fonts-vs-registered-fonts) for how to register a `.ttf`.

## Limitation: BBCode Inline Runs on Raylib

On Raylib, oversampling only re-rasterizes a `TextRuntime`'s base font. An inline BBCode run (`[FontScale=...]`, `[FontSize=...]`, etc.) inside the text keeps drawing at its own independently-resolved size, unaffected by oversampling. This means a Text that mixes plain and BBCode-styled runs can end up with plain runs crisp and styled runs at their normal (unoversampled) size while zoomed in. MonoGame/KNI/FNA don't have this limitation -- oversampling compensation composes correctly with inline runs there.

## Try It

{% hint style="warning" %}
Interactive XnaFiddle demo pending — XnaFiddle's pinned Gum package predates font oversampling, so the fiddle link can't be added yet (tracked: [XnaFiddle#127](https://github.com/vchelaru/XnaFiddle/issues/127)). The sample below is a reference you can paste into your own project.
{% endhint %}

Scroll to zoom the camera; the text stays crisp because it's oversampled. Toggle the checkbox off to compare against undersampled text at the same zoom. The font (`std/DroidSans.ttf`) is XnaFiddle's standard-content Droid Sans — no upload needed.

```csharp
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using MonoGameGum.GueDeriving;
using Gum.Wireframe;
using KernSmith;
using KernSmith.Gum;
using RenderingLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class Game1 : Game
{
    GraphicsDeviceManager graphics;
    GumService GumUI => GumService.Default;

    TextRuntime previewText;
    Label zoomLabel;
    int previousScrollWheel;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        // StbTrueType is required in Blazor WASM -- no native FreeType binary is available there.
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(GraphicsDevice, RasterizerBackend.StbTrueType);
        KernSmithFontCreator.RegisterFont("Droid Sans",
            System.IO.Path.Combine(Content.RootDirectory, "std/DroidSans.ttf"));

        TextRuntime.UseFontOversampling = true;

        previewText = new TextRuntime();
        previewText.Font = "Droid Sans";
        previewText.FontSize = 28;
        previewText.Text = "Scroll to zoom the camera";
        previewText.Anchor(Gum.Wireframe.Anchor.Center);
        previewText.AddToRoot();

        var oversampleCheck = new CheckBox();
        oversampleCheck.Text = "Oversampling";
        oversampleCheck.IsChecked = true;
        oversampleCheck.Width = 160;
        oversampleCheck.Anchor(Gum.Wireframe.Anchor.TopLeft);
        oversampleCheck.X = 8;
        oversampleCheck.Y = 8;
        oversampleCheck.Checked += (_, _) => TextRuntime.UseFontOversampling = true;
        oversampleCheck.Unchecked += (_, _) => TextRuntime.UseFontOversampling = false;
        oversampleCheck.AddToRoot();

        zoomLabel = new Label();
        zoomLabel.Text = "Zoom: 1.0x";
        zoomLabel.Width = 160;
        zoomLabel.Anchor(Gum.Wireframe.Anchor.TopLeft);
        zoomLabel.X = 8;
        zoomLabel.Y = 40;
        zoomLabel.AddToRoot();

        previousScrollWheel = Mouse.GetState().ScrollWheelValue;

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        var mouseState = Mouse.GetState();
        int scrollDelta = mouseState.ScrollWheelValue - previousScrollWheel;
        previousScrollWheel = mouseState.ScrollWheelValue;

        var camera = SystemManagers.Default.Renderer.Camera;
        camera.Zoom = MathHelper.Clamp(camera.Zoom + scrollDelta * 0.001f, 1f, 8f);
        zoomLabel.Text = $"Zoom: {camera.Zoom:0.0}x";

        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
```

## Related Pages

* [Font Strategies](font-strategies.md) — dynamic KernSmith generation and registering `.ttf` fonts.
* [Camera](../gum-code-reference/camera.md) — zooming the camera.
* [Layer](../gum-code-reference/layer.md) — `LayerCameraSettings` and screen-space layers.
