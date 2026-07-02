# Gum.Themes.Template

A starting point for building a new Gum theme. A theme restyles Gum Forms controls
by subclassing their V3 default visuals; this project sets up that structure so you
can focus on the look instead of the plumbing.

> For how a *finished* theme is consumed (install the package, call `Apply`), see the
> user guide: https://docs.flatredball.com/gum/code/styling/themes

## What's here

| File | Role |
|------|------|
| `TemplateTheme.cs` | Entry point. The parameterless `Apply()` registers fonts, sets the shared styling tokens, and installs the visuals as the default Forms templates. |
| `TemplateStyling.cs` | **The one place colors and font selection live.** Holds `TemplateStyling` (the mutable `ActiveStyle` singleton), `TemplateColors` (base tokens transcribed from your design's CSS, plus derived hover/press colors computed via `Adjust`), and `TemplateText` (font family/size selection). Consumers restyle by mutating `TemplateStyling.ActiveStyle.Colors`/`.Text` before calling `TemplateTheme.Apply()` — the same "mutate before construct" ordering as Gum's own `Styling.ActiveStyle`. |
| `TemplateShapes.cs` | Factory helpers for the Apos.Shapes runtimes (filled rect, stroked border, focus ring, circles). Keeps each visual short. |
| `TemplateTextInputDecoration.cs` | Shared shape stack + state wiring for TextBox and PasswordBox. |
| `*Visual.cs` | One per styled control. Each subclasses a `Gum.Forms.DefaultVisuals.V3.*Visual`, swaps in shapes, and re-wires the state callbacks. |
| `Variants/` | Opt-in **Rich** alternates for a representative control subset (pill + hard-offset-shadow button, rounded box, soft focus glow, dashed-outline panel, circular drop-shadow thumb). Not registered by default — copy the ones matching your design. See *Choosing a style* below. |
| `Content/Fonts/` | Embedded TTFs — a display font, a body font (the multi-font demo), and an icon font for glyphs the others lack. |

The `.Kni` and `.Raylib` projects source-share every `.cs` from this project and
re-embed the fonts under their own assembly names — one set of code, three backend
packages.

## Consuming a built theme

A finished clone ships one package per rendering backend; install the one matching
your runtime:

```
dotnet add package Gum.Themes.Template.MonoGame
dotnet add package Gum.Themes.Template.Kni
dotnet add package Gum.Themes.Template.Raylib
```

Then call the parameterless `Apply()` after initializing Gum — the same call on
every backend:

```csharp
using Gum.Themes.Template;

// after GumService.Default.Initialize(...)
TemplateTheme.Apply();
```

> On MonoGame/KNI a legacy `TemplateTheme.Apply(GraphicsDevice)` overload remains for
> source compatibility; the graphics device is now resolved internally, so prefer `Apply()`.

## Making your own theme

1. **Clone all three projects** (`.MonoGame`, `.Kni`, and `.Raylib`) and rename `Template` →
   `YourTheme`: folder names, file names, the `<PackageId>`, the `namespace`, and the
   theme-owned `Template*` type names (`TemplateTheme`, `TemplateStyling`, `TemplateColors`,
   `TemplateText`, `TemplateShapes`, `TemplateTextInputDecoration`). The per-control visual
   class names (`ButtonVisual`, etc.) stay the same — the namespace distinguishes them.
   **Don't do a blind global find-replace of the word `Template`.** Three Gum
   *framework* identifiers also contain it and must **not** be renamed:
   `VisualTemplate`, `DefaultFormsTemplates`, and `ScrollViewerVisualTemplate`.
   The template deliberately keeps its own element names prefix-free (e.g. `"BoxFill"`,
   not `"TemplateBoxFill"`), so the only theme-owned `Template` tokens are the
   namespace and those type names — rename exactly those (a whole-word /
   case-sensitive replace of each), and leave the framework identifiers alone.
   **Leave `<AssemblyName>` and `<RootNamespace>` unset** in all three csprojs: they
   default to the project name and must stay equal, because the embedded fonts are
   looked up by assembly name but live under the root namespace. If those diverge,
   the build still succeeds but fonts throw `FileNotFoundException` at runtime — so
   rename the `.csproj` files and folders, not just the `<PackageId>`.
2. **Fill in the palette.** Transcribe your design's `:root { --bg: …; }` block into
   the base tokens in `TemplateColors` (in `TemplateStyling.cs`). Leave the derived
   colors computed unless your design pins an exact value. The standard slots are a
   starting vocabulary, not a cage — add tokens for anything your design defines
   (extra accents, success/danger, etc.) and delete the ones you don't use. Keep the
   4-token guardrail (`TextPrimary`/`TextMuted`/`Primary`/`Accent`) — alias them to
   your own vocabulary as get-only properties if the names don't already match.
3. **Swap the fonts.** Drop your TTFs into `Content/Fonts/`, update the
   `<EmbeddedResource>` entries in all three csprojs, and update the fixed
   `Bundled*FontFamily` constants + `RegisterBundledFonts` in the theme class (font
   *registration*) and the defaults on `TemplateText` (font *selection* — what
   visuals actually pick, mutable via `TemplateStyling.ActiveStyle.Text`). Keep the
   license files alongside the fonts and packed in the csproj.
   **Need static TTFs, not a variable font.** KernSmith rasterizes static TTFs; it
   does not select instances from a variable font (a single `Foo[wght].ttf` carrying
   every weight). Many modern Google Fonts ship VF-only — if yours does, fetch its
   static weight cuts, or approximate with a static sibling family (e.g. the
   `Condensed` variant) and map one cut to each Gum style slot (`null` → Normal,
   `"Bold"`, etc.). **google-webfonts-helper** (`gwfh.mranftl.com`) serves static cuts
   of VF-only Google fonts. This template ships **two** families — see *Two typefaces*.
4. **Restyle the visuals**, and as you build out a control, move it from the
   "stock V3" block in `RegisterVisuals` up to the styled block.
5. **Verify by running, not just building.** Build all three (`.MonoGame`, `.Kni`,
   and `.Raylib`), then add the theme to the `MonoGameGumThemesShowcase` sample (a `ProjectReference`
   plus one `ThemeOption` entry) and run it. `Apply` and font loading only fail at
   runtime — this is where a font `FileNotFoundException` from an uneven rename shows.
6. **Publish:** flip `<GeneratePackageOnBuild>` to `true` in all three csprojs.

## Choosing a style: flat default, or the Rich variants

The registered visuals are flat and rectangular — no shadows, gradients, bevels, or
pills. The `Variants/` folder ships **Rich** alternates (`Gum.Themes.Template.Variants`)
for a representative subset of controls, each demonstrating one technique: a pill
button with a flat hard-offset "stacked card" shadow, a rounded check box, a soft
focus-ring glow, a dashed-outline list panel, and a circular drop-shadow slider thumb.
Each variant copies its flat sibling's palette and state wiring and changes only the
*shapes*, so the two are interchangeable per control.

They're **not** registered by default — `RegisterVisuals` carries a commented swap-in
line per control. To adopt one, uncomment its line (or register
`new Variants.YourControlVisual(...)`), then delete the variant files you don't want.

For looks beyond the gallery, crib from the closest shipped theme:

- **Drop shadows / soft glows** → Bubblegum, plus the "Drop shadows" section of the
  `gum-theming` skill (native Apos.Shapes shadow — *soft* = bump the alpha; *hard*
  "stacked card" edge = an opaque color with blur 0).
- **Gradients** → ForestGlade.
- **Bevels / inset edges** (Win95-style) → Retro95.
- **Pills / large rounded corners** → a larger `CornerRadius` on the rect shapes.
- **NineSlice instead of Apos.Shapes** → Editor.

## Two typefaces

`TemplateStyling.ActiveStyle.Text` carries a single family — the **display** default
(`FontFamily`), which flows to every control. This template also bundles a **body**
family (`BodyFontFamily`) and opts the typed / list / menu / tooltip visuals into it via
`TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily`. Collapse to one
family by deleting `BodyFontFamily`, its `RegisterBundledFonts` lines, and those
per-visual opt-ins; add a third family the same way. For a family with no italic cut,
point the `Italic` / `BoldItalic` style slots at the upright files (as the template does
for the body font) so a stray italic request still resolves to a real font.

## Conventions worth keeping

- **Read colors from `TemplateStyling.ActiveStyle.Colors`, never inline `new Color(...)`
  in a visual.** That's what keeps a restyle to one file — and what lets a consumer
  restyle by mutating `Colors`/`Text` before calling `Apply()`, no fork required.
- **Build chrome with `TemplateShapes`** for the common centered/full-parent shapes;
  build bespoke geometry (e.g. a left-anchored fill bar) inline.
- **Re-wire state callbacks with `=`, not `+=`** — you're replacing the base's
  behavior (which targets children you detached), not adding to it.
- See the `gum-theming` skill for the full set of gotchas (clip-container ordering,
  `HasEvents` on wrappers, focus-ring z-order, the cross-runtime packaging pattern).
