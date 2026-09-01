---
name: gum-forms-theme-import
description: Add Forms dialog's tool-content theme system (Templates/FormsThemes/*). Triggers: FormsFileService, AddFormsViewModel, ThemeRequirements, theme.txt, GetSourceDestinations, Bubblegum/Hazard theme content, ThemeRecolorHelper, porting a new Forms theme.
---

# Add Forms Theme Import

Distinct from code-only themes (`gum-theming` skill — C# `*Visual` subclasses, NuGet
packages). The two are **alternative delivery paths, not layers**: a theme imported here renders
from the user's own `.gumx`, so a `Gum.Themes.*` package is never needed for the styled look.
KernSmith and the runtime's shapes package are still recommended (fonts, shape-backed visuals) but
bypassable — the tool never edits the user's game project.

This is the **tool-content** side: a theme is a self-contained Gum project under
`Tools/Gum.ProjectServices/Templates/FormsThemes/<Name>/` (its own `GumProject.gumx`,
`Components/`, `Behaviors/`, `Screens/`, `Standards/`) that Add Forms copies into the user's
project. `Bubblegum`, `Hazard`, `DarkPro`, `Retro95`, `ForestGlade`, `Neon`, and `Meadow` have
parity today (staged per-theme in `Gum/GumFormsPlugin/GumFormsPlugin.csproj`'s postbuild, alongside
the default `Standard` theme — 8 entries in the tool's theme dropdown). `Editor` exists as a
code-only theme (`Themes/Gum.Themes.Editor.*`) but was investigated and intentionally skipped: it
doesn't follow the full-chrome-rebuild pattern the tool-content model assumes (it recolors V3's
stock NineSlice visuals in place rather than replacing them), restyles only 9 of ~21 Forms
controls, and introduces `Expander` — a control with no Add-Forms behavior infrastructure. Porting
it would mean inventing a look the theme's own author chose not to build.

## Porting a new theme (#3527): do the landmines up front, not reactively

Each gotcha below is cheap to prevent up front and expensive to discover one at a time via manual
testing after the fact. Sequence a new theme port like this:

