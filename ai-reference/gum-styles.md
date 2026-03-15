# Gum Style System Reference

## What the Styles Component Is

Projects created from the Forms template include a `Components/Styles.gucx` component — a centralized design token repository. It defines the color palette and typography scale in one place. All other components reference it by name rather than hardcoding values.

**Before editing any component's colors or typography, check whether a Styles component exists and whether the component references it. If it does, edit Styles — not the individual component.**

Note: the Styles component is optional. Some projects won't have one, and some users may have partially or fully broken from it by hardcoding values directly on components. Always inspect the actual files before assuming this pattern is in use.

---

## How It Works

The Styles component contains two containers:
- **Colors** — named `ColoredRectangle` instances (e.g. `Primary`, `White`, `Danger`), each storing `Red`, `Green`, `Blue`
- **TextStyles** — named `Text` instances (e.g. `Normal`, `Strong`, `H1`), each storing `FontSize`, `IsBold`, `IsItalic`

Components reference these by name using category state variables:
- `ColorCategoryState = "Primary"` — picks up that color's RGB from Styles at runtime
- `StyleCategoryState = "Strong"` — picks up that text style's font properties from Styles at runtime

These are set on child instances within a component (e.g. `Background.ColorCategoryState`, `TextInstance.StyleCategoryState`). Changing a value in Styles propagates to every component referencing that name.

---

## States and Overrides

Component behavior categories (e.g. `ButtonCategory`) define states like `Enabled`, `Disabled`, `Highlighted`, `Pushed`, `Focused`. Each state can assign different category names:

```
Enabled:   Background.ColorCategoryState = "Primary"
Disabled:  Background.ColorCategoryState = "DarkGray"
```

To change a component's appearance in a specific state, edit that state's category assignment — either in the component file (to change which style name is used) or in Styles (to change what that style name resolves to).

---

## Restyling Decision Tree

| Goal | Where to edit |
|------|--------------|
| Change a color or font globally | `Styles.gucx` |
| Change which style a component uses in a state | The component's behavior category state |
| Change layout or structure | The component file directly — not style-related |

---

## Font Files

After changing font sizes in Styles, run `gumcli fonts <project.gumx>` to generate missing bitmap font files. Never write `.fnt` files manually.
