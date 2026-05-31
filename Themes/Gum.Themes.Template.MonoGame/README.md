# Gum.Themes.Template

A starting point for building a new Gum theme. A theme restyles Gum Forms controls
by subclassing their V3 default visuals; this project sets up that structure so you
can focus on the look instead of the plumbing.

> For how a *finished* theme is consumed (install the package, call `Apply`), see the
> user guide: https://docs.flatredball.com/gum/code/styling/themes

## What's here

| File | Role |
|------|------|
| `TemplateTheme.cs` | Entry point. `Apply(GraphicsDevice)` registers fonts, sets the shared styling tokens, and installs the visuals as the default Forms templates. |
| `TemplatePalette.cs` | **The one place colors live.** Base tokens transcribed from your design's CSS, plus derived hover/press colors computed via `Adjust`. |
| `TemplateShapes.cs` | Factory helpers for the Apos.Shapes runtimes (filled rect, stroked border, focus ring, circles). Keeps each visual short. |
| `TemplateTextInputDecoration.cs` | Shared shape stack + state wiring for TextBox and PasswordBox. |
| `*Visual.cs` | One per styled control. Each subclasses a `Gum.Forms.DefaultVisuals.V3.*Visual`, swaps in shapes, and re-wires the state callbacks. |
| `Content/Fonts/` | Embedded TTFs (a body font + an icon font for glyphs the body font lacks). |

The `.Kni` project source-shares every `.cs` from this project and re-embeds the
fonts under its own assembly name — one set of code, two backend packages.

## Making your own theme

1. **Clone both projects** (`.MonoGame` and `.Kni`) and rename `Template` →
   `YourTheme` everywhere: folder names, file names, the `<PackageId>`, the
   `namespace`, and the `Template*` type names (`TemplateTheme`, `TemplatePalette`,
   `TemplateShapes`, `TemplateTextInputDecoration`). The per-control visual class
   names (`ButtonVisual`, etc.) stay the same — the namespace distinguishes them.
   **Leave `<AssemblyName>` and `<RootNamespace>` unset** in both csprojs: they
   default to the project name and must stay equal, because the embedded fonts are
   looked up by assembly name but live under the root namespace. If those diverge,
   the build still succeeds but fonts throw `FileNotFoundException` at runtime — so
   rename the `.csproj` files and folders, not just the `<PackageId>`.
2. **Fill in the palette.** Transcribe your design's `:root { --bg: …; }` block into
   the base tokens in `TemplatePalette`. Leave the derived colors computed unless
   your design pins an exact value. The standard slots are a starting vocabulary,
   not a cage — add tokens for anything your design defines (extra accents,
   success/danger, etc.) and delete the ones you don't use.
3. **Swap the fonts.** Drop your TTFs into `Content/Fonts/`, update the
   `<EmbeddedResource>` entries in both csprojs, and update `FontFamily` /
   `IconFontFamily` / `RegisterBundledFonts` in the theme class. Keep the license
   files alongside the fonts and packed in the csproj.
4. **Restyle the visuals**, and as you build out a control, move it from the
   "stock V3" block in `RegisterVisuals` up to the styled block.
5. **Verify by running, not just building.** Build *both* `.MonoGame` and `.Kni`,
   then add the theme to the `MonoGameGumThemesShowcase` sample (a `ProjectReference`
   plus one `ThemeOption` entry) and run it. `Apply` and font loading only fail at
   runtime — this is where a font `FileNotFoundException` from an uneven rename shows.
6. **Publish:** flip `<GeneratePackageOnBuild>` to `true` in both csprojs.

## If your design isn't flat

The template is flat and rectangular — no shadows, gradients, bevels, or pills. The
palette + shape conventions still apply, but matching a richer look needs shape
changes beyond a palette swap. Crib from the closest shipped theme:

- **Drop shadows / soft glows** → Bubblegum, plus the "Drop shadows" section of the
  `gum-theming` skill (use the native Apos.Shapes shadow; don't stack rects).
- **Gradients** → ForestGlade.
- **Bevels / inset edges** (Win95-style) → Retro95.
- **Pills / large rounded corners** → a larger `CornerRadius` on the rect shapes.
- **NineSlice instead of Apos.Shapes** → Editor.

## Conventions worth keeping

- **Read colors from the palette, never inline `new Color(...)` in a visual.** That's
  what keeps a restyle to one file.
- **Build chrome with `TemplateShapes`** for the common centered/full-parent shapes;
  build bespoke geometry (e.g. a left-anchored fill bar) inline.
- **Re-wire state callbacks with `=`, not `+=`** — you're replacing the base's
  behavior (which targets children you detached), not adding to it.
- See the `gum-theming` skill for the full set of gotchas (clip-container ordering,
  `HasEvents` on wrappers, focus-ring z-order, the cross-runtime packaging pattern).
