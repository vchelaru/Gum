# Font Oversampling

## Introduction

When the camera or a layer zooms in, text is scaled up along with everything else. The font was drawn at its normal `FontSize`, so the letters end up soft or blocky next to art that is drawn at full size. Font oversampling fixes this. While the view is zoomed in, Gum builds the font again at a larger size, then draws the letters smaller so they fit. The text takes up the same space in your layout, but it is made of more pixels, so the edges stay sharp.

{% hint style="info" %}
Font oversampling is a preview feature. It first shipped in the `2026.8.18.1-preview.1` packages. You can also use it by building Gum from source.
{% endhint %}

{% hint style="info" %}
Works on MonoGame, KNI, FNA, and Raylib. SkiaGum and Silk.NET do not need it, since SkiaSharp draws text at whatever size it is asked for, so the text stays sharp when you zoom.
{% endhint %}

## Enabling Oversampling

You need two things:

1. Turn on the flag: `TextRuntime.UseFontOversampling = true`. This is one `static` switch for the whole game, not a property on each text. If you want blocky text in a pixel art game, leave it off.
2. Give Gum a way to build fonts while the game runs (`IInMemoryFontCreator` on MonoGame/KNI/FNA, `IRaylibFontCreator` on Raylib). KernSmith does both, see [Dynamic KernSmith Generation](font-strategies.md#dynamic-kernsmith-generation). Oversampling needs this, because a `FontCache` folder only holds the sizes you built ahead of time and cannot add a new one later.

```csharp
// Initialize
TextRuntime.UseFontOversampling = true;
CustomSetPropertyOnRenderable.InMemoryFontCreator =
    new KernSmithFontCreator(GraphicsDevice);
```

After that it runs on its own. Each frame, every text you can see checks the zoom of the `Layer` it sits on. If that zoom moved enough to change the font by a full pixel, the text builds its font again. You do not need to write any code that runs each frame.

The layer matters here. A layer with `LayerCameraSettings.IsInScreenSpace = true`, which is how most HUDs are set up, keeps its own `Zoom` (1 by default) no matter where the camera goes. Text on that layer never builds a new font, which is right, because it never looks zoomed. Only text on a layer that follows the camera zoom is oversampled. See [Layer, LayerCameraSettings](../gum-code-reference/layer.md#layercamerasettings).

## Manual Control

`RegenerateOversampledFont(oversampleRatio)` is the method the automatic path calls. You can call it yourself to pick a size, such as during a cut scene that zooms without touching `Camera.Zoom`. It returns `false` and does nothing if `UseFontOversampling` is off, if no `IInMemoryFontCreator` is set, or if `oversampleRatio` is zero or less.

The automatic path debounces continuous zooming: it only rebuilds a font's atlas once the requested size has moved `TextRuntime.OversamplingRegenerateThresholdPixels` (1 pixel by default) from what was last rasterized. That 1px absolute threshold is a bigger fraction of a small `FontSize` than a large one, so small text can stay noticeably blurry for longer while zooming. Lower the threshold (down to `0` to regenerate on any change) if you want small text to re-crisp sooner, at the cost of rebuilding the atlas more often.

## Limitation: System Fonts vs. Registered `.ttf`

For the text to keep the same size while its font is built again at other sizes, `Font` (or `CustomFontFile`) has to point at a real `.ttf` file, not just a font name like `"Arial"`. Oversampling still runs with a font name, but the width and the line breaks may shift a little each time the font is built. See [Font Strategies, System Fonts vs Registered Fonts](font-strategies.md#system-fonts-vs-registered-fonts) to learn how to register a `.ttf`.

## Limitation: `[FontSize]`/`[IsBold]`/`[IsItalic]`/`[OutlineThickness]` BBCode Runs on Raylib

On Raylib, oversampling builds the base font of a `TextRuntime` again. Runs that only scale that font, meaning runs with no tag or with a `[FontScale=...]` tag, come out at the right size, the same as on MonoGame, KNI, and FNA. Runs that ask for a different font, meaning `[FontSize=...]`, `[IsBold=...]`, `[IsItalic=...]`, and `[OutlineThickness=...]`, keep their own size and are not oversampled. Changing them would upset the line height and baseline math they already rely on. MonoGame, KNI, and FNA do not have this limit, and oversampling works with every kind of run there.

## Try It

Drag the **Zoom** slider to zoom the camera. The text stays sharp because it is oversampled. Turn off **Oversampling** to see how the same text looks without it. The font (`std/DroidSans.ttf`) ships with XnaFiddle, so there is nothing to upload.

```csharp
using MonoGameGum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using MonoGameGum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using RenderingLibrary;
using Microsoft.Xna.Framework;

public class Game1 : Game
{
    GraphicsDeviceManager graphics;
    GumService GumUI => GumService.Default;

    TextRuntime previewText;
    Label zoomLabel;
    Slider zoomSlider;
    CheckBox oversampleCheck;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.V3);

        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(GraphicsDevice);
        KernSmithFontCreator.RegisterFont("Droid Sans",
            System.IO.Path.Combine(Content.RootDirectory, "std/DroidSans.ttf"));

        TextRuntime.UseFontOversampling = true;

        previewText = new TextRuntime();
        previewText.Font = "Droid Sans";
        previewText.FontSize = 48;
        previewText.Text = "Crisp under zoom";
        previewText.AddToRoot();
        previewText.Anchor(Gum.Wireframe.Anchor.Center);

        oversampleCheck = new CheckBox();
        oversampleCheck.Text = "Oversampling";
        oversampleCheck.Width = 200;
        oversampleCheck.IsChecked = true;
        oversampleCheck.X = 8;
        oversampleCheck.Y = 8;
        oversampleCheck.AddToRoot();
        oversampleCheck.Checked += (_, _) => TextRuntime.UseFontOversampling = true;
        oversampleCheck.Unchecked += (_, _) => TextRuntime.UseFontOversampling = false;

        zoomLabel = new Label();
        zoomLabel.Text = "Zoom: 1.0x";
        zoomLabel.Width = 200;
        zoomLabel.X = 8;
        zoomLabel.Y = 40;
        zoomLabel.AddToRoot();

        zoomSlider = new Slider();
        zoomSlider.Minimum = 1;
        zoomSlider.Maximum = 4;
        zoomSlider.Value = 1;
        zoomSlider.Width = 200;
        zoomSlider.X = 8;
        zoomSlider.Y = 70;
        zoomSlider.AddToRoot();
        zoomSlider.ValueChanged += (_, _) =>
        {
            GumUI.SystemManagers.Renderer.Camera.Zoom = (float)zoomSlider.Value;
            zoomLabel.Text = $"Zoom: {zoomSlider.Value:0.0}x";
        };

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
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

[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACp1WbW_aMBD-zq-w0D4EDXl0q7SpVSdtQa3QhloBfdm-TCYcYDWxke2U0qn_fefECSZ1tmr5gnv3-O65N19zzcWKjKWQFyyDizw77eSFCI_0XKpMvxDQWAqjZBrQDGHJ8tTccJ2zVNObDxXEc0AvchiC4g8o9y3ccgVLhZhK-A2UmGbcrF8IqMdzAmKB1sTqO58rpna1R54oqeXS0DvB6Lk1vJXq_rTT2eTzlCckSZnWxJI6IifFb-d3h-B3odhmzRM9hAeewJgJtgJFVk56WmLybArK6u3xekTOPnuyKg_ozIJn8GgmuTA8A7JRaBW2VlRa-s7mkJInKbPiVAqnKcegCml5LMXxGpL7r_KRyAdQmmWbFAqR8-MCK0KKeoWojMh-FX9yRgRsw0FGZs11r_Rlv5Eey1wDlpPPU8CbRuVYHqt6di6VNJAYWBSUFFIlD5IvyEhww1nKn-AFkSJf1ANYp31y2Do3GB-XAjsI6dRX41wbzAiYKyU3oMzuUpTlZ0gPTY4hk2p3ju0ZK2BGKnJW37WfDbxuIg8WHWajTyZMG2yqJ1BfWXKPLujUzGcY_Gy3AS9BIWN0Aitur1tZ1B0qm48pE7rbPyAz3SEoo6NLesWwpWOZzbmAyA4XCEMnUpohjkSCJnd90tVm8a6wZU1RY5bdnp8ar8fotQbr-7JqEjsOVe3qC14jupbwTERejB4Qp1xYtB9UO3CK-UPw8acwxHnuxorrDclF1fAtFr8sFjNpk9LG7YtI1raU_lPihDTGjILy89WYIJeCasB8Hw1kzdtPb7cdfssXZo3494NBO2iki18cI3_GQsg7RHxqV__4uzqYxCaoovL2jES_-uRXzz5ur-2vNqPXWIj_MrvE18Dv2_qldCUrzn44NaCu1E-UnJAjOnjshmDBCu3VjYzvFTbXx8ErB2k-0LuHvaRe_tHkXkrpmAue5RlCj8J69uj0x0H9DUtzaL3dGrPTB4J2Ghv1x_ClYHc1KcVrJlaNNqjB-y2x3xTlO-k2lMbX1b4UaC3G-VaM2uIipWiZSmZ6TW97HsHWeON643fz3smADp79dnn2Cjln2m6b_YZ71Uq83iyYgchu55n9T2DlDuH96NA1aM-k8B5W_4PBULHtK_wf7EIap8AUbqXUPqNSCczzFtci5sjjVHIu7DeZFsIXPJ87fwBk4c0ZeQoAAA)

{% hint style="info" %}
The fiddle runs in a browser, so it creates its font creator as `new KernSmithFontCreator(GraphicsDevice, KernSmith.RasterizerBackend.StbTrueType)`. KernSmith always starts with the FreeType backend, and FreeType is native code that a browser cannot run. Without that second argument, Gum throws a `PlatformNotSupportedException` on browser-wasm the moment it tries to build a font, rather than failing silently. On desktop you can use the code above as it is. See [Backend Selection](advanced-font-effects.md#backend-selection-freetype-vs-stbtruetype).
{% endhint %}

## Related Pages

* [Font Strategies](font-strategies.md): building fonts with KernSmith and registering `.ttf` files.
* [Automatic Glyph Growth](font-automatic-growth.md): adding characters to a live font that weren't baked in ahead of time.
* [Camera](../gum-code-reference/camera.md): zooming the camera.
* [Layer](../gum-code-reference/layer.md): `LayerCameraSettings` and screen space layers.
