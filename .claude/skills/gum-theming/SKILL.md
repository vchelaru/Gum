---
name: gum-theming
description: Building or modifying a Gum theme package (Themes/Gum.Themes.*) — restyling Forms controls by subclassing their V3 default visuals. Triggers: any file under Themes/, custom *Visual subclassing Gum.Forms.DefaultVisuals.V3.*, theme entry-point methods like EditorTheme.Apply / DarkProTheme.Apply.
---

# Gum Theming

A theme restyles Forms controls by subclassing each V3 default visual, swapping the children that define the look, and re-wiring the state callbacks. See `Themes/Gum.Themes.Editor.MonoGame/` (NineSlice-only) and `Themes/Gum.Themes.DarkPro.MonoGame/` (NineSlice + Apos.Shapes) for two working examples on opposite ends of the visual-primitive spectrum.

## Theme entry point

`MyTheme.Apply(GraphicsDevice)` does in order:

1. Wire KernSmith if the theme uses dynamic fonts: `CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(gd);`
2. `BmfcSave.AddCharacters("…")` for any non-ASCII glyphs visuals will render. Must come before any text rendering — KernSmith only bakes characters it knows about at atlas-creation time.
3. Register embedded TTFs via `KernSmithFontCreator.RegisterFont(family, bytes, style)`. See "Icon fonts" below.
4. If your visuals use Apos.Shapes runtimes (`RoundedRectangleRuntime`, `ColoredCircleRuntime`, etc.), `if (!ShapeRenderer.Self.IsInitialized) ShapeRenderer.Self.Initialize();` — consumers shouldn't have to know your theme reaches for shapes internally.
5. Populate `Styling.ActiveStyle` (Text.Normal/Strong, Colors, optional SpriteSheet override) and `FrameworkElement.DefaultFormsTemplates[typeof(Button)] = new VisualTemplate((_, c) => new MyButtonVisual(tryCreateFormsObject: c));` for each restyled control.

## Visual-primitive options

The V3 baseline draws backgrounds with `NineSliceRuntime` referencing `Styling.ActiveStyle.SpriteSheet`. A theme can:

- **Stay on NineSlice** and just retexture the sprite sheet plus state tweaks (Editor theme's approach). Lightest touch.
- **Replace specific renderables with Apos.Shapes** for rounded corners, circles, or drop shadows (Dark Pro's approach). Pulls in the `MonoGameGumShapes` package and forces a `ShapeRenderer.Self.Initialize()` in `Apply`.
- **Mix** — e.g. NineSlice fills for large panels (cheap), Apos rounded rects for small interactive controls (clean corners at any size).

Pick what matches the design. The replacement pattern below applies regardless of which primitives you swap in.

## The replacement pattern

Each visual subclasses `Gum.Forms.DefaultVisuals.V3.*Visual`. In its constructor:

1. **Detach** the children you're replacing — typically `Background.Parent = null; FocusedIndicator.Parent = null;` (and `ClipContainer.Parent = null;` for TextBox-style controls — see "ClipContainer reordering").
2. **Add** the replacement children — NineSlice with new textures, Apos shapes, ColoredRectangles, whatever fits.
3. **Reattach** anything you detached for ordering reasons (TextInstance, ClipContainer) last so it renders on top of the new background layer.
4. **Re-wire state callbacks with `=` (not `+=`)** so the base's color-pumping into the now-detached children is fully replaced. Each `States.{Enabled, Highlighted, Pushed, Focused, …}.Apply` becomes a fresh lambda that mutates your replacement children.

Different controls have different state sets — Button has 7, TextBox has 4 (no Pushed), CheckBox has 21 (7 visual × 3 value), Slider has 7.

## ClipContainer reordering

`TextBoxBaseVisual` adds children in order `[Background, ClipContainer, FocusedIndicator]`. If you only detach Background and FocusedIndicator and then `AddChild` your replacements, they end up *in front of* ClipContainer — the text renders behind. Fix: also detach ClipContainer, add your replacements, reattach ClipContainer last.

## Hover/press color consistency

A control with a Pushed state (Button) shouldn't switch border color families between hover and press — gray→blue→gray flickers visibly through a hover-press-release motion. Use the accent for hover too. A control without Pushed (TextBox, ComboBox closed) is fine using a softer hover color and reserving the accent for sustained focus.

## Slider thumb is a Button

`V3.SliderVisual` creates the thumb as `new Button()`, so its visual is whatever `DefaultFormsTemplates[typeof(Button)]` resolves to — your theme's Button, which is the wrong shape for a thumb in most designs. Fix: pass `tryCreateFormsObject:false` to the base, detach `ThumbInstance.Parent`, add your own thumb visual named `"ThumbInstance"` to `TrackInstance`, then create the Forms `Slider` yourself at the end of the ctor. `RangeBase.ReactToVisualChanged` looks up `"ThumbInstance"` by name and wraps it in a Button — your thumb just needs `"ButtonCategory"` state callbacks (use `Button.ButtonCategoryName`) and `HasEvents = true`.

## Value-driven visuals

Visuals that need to react to a continuous Forms-control value (a slider fill bar tracking `Slider.Value`, a progress indicator, etc.):

- `RangeBase.Value` raises `ValueChanged` but does NOT call `OnPropertyChanged`, so `INotifyPropertyChanged` is not a viable hook.
- The Forms control is assigned to the visual via `FormsControlAsObject`. **Override the setter** (`InteractiveGue.FormsControlAsObject` is `virtual`) to subscribe on assignment and unsubscribe on reassignment. This is the only hook that works for both construction paths: `tryCreateFormsObject:true` (visual creates the Forms control in its ctor) and `tryCreateFormsObject:false` (FrameworkElement creates the visual via the template and then assigns the externally-created Forms control).
- `Minimum`/`Maximum` setters fire no external event. Consumers conventionally set them before `Value`, so an initial update at assignment plus a `ValueChanged` subscription covers practical cases.

## Icon fonts

User-facing monospace fonts (DM Mono, JetBrains Mono, etc.) almost always lack the Dingbats (`✓ ✕`) and Geometric Shapes (`▾ ▴ ▲ ▼ ◀ ▶`) blocks. Setting any of those characters with such a font silently fails — KernSmith's atlas generator can't bake a glyph the source font doesn't have, the rendered text is blank.

If your theme renders these glyphs as text, bundle a separate icon-coverage font (DejaVu Sans Mono is the proven choice, Bitstream Vera / DejaVu license, redistribution permitted) under a distinct family name like `"DM Mono Icons"`. Register via `KernSmithFontCreator.RegisterFont(iconFamily, bytes)` and use it in visuals via `runtime.Font = MyTheme.IconFontFamily`. Add the license file to the NuGet root.

Alternative: render the glyph as a sprite sheet icon (via `Styling.ActiveStyle.Icons`) or build the shape from primitives (e.g. two `LineRuntime`s for a check mark). The font path is simplest when there are many such glyphs.

Size the glyph's `TextRuntime` Absolute, **larger** than the box it sits in (~1.5x), centered. DejaVu Sans Mono and most icon-coverage fonts aren't truly monospaced for non-Latin — symbol glyphs have wider advance widths than ASCII, and a runtime sized exactly to the box clips or drops them.

## csproj gotcha: PrivateAssets on KernSmith

If your theme calls `KernSmithFontCreator` directly, KernSmith is a runtime dependency — it must flow transitively to consumers. **Do not** mark it `<PrivateAssets>All</PrivateAssets>` on the `<PackageReference>`. MonoGame.Framework can stay private (consumers always bring their own).

`Gum.Themes.Editor.MonoGame` currently has `PrivateAssets=All` on KernSmith — that's a latent bug; consumers would need to install KernSmith manually even though the theme depends on it.

## Cross-references

- Apos.Shapes runtime types and the shape-batch scissor plumbing: [gum-monogame-rendering](../gum-monogame-rendering/SKILL.md).
- V3 visual base classes and the state-category contract: [gum-forms-default-visuals](../gum-forms-default-visuals/SKILL.md).
- KernSmith and font generation: [gum-runtime-fonts](../gum-runtime-fonts/SKILL.md).
