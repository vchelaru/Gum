# Migrating to 2026 May

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 April` to `2026 May`.

{% hint style="warning" %}
The `2026 May` version of Gum has not yet been released. This page is a work in progress and will be updated when the release is published. In the meantime, if you want to use the changes described below, you will need to build Gum from source.
{% endhint %}

## Upgrading Gum Tool

{% tabs %}
{% tab title="Windows" %}


To upgrade the Gum tool:

1. Download Gum.zip from the release on Github (link will be added once published)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

## Upgrading Runtime

The `2026.5` NuGet packages have not yet been published. Once released, upgrade your Gum NuGet packages to the new version. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* .NET MAUI - [https://www.nuget.org/packages/Gum.SkiaSharp.Maui](https://www.nuget.org/packages/Gum.SkiaSharp.Maui)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

If using GumCommon directly, you can update the GumCommon NuGet:

* GumCommon - [https://www.nuget.org/packages/FlatRedBall.GumCommon](https://www.nuget.org/packages/FlatRedBall.GumCommon)

If using the Apos.Shapes library, update the library for your target platform:

* Gum.Shapes.MonoGame - [https://www.nuget.org/packages/Gum.Shapes.MonoGame](https://www.nuget.org/packages/Gum.Shapes.MonoGame)
* Gum.Shapes.KNI - [https://www.nuget.org/packages/Gum.Shapes.KNI](https://www.nuget.org/packages/Gum.Shapes.KNI)

For other platforms you need to build Gum from source.

See below for breaking changes and updates.

### MonoGameGum.GueDeriving and SkiaGum.GueDeriving Namespaces Obsolete

Runtime classes (`SpriteRuntime`, `TextRuntime`, `ContainerRuntime`, etc.) have been unified under the single `Gum.GueDeriving` namespace, regardless of backend. The old `MonoGameGum.GueDeriving` and `SkiaGum.GueDeriving` namespaces still exist and still expose every runtime class, but each is now a `[Obsolete]` shim that forwards to the real class in `Gum.GueDeriving`.

Existing code continues to compile, but you will see compiler warnings (`CS0618`) on each old-namespace reference, plus an analyzer warning (`GUM001`) from the bundled `Gum.Analyzers` package. To migrate, change:

```csharp
using MonoGameGum.GueDeriving;
```

to:

```csharp
using Gum.GueDeriving;
```

The `Gum.Analyzers` package ships a one-click code fix for `using` directives — place the cursor on the warning, trigger the lightbulb (Ctrl+.), and choose **Change to 'using Gum.GueDeriving'**. Use **Fix all in solution** to migrate the entire project at once.

The compatibility shims will remain in place until at least the November 2026 release. After that window, they will be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them.

For full details, including handling of fully-qualified references and a `RenderingLibrary` namespace-shadowing gotcha, see [Syntax Version 1](syntax-version-1.md).

### FrameworkElement tree-traversal extensions

Forms controls (`Button`, `TextBox`, `Window`, etc.) gained extension methods for finding other controls and reaching into their underlying visuals without going through `.Visual` first:

```csharp
// Drop into the visual layer:
TextRuntime label = okButton.FindVisual<TextRuntime>()!;

// Find another Forms control:
Button cancel = dialog.Find<Button>("CancelButton")!;

