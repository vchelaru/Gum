# Theme Authoring Follow-Up

Follow-up work captured while building the **Meadow** theme. Two independent
efforts:

1. **Template: ship two control "styles" to copy from, plus multi-font support** —
   make `Gum.Themes.Template` a real starting point for *both* flat and richly
   decorated designs, and make a second typeface a first-class part of the
   recipe.
2. **Bubblegum: adopt the shape factory** — bring the shipped Bubblegum theme in
   line with the `TemplateShapes` factory convention it currently ignores.

Neither is started. This document is the design/scope; engineers fill in the
technical details during implementation.

---

## Part 1 — Template: two control styles + multi-font support

### Why

The `gum-theming` skill tells a new author to **clone the Template**, but the
Template only demonstrates **one** visual language: flat, rectangular, no shadow,
single border, single font. The moment a design has pills, drop shadows, dashed
outlines, circular check/radio marks, or a second typeface, the Template stops
being a useful starting point and the skill quietly redirects the author to
"crib from the closest shipped theme" instead (Bubblegum, Dark Pro, …).

That is two competing pieces of advice. Building Meadow, the Template was *not*
the right base — Bubblegum was — because Meadow needed shadows, pills, dashed
panels, circles, and two fonts, none of which the Template shows. The fix is to
make the Template a genuine superset: demonstrate the **rich** techniques
alongside the **flat** ones, organized so a cloner can copy whichever control
treatments match their design and delete the rest.

### Goal

A cloner should be able to:

- See each common technique (flat fill, rounded/pill fill, soft drop shadow,
  hard offset shadow, dashed outline, circular control, single vs. two fonts)
  demonstrated on a real control.
- Copy the treatment they want for a given control and delete the one they don't.
- End up with a theme that still builds and runs at every step.

### Concept: a "Flat" style and a "Rich" style

Define the two "types of controls" the user asked for as two named styles:

- **Flat** — the current Template look. Rectangular or low-radius, solid fills,
  a single hairline border, no shadow. Matches utilitarian / editor / HUD
  designs. Cheapest to render.
- **Rich** — Apos.Shapes-backed. Pill / rounded corners, soft *and* hard drop
  shadows, dashed container outlines, circular check / radio marks. Matches
  casual / cozy / branded designs.

Both styles read the **same** palette and use the **same** `TemplateShapes`
factory and the **same** theme entry point. Only the per-control visual classes
differ.

### File organization — recommended: a non-registered "Variants" gallery

Duplicating all ~20 visuals × 2 styles = ~40 files doubles maintenance forever.
Avoid that. Instead:

- Keep the Template's **canonical working set** exactly as today (one coherent
  style — make it the **Rich** style, since rich is the harder thing to derive
  from scratch). This is what `RegisterVisuals` installs, what builds, and what
  the showcase shows.
- Add a sibling **`Variants/`** (or `Gallery/`) folder containing **alternate-style
  versions of a representative subset** of controls — enough to demonstrate every
  technique once, not every control twice. These are **not** registered by
  default and are excluded from the default look; each is heavily commented to
  explain the technique it shows.

A cloner picks a control, opens its canonical version and its variant, copies
whichever matches their design over the canonical one, and updates the single
registration line. The Template keeps building because the canonical set is
always complete.

Representative subset (covers the full technique vocabulary):

| Control | Flat variant shows | Rich variant shows |
| --- | --- | --- |
| Button | flat rect fill, no shadow | pill + **hard offset** drop shadow |
| CheckBox | square box, NineSlice/flat | **circular-radius** box + soft focus ring |
| RadioButton | (shares CheckBox techniques) | **circle** primitives |
| TextBox (via the input decoration) | flat fill + 1px border | rounded fill + translucent focus-ring glow |
| ListBox (container) | solid 1px border | **dashed** outline + border-on-top rounded-clip trick |
| Slider thumb | flat square handle | **circle** + soft drop shadow |

The rest of the controls ship in a single (Rich) style; the cloner extrapolates
from the subset. Document this explicitly so the subset choice doesn't read as an
accidental omission.

