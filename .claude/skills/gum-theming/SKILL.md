---
name: gum-theming
description: Building or modifying a Gum theme package (Themes/Gum.Themes.*) — restyling Forms controls by subclassing their V3 default visuals. Triggers: any file under Themes/, custom *Visual subclassing Gum.Forms.DefaultVisuals.V3.*, theme entry-point methods like EditorTheme.Apply / DarkProTheme.Apply.
---

# Gum Theming

A theme restyles Forms controls by subclassing each V3 default visual, swapping the children that define the look, and re-wiring the state callbacks. See `Themes/Gum.Themes.Editor.MonoGame/` (NineSlice-only) and `Themes/Gum.Themes.DarkPro.MonoGame/` (NineSlice + Apos.Shapes) for two working examples on opposite ends of the visual-primitive spectrum.

This skill is about *authoring* a theme. For how a theme is *consumed* — installing the NuGet package, calling `Apply`, and the catalog of shipped themes — see the user guide: https://docs.flatredball.com/gum/code/styling/themes.

## Fastest path: clone the template

`Themes/Gum.Themes.Template.MonoGame` (+ its `.Kni` sibling) is a complete, building theme whose purpose is to be **cloned, not consumed**. Start a new theme from it rather than from scratch — it already wires the two-project MonoGame/KNI split, font embedding, and the `Apply` skeleton, and it encodes the two conventions below. Recipe for turning a CSS/HTML design into a theme:

1. **Clone both projects** and rename `Template` → `YourTheme`: folder names, file names, `PackageId`, `namespace`, and the six `Template*` type names (`TemplateTheme`, `TemplateStyling`, `TemplateColors`, `TemplateText`, `TemplateShapes`, `TemplateTextInputDecoration`). Per-control visual class names (`ButtonVisual`, …) stay — the namespace disambiguates. **Do not blind-replace the word `Template`** — three Gum *framework* identifiers contain it and must NOT be renamed: `VisualTemplate`, `DefaultFormsTemplates`, `ScrollViewerVisualTemplate`. The template keeps its own element names prefix-free (`"BoxFill"`, not `"TemplateBoxFill"`), so the only theme-owned `Template` tokens are the namespace + those six types; rename exactly those (whole-word, case-sensitive) and leave the framework identifiers. Add both projects to `AllLibraries.sln` under the Themes folder and flip `<GeneratePackageOnBuild>` to `true`. **Leave `AssemblyName`/`RootNamespace` unset** — they default to the project name and must stay equal, because fonts are looked up by assembly name but embedded under the root namespace; if they diverge the build still succeeds but fonts throw `FileNotFoundException` at runtime.
2. **Transcribe the design's `:root` block into `YourThemeColors`** (lives alongside `YourThemeStyling` and `YourThemeText` in one `YourThemeStyling.cs`) — one CSS custom property → one settable `Color` property, keeping the `// --var #hex` comment so the mapping back to the mockup stays auditable. This is the bulk of "make it match the design." The standard slots are a starting vocabulary: add tokens for anything the design defines beyond them, delete unused ones. Every theme's `Colors` class must also expose `TextPrimary`/`TextMuted`/`Primary`/`Accent` — the 4-token guardrail every theme's `Apply()` pushes into `V3.Styling.ActiveStyle.Colors`, enforced by a reflection test. Where the theme's own vocabulary already covers one of these concepts under a different name, expose the guardrail name as a get-only alias (e.g. `public Color TextPrimary => Text;`) rather than duplicating the value.
3. **Leave derived colors computed.** Hover/press/selection tints are get-only properties using `ColorExtensions.Adjust(±n)` (lighten/darken — already shipped with V3 styling). Only pin an explicit value when the design specifies an exact color that `Adjust` can't express (e.g. an eye-tuned glow alpha).
4. **Swap the fonts** (TTFs + `<EmbeddedResource>` in both csprojs, plus the family names, `RegisterBundledFonts`, and `AddCharacters` in the theme class). KernSmith rasterizes **static TTFs** and does not select instances from a **variable font** (a single `Foo[wght].ttf` carrying every weight) — many modern Google Fonts ship VF-only. If the design's typeface is VF-only, fetch its static weight cuts or approximate with a static sibling family (e.g. the `Condensed` variant), mapping one cut per Gum style slot (`null` → Normal, `"Bold"`, …). Get static cuts of a VF-only Google font from **google-webfonts-helper** (`gwfh.mranftl.com/api/fonts/<id>?…&formats=ttf`); a TTF is static if it has no `fvar` table.
5. **Restyle visuals**, promoting each from the stock-V3 block in `RegisterVisuals` to a styled subclass as you build it.
6. **Verify by running, not just building.** Build both backends and add the theme to `MonoGameGumThemesShowcase` to smoke-test — `Apply` and font loading only fail at runtime, not at compile.