// Walk ancestors:
Window? containing = nestedControl.Ancestors().OfType<Window>().FirstOrDefault();
```

This is purely additive — no existing code changes. See [Finding Elements](../../code/visual-tree/finding-elements.md) for the full set.

### GraphicalUiElement tree-traversal methods replaced

Five recursive lookup methods on `GraphicalUiElement` are now `[Obsolete]` in favor of LINQ-friendly extension methods that compose more cleanly. Existing calls keep working, but they now produce `CS0618` compiler warnings.

The replaced methods:

* `GetChildByNameRecursively(string)` → `FindByName(string)`
* `GetChildByTypeRecursively(Type)` → `Find<T>()`
* `GetParentByNameRecursively(string)` → `Ancestors().FirstOrDefault(a => a.Name == name)`
* `GetParentByTypeRecursively(Type)` → `Ancestors().OfType<T>().FirstOrDefault()`
* `FillListWithChildrenByTypeRecursively<T>(...)` → `Descendants().OfType<T>().ToList()`

The new generic methods (`Find<T>`, `OfType<T>`) match subclasses (`is T` semantics). The old methods only matched the exact type. If your code relied on the exact-type behavior, add an explicit `GetType() == typeof(T)` filter to the LINQ pipeline.

❌ Old:

```csharp
var textInstance = (TextRuntime)textBox.Visual.GetChildByNameRecursively("TextInstance")!;
```

✅ New:

```csharp
TextRuntime textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;
```

For the full set of new methods and how they compose with LINQ, see [Finding Elements](../../code/visual-tree/finding-elements.md).

### GraphicalUiElement → Forms control lookup methods replaced

Two extension methods on `GraphicalUiElement` for reaching from a visual down to a Forms control are now `[Obsolete]` in favor of a single unified replacement. Existing calls keep working but now produce `CS0618` compiler warnings.

The replaced methods:

* `GetFrameworkElementByName<T>(name)` → `FindFormsControl<T>(name)`
* `TryGetFrameworkElementByName<T>(name)` → `FindFormsControl<T>(name)`

Both old methods migrate to the same replacement because `FindFormsControl<T>` returns `T?` — it is nullable by design and returns `null` when no match is found, so the `Try` prefix is unnecessary. The old `Get...` overload's "throw on miss" behavior was already gated behind `FULL_DIAGNOSTICS` and silently returned `null` in release builds, so the new method matches the actual runtime behavior of both old overloads.

❌ Old:

```csharp
TextBox nameBox = screenRoot.GetFrameworkElementByName<TextBox>("NameTextBox");
Button? cancel = screenRoot.TryGetFrameworkElementByName<Button>("CancelButton");
```

✅ New:

```csharp
TextBox nameBox = screenRoot.FindFormsControl<TextBox>("NameTextBox")!;
Button? cancel = screenRoot.FindFormsControl<Button>("CancelButton");
```

For the full set of visual-to-Forms lookup methods, see [Finding Elements](../../code/visual-tree/finding-elements.md).

### Default visual changes from runtime unification

The ongoing runtime-unification work collapses per-backend runtime classes (MonoGame, FNA, KNI, Raylib, Skia, Apos.Shapes) into a single shared source file gated by `#if`. Where backends historically disagreed on constructor defaults, the unification picks one value and the others change to match — which means a freshly-constructed runtime can render visibly differently on the affected backends after upgrading.

This section catalogs each visible default change. **Saved Gum content takes precedence over constructor defaults**: if you instantiate runtimes from `.gucx` / `.glux` files and the property is set in your default state, no action is needed. The migrations below only affect code that constructs runtimes directly and relied on the old per-backend defaults.

Expect additional entries to land here as more runtimes are unified — if you upgrade and a visual looks different, check this section first.

#### ArcRuntime — Apos `IsEndRounded` now defaults to `false`

`ArcRuntime` was previously two separate per-backend classes (one for Apos.Shapes, one for SkiaGum) that had drifted apart over time. They are now a single shared source file with platform-specific code behind `#if SKIA`. Most of the unification is transparent, but **one default has changed visibly on the Apos backend**:

| Property | Apos before | Apos after | Skia before & after |
| --- | --- | --- | --- |
| `IsEndRounded` | `true` | `false` | `false` |

A freshly-constructed Apos `ArcRuntime` now renders with **flat end caps** instead of rounded. If your project relied on the rounded default — for example, when constructing arcs in code without setting `IsEndRounded` explicitly — those arcs will look different after upgrading.

To preserve the previous behavior, set `IsEndRounded = true` explicitly:

❌ Old (relied on the rounded default):