### Shared infrastructure both styles rely on

- **`TemplateShapes`** already centralizes the full-parent fill / border /
  focus-ring (rect and circle) builders. Both styles call it. **Add a
  drop-shadow-capable fill variant** so the Rich style's soft *and* hard shadows
  are factory calls rather than hand-set shadow properties at each call site
  (the hard-offset case is `blur = 0` + an opaque color; the soft case is a
  blurred translucent color — see the shadow note below).
- **Palette** — both styles read the same palette file. No change.

### Multi-font support

**Problem.** `Styling.ActiveStyle.Text.Normal` / `.Strong` describe a **single**
font family. A design with a display face *and* a body face (Meadow: Baloo 2 for
buttons/labels/titles, Quicksand for inputs/lists/menus) has no first-class way
to express the second family. Building Meadow, every body-text visual had to set
`TextInstance.Font = BodyFontFamily` by hand in its constructor, and the
`gum-theming` skill doesn't mention this at all — it assumes one family.

**Recommended now (Template-level, no Gum API change):** make the second family a
documented convention in the Template.

- Add a `BodyFontFamily` constant alongside the existing display `FontFamily`.
- Register the second family's TTFs in the theme's font-registration step, with
  the same four style slots (Normal / Bold / Italic / BoldItalic). For a
  weight-only family with no italic cut, map the italic slots to the upright
  files so a stray italic request resolves to a real font instead of risking a
  missing-style lookup.
- Establish and document **which** visuals opt into the body face (text inputs,
  combo field, list rows, menu items, tooltip) versus which keep the default
  display face (buttons, check/radio labels, window title). The default
  (`Styling.Text`) should be whichever family more controls use.
- Add a "Two typefaces" section to the Template README and to the `gum-theming`
  skill.

**Future (Gum-level, larger):** add a first-class secondary/body text slot to
`Styling` so visuals opt in through styling state rather than a hard-coded family
string. Bigger change; not required for this follow-up.

**Font sourcing note (belongs in README + skill).** KernSmith rasterizes **static**
TTFs and cannot select instances from a variable font. Many modern Google Fonts
(Baloo 2, Quicksand, …) ship variable-only in the `google/fonts` repo.
**google-webfonts-helper** (`gwfh.mranftl.com/api/fonts/<id>?...&formats=ttf`)
serves per-weight static cuts and is the fastest way to get them. Verify a file
is static by confirming it has no `fvar` table.

### Drop-shadow note (also a skill gap)

The skill frames drop shadows only as soft Gaussian halos (with the
sRGB alpha-bump caveat). Meadow's signature button shadow is the **opposite**: a
hard, opaque, `blur = 0` offset edge (CSS `0 4px 0 color`). It uses the same
native shadow API but, being opaque, needs **no** alpha bump. The Template's Rich
Button is the right place to demonstrate this, and the skill should add a sentence
distinguishing the two shadow flavors.

### Tasks

| ID | Task | Suggested owner |
| --- | --- | --- |
| T1 | Add a drop-shadow-capable fill variant to `TemplateShapes` (covers soft + hard) | coder |
| T2 | Make the canonical Template set the **Rich** style; add the non-registered `Variants/` gallery for the representative subset (Flat alternatives) | coder |
| T3 | Add `BodyFontFamily` + second-family registration + documented per-visual body-font opt-ins to the Template | coder |
| T4 | Template README: "Choosing a style" + "Two typefaces" + static-font sourcing | docs-writer |
| T5 | `gum-theming` skill: two-style gallery, multi-font convention, hard-vs-soft shadow, static-font source | docs-writer |

**Risks / open questions**

- Full per-control duplication is the obvious-but-wrong reading; the
  representative-subset gallery is the mitigation. Make the subset boundary
  explicit so it doesn't look like missing work.
- Picking which family is the styling default affects how many visuals need the
  override; decide per the Template's own demo design.

---

## Part 2 — Bubblegum: adopt the shape factory

### Why