Two conventions the template encodes, worth keeping in any theme:

- **One styling object, read everywhere.** Every visual reads its colors from `YourThemeStyling.ActiveStyle.Colors` (and fonts from `.Text`); never inline `new Color(...)` in a visual, so a restyle touches one file. `YourThemeStyling.ActiveStyle` is get-only from outside the assembly — consumers restyle by mutating `Colors`/`Text` *before* calling `YourTheme.Apply()`, the same "mutate before construct" ordering V3's own `Styling.ActiveStyle` already uses. It is the theme's analog of V3's `Colors`, but each theme owns its full palette (no shared base type) so the theme stays a self-contained, copyable reference.
- **Shape factories for the common case.** `TemplateShapes` builds the centered/full-parent fill, border, and focus-ring shapes (rect and circle) that every visual reuses, collapsing the repeated ~15-line `RectangleRuntime` setup. `FillWithDropshadow` / `FilledCircleWithDropshadow` add a shadow variant; bespoke geometry (fixed-size sub-boxes, glyph `TextRuntime`s, edge strips, percentage-width bars) is still built inline. The shipped themes follow this convention (Bubblegum included).
- **Two font families, when the design needs them.** `Styling.Text` carries one family — the display default that flows to every control. For a display + body split, register a second family and set `TextInstance.Font = YourTheme.BodyFontFamily` on the typed/list/menu/tooltip visuals (the template demonstrates this). For an italic-less family, point the Italic/BoldItalic style slots at the upright cuts so a stray italic request still resolves to a real font.

**Match the design's visual language.** The template's *default* visuals are flat/rectangular, but it also ships an opt-in `Variants/` gallery (`Gum.Themes.Template.Variants`) demonstrating the common rich treatments on a representative control subset — pill + hard-offset shadow, rounded box, dashed-outline panel, soft focus-ring glow, circular drop-shadow thumb — each wired via a commented swap-in line in `RegisterVisuals`. Copy the per-control treatment matching your design from there; for looks beyond the gallery, crib from the closest shipped theme: drop shadows/glows → Bubblegum (+ the "Drop shadows" section below); gradients → ForestGlade; bevels → Retro95; pills → a larger `CornerRadius`; NineSlice instead of Apos.Shapes → Editor.

## Theme entry point

`MyTheme.Apply(GraphicsDevice)` does in order:

1. Wire KernSmith if the theme uses dynamic fonts: `CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(gd);`
2. `BmfcSave.AddCharacters("…")` for any non-ASCII glyphs visuals will render. Must come before any text rendering — KernSmith only bakes characters it knows about at atlas-creation time.
3. Register embedded TTFs via `KernSmithFontCreator.RegisterFont(family, bytes, style)`. See "Icon fonts" below.
4. If your visuals use `RectangleRuntime` / `CircleRuntime` features backed by Apos.Shapes (rounded corners, circles, drop shadows — i.e. the `MonoGameGumShapes` package), `if (!ShapeRenderer.Self.IsInitialized) ShapeRenderer.Self.Initialize();` — consumers shouldn't have to know your theme reaches for shapes internally.
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
2. **Add** the replacement children — NineSlice with new textures, `RectangleRuntime` / `CircleRuntime` shapes, whatever fits. (Mind the split fill/stroke surface: a fresh `RectangleRuntime`/`CircleRuntime` defaults to a *transparent fill + 1px white outline*, so a solid fill needs `FillColor = c; StrokeWidth = 0;` and an outline/ring needs `IsFilled = false; StrokeColor = c;`. `CornerRadius` rounds a Rectangle; both are Apos-backed when `MonoGameGumShapes` is referenced.)
3. **Reattach** anything you detached for ordering reasons (TextInstance, ClipContainer) last so it renders on top of the new background layer.
4. **Re-wire state callbacks with `=` (not `+=`)** so the base's color-pumping into the now-detached children is fully replaced. Each `States.{Enabled, Highlighted, Pushed, Focused, …}.Apply` becomes a fresh lambda that mutates your replacement children.

Different controls have different state sets — Button has 7, TextBox has 4 (no Pushed), CheckBox has 21 (7 visual × 3 value), Slider has 7.

## ClipContainer reordering

`TextBoxBaseVisual` adds children in order `[Background, ClipContainer, FocusedIndicator]`. If you only detach Background and FocusedIndicator and then `AddChild` your replacements, they end up *in front of* ClipContainer — the text renders behind. Fix: also detach ClipContainer, add your replacements, reattach ClipContainer last.

## Dashed strokes are built into Apos.Shapes