1. Clone an existing theme's whole tree (fastest correct starting point — layout, state set, and
   structure are almost all theme-agnostic) rather than hand-authoring controls from scratch;
   namespace it and scan physical files on disk (not just the `.gumx`'s reference list) for
   orphans, before anything else.
2. Build `Styles.gucx` matching the code-only `*Colors`/`*Text` class names first — every control
   wires against it, so getting names right early avoids a rename pass later.
3. Recolor per "Recoloring after a clone" below instead of hand-placing literals control-by-control.
4. Grep the code-only theme's `*Visual.cs` files for `CornerRadius` and `HasDropshadow`/
   `Dropshadow*` constants — a separate axis from color, untouched by any Styles-swatch rename. A
   theme whose corner radius or shadow use differs uniformly from the clone's needs a project-wide
   mechanical fix (rewrite every `CornerRadius`, flip every `HasDropshadow`) that nothing catches
   automatically. Do this sweep across **`Screens/` too, not just `Components/`** — a demo screen's own instances
   (e.g. a panel background) are just as likely to still carry the cloned theme's raw values, and
   nothing propagates a Components-only fix to them.
5. Run `AllColorVariables_ShouldBeStylesWired` from the start, not as a final audit — see "Wire
   the controls to Styles" below for what it does and doesn't catch.
6. After every batch of edits: `gumcli check`/`check-references`/`diff-standards`, **and** a
   rebuild plus fresh-project Add Forms import in the actual tool — `gumcli` never exercises the
   postbuild-copy path where duplicate-import bugs live. `gumcli check`'s summary line reads "N
   error(s) found" even when every listed line is `warning:` severity, so a pre-existing warning
   inherited from the clone (e.g. a `Category` state illegally selecting its own category) is easy
   to mistake for a pass — read the lines, not just the count.
7. **A swatch-name recolor pass never touches geometry, and geometry is where the worst-looking bugs hide.** Ring/indicator `X`/`Y`/`Width`/`Height`/`CornerRadius` literals are carried over from the clone verbatim by every rename/recolor script — nothing about "fix the colors" touches them. Diff each focus-ring/indicator explicitly against the code-only theme's actual constants: a ring built `RelativeToParent` + Center-anchored (`X=Y=0`) can only be wrong in *size* (its Width/Height delta must equal the code's `FocusRingInset*2`, its CornerRadius must equal body-radius+inset — not whatever the clone had); one built `Absolute` + edge-anchored (a Left/Bottom-anchored circle or box — CheckBox/RadioButton's pattern) can *also* be positionally asymmetric, since the clone's edge offset doesn't automatically rescale to a different inset — diff its X/Y against `-Inset` and its Width/Height against `bodySize + 2*Inset` by hand.
8. **Don't cut a visual feature (a glow, a border, a state-specific effect) as a silent scope-reduction call.** If the code-only theme's `*Visual.cs` sets it (`HasDropshadow`, a stroke, anything state-conditional), port it or flag the omission explicitly to the user — folding "skipped for time" into a summary aside is not the same as getting a decision. Missing a whole control this way (not just a wrong value) is the most visually obvious kind of incomplete port.
9. **The rebuild in step 6 can silently no-op.** `GumFormsPlugin`'s postbuild `xcopy` only runs
   when MSBuild actually re-executes that project's build — and theme `.gucx`/`.gusx` content
   isn't a tracked input of `GumFormsPlugin.csproj` (it lives in a different project entirely), so
   an IDE-driven incremental "Build" can decide the project is up to date and skip the postbuild
   even though the theme source changed. The result: every content-only fix looks correct on disk
   and in every automated check, but the *staged* output the running tool actually reads from
   (`Gum/bin/<Config>/Content/FormsThemes/<Name>/`) is still the old content, and a fresh Add
   Forms import reproduces the exact bug you just fixed. After any content-only change, diff a
   file directly from the staged output (not just the source template) before calling the fix
   verified — a full `dotnet build GumFull.sln` from the command line has reliably re-triggered
   the postbuild in practice; an IDE "Build" may not.

## Key files

| File | Role |
|------|------|
| `Tools/Gum.Presentation/GumForms/Services/FormsFileService.cs` | `GetAvailableThemes`, `GetThemeDirectory`, `GetSourceDestinations` — computes what gets copied where |
| `Tools/Gum.Presentation/GumForms/ViewModels/AddFormsViewModel.cs` | Add Forms dialog: theme selection, save/import |
| `Tools/Gum.Presentation/GumForms/Services/ThemeRequirements.cs` | Parses optional `theme.txt` (font generator, Skia shapes) — project-level prerequisites, not content |
| `Gum/GumFormsPlugin/GumFormsPlugin.csproj` | Postbuild `<Exec>` stages each theme into the built `Content/FormsThemes/<Name>/` — **one hand-written `xcopy` + `stage-forms-behaviors` block per theme, not a loop over the folder.** A new theme is invisible to `FormsFileService.GetAvailableThemes()` in a built tool until its own block is added here, mirroring the existing per-theme blocks exactly. |

## A theme's qualified names must be self-prefixed — there's no folder-nesting shortcut

`Components/`, `Screens/`, `Standards/` are hardcoded literal subfolders appended directly to
the project root (`GumDataTypes/ElementReference.cs` `Subfolder`; `Behaviors/` the same way in
`BehaviorReference.cs`) — an element's file path is always `<projectRoot>/<Subfolder>/<Name>.<ext>`.
So a theme can't be namespaced by nesting its whole tree one level deeper (e.g. under
`<Theme>/Components/...`) — that path segment never resolves.

