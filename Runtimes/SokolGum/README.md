# SokolGum

**Experimental** Gum backend targeting [Sokol.NET](https://github.com/lithiumtoast/Sokol.NET)
(sokol_gfx / sokol_gp / sokol_app / fontstash via `sokol_fontstash.h`).

Sibling to `Runtimes/RaylibGum/` and `Runtimes/SkiaGum/`. Renders UI through
`sokol_gp` (2D primitives + scissor + blend) with text emitted through
fontstash callbacks into the same `sgp` command stream, so glyphs batch
alongside sprites and rectangles in scene-graph order.

The experimental label is real — this backend is newer than RaylibGum
and hasn't been battle-tested in a shipped game. Core rendering works;
Forms / input / data-binding are out of scope (see below).

---

## Scope

**In scope**: all 7 Gum renderables (Sprite, NineSlice, SolidRectangle,
Text, LineRectangle, LineCircle, LinePolygon), plus `Line` and `LineGrid`
for parity. Per-renderable blend modes. Centralized rotation. Scissor
clipping with nesting. Camera pan/zoom. Text wrapping + alignment +
outline + line-height + pixel snap + typewriter reveal. `.achx` animation
chains for Sprite and NineSlice. Code-only UI construction and `.gumx`
project loading.

**Out of scope** (intentional):

- **Input** — mouse / keyboard / touch. SystemManagers intentionally
  omits `Cursor` / `Keyboard` properties. A follow-up could add a
  backend-local `Input/` folder following RaylibGum's pattern.
- **Forms controls** — Button, Slider, ListBox, ScrollViewer, etc.
  RaylibGum's csproj `<Compile Include>` s ~50 files from
  `MonoGameGum/Forms/`; we don't.
- **Data binding** — `Gum.Forms.Data` classes live under
  `MonoGameGum/Forms/Data/`. Binding depends on `Gum.Wireframe` core,
  not Forms controls, so it could be added without pulling in the full
  Forms stack.
- **Atlased textures** — scaffolding exists in `RenderingLibrary` but no
  backend in this repo has a working atlas loader, us included.

---

## File layout

```
Runtimes/SokolGum/
├── Animation/          # AnimationChainList/Chain/Frame — backend-local
│                       # runtime classes wrapping our Texture2D
├── GueDeriving/        # ColoredRectangleRuntime, ContainerRuntime,
│                       # NineSliceRuntime, SpriteRuntime, TextRuntime,
│                       # InteractiveColoredRectangleRuntime
├── Renderables/        # Sprite, NineSlice, SolidRectangle, Text,
│                       # Line, LineRectangle, LineCircle, LinePolygon,
│                       # LineGrid
├── Color.cs            # RGBA8 struct (root-namespaced so SystemManagers
│                       # etc. can use it without a Renderables import)
├── ContentLoader.cs    # Texture2D (stb_image), Font (TTF → fontstash),
│                       # AnimationChainList (.achx XML)
├── CustomSetPropertyOnRenderable.cs  # .gumx property routing + enum /
│                       # nullable reflection fallback the core lacks
├── Font.cs             # Non-disposable handle to a fontstash font id.
│                       # Lifetime tied to FontAtlas, not individual.
├── FontAtlas.cs        # fontstash context whose render callbacks emit
│                       # into sokol_gp. Owns TTF byte buffers.
├── RenderableCreator.cs  # Maps .gumx base-type names to renderables
├── Renderer.cs         # IRenderer — BeginFrame / Draw / EndFrame /
│                       # Update (animation tick) with scissor stack +
│                       # centralized rotation + blend-mode switching
├── SystemManagers.cs   # Initialize pipeline — samplers, font atlas,
│                       # delegate registration
├── Sokol.Native.targets  # Shared lib-copy MSBuild logic for samples
└── Texture2D.cs        # sg_image + sg_view + size handle
```

---

## Design notes / quirks

Things a future maintainer will hit and wonder about:

### Text renders through fontstash, not a sidecar pipeline

Fontstash is wired so its `renderDraw` callback emits `sgp_draw(...)`
calls into the same stream as every other renderable. That means text
honours `Z`, scissor, blend mode, and the sgp transform stack exactly
like sprites do — no "text always on top" issue that afflicts backends
that route text through a separate pipeline.

The atlas is single-channel from fontstash's side; we expand to RGBA8
on upload (`R=G=B=255`, `A=glyph_alpha`) because sokol_gp's default
textured pipeline wants RGBA.

### FontAtlas owns TTF byte buffers, not Font

fontstash's `fonsAddFontMem(..., freeData=0)` retains the raw pointer
for the whole context lifetime. We allocate unmanaged memory, hand
fontstash the pointer, and stash the pointer on **the atlas** for
eventual `Marshal.FreeHGlobal` after `fonsDeleteInternal`.

`Font.Dispose` does **not** exist — Font is not `IDisposable`. Releasing
a Font individually would leave fontstash reading freed memory on the
next glyph rasterization.

### Atlas update is once per frame, accumulated

`sokol_gfx` enforces one `sg_update_image` per image per frame.
fontstash can call `renderUpdate` several times within a single
`fonsDrawText` when rasterizing new glyphs at different sizes/fonts, so
we accumulate the union dirty region in a staging `byte[]` and upload
it all at once in `Renderer.EndFrame` via `FontAtlas.FlushPendingUpload`.
Trade-off: a glyph hits screen one frame after it's first rasterized.

### 2× glyph atlas oversampling

Vanilla fontstash produces blurry text at typical UI sizes because
stb_truetype has no TrueType hinting. `FontAtlas.Oversample` (default
`2.0f`) makes fontstash rasterize at `FontSize × scale` into a denser
atlas, while `FontAtlas.RenderDraw` divides emitted vertex positions by
the same scale so on-screen size stays at the requested `FontSize`.

Under high-DPI, each physical pixel then samples a 2×2 region of the
oversampled atlas via the linear sampler — proper bilinear downsampling
that's both sharp and smooth. Set to 1.0 to disable; 3.0 or 4.0 for
even denser text at the cost of atlas memory.

`Text.cs` must thread the scale factor through every fontstash call:
`fonsSetSize(FontSize * scale)`, `fonsDrawText((x,y) * scale, ...)`,
divide `fonsVertMetrics` / `fonsTextBounds` results by `scale`. All
in-place in the renderable; no external contract change.

### Scissor stack is Renderer-local

`sokol_gp` has `sgp_scissor` / `sgp_reset_scissor` but no push/pop.
Nested `ClipsChildren` elements need us to intersect the inner rect
with the outer rect on push, and re-apply the outer on pop. `Renderer`
keeps a `List<ScissorRect>` for this. `sgp_reset_scissor` is called
only when the stack empties.

### Scissor rects are in framebuffer pixels, not logical pixels

`sgp_scissor` operates in framebuffer-pixel space — it isn't affected
by `sgp_project` or the current transform. Our `ClipsChildren` bounds
come from `GetAbsoluteLeft()` etc., which are in *logical* pixels. Under
high-DPI + stretch-to-fit projection, logical and framebuffer diverge
(e.g. 1280 vs 2560), so `Renderer` caches `_scissorScaleX/Y` at
`BeginFrame` and multiplies the clip rect through. Easy to miss if
you're touching scissor code — Tests don't exist for this yet.

### Centralized rotation in DrawGumRecursively

Gum's `Rotation` is in degrees with positive = CCW; sokol_gp's
`sgp_rotate_at` with our Y-down projection produces CW-visible rotation
for positive angles, so we negate. `DrawGumRecursively` applies the
transform before rendering an element **and** its children, so
rotations compose. Note that `sgp_scissor` doesn't follow the transform
— a rotated `ClipsChildren` element still clips against an
axis-aligned scissor rect (hardware limitation matching MonoGame/XNA).

### BlendState comparisons use `ReferenceEquals`

`Gum.BlendState` is an XNA-style class with `public static readonly`
instances (`Opaque`, `AlphaBlend`, `Additive`, `NonPremultiplied`). Use
`ReferenceEquals`, not `==`, when mapping them — value equality isn't
overridden. `Renderer.MapBlend` has the full table.

### Reflection fallback handles int→enum, string→enum, primitive→Nullable

Gum core's `GraphicalUiElement.SetPropertyThroughReflection` uses
`Convert.ChangeType`, which doesn't handle any of those. `.gumx`
stores enum values as their underlying int (`<Value xsi:type="xsd:int">1</Value>`)
and nullable floats as plain floats — both constantly fail the core
path. `CustomSetPropertyOnRenderable.SetPropertyWithEnumConversion`
wraps the core helper and does the missing conversions, falling back
to the core path on any exception. Also covers string enum names
("Center") which core doesn't handle either.

### Animation chain dispatch

`Renderer.TickRecursively` walks every layer looking for Sprite and
NineSlice renderables and calls `AnimateSelf(dt)` on them. Invisible
subtrees still tick so that a paused-but-visible element resumes
in-sync when re-shown — matches shared Gum behaviour.

`AnimateSelf` has a safety cap of `chain.Count + 1` iterations plus
a modulo on remaining time, so a wildly-high `AnimationSpeed` landing
a large dt in one frame still lands on the correct frame rather than
running unbounded.

### Outline rendering

No native outline in fontstash. `Text.DrawLineWithOptionalOutline`
stamps the glyph multiple times at offsets on concentric rings (one
ring per integer radius from 1 to `OutlineThickness`, samples spaced
about 1px of arc apart). Even radii get a half-step phase offset so
stamps across adjacent rings don't align into radial streaks.

Cost is `(2πr)` stamps per radius — modest at thickness 1–3, gets
expensive at larger values. FontStashSharp's `FontSystemEffect.Stroked`
does this via morphological dilation at bake time (one stamp per
glyph) — a future refactor path if thickness values > 3 become common.

### Logical vs framebuffer sizes in BeginFrame

`Renderer.BeginFrame(int width, int height)` assumes 1:1. The 4-arg
overload `BeginFrame(int logicalW, int logicalH, int fbW, int fbH)`
drives high-DPI and dynamic resize: UI lays out in logical pixels
(1280×720 design), rasterizes into whatever the physical framebuffer
is at the moment. Samples feed `sapp_width()/sapp_height()` for the
framebuffer dims so resize just works. Aspect distorts at non-16:9
windows — aspect-preserving letterbox would need a non-full viewport.

---

## Dependencies

- `Sokol.NET` — git submodule at `/Sokol.NET/` (sibling to `/fna/`).
  No NuGet published.
- `Runtimes/Sokol/Sokol.csproj` — proxy project wrapping the submodule's
  `src/sokol/` + `src/imgui/` sources. The submodule's own
  `sokol.csproj` can't be referenced directly because `SDebugUI.cs`
  imports types from the sibling `imgui/` folder.
- `GumCommon.csproj` — standard core Gum dependency.

Target framework: `net10.0` (matches Sokol.NET). Other Gum backends
target `net8.0`; consumers of `Gum.sokol` need a `net10.0` SDK.

---

## Samples

```sh
# From the repo root
dotnet run --project Samples/SokolGum/SokolGumSample.csproj        # code-only UI
dotnet run --project Samples/SokolGumFromFile/SokolGumFromFile.csproj  # loads .gumx
```

Both windows are resizable; content stretches to fill on resize. No
keyboard shortcut to quit — use the window's close button or `⌘Q`.

---

## Known limitations

- **stb_truetype has no TrueType hinting.** Oversampling helps but
  doesn't match FreeType/DirectWrite quality at very small point sizes.
  FontStashSharp supports FreeType as a pluggable rasterizer (Windows
  only via their NuGet) — not a trivial drop-in.
- **Aspect-preserving letterbox isn't implemented.** Resizing to
  non-16:9 distorts the UI. Would need a framebuffer-pixel viewport
  offset + black-bar clearing around the content.
- **No `RenderTarget` / `IsRenderTarget` caching.** That's a Sprite-
  specific XNA feature (`RenderTargetTextureSource`), not general UI
  caching, and not implemented by any backend in the repo.
- **Atlased textures** (`.atlas` files) — scaffolding exists but no
  backend has a working loader. `ContentLoader` would need an
  `AnimationChainList`-style XML parser and a texture-region wrapper.

---

## Contributing

Keep commits granular and scoped (one concern per commit). If you
touch text rendering, test both samples at multiple window sizes,
verify `.gumx` still loads, and run at least thickness-0, 1, 2, 3
outlines to catch off-by-one artifacts. There are no automated tests
for this backend yet; a `Tests/SokolGum.Tests/` project following
`Tests/RaylibGum.Tests/`'s pattern would be a good first addition.