Need a dotted / dashed outline (Win95 focus rectangle, marching-ants selection, etc.)? Don't spawn many small `RectangleRuntime` "dot" instances along the edges. `RectangleRuntime` / `CircleRuntime` (and the other Apos shape runtimes) expose `StrokeDashLength` and `StrokeGapLength`. Set `IsFilled = false`, `StrokeWidth = 1`, both dash properties to your desired pattern, and the shape renders as a properly-rasterized dashed stroke — one node in the render tree, sized via the usual `RelativeToParent` units, with no per-frame dot bookkeeping and no sensitivity to layout timing.

The dotted-rectangle materialization approach (place N tiny rectangles at fixed offsets) looks superficially simpler, but ends up needing `GetAbsoluteWidth()` to compute N — and that returns stale values when the consumer sets `control.Width = X; control.IsFocused = true;` back-to-back: the state callback fires before the next layout pass, so the dot count is computed off the construction-time size. The visible artifact is a half-drawn focus rect that "self-heals" once a hover fires another state change after layout has run.

## ContainerRuntime sub-wrappers eat clicks

`ContainerRuntime`'s constructor sets `HasEvents = true`. If you wrap a control's chrome in a sub-container to constrain a fill primitive to a sub-region of the parent (the 13×13 checkbox box inside a 200×16 CheckBox visual, a dropdown-button-sized area inside a ComboBox, the slider-track inside the SliderVisual root), that wrapper will capture clicks that should bubble up to the InteractiveGue root — the control will look right and refuse to register clicks on the wrapped area.