Instead, prefix the theme name into every qualified `Name` itself, the same way `Controls/` and
`Elements/` already subfolder within `Components/`: `Controls/Button` → `Bubblegum/Controls/Button`,
landing on disk at `Components/Bubblegum/Controls/Button.gucx`. Apply this to every Component/Screen
reference surface: `.gumx` `ComponentReference`/`ScreenReference` `Name` attributes, each file's own
`<Name>` tag, `BaseType="..."` instance attributes, `VariableReferences` strings
(`Components/Styles.X` → `Components/Bubblegum/Styles.X`), and a `.behx`'s
`<DefaultImplementation>` (points at a themed *component*, so it gets the prefix even though the
behavior file itself doesn't — see below). `StandardElementReference` and `BehaviorReference` stay
unprefixed. Without this, importing a second theme into the same project silently overwrites the
first theme's entire Components/Screens tree (`GetSourceDestinations` maps every theme file to an
identical unnamespaced destination with `overwrite: true`).

**A file that exists on disk but isn't referenced in the theme's own `.gumx` still gets copied and
imported** — `GetSourceDestinations` walks every `.gucx`/`.behx` file under the theme folder, not
just the ones listed as a `ComponentReference`/`BehaviorReference`. A rename pass built only from
the `.gumx`'s reference list misses these orphans, leaving a stale un-prefixed `<Name>` that
duplicates or collides with the correctly-renamed content. Verify by scanning physical files
directly (`Directory.GetFiles(..., SearchOption.AllDirectories)`), not `project.AllElements`, which
only reflects what's registered.

## Behaviors are shared infrastructure, not per-theme content

Unlike Components (a theme's look) and Screens (a theme's demo content), Behaviors declare the
generic category-state/`FormsProperty` contract every theme's visuals plug into — the same
`ButtonBehavior`, `CheckBoxBehavior`, etc. every theme and project needs. They live flat at
`Behaviors/*.behx`, unprefixed, matching how `Standards/` is shared rather than namespaced. A
`.behx`'s own `<Name>` and a `.gucx`'s `<ElementBehaviorReference><BehaviorName>` stay unprefixed;
only `<DefaultImplementation>` (which names a themed *component*) gets the theme prefix. Each
theme still ships its own copy of every behavior file (same reason as Standards, above) — those
copies are expected to be identical to every other theme's, so importing one theme after another
just re-overwrites the shared file rather than duplicating it. There's no mechanism yet to
guarantee the copies actually stay identical as Forms controls gain new properties — see #4070.

## Standard elements: only the default theme may touch them

`GetSourceDestinations` skips `.gutx` files for any theme except `DefaultThemeName` ("Standard") —
a theme's own styling must live entirely in its Components, never in shared Standards, or
importing it stomps the destination project's real defaults (and any other theme already
imported). A theme's `Standards/` folder still needs to exist on disk with real content, though —
without it the theme's `GumProject.gumx` isn't a loadable, standalone Gum project, which is how a
theme is meant to be opened/edited/previewed directly in the tool.

## A theme's Styles component must mirror its code-only palette by name, not just value

A theme's centralized styling component (e.g. `Components/Bubblegum/Styles.gucx`) should expose
exactly the color/text tokens its code-only counterpart defines (`Themes/Gum.Themes.<Name>.*`'s
`*Colors`/`*Text` classes — see `gum-theming`), under the *same names*, with no extra swatch that
class doesn't define. A swatch can hold the correct value under the wrong name — verify by diffing
the tool content's swatch/text-instance names against the code-only class's property names
directly, not by eyeballing rendered colors. "Exactly" is scoped to swatches something actually
references, not the full C# property list: a get-only *alias* (returns another property's value
unchanged, e.g. `HoverFill => Surface2`) needs no swatch of its own — point references at the
underlying token; a get-only property with a genuinely *computed* value (e.g.
`AccentHover => Accent.Adjust(+15f)`) has no equivalent in static XML and must be precomputed
(same lighten/darken math as `ColorExtensions.Adjust` — see `gum-theming`) and baked in as an
ordinary swatch. A theme's own `StylesPalette_ShouldExactlyMatch...` test in
`Tests/Gum.ProjectServices.Tests/` pins the resulting (non-exhaustive) swatch list once decided.

## "Wire the controls to Styles" means every state, not just Default