```csharp
ArcRuntime arc = new ArcRuntime();
arc.SweepAngle = 270;
arc.AddToRoot();
```

✅ New (explicit):

```csharp
ArcRuntime arc = new ArcRuntime();
arc.IsEndRounded = true;
arc.SweepAngle = 270;
arc.AddToRoot();
```

Other locked-in defaults from the same unification (no migration needed, listed for reference):

- `StrokeWidth` defaults to `10` on both backends (previously only Apos seeded this; freshly-constructed Skia `ArcRuntime` instances now render visibly without needing an explicit `StrokeWidth`).
- Dropshadow defaults (`DropshadowAlpha = 255`, `DropshadowOffsetY = 3`, etc.) are now seeded on both backends. These values are inert until `HasDropshadow` is set to `true`.

Dashed strokes (`StrokeDashLength`, `StrokeGapLength`) continue to render on Skia only — the underlying Apos.Shapes `Arc` primitive does not support dashing.

#### CircleRuntime — Skia default size is now 32×32 (was 100×100)

`CircleRuntime` is now a single shared source file across MonoGame, FNA, KNI, Raylib, and Skia. The Skia copy that previously lived under `SkiaGum/GueDeriving/` has been removed and its replacement is the file-linked shared runtime.

| Property | Skia before | Skia after | XNA-likes / Raylib before & after |
| --- | --- | --- | --- |
| `Width` | `100` | `32` | `32` |
| `Height` | `100` | `32` | `32` |
| `Radius` | (derived) `50` | `16` | `16` |

A freshly-constructed Skia `CircleRuntime` now renders at **roughly one-tenth the area** of the previous default. If your project instantiates `CircleRuntime` in code without setting `Width`/`Height` (or `Radius`) explicitly, those circles will appear smaller after upgrading.