The Template centralizes shape construction in `TemplateShapes` so a restyle
touches one place and each visual stays short. **Bubblegum predates / ignores
this** — every Bubblegum visual repeats the ~15-line `RectangleRuntime` /
`CircleRuntime` setup inline (its own `CreateFill` / `CreateBorder` /
`CreateFocusRing` / `CreateBody` per file). That is the exact inconsistency
called out while building Meadow: the skill points authors at Bubblegum as the
shadow/pill reference, but Bubblegum doesn't follow the factory pattern the
Template promotes, so cloning Bubblegum inherits the verbosity.

Bringing Bubblegum onto a factory makes it a cleaner reference and shrinks each
visual to its actual identity (colors, radii, state wiring).

### Target

- Add a `BubblegumShapes` factory to the Bubblegum theme, mirroring
  `TemplateShapes` (full-parent filled rect, stroked rect, offset focus ring, and
  the circle equivalents). Per the "each theme owns a self-contained, copyable
  set" convention, this is a Bubblegum-local file, not a shared library — see the
  open question below.
- The factory must include a **soft drop-shadow fill variant**, since Bubblegum's
  Button, ToggleButton, Window, and SliderThumb carry soft Gaussian shadows.
- Refactor each Bubblegum visual to call the factory for its **full-parent**
  fill / border / focus-ring shapes, deleting the corresponding inline `Create*`
  methods.

### Scope — what migrates and what stays inline

Migrate (these are plain full-parent shapes the factory already models):

- Button, ToggleButton (fill + focus ring; fill needs the shadow variant)
- TextBox / PasswordBox input decoration (fill + border + focus ring)
- ListBox, ScrollViewer, ComboBox, Tooltip (fill + border + focus ring)
- Window (fill + border; fill needs the shadow variant)
- Splitter, Menu (fill / separator)

Keep inline (the factory's full-parent helpers don't fit; migrating them would
need fixed-size / percentage overloads and isn't worth it now):

- CheckBox box + check glyph + dash, RadioButton circles (fixed-size, offset to
  the left of the label)
- Slider track + fill (fixed height, percentage width) and the slider/scrollbar
  thumbs (relative-to-parent inside their containers)
- ListBoxItem / MenuItem row fills

### Approach

- Pure, behavior-preserving refactor: **no visual change expected.** This is a
  `refactoring-specialist` task; follow the `refactoring-direction` and `tdd`
  skills.
- Migrate in small batches, building and running the showcase after each so any
  regression is caught immediately.
- Watch the focus-ring inset math: the factory's focus-ring helper grows the
  shape and bumps the corner radius by the inset to stay concentric — confirm it
  matches each visual's existing hand-rolled inset before deleting the inline
  version.

### Verification

- Build both backends (`AllLibraries.sln`) and run `MonoGameGumThemesShowcase`,
  Bubblegum theme; confirm every control looks identical to pre-refactor.
- No unit tests cover theme visuals today; verification is build + visual diff.

### Tasks

| ID | Task | Suggested owner |
| --- | --- | --- |
| T6 | Add `BubblegumShapes` (port from `TemplateShapes`; add soft drop-shadow fill variant) | refactoring-specialist |
| T7 | Migrate full-parent shapes in each Bubblegum visual to factory calls; delete dead inline `Create*` | refactoring-specialist |
| T8 | Build both backends + showcase smoke test; confirm zero visual regression | manual |

**Risk** — subtle positioning drift if a factory default differs from an inline
value (e.g. `StrokeWidthUnits`, origin/units). The factory was extracted from the
same pattern, so parity is expected, but verify per visual rather than trusting it.

### Open question — shared factory vs. per-theme copies

The current convention is that **each theme owns its own shapes file** so a theme
stays a self-contained, copyable reference. That means Bubblegum, Dark Pro,
Hazard, Neon, ForestGlade each carry a near-identical factory. If several themes
adopt it, consider whether a single shared internal helper is worth breaking the
self-contained convention. Recommend keeping per-theme copies for now (consistent
with the stated design) and revisiting only if the duplication becomes a real
maintenance cost. Migrating the other inline-shape themes is out of scope here.