A control can look fully wired at a glance — its Default state references `Styles.*` — while every
one of its categorized states (Enabled/Disabled/Highlighted/Pushed/...) still hardcodes the same
colors directly, since Default and category states are separate `StateSave`s with independent
`VariableReferences`. `Tests/Gum.ProjectServices.Tests/BubblegumTemplateTests.cs`'s
`AllColorVariables_ShouldBeStylesWired` is the authoritative check-all for this — a fully
transparent fill (`FillAlpha` set to `0`) is exempted, since its RGB is never visible. It only
proves *some* Styles swatch is wired, not the *right* one — a state referencing the wrong swatch
(right structure, wrong concept) passes silently. Spot-check a control's trickiest states (the
ones whose interaction language most diverges between the cloned theme and the new one) against
the code-only theme's state-wiring method directly.

## Recoloring after a clone

A cloned theme's controls already reference the old theme's swatch names throughout.

**A swatch name surviving into the new palette does not mean the reference is still correct.** Two
themes can share a whole vocabulary and still assign it differently per control and per state — one
fills a resting Button with `Accent`, another with `Surface1` and reserves `Accent` for the focus
stroke. Derive the mapping from the code-only theme's `*Visual.cs` `States.<X>.Apply` bodies as a
function of (instance, property, state name); a global old-name → new-name rename yields a
theme that passes every automated check and looks nothing like its counterpart. Where a swatch's
*concept* really does carry over, the reference string needs no edit — only the value differs, and
that's handled below. Rename only the reference strings whose
target swatch name no longer exists under the new palette (e.g. old `White` → new `Ink`); grep
each renamed swatch's actual usage sites first; a single old swatch can legitimately split across
several new ones by *role*, and the local left-hand property prefix on each `VariableReferences`
line reliably signals which — bare `Red=`/`Green=`/`Blue=` is a `Text`-type instance, `Stroke*=` is
a border, `Fill*=` is a fill — so this is a small, closed decision per swatch, not per site.

Once every reference string names the *correct* swatch, every state's redundant hardcoded scalar
(`FillRed`, etc.) is still the *old* theme's value until something re-runs
`ApplyVariableReferences` and re-saves — **`gumcli check-references --fix` will not do this**: it
only materializes a scalar that's entirely missing, never corrects one that exists but no longer
matches what its reference currently resolves to (confirmed empirically — running it after a
rename with the values already stale made zero changes). `Tests/Gum.ProjectServices.Tests/ThemeRecolorHelper.cs`
is a reusable, `[Fact(Skip=...)]`-guarded migration step that loads the theme project, calls
`GumProjectSave.ApplyAllVariableReferences()` (from `GumRuntime`), and re-saves every component
**and screen** (see the previous section for why screens need the same pass) — point it at the new
theme and run it once via `dotnet test --filter`. When re-saving any
`ElementSave` programmatically, `Save(path, useCompactFormat: true)` is load-bearing: the default
`false` serializes `VariableSave`/`InstanceSave` as child elements (the legacy v1 shape) instead of
the attribute-based shape every theme file on disk actually uses — both round-trip through
`ProjectLoader` fine, so passing the default silently produces a valid-but-inconsistent file.

## A bundled custom font must be referenced by its .ttf filename, not its family name

Setting a `Text`/`Styles` instance's `Font` value to a human-readable family name (e.g.
`"Saira Condensed"`) makes Gum look that name up as a **Windows-installed system font** — both at
runtime (`CustomSetPropertyOnRenderable.UpdateToFontValues` → `BmfcSave.ResolveTtfSourcePath`) and
in the tool's headless generator. A theme's own bundled `Fonts/*.ttf` is invisible to that lookup.
If the family isn't actually installed on the machine (a Google Font bundled as project content
almost never is), generation fails — logged as `KernSmith error: System font '<name>' not found`
in the tool's Output panel, easy to miss — and the text silently falls back to the embedded
default (`Font18Arial`), which looks like a rendering/alpha bug at a glance, not a missing-font one.