❌ Old (relied on Skia's 100×100 default):

```csharp
CircleRuntime circle = new CircleRuntime();
circle.AddToRoot();
```

✅ New (explicit):

```csharp
CircleRuntime circle = new CircleRuntime();
circle.Radius = 50; // or set Width = 100; Height = 100;
circle.AddToRoot();
```

Other defaults preserved (no migration needed, listed for reference):

- `IsFilled` still defaults to `false`, `StrokeWidth` still defaults to `1` with `StrokeWidthUnits = ScreenPixel`.
- Dropshadow defaults (`DropshadowAlpha = 255`, `DropshadowOffsetY = 3`, `DropshadowBlurY = 3`) are still seeded; inert until `HasDropshadow` is set to `true`.

Gradients, dropshadow, and dashed strokes remain Skia-only — those features have no equivalent on the XNA-likes / Raylib backends and stay gated behind `#if SKIA` in the shared source.

### Shape runtime shims obsolete: `ColoredCircleRuntime`, `ColoredRectangleRuntime`, `SolidRectangleRuntime`, `RoundedRectangleRuntime`

`CircleRuntime` and `RectangleRuntime` now cover the full fill + stroke + corner-radius surface on every backend (MonoGame, FNA, KNI, Raylib, Skia, and Apos.Shapes) via a two-slot composition model — one renderable for the fill, a second parented under it for the stroke. With that consolidation in place, the older shape runtime types that wrapped a single renderable each are now `[Obsolete]` and will be removed in a future release.

Existing code continues to compile, but each reference now produces a `CS0618` compiler warning. The replacement types live alongside the obsolete ones in `Gum.GueDeriving`, so the only change at most call sites is the type name (and the property name, per the mapping below).

The obsoleted types and their replacements:

| Old type | Replacement | Color property mapping |
| --- | --- | --- |
| `ColoredCircleRuntime` (Apos + Skia) | `CircleRuntime` | `Color` → `FillColor` when `IsFilled` is `true` (the Apos default), `Color` → `StrokeColor` when `IsFilled` is `false` (see note below) |
| `ColoredRectangleRuntime` (core, used by all XNA-likes + Raylib + Skia) | `RectangleRuntime` | `Color` → `FillColor` |
| `SolidRectangleRuntime` (Skia) | `RectangleRuntime` | `Color` → `FillColor` |
| `RoundedRectangleRuntime` (Apos + Skia) | `RectangleRuntime` + `CornerRadius` | `Red` / `Green` / `Blue` → `FillColor`; existing `CornerRadius` maps 1:1 |

{% hint style="info" %}
**`ColoredCircleRuntime.Color` is a passthrough — the rendered slot depends on `IsFilled`.** The Apos constructor defaults `IsFilled = true`, so `Color` paints the **fill** in the common case. If your code sets `IsFilled = false` (outline-only circle), migrate `Color` to `StrokeColor` instead. If the circle is both filled and outlined, set `FillColor` and `StrokeColor` explicitly on the new `CircleRuntime`.
{% endhint %}

{% hint style="warning" %}
**`CircleRuntime` ships with a default 1 px white outline.** `ColoredCircleRuntime` had no stroke slot, so a freshly-constructed `ColoredCircleRuntime` with only `Color` set rendered as a solid disc with no outline. `CircleRuntime` (#2790 two-slot model) defaults to `StrokeColor = White` so cells that only set `FillColor` still get a visible outline — which means a literal `new CircleRuntime { FillColor = Color.Red }` renders as a red disc surrounded by a thin white ring. If you want the old solid-disc visual, suppress the outline explicitly:

```csharp
CircleRuntime circle = new();
circle.FillColor = Color.Red;
circle.StrokeColor = null; // disable the default white outline
```

The same caveat applies anywhere you migrate a fill-only `ColoredCircleRuntime` — including the Maui / Skia counter circles in the bundled samples, which add this line for the same reason.
{% endhint %}

❌ Old:

```csharp
var rect = new ColoredRectangleRuntime();
rect.Color = Color.Red;

var disc = new ColoredCircleRuntime(); // IsFilled = true by default
disc.Color = Color.Yellow;

var ring = new ColoredCircleRuntime();
ring.IsFilled = false;
ring.Color = Color.Yellow; // paints the stroke when IsFilled = false

var rounded = new RoundedRectangleRuntime();
rounded.Red = 0; rounded.Green = 128; rounded.Blue = 255;
rounded.CornerRadius = 8;
```

✅ New:

```csharp
var rect = new RectangleRuntime();
rect.FillColor = Color.Red;

var disc = new CircleRuntime();
disc.FillColor = Color.Yellow;

var ring = new CircleRuntime();
ring.StrokeColor = Color.Yellow;

var rounded = new RectangleRuntime();
rounded.FillColor = new Color(0, 128, 255);
rounded.CornerRadius = 8;
```

An automated code fix is planned via the `Gum.Analyzers` package (`GUM002`) — once published, place the cursor on the warning, trigger the lightbulb (Ctrl+.), and choose the **Change to `RectangleRuntime`** / **Change to `CircleRuntime`** fix. **Fix all in solution** will migrate the entire project at once.

The obsolete types will remain in place until at least the November 2026 release. After that window, they may be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them.

### Forms controls moved to GumCommon — input types widened

`FrameworkElement` and the rest of the Forms control infrastructure now live in `GumCommon` so any GumCommon consumer can use Forms directly, independent of the rendering backend. To make that compile cross-platform, a handful of Forms APIs that previously surfaced MonoGame-specific input types have been widened to platform-neutral abstractions in `Gum.Input` and `Gum.Forms.Input`.

Most user code is unaffected. The common path — reading `GumService.Keyboard` / `GumService.GamePads` and calling members on them — keeps working unchanged, and a new `XnaKeyboardExtensions` lets XNA `Keys` keep flowing into `IInputReceiverKeyboard.KeyDown(...)` and friends without a cast. The breaking changes below only affect code that handles Forms control events, builds `KeyCombo` values, or stores the `FrameworkElement.KeyboardsForUiControl` / `GamePadsForUiControl` collections in strongly-typed local variables.

| API | Before (MonoGame) | After |
| --- | --- | --- |
| `FrameworkElement.KeyboardsForUiControl` | `List<IInputReceiverKeyboardMonoGame>` | `List<IInputReceiverKeyboard>` |
| `FrameworkElement.GamePadsForUiControl` | `List<MonoGameGum.Input.GamePad>` | `List<IGamePad>` |
| `ComboBox.ControllerButtonPushed` event arg | XNA `Buttons` | `Gum.Input.GamepadButton` |
| `ListBox.ControllerButtonPushed` event arg | XNA `Buttons` | `Gum.Input.GamepadButton` |
| `KeyCombo.PushedKey` / `HeldKey` | XNA `Keys` | `Gum.Forms.Input.Keys` |
| `KeyEventArgs.Key` | XNA `Keys` | `Gum.Forms.Input.Keys` |

The `MonoGameGum.Input.GamePad` **class** itself is unchanged — `GumService.GamePads` still returns instances of it, and they still expose `XnaGamePad`, `Capabilities`, etc. Only the static type of the `FrameworkElement.GamePadsForUiControl` list element changed.

#### `ControllerButtonPushed` event handlers

Handlers attached to `ListBox.ControllerButtonPushed` or `ComboBox.ControllerButtonPushed` now receive `Gum.Input.GamepadButton`. The enum values are aligned with XNA `Buttons` (guarded by a unit test), so the migration is a type swap.

❌ Old:

```csharp
listBox.ControllerButtonPushed += (sender, button) =>
{
    if (button == Microsoft.Xna.Framework.Input.Buttons.A)
    {
        // ...
    }
};
```

✅ New:

```csharp
listBox.ControllerButtonPushed += (sender, button) =>
{
    if (button == Gum.Input.GamepadButton.A)
    {
        // ...
    }
};
```

#### `KeyCombo` and `KeyEventArgs`

`KeyCombo.PushedKey` / `HeldKey` and `KeyEventArgs.Key` are now typed `Gum.Forms.Input.Keys` instead of XNA `Keys`. The enum mirrors the XNA values, so any code that constructs combos or reads `KeyEventArgs.Key` needs the type swap.

❌ Old:

```csharp
var combo = new KeyCombo { PushedKey = Microsoft.Xna.Framework.Input.Keys.Enter };
textBox.KeyDown += (s, e) =>
{
    if (e.Key == Microsoft.Xna.Framework.Input.Keys.Escape) { /* ... */ }
};
```

✅ New:

```csharp
var combo = new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Enter };
textBox.KeyDown += (s, e) =>
{
    if (e.Key == Gum.Forms.Input.Keys.Escape) { /* ... */ }
};
```

#### `DPadDirection` namespace

The `MonoGameGum.Input.DPadDirection` enum has been deleted. The canonical copy in `Gum.Input.DPadDirection` (with identical values) is now the only one. Code that calls `gamepad.AsDPadPushed(DPadDirection.Up)` keeps working as long as `Gum.Input` is in scope — add `using Gum.Input;` and remove `using MonoGameGum.Input;` if you hit `CS0246: The type or namespace name 'DPadDirection' could not be found`.

#### `KeyboardsForUiControl` / `GamePadsForUiControl` collections

If you read these lists into a strongly-typed local variable, widen the type. Most code that just iterates and calls members through the interface keeps compiling.

❌ Old:

```csharp
List<IInputReceiverKeyboardMonoGame> keyboards = FrameworkElement.KeyboardsForUiControl;
List<MonoGameGum.Input.GamePad> gamepads = FrameworkElement.GamePadsForUiControl;
```

✅ New:

```csharp
List<IInputReceiverKeyboard> keyboards = FrameworkElement.KeyboardsForUiControl;
List<IGamePad> gamepads = FrameworkElement.GamePadsForUiControl;
```

If you need to reach a MonoGame-specific member on a list element, cast back: `(MonoGameGum.Input.GamePad)FrameworkElement.GamePadsForUiControl[0]`.