**Why:** `Bubblegum` and `DarkPro` never hit this because their primitives are single `RectangleRuntime`/`CircleRuntime` shapes positioned and sized directly on the visual root (no wrapper needed). A theme that builds its chrome from multiple `RectangleRuntime` strips (Retro 95's bevels, dotted focus rings, etc.) typically needs a sized wrapper, and that's where the gotcha bites.

**How to apply:** Any time you write `new ContainerRuntime()` inside a visual subclass, immediately set `HasEvents = false` unless you specifically want that container to absorb clicks (rare — usually only the Forms-control root and explicit drag-handle InteractiveGues should). Same goes for any other `InteractiveGue`-derived wrapper you introduce.

The thumb visuals (SliderThumbVisual, ScrollBarThumbVisual) are the explicit exception — they *do* want `HasEvents = true` so RangeBase's drag pickup works.

## Hover/press color consistency

A control with a Pushed state (Button) shouldn't switch border color families between hover and press — gray→blue→gray flickers visibly through a hover-press-release motion. Use the accent for hover too. A control without Pushed (TextBox, ComboBox closed) is fine using a softer hover color and reserving the accent for sustained focus.

## Slider thumb is a Button

`V3.SliderVisual` creates the thumb as `new Button()`, so its visual is whatever `DefaultFormsTemplates[typeof(Button)]` resolves to — your theme's Button, which is the wrong shape for a thumb in most designs. Fix: pass `tryCreateFormsObject:false` to the base, detach `ThumbInstance.Parent`, add your own thumb visual named `"ThumbInstance"` to `TrackInstance`, then create the Forms `Slider` yourself at the end of the ctor. `RangeBase.ReactToVisualChanged` looks up `"ThumbInstance"` by name and wraps it in a Button — your thumb just needs `"ButtonCategory"` state callbacks (use `Button.ButtonCategoryName`) and `HasEvents = true`.

## Value-driven visuals

Visuals that need to react to a continuous Forms-control value (a slider fill bar tracking `Slider.Value`, a progress indicator, etc.):

- `RangeBase.Value` raises `ValueChanged` but does NOT call `OnPropertyChanged`, so `INotifyPropertyChanged` is not a viable hook.
- The Forms control is assigned to the visual via `FormsControlAsObject`. **Override the setter** (`InteractiveGue.FormsControlAsObject` is `virtual`) to subscribe on assignment and unsubscribe on reassignment. This is the only hook that works for both construction paths: `tryCreateFormsObject:true` (visual creates the Forms control in its ctor) and `tryCreateFormsObject:false` (FrameworkElement creates the visual via the template and then assigns the externally-created Forms control).
- `Minimum`/`Maximum` setters fire no external event. Consumers conventionally set them before `Value`, so an initial update at assignment plus a `ValueChanged` subscription covers practical cases.

## Icon fonts — bundle DejaVu Sans Mono

Most user-facing fonts (DM Mono, JetBrains Mono, Nunito, etc.) lack the Dingbats (`✓ ✕`) and Geometric Shapes (`▾ ▴ ▲ ▼ ◀ ▶`) blocks. Setting any of those characters with such a font silently fails — and **without** `BmfcSave.AddCharacters` even being called the glyph never reaches the atlas generator in the first place.

**Convention for Gum themes**: bundle DejaVu Sans Mono (Bitstream Vera / DejaVu license, redistribution permitted) as your icon font alongside whatever user-facing typeface you ship. Register it under a distinct family name like `"<Theme> Icons"` (e.g. `"DM Mono Icons"`, `"Nunito Icons"`) so visual code addresses it explicitly via `MyTheme.IconFontFamily` and stays decoupled from the specific TTF.

Wiring (all four steps required):

1. Add `DejaVuSansMono.ttf` and `DejaVuSansMono-LICENSE.txt` to `Content/Fonts/` in the MonoGame variant project. Embed via `<EmbeddedResource>` (and re-`<Link>` into the KNI variant via the cross-runtime packaging pattern).
2. Pack the license file at the NuGet root with `<None Include="Content/Fonts/DejaVuSansMono-LICENSE.txt" Pack="true" PackagePath="\" />`.
3. In `MyTheme.Apply`: `BmfcSave.AddCharacters("…")` listing every non-ASCII glyph the theme will render — **before** the first font generation. KernSmith bakes only declared characters. Then `RegisterEmbeddedFont(IconFontFamily, "DejaVuSansMono.ttf", style: null);`.
4. In visuals: `runtime.Font = MyTheme.IconFontFamily; runtime.Text = "✓";`.

Size the glyph's `TextRuntime` `Absolute`, **larger** than the box it sits in (~1.5x), centered. DejaVu Sans Mono and most icon-coverage fonts aren't truly monospaced for non-Latin — symbol glyphs have wider advance widths than ASCII, and a runtime sized exactly to the box clips or drops them.

Building glyphs from Apos.Shapes primitives (`LineRuntime` strokes, rotated `RectangleRuntime`s) is technically possible but rarely worth it once you have more than one or two glyphs. Stick with DejaVu unless you have a strong reason — the icon font's ~600 KB is the cheapest entry point in the theme.

## Drop shadows: use the native Apos.Shapes API, not stacked rects

Apos.Shapes-backed runtimes (`RectangleRuntime`, `CircleRuntime`) expose a native Gaussian drop shadow. Use it. Do not stack progressively-larger, fainter shapes underneath the body to fake a falloff — that approach is a holdover from a misreading of the Apos.Shapes capabilities and produces visible concentric banding because each layer is a hard-edged shape.

Properties (all forwarded from the runtime to the underlying renderable):

- `HasDropshadow` (`bool`) — master switch. Toggle per state.
- `DropshadowColor` (`Color`) — RGBA. Match the CSS source alpha directly (`.4 alpha = 102 / 255`).
- `DropshadowOffsetX`, `DropshadowOffsetY` (`float`) — pixel offset of the shadow from the body.
- `DropshadowBlur` (`float`) — blur radius. Match the CSS `box-shadow` blur radius value directly. `0` = sharp.

Set the shadow once at construction (on the same `RectangleRuntime` that paints the body fill), then flip `HasDropshadow` in state callbacks for press/disabled — exactly the way state code already toggles `Visible`. No separate child shape, no z-order to manage.

CSS-to-Apos translation cheatsheet — `box-shadow: <offsetX> <offsetY> <blur> rgba(<r>,<g>,<b>,<a>)` maps to:

```
fill.HasDropshadow = true;
fill.DropshadowOffsetX = <offsetX>;
fill.DropshadowOffsetY = <offsetY>;
fill.DropshadowBlur    = <blur>;
fill.DropshadowColor   = new Color(r, g, b, (int)(a * 255));
```

The CSS `spread` argument (the optional fourth length value) has no direct equivalent; usually 0 or omitted in modern designs.

**Visual fidelity vs numerical fidelity.** A 1:1 number translation will not look the same as the CSS source. Two pipeline differences stack:

- **Blur kernel semantics.** CSS treats `blur-radius` roughly as Gaussian standard deviation; Apos.Shapes interprets `DropshadowBlur` differently. Same value → different falloff width → different perceived softness.
- **Color space.** Browsers composite alpha in linear RGB (perceptually correct). MonoGame / Apos.Shapes composite in sRGB. Identical alpha math reads markedly darker in a browser; the in-game render comes out fainter.

Treat CSS values as a *starting point* — typically you'll bump alpha by ~1.5–2× and tweak blur by eye until the perceived weight matches the source. The Bubblegum Button shadow ended at alpha 160 / blur 12, up from the spec's 102 / 10.

**Soft vs hard shadows.** The alpha-bump advice is for *soft* Gaussian shadows. A *hard* offset edge — the flat "stacked card" look (CSS `0 4px 0 <opaque-color>`, e.g. a chunky cozy button) — is the opposite: an **opaque** color with `DropshadowBlur = 0`. It needs **no** alpha bump (nothing is composited), and toggles off for the pressed state just like a soft one. Same API, blur 0.

### Offset focus rings render AFTER the glowing body, not before

If you have both: (a) a body with `HasDropshadow = true` and (b) a separate focus ring sized larger than the body (sitting *outside* the body's pixel bounds), the focus ring MUST be added to the parent **after** the fill, not before.

The halo isn't a separate render pass — it's part of the fill's draw call and extends past the body's own pixel bounds. Anything drawn earlier in that overlapping outer region gets alpha-blended over by the halo and dims. On bright bodies (Neon's "On" toggle, a saturated accent fill, etc.) the halo is opaque enough to render a 1 px white ring nearly invisible.

```csharp
// WRONG — ring is dimmed by the halo of the fill below it
AddChild(_focusRing);   // outer ring
AddChild(_fill);        // body with HasDropshadow=true → halo paints over ring
AddChild(_border);

// RIGHT — ring paints on top of halo, stays crisp
AddChild(_fill);
AddChild(_border);
AddChild(_focusRing);   // outer ring renders last
```

This is the *opposite* of the rule for concentric focus rings inside a clip-container shape (see "Rounded outline + rectangular clip container" below — the focus ring there sits *behind* the body so the body fills over it). The distinguishing factor is whether the ring lives entirely outside the body's pixel bounds:

- **Concentric / inside the parent bounds** → render first, body draws on top.
- **Offset outside the body** → render last, on top of the halo.

The ring living outside the body means painting it on top doesn't obscure any inner content — its pixels are all in the empty region around the control.

## Rounded outline + rectangular clip container: paint the border last

Gum's clip containers are axis-aligned rectangles. They do not clip to rounded paths. If a themed container has a rounded outline (`RectangleRuntime` border with `CornerRadius`) AND a child clip container that renders content (text, list items, hovered rows with their own pink fills), naively painting the border *behind* the clip container makes content visibly poke past the rounded outline at the corners.

Fix: reattach so the **border renders last** (on top of the clip container). The stroke masks the corner-region content with the theme's accent color, and the result reads as rounded clipping even though no actual rounded clipping is happening.

Order for any TextBox-/ListBox-/ScrollViewer-shaped visual:

```
1. focus ring     (added first → renders behind)
2. fill           (rounded rect, behind content)
3. clip container (text, items, hovered row backgrounds)
4. border         (rounded stroke, on top of everything)
```

Caveat: any sub-control that lives inside the clip container and needs to be visually unobscured by the border — most commonly a vertical scroll bar — must be inset from the parent's right edge by at least the border's stroke width. The Bubblegum and Dark Pro ListBox/ScrollViewer subclasses set `VerticalScrollBarInstance.X = -2f` for exactly this reason.

When per-corner radii arrive in Apos.Shapes ([apos-shapes#32](https://github.com/Apostolique/Apos.Shapes/pull/32)), this trick becomes redundant for the title-bar-style "round top corners only" case — but the border-on-top approach remains the right pattern any time rectangular clipping meets a rounded outline.

## Items inside rounded containers should be square-cornered

`ListBoxItem`, `MenuItem`, `ComboBox` dropdown rows, etc. tile flush inside a rounded container. Give them `CornerRadius = 0f` — not the container's radius. Rounded items inside a rounded container produce visible donut gaps at the corners where item edges don't reach the shell's rounded perimeter. The container's border (painted last per the section above) handles the visible rounded outline; the items themselves are just rectangular bands of color.

The corollary: when a row hover/selection fill should extend to the visible edge, let it paint to its rectangular bounds and trust the container's border-on-top to mask the corner overhang.

## Sharing shape stacks via a helper class

When two visuals need identical decoration but inherit from different V3 bases — most commonly `TextBoxVisual` and `PasswordBoxVisual`, both inheriting from `TextBoxBaseVisual` — extract the shape stack into a helper class instead of duplicating it in both subclasses.

Pattern (see Bubblegum's `BubblegumTextInputDecoration` for the worked example):

```csharp
internal sealed class MyTextInputDecoration
{
    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public MyTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.ClipContainer.Parent = null;
        // ... add focus ring, fill, clip container, border ...
        WireStates(host);
    }
    private void WireStates(TextBoxBaseVisual host) { /* host.States.Enabled.Apply = ... */ }
}

public class TextBoxVisual : V3.TextBoxVisual
{
    private readonly MyTextInputDecoration _decoration;
    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new MyTextInputDecoration(this);
    }
}
// PasswordBoxVisual is identical, swap the base class.
```

Holding the helper reference in a `private readonly` field (rather than constructing-and-discarding) keeps the shape rects alive for state callbacks — they're owned by the host visual via `AddChild`, but the helper still holds the references it needs to mutate from `WireStates` lambdas.

## csproj gotcha: PrivateAssets on KernSmith

If your theme generates fonts at runtime (any theme that wires KernSmith — directly or via the `ThemePlatform` shim below), KernSmith is a runtime dependency — it must flow transitively to consumers. **Do not** mark it `<PrivateAssets>All</PrivateAssets>` on the KernSmith reference. MonoGame.Framework / nkast / Raylib-cs can stay private (consumers always bring their own).

The original Editor theme shipped `PrivateAssets=All` on KernSmith — a latent bug (consumers would have to install KernSmith manually); it was corrected when the `.Raylib` variant landed. Mirror the corrected `Gum.Themes.Editor.*` csprojs for new themes.

## Visual-side inheritance doesn't match Forms-side inheritance

The Forms control hierarchy is `ScrollViewer ← ItemsControl ← ListBox` (plus `Menu` / `MenuItem : ItemsControl`). The V3 default visual hierarchy is **not** parallel:

- `ScrollViewerVisual : InteractiveGue` ✓
- `ItemsControlVisual : ScrollViewerVisual` ✓ (thin wrapper; just swaps the Forms control type)
- `ListBoxVisual : InteractiveGue` ✗ — **parallel reimplementation, not a subclass of `ItemsControlVisual`**

Field names differ between the two paths too (`ListBoxVisual.ClipAndScrollContainer` vs `ScrollViewerVisual.ScrollAndClipContainer`, `ClipContainerParent` vs `ClipContainerContainer`, etc.) so even a textual diff doesn't reveal them as the same concept.

Practical consequences for a theme:

- **ScrollBar styling cascades for free** — `new ScrollBar()` from V3.ScrollViewerVisual and V3.ListBoxVisual both resolve through `DefaultFormsTemplates[typeof(ScrollBar)]`. One ScrollBar template covers everything.
- **ScrollViewer shell cascades to ItemsControl / Menu / MenuItem for free** — they inherit `ScrollViewerVisual`, so a Dark-Pro-style `ScrollViewerVisual` subclass automatically themes those too.
- **ListBox shell does NOT cascade.** Even after subclassing `ScrollViewerVisual`, you still need a separate subclass of `ListBoxVisual` to apply the same shell (Background, FocusedIndicator, scrollbar inset, state callbacks). Expect to copy/paste the shell pattern between the two.

Fixing this on the V3 side (making `ListBoxVisual : ItemsControlVisual`) is a dedicated refactor — every existing theme and every consumer reading `ListBoxVisual` field names directly would need updates. Until that happens, treat the duplication as inherent and don't try to be clever about it in a theme.

## Cross-runtime NuGet packaging

A theme that wants to support both MonoGame and KNI ships two NuGet packages, not one. NuGet's restore graph picks `lib/<tfm>/<dll>` by TFM — and since both `Gum.MonoGame` and `Gum.KNI` currently target `net8.0`, there's no TFM-based discriminator that lets one package serve both backends. (FRB2's single-package multi-target trick at `src/FlatRedBall2.csproj` works only because it forces KNI=net8.0 and MonoGame=net10.0 as a backend-discrimination convention. Don't replicate that here unless you're willing to force consumers onto a specific .NET version.)

Pattern (see `Themes/Gum.Themes.DarkPro.MonoGame/` and `Themes/Gum.Themes.DarkPro.Kni/` for the working example):

- One "primary" csproj holds the `.cs` files, the embedded fonts, and the README. Name it `Gum.Themes.<Name>.MonoGame.csproj`. References `KernSmith.MonoGameGum`, `MonoGame.Framework.DesktopGL`, `MonoGameGum.csproj`, `MonoGameGumShapes.csproj`.
- A second csproj (`Gum.Themes.<Name>.Kni.csproj`) source-shares the same `.cs` files via `<Compile Include="..\Gum.Themes.<Name>.MonoGame\**\*.cs" />` and re-embeds each TTF via `<EmbeddedResource Include="..\…\<file>.ttf"><Link>Content\Fonts\<file>.ttf</Link></EmbeddedResource>`. The `<Link>` is load-bearing — it makes the resource manifest in the KNI assembly resolve to `Gum.Themes.<Name>.Kni.Content.Fonts.<file>` instead of some `..\` path. References `KernSmith.KniGum`, `nkast.Xna.Framework{,Graphics,Input}`, `KniGum.csproj`, `KniGumShapes.csproj`.
- Inside `RegisterEmbeddedFont` (or wherever you read TTFs from manifest), derive the prefix from `assembly.GetName().Name` — **not** a hard-coded string. The same source compiles in both assemblies and finds its fonts in both.

Match versions across the two csprojs so they read as one logical release.

## raylib theme variants

A theme adds raylib support with a third source-sharing project, `Gum.Themes.<Name>.Raylib.csproj`, alongside the MonoGame/KNI pair. raylib is **not** XNA-shaped, so the naive KNI-style source-share (which links *all* `.cs`, `Apply` included) doesn't work directly. A few things diverge; isolate them so the theme bodies stay platform-agnostic and source-shared across all three projects. Worked examples: `Themes/Gum.Themes.Editor.*` (NineSlice, system font — the minimal case) and `Themes/Gum.Themes.DarkPro.*` (Apos.Shapes + embedded fonts — the full case).

- **`Color` namespace.** V3 visuals get `Color` from a global `using Raylib_cs;` baked into `RaylibGum.csproj`, but a theme assembly doesn't inherit that — and global usings are avoided here so theme authors copying a theme aren't surprised. So guard the import per file: `#if RAYLIB using Raylib_cs; #else using Microsoft.Xna.Framework; #endif`. Replace any fully-qualified `new Microsoft.Xna.Framework.Color(...)` with `new Color(...)` so the guarded import resolves it. Watch for divergent statics: `Color.Transparent` is XNA-only (raylib's is `Color.Blank`) — normalize to `new Color(0, 0, 0, 0)`, which is identical RGBA on both. `new Color(r,g,b)` / `new Color(r,g,b,a)` exist on both types.
- **`Apply` signature + font creator** live in a shared shim, `Themes/Shared/ThemePlatform.cs`, `<Compile>`-linked into every theme project (it's a home for `#if RAYLIB`). `WireInMemoryFontCreator()` assigns `CustomSetPropertyOnRenderable.InMemoryFontCreator` to `new KernSmithRaylibFontCreator()` on raylib, or `new KernSmithFontCreator(gd)` on XNA-likes — where `gd` is read from `SystemManagers.Default.Renderer.GraphicsDevice` (no longer a parameter). So `Apply()` is **parameterless on every backend** and the theme body is `#if`-free. Keep a `#if !RAYLIB public static void Apply(GraphicsDevice gd) => Apply();` back-compat overload so existing MonoGame/KNI callers (and the `Action<GraphicsDevice>`-keyed showcase) still compile.
- **Embedded fonts.** `ThemePlatform.RegisterFont(family, bytes, style)` wraps the backend's `KernSmith*FontCreator.RegisterFont` (same static surface on both), so a theme's `RegisterEmbeddedFont` reads the TTF from its manifest and registers it without naming a per-backend type. The `.Raylib.csproj` re-embeds each TTF via `<EmbeddedResource><Link>Content\Fonts\<file></Link>` exactly like the `.Kni.csproj` (the resource prefix derives from `assembly.GetName().Name`, so the same code finds the fonts in all three assemblies).
- **Shapes init** (`ShapeRenderer.Self.Initialize()`) is XNA-like-only; raylib renders `RectangleRuntime`/`CircleRuntime` natively. This lives in a **separate** shim, `Themes/Shared/ThemeShapePlatform.cs` (`InitializeShapeRenderer()` — Apos init on XNA-likes, no-op on raylib), linked **only** into shape themes. It is intentionally NOT in `ThemePlatform`: that shim is linked into every theme including NineSlice-only ones (Editor), and referencing the Apos.Shapes `ShapeRenderer` there would force them to take the Apos.Shapes package dependency. The shape *runtime types* (`new RectangleRuntime { CornerRadius=…, IsFilled=…, StrokeColor=… }`) are already portable — only the renderer init diverges. The `.Raylib.csproj` therefore does **not** reference `MonoGameGumShapes`/`KniGumShapes`.

The `.Raylib.csproj` defines `RAYLIB`, references `RaylibGum.csproj` + `KernSmith.RaylibGum` + `Raylib-cs` (no Apos.Shapes package), and source-shares via `<Compile Include="..\Gum.Themes.<Name>.MonoGame\**\*.cs" />` plus the shim link(s). Runtime-verify with `Samples/RaylibGumThemesShowcase` (number keys swap themes) — `Apply`, shapes, and font loading only fail at runtime, not compile.

### Two divergence classes a rich theme hits on the first raylib build

The shim pattern above makes `Apply` and font loading portable, but porting a *visually rich* theme (gradients, per-corner radii, scaled/translucent colors) surfaces two more divergences the simple themes (Editor, DarkPro) never hit. Both are compile errors on the `.Raylib.csproj`, found by building it once:

- **Shape-runtime API gaps.** RaylibGum's `RectangleRuntime`/`CircleRuntime` source-share the *same* `MonoGameGum/GueDeriving/*Runtime.cs` as MonoGame, but the richer members live in the `#if XNALIKE` block and are only re-surfaced for raylib in a deliberate `#if RAYLIB` parity block (issue #2757). If a theme uses a member that block doesn't list yet — seen so far: `CustomRadiusTopLeft/TopRight/BottomLeft/BottomRight`, the gradient-endpoint/radius unit props (`GradientX1Units`/`GradientY1Units`/`GradientX2Units`/`GradientY2Units`/`GradientInnerRadiusUnits`/`GradientOuterRadiusUnits`), `IsAntialiased` — add it to that RAYLIB block as a backing-field round-trip property (forward compat; the raylib renderable doesn't consume it yet, same as the SOKOL stubs) and pin the round-trip in `Tests/RaylibGum.Tests`. `GeneralUnitType` lives in `Gum.Converters` (add the `using` to the RAYLIB block; `DimensionUnitType` from `Gum.DataTypes` is already imported). The base gradient floats (`GradientX1` etc.) *are* pushed to the renderable, so gradients still render on raylib — only the unit qualifiers are stubs.
- **`Color` operator/ctor gaps.** `Raylib_cs.Color` has neither the `==`/`!=` operators, the `Color * float` operator, nor the `new Color(Color, alpha)` two-arg ctor that `Microsoft.Xna.Framework.Color` provides. Theme source (shared across both backends) must avoid all three: compare channel-wise (`a.R==b.R && a.G==b.G && …`); scale channel-wise (`new Color((int)(c.R*f), (int)(c.G*f), (int)(c.B*f), (int)(c.A*f))`); re-alpha channel-wise (`new Color((int)c.R, (int)c.G, (int)c.B, alpha)`). **Cast all four args to `int`** — `.R`/`.G`/`.B` are `byte`, and mixing a `byte` channel with an `int` alpha makes raylib's `Color(byte,…)` vs `Color(int,…)` ctors ambiguous (CS0121). `new Color(0,0,0,0)` / `new Color(r,g,b)` with plain int literals are unambiguous and fine.

## InteractiveGue children capture input — reattach in V3 order

When detaching a chunk of V3's children to insert custom chrome, anything containing an `InteractiveGue` (InnerPanel, ContainerInstance, ListBox.ScrollAndClipContainer, etc.) must go back at its original z-order or it eats clicks meant for sibling chrome. The Dark Pro Window's title-bar-drag bug was an hour of head-scratching because `InnerPanel` was re-added after `TitleBar`, so the panel's invisible InteractiveGue covered the visible drag bar.

Rule of thumb: when you `Parent = null` a block of V3 children to insert layers behind them, reattach them in the same order V3 added them originally. Read the base visual's constructor to see that order; don't rely on memory.

## Bake ScrollBar insets into the bar, not consumers

A scroll bar's thumb shouldn't visually touch its container's border at scroll extremes. Resist the urge to push the inset onto consumers (`scrollBar.X = -3; scrollBar.Height -= 6;`) — that's brittle and forces every parent visual to know the magic numbers.

Instead, shrink things inside the bar's own visual:

- Shrink `ThumbContainer` on the long axis with negative `RelativeToParent` units (e.g. `ThumbContainer.Height = -ThumbInset * 2f; ThumbContainer.HeightUnits = RelativeToParent;` in vertical mode). RangeBase still gets to size the thumb freely within the shrunken container.
- Inset the thumb on the short axis with negative `RelativeToParent` units on the thumb itself.

Now consumers (ListBox, ScrollViewer, free-floating) can place the bar flush against any edge and the visible thumb still has consistent breathing room. The bar's track is transparent, so the flush bounding box is invisible.

## Optional chrome — toggle visibility, don't restructure the tree

When adding an opt-in chrome property (`ShowFrame`, `ShowShadow`, `ShowDivider`), create the chrome at construction with `Visible = false` and have the setter flip visibility. **Don't** `AddChild` / `Parent = null` at toggle time — re-parenting interacts badly with state callbacks (which target specific child references), z-order assumptions, and the rendering tree's batch ordering. Visibility toggling is O(1) and free; restructuring the tree at runtime is the kind of thing that produces "first click works, second click doesn't" reports.

If the chrome's geometry depends on its visibility (e.g. ScrollBar's `ShowFrame` shifts the thumb inset by `FrameBorderThickness` so the visible gap stays symmetric), extract the geometry-applying code into its own method and call it both from the orientation/state callback **and** from the setter. Re-running the full orientation callback from the setter risks clobbering consumer-set dimensions (the Dark Pro `ScrollBarVisual.ShowFrame` regression was exactly this — `Apply` re-set `Width = 14; Height = 128` on every invocation, overwriting `bar.Width = 16; bar.Height = 130` set in the showcase).

## Cross-references

- Apos.Shapes runtime types and the shape-batch scissor plumbing: [gum-monogame-rendering](../gum-monogame-rendering/SKILL.md).
- V3 visual base classes and the state-category contract: [gum-forms-default-visuals](../gum-forms-default-visuals/SKILL.md).
- KernSmith and font generation: [gum-runtime-fonts](../gum-runtime-fonts/SKILL.md).