The fix is to set `Font` to the literal bundled path instead — `IsFontFilePath`/`ResolveTtfSourcePath`
treat any `Font` value ending in `.ttf` as a direct file reference, bypassing system-font lookup
entirely. **The value must include the `Fonts/` folder segment** (`"Fonts/SairaCondensed-Regular.ttf"`,
not the bare filename) — `ResolveFontFilePath` resolves a non-rooted path against the *project
root* (the `.gumx`'s own directory), not the `Fonts/` folder specifically, so a bare filename looks
for the ttf directly in the project root and silently fails to find it there. That failure is
swallowed by a bare `catch { }` around the font-creation call (`CustomSetPropertyOnRenderable.
GetOrCreateBakedFont`) with **no Output-panel message at all** — quieter than the system-font-lookup
failure above, and easy to mistake for "the fix didn't take" after already fixing the family-name
half of this. There's no separate "pick the bold weight" mechanism on this path — a `Strong`/bold
style must point `Font` at the bold-weight file directly (`"Fonts/SairaCondensed-SemiBold.ttf"`);
`IsBold` alone does nothing for a `.ttf`-valued `Font`. This has no color-style parallel to check against:
`AllColorVariables_ShouldBeStylesWired` only inspects color-channel suffixes, so a family-name
`Font` value passes every existing automated check. After fixing `Styles.gucx`, rerun
`ThemeRecolorHelper` (above) to cascade to every control and screen that references
`Styles.Normal.Font`/`Styles.Strong.Font` — then grep the whole theme for the bare family-name
string, since a demo screen's own `Text` instances can hardcode `Font` directly instead of routing
through `Styles`, and a hardcoded value has no reference for the reapply pass to cascade into.

## Verifying theme content changes

**Structural checks prove the XML is well-formed; they prove nothing about whether the theme
actually works.** `gumcli check`/`check-references`/`diff-standards` and the `Gum.ProjectServices.Tests`
suite all validate *shape* — types match, references resolve to *some* materialized scalar,
Standards match canonical defaults. None of them generate a font, render a color, or read the
built tool's output. A theme can pass every one of these while still being visibly broken — a
family-name `Font` value, a missing `Fonts/` path segment, and an un-recolored screen all do.
Treat a clean run of these as "no *regression*," never as "the theme works." Three checks actually
exercise real behavior and catch what structural checks can't:

- **Font generation, for real.** `GumProjectFontGenerator` (`GumProjectFontGenerator/Program.cs`)
  runs the same headless pipeline (`HeadlessFontGenerationService`) the tool's own "Checking N font
  files..." step uses. Copy the theme to a scratch folder, empty its `FontCache/`, and run
  `dotnet GumProjectFontGenerator.dll <path>\GumProject.gumx` — a real `.fnt`/`.png` pair appearing
  in `FontCache/` for every custom font is the only proof font resolution actually succeeds; a clean
  `gumcli check` proves nothing about it.
- **Render it — and every state that changes appearance, not just the one you get for free.**
  `gumcli screenshot <theme>/GumProject.gumx <element> --background <hex>` (see `gum-cli`) renders
  the theme's own demo screen or any single control to a PNG, but only ever the **first category
  state in the `.gucx`** — almost always the unfocused/unchecked/resting look. A focus ring, a
  selected-row tint, a checked-state fill can be badly broken (wrong size, wrong position, missing
  entirely) and invisible in every render you take by default; the resting-state screenshot proves
  nothing about them. To actually see a specific state, temporarily flip its own `Visible`/color
  `<Variable>` values in place (the state that's actually rendered, not `Default` — category-state
  values override `Default` at render/instantiation time), screenshot, then revert before moving on
  — do this per control for every state whose geometry or color meaningfully diverges from resting.
- **The staged build, not the source.** Add Forms reads from `Gum/bin/<Config>/Content/FormsThemes/<Theme>/`,
  not from `Templates/FormsThemes/<Theme>/` — and per the postbuild-can-no-op landmine above, those
  two can silently diverge even after a correct, committed fix. Diff a file straight from the staged
  path after rebuilding, not just the source template, before calling anything verified.

Beyond those two, still run `gumcli diff-standards` (theme's Standards vs.
`StandardElementsManager` canonical defaults — should always read "No drift found" for a
non-default theme), `gumcli check` (structural errors), and `gumcli check-references`
(unmaterialized `VariableReferences`) — see the `gum-cli` skill. Run before and after a change;
identical output (same pre-existing warnings, nothing new) is the proof a mechanical edit didn't
regress anything.

**Hand-editing a `VariableReferences` block needs the correct LHS property name for the
*target* instance's type, not the swatch's.** `Text` instances use `Red`/`Green`/`Blue`; `Rectangle`/
`Circle` fills use `FillRed`/`FillGreen`/`FillBlue`; strokes use `StrokeRed`/`StrokeGreen`/`StrokeBlue`
regardless of instance type. Getting this wrong doesn't error — if the pre-existing hardcoded
`<Variable>` value already happens to match the swatch, `gumcli check` stays clean and the visual
is unchanged, so the mistake is invisible until `gumcli check-references` reports "has
VariableReferences but missing materialized scalars" (it matches by property *name*, not value).
Run `gumcli check-references --fix` after any hand-authored reference to materialize it for real —
propagation is never automatic (not on project load, not on opening a screen in the tool), so
authoring the reference row is the whole job; don't also hand-copy the resolved value into
instances yourself, `--fix` already writes it into every state. This only works when the scalar is
missing outright, though; see "Recoloring after a clone" above for the stale-but-present case it
can't touch.

**A `Red`/`Green`/`Blue` triple collapses to one `*Color` line whenever both sides decompose to the
same composite token** — the two names need not match, so cross-named forms like
`DropshadowColor = X.FillColor` and `Color = X.FillColor` are valid. All three lines must target
the same object, since channels pair positionally. The hard requirement is on the left side only: the owning instance's type must
declare every channel (`ExpandCompositeReferenceLine`/`OwnerHasAllCompositeChannels`), otherwise
the line silently fails to expand at apply time and `gumcli check-references` reports the
unmaterialized `*Color` scalar.

**A composite `Color` reference only expands to the RGB triple — alpha is a separate channel it never touches** (`ExpandCompositeReferenceLine` pairs R/G/B only). Any translucent swatch (`FillAlpha < 255` in `Styles.gucx`) referenced via `FillColor = Source.FillColor` or `DropshadowColor = Source.FillColor` renders **fully opaque** until you add a parallel plain reference — `FillAlpha = Source.FillAlpha` / `DropshadowAlpha = Source.FillAlpha` — next to the composite line. This has no color-style parallel to check against: it's silent in every automated check, and a screenshot of an *opaque* wrong render can still look plausible at a glance (a solid-color panel instead of a translucent tint doesn't scream "bug" the way a missing color does) — compare the rendered alpha against the swatch's intended value deliberately. Same "stale but present" trap as recoloring applies here too: a clone that already carried a hardcoded opaque literal on that property keeps it until `ThemeRecolorHelper` reapplies and resaves — `check-references --fix` only fills a scalar that's entirely missing.

One more staged-output-specific gotcha, beyond the postbuild simply not rerunning (item 9 /
"the staged build, not the source" above): `xcopy` never deletes, so a rename or removal in the
template leaves the stale old file sitting in an already-built output even when the postbuild
*does* rerun, and importing the theme pulls in both the old and new copy. The postbuild step
deletes the theme's output folder before `xcopy`-ing to prevent this; verify a source-side rename
actually lands clean by planting a throwaway file in the built output and confirming a rebuild
removes it.

**A theme's checked-in FontCache silently goes stale when a Font value is repointed to a new
bundled `.ttf`** — old entries stay valid, but nothing regenerates or re-commits the new one
automatically. `gumcli screenshot` is where this becomes visible: unlike the real Gum tool (which
bakes missing fonts on project load via `FontService`), `MonoGameScreenshotService`/
`RaylibScreenshotService` never wire font generation at all, so a font missing from `FontCache`
silently renders as the default embedded font with no glyph and no error. Fix by running
`dotnet GumProjectFontGenerator.dll <path>\GumProject.gumx` directly against the theme's own
folder (not a scratch copy) — it only fills in missing entries, so existing cached fonts are
untouched — then delete any `FontCache` entry whose exact name/size key is no longer referenced
anywhere in the theme.
