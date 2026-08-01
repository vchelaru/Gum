# Render Targets

### Introduction

Rendering translucent content onto an offscreen render target (MonoGame's `RenderTarget2D`, raylib's `RenderTexture2D`) and then compositing that target elsewhere is a common pattern, but it has a gotcha that does not show up when drawing straight to the screen: the render target's own alpha channel matters.

When you draw straight to the screen, nothing reads the screen's alpha channel afterward, so a blend mode that leaves it in whatever state is harmless. A render target is different: its alpha channel gets read again the next time the target itself is composited onto something else. If drawing multiple translucent objects onto the target uses a blend mode that does not correctly accumulate that alpha, the target ends up with an alpha channel that is wrong even though the colors on screen look correct at the time. The most common symptom is content that appears fine while you are drawing to the target, but darkens, fades unevenly, or loses transparency where you did not expect it once the target itself is drawn somewhere else.

The fix in every backend is the same idea: use blend factors that add to the target's alpha rather than ones designed for compositing straight onto an opaque screen. The exact code differs per backend.

{% hint style="info" %}
This page covers **manually managed** render targets, where you create the render target yourself and control the drawing and compositing (MonoGame `RenderTarget2D` via `GumBatch`/`SpriteBatch`, or raylib `RenderTexture2D` via raw raylib calls). If you want Gum to manage the render target for you, see [Is Render Target](../../gum-tool/gum-elements/container/is-render-target.md) and [RenderTargetTextureSource](../standard-visuals/spriteruntime/rendertargettexturesource.md), which bake a container's contents to a texture without you having to set blend factors yourself.
{% endhint %}

### MonoGame

`GumBatch` can draw Gum objects onto a `RenderTarget2D` the same way a regular `SpriteBatch` can:

```csharp
// Draw
// Assuming MyRenderTarget is a valid render target:
GraphicsDevice.SetRenderTarget(MyRenderTarget);
gumBatch.Begin();
gumBatch.Draw(SomeGumObject);
gumBatch.End();

// now set the render target to null to draw it to screen:
GraphicsDevice.SetRenderTarget(null);
spriteBatch.Draw(MyRenderTarget, new Vector2(0, 0), Color.White);
```

{% hint style="info" %}
Content drawn to a render target this way is not interactive by default, because the cursor is measured in window pixels while the content was drawn, and then scaled or offset, into the render target. To keep it clickable, map the cursor back into render-target space with [`HitTestTransformMatrix`](../events-and-interactivity/mouse-and-touch-screen-cursor.md#several-coordinate-spaces-at-once-hittesttransformmatrix).
{% endhint %}

{% hint style="warning" %}
A container only ever drawn through `GumBatch` (never `AddToRoot`) has no `EffectiveManagers`, which breaks Forms controls that depend on it — most notably a `Menu`/`ComboBox` popup, which opens but never closes on an outside click. Call `container.AttachManagersOnly(SystemManagers.Default)` once after creating it so this works correctly.
{% endhint %}

For a runnable example, see the `RenderTarget` screen in the Gum immediate-mode sample. It draws a scaled, offset render target alongside a full-screen UI drawn at 1:1, both interactive in the same frame:

{% embed url="https://github.com/vchelaru/Gum/tree/main/Samples/MonoGameGumImmediateMode" %}

If you are rendering multiple translucent objects onto a render target, the `BlendState` must be set so that alpha accumulates rather than getting overwritten. The default `BlendState` can "remove" alpha from the render target when new instances are drawn on top of existing content.

The following shows a `BlendState` for objects which have partial transparency and are drawn onto a render target, using separate alpha source/destination factors:

```csharp
// Initialize
var blendState = new BlendState();

blendState.ColorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend;
blendState.ColorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend;
blendState.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;

blendState.AlphaSourceBlend = Blend.SourceAlpha;
blendState.AlphaDestinationBlend = Blend.DestinationAlpha;
blendState.AlphaBlendFunction = BlendFunction.Add;

halfTransparentRectangle.BlendState = blendState;
```

### Raylib

Raylib's canned `BlendMode.Alpha` uses the same source-alpha factor for both the color and alpha channels. That is correct for compositing onto an opaque screen, but drawing multiple translucent objects onto a `RenderTexture2D` with `BlendMode.Alpha` leaves the texture's alpha channel under 255 even when the colors look correct, because each draw multiplies the destination alpha down instead of accumulating it. Compositing that texture again elsewhere (for example, blitting it to the window) then darkens the result a second time using that leftover alpha.

The fix is to set separate alpha blend factors with `Rlgl.SetBlendFactorsSeparate` instead of using the canned `BlendMode.Alpha`, then select `BlendMode.CustomSeparate`. Keep the color factors as a normal alpha blend, but set the alpha factors to accumulate (`GL_ONE`, `GL_ONE_MINUS_SRC_ALPHA`) instead of overwrite:

```csharp
// Draw
// GL blend-factor / equation constants (OpenGL spec, not raylib-specific).
const int GL_SRC_ALPHA = 0x0302;
const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
const int GL_ONE = 1;
const int GL_FUNC_ADD = 0x8006;

Raylib.BeginTextureMode(myRenderTexture);

Rlgl.SetBlendFactorsSeparate(
    glSrcRGB: GL_SRC_ALPHA,
    glDstRGB: GL_ONE_MINUS_SRC_ALPHA,
    glSrcAlpha: GL_ONE,
    glDstAlpha: GL_ONE_MINUS_SRC_ALPHA,
    glEqRGB: GL_FUNC_ADD,
    glEqAlpha: GL_FUNC_ADD);
Raylib.BeginBlendMode(BlendMode.CustomSeparate);

// draw your translucent content here

Raylib.EndBlendMode();
Raylib.EndTextureMode();
```

{% hint style="info" %}
`Raylib.EndBlendMode` always resets to `BlendMode.Alpha`, it does not restore whatever blend mode was active before. If you are drawing a mix of translucent objects onto the same render target and need to change blend modes mid-bake, re-select `BlendMode.CustomSeparate` with these same factors after each `EndBlendMode` rather than assuming the accumulating alpha blend is still active.
{% endhint %}
