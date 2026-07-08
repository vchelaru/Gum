# Migrating to 2026 May

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 April` to `2026 May`.

## Upgrading Gum Tool

{% tabs %}
{% tab title="Windows" %}


To upgrade the Gum tool:

1. Download Gum.zip from the release on Github:\
   [https://github.com/vchelaru/Gum/releases/tag/Release\_June\_11\_2026](https://github.com/vchelaru/Gum/releases/tag/Release_June_11_2026)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations
{% endtab %}

{% tab title="Linux" %}
Run the upgrade `gum upgrade` or `~/bin/gum upgrade`
{% endtab %}
{% endtabs %}

### Shape standard elements in the tool

New projects now seed the **Circle** and **Rectangle** standard elements, which provide a full fill, outline, gradient, drop shadow, dashed-stroke, and corner-radius surface. The older shape standard elements — **ColoredCircle**, **ColoredRectangle**, **RoundedRectangle**, and **SolidRectangle** — are being phased out, but existing projects that use them keep working; you do not need to replace them to upgrade.

{% hint style="info" %}
The tool only shows the new Circle and Rectangle fill, gradient, and drop shadow variable categories for version 3 (or later) projects. New projects use version 3 by default.
{% endhint %}

## Upgrading Runtime

Upgrade your Gum NuGet packages to version **2026.5.31.1**. For more information, see the NuGet packages for your particular platform:

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

The compatibility shims will remain in place until at least the December 2026 release. After that window, they will be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them.

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

### `MonoGameGum.AddToRoot(FrameworkElement)` forwarder removed — `CS0121` ambiguity fix

The convenience extension method `MonoGameGum.GraphicalUiElementExtensionMethods.AddToRoot(this FrameworkElement)` has been removed. It was a thin forwarder to the canonical `Gum.Forms.Controls.FrameworkElementExt.AddToRoot(this FrameworkElement)`. Because `GumService` lives in the `MonoGameGum` namespace, every consumer needs `using MonoGameGum;`, and because Forms code references `FrameworkElement`, `TextBox`, `Button`, etc., it also needs `using Gum.Forms.Controls;`. With both `using` directives in scope, the forwarder and the canonical method had identical signatures and the compiler reported `CS0121` ("ambiguous call") on every call site:

```
error CS0121: The call is ambiguous between the following methods or properties:
'MonoGameGum.GraphicalUiElementExtensionMethods.AddToRoot(...)' and
'Gum.Forms.Controls.FrameworkElementExt.AddToRoot(...)'
```

If you see `CS0121` on a call like `textBox.AddToRoot();`, add `using Gum.Forms.Controls;` to the file if it isn't already there. No call-site changes are needed.

❌ Old (compiles in April 2026, ambiguous in May 2026 once `Gum.Forms.Controls` is also in scope):

```csharp
using MonoGameGum;
// ...
textBox.AddToRoot(); // CS0121
```

✅ New (canonical extension method lives in `Gum.Forms.Controls`):

```csharp
using MonoGameGum;
using Gum.Forms.Controls;
// ...
textBox.AddToRoot();
```

**Note on `AddChild`:** The companion `AddChild(this GraphicalUiElement, FrameworkElement)` forwarder is **kept** in `MonoGameGum` because the MonoGameForms code generator emits `someRuntime.AddChild(someFormsControl)` patterns and the generated code does not (and cannot, without name-collision risk) import `Gum.Forms.Controls`.

If hand-written code that imports both `MonoGameGum` and `Gum.Forms.Controls` calls `.AddChild()` on a `GraphicalUiElement` with a `FrameworkElement` child, the same CS0121 will fire on that line for the same reason — two identically-shaped extensions in scope:

```
error CS0121: The call is ambiguous between the following methods or properties:
'MonoGameGum.GraphicalUiElementExtensionMethods.AddChild(...)' and
'Gum.Forms.Controls.FrameworkElementExt.AddChild(...)'
```

The fix is the same: drop `using MonoGameGum;` from that file (use `MonoGameGum.GumService.Default` fully-qualified if needed), or fully-qualify the AddChild call:

```csharp
Gum.Forms.Controls.FrameworkElementExt.AddChild(topLevelContainer, textBox);
```

This case is rare in hand-written code — `AddChild` is mostly emitted by codegen, where it's resolved by the forwarder and never sees the canonical.

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
circle.Width = 100;  // restore the previous Skia default (Radius is now obsolete)
circle.Height = 100;
circle.AddToRoot();
```

Other defaults preserved (no migration needed, listed for reference):

- `IsFilled` still defaults to `false`, `StrokeWidth` still defaults to `1` with `StrokeWidthUnits = ScreenPixel`.
- Dropshadow defaults (`DropshadowAlpha = 255`, `DropshadowOffsetY = 3`, `DropshadowBlur = 3`) are still seeded; inert until `HasDropshadow` is set to `true`. (Note: the plain `CircleRuntime` / `RectangleRuntime` expose a single isotropic `DropshadowBlur` — the older per-axis `DropshadowBlurX` / `DropshadowBlurY` properties only exist on the obsolete `ColoredCircleRuntime` / `RoundedRectangleRuntime` / `SkiaShapeRuntime` Skia surface.)

These richer effects — gradient, drop shadow, and dashed strokes — are built in natively on Skia and raylib. On MonoGame and KNI they are provided by the shape support package (`Gum.Shapes.MonoGame` / `Gum.Shapes.KNI`); FNA renders the outline only. See the [Shapes](../../code/standard-visuals/shapes-apos.shapes.md) page for the per-platform matrix.

### Shape runtime shims obsolete: `ColoredCircleRuntime`, `ColoredRectangleRuntime`, `SolidRectangleRuntime`, `RoundedRectangleRuntime`

`CircleRuntime` and `RectangleRuntime` now cover the full fill + outline (stroke) + corner-radius surface on every backend (MonoGame, FNA, KNI, Raylib, Skia). With that consolidation in place, the older shape runtime types are now `[Obsolete]` and will be removed in a future release.

On MonoGame, FNA, and KNI, the outline (`StrokeColor`, `StrokeWidth`, `StrokeWidthUnits`) renders out of the box, but the fill and the richer effects (`FillColor`, gradient, drop shadow, dashed stroke, anti-aliasing) require the shape support package. This package ships for MonoGame (`Gum.Shapes.MonoGame`) and KNI (`Gum.Shapes.KNI`) only; FNA has no shape support package, so on FNA only the outline renders. Without the package those properties round-trip but silently do not draw. On Skia and raylib the full surface is supported natively. See the [Shapes](../../code/standard-visuals/shapes-apos.shapes.md) page for the per-platform details.

Existing code continues to compile, but each reference now produces a `CS0618` compiler warning. The replacement types live alongside the obsolete ones in `Gum.GueDeriving`, so the only change at most call sites is the type name (and the property name, per the mapping below).

The obsoleted types and their replacements:

| Old type | Replacement | Color property mapping |
| --- | --- | --- |
| `ColoredCircleRuntime` (Apos + Skia) | `CircleRuntime` | `Color` → `FillColor` when `IsFilled` is `true` (the Apos default), `Color` → `StrokeColor` when `IsFilled` is `false` (see note below) |
| `ColoredRectangleRuntime` (core, used by all XNA-likes + Raylib + Skia) | `RectangleRuntime` | `Color` → `FillColor` |
| `SolidRectangleRuntime` (Skia) | `RectangleRuntime` | `Color` → `FillColor` |
| `RoundedRectangleRuntime` (Apos + Skia) | `RectangleRuntime` + `CornerRadius` | `Red` / `Green` / `Blue` → `FillColor`; existing `CornerRadius` maps 1:1 |

{% hint style="info" %}
**`ColoredCircleRuntime.Color` is a passthrough — what it paints depends on `IsFilled`.** The Apos constructor defaults `IsFilled = true`, so `Color` paints the **fill** in the common case. If your code sets `IsFilled = false` (outline-only circle), migrate `Color` to `StrokeColor` instead. If the circle is both filled and outlined, set `FillColor` and `StrokeColor` explicitly on the new `CircleRuntime`.
{% endhint %}

{% hint style="warning" %}
**`CircleRuntime` ships with a default 1 px white outline.** `ColoredCircleRuntime` had no outline, so a freshly-constructed `ColoredCircleRuntime` with only `Color` set rendered as a solid disc with no outline. `CircleRuntime` defaults to `StrokeColor = White` so cells that only set `FillColor` still get a visible outline — which means a literal `new CircleRuntime { FillColor = Color.Red }` renders as a red disc surrounded by a thin white ring. If you want the old solid-disc visual, suppress the outline explicitly:

```csharp
CircleRuntime circle = new();
circle.FillColor = Color.Red;
circle.StrokeWidth = 0; // disable the default white outline
```

The same caveat applies anywhere you migrate a fill-only `ColoredCircleRuntime` — including the Maui / Skia counter circles in the bundled samples, which add this line for the same reason.
{% endhint %}

### Breaking: `FillColor` / `StrokeColor` are now non-nullable

Earlier in this same unreleased cycle (issue #2790 / #2814) `CircleRuntime` and `RectangleRuntime` briefly exposed `FillColor` and `StrokeColor` as nullable (`Color?` on XNA-likes / Raylib, `SKColor?` on Skia), with `null` meaning "hide the fill / outline." Issue #2938 walks that back: the properties are non-nullable again, and visibility is gated by orthogonal knobs:

- **Hide fill** — set `IsFilled = false` (survives round-tripping the color value).
- **Hide stroke** — set `StrokeWidth = 0` (a zero-width stroke is already a no-op in every backend, so this expresses intent without a separate flag).

**Default visual is unchanged:** a freshly-constructed `CircleRuntime` / `RectangleRuntime` still renders as a stroke-only outline, preserving the pre-#2938 visual that existing sample code assumes ("construct + only set `StrokeColor`"). This is achieved by defaulting `FillColor` to transparent (alpha 0) while leaving `IsFilled = true`. Assigning `FillColor` to a visible color lights up the fill without flipping `IsFilled` — so existing code like `frame.FillColor = darkGray;` continues to work.

The runtimes also gain channel-decomposition setters on both colors so animations and the Gum tool's variable system can drive each channel independently:

- `FillRed` / `FillGreen` / `FillBlue` / `FillAlpha` — `int`, 0-255, compose into `FillColor`.
- `StrokeRed` / `StrokeGreen` / `StrokeBlue` / `StrokeAlpha` — `int`, 0-255, compose into `StrokeColor`.

❌ Old (the brief nullable API):

```csharp
circle.FillColor = null;    // hide the fill
circle.StrokeColor = null;  // hide the stroke
```

✅ New:

```csharp
circle.IsFilled = false;    // hide the fill
circle.StrokeWidth = 0;     // hide the stroke
```

This is a breaking change relative to the nullable form, but only against code written against an unreleased prerelease. Released callers were on the legacy single-`Color` API and are unaffected.

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

The `Gum.Analyzers` package ships an automated code fix (`GUM002`) — place the cursor on the warning, trigger the lightbulb (Ctrl+.), and choose **Replace '`ColoredCircleRuntime`' with '`CircleRuntime`'** (or the matching `RectangleRuntime` fix for the rectangle variants). The fix also renames `.Color` accesses on rewritten instances: `ColoredCircleRuntime.Color` → `CircleRuntime.StrokeColor` (matching the legacy outline-painting semantic), and `ColoredRectangleRuntime.Color` / `SolidRectangleRuntime.Color` → `RectangleRuntime.FillColor`. `RoundedRectangleRuntime` rewrites to `RectangleRuntime` with `CornerRadius` carried over unchanged. Use **Fix all in solution** to migrate the entire project at once.

The obsolete types will remain in place until at least the December 2026 release. After that window, they may be marked `[Obsolete(error: true)]` in a subsequent release, breaking compilation for any code still using them.

### Code generation targets the collapsed shapes at syntax version 2

Code generation (the Gum tool's **Code** tab and `gumcli codegen`) now emits the collapsed shape runtimes for the legacy shape standard elements. When the detected [syntax version](syntax-versions.md) is 2 or higher, the project's output library is MonoGame or MonoGameForms, and object instantiation is fully-in-code, regenerating produces:

| `.gumx` standard element | Generated type before | Generated type now |
| --- | --- | --- |
| `ColoredCircle` | `ColoredCircleRuntime` | `CircleRuntime` |
| `ColoredRectangle` | `ColoredRectangleRuntime` | `RectangleRuntime` |
| `RoundedRectangle` | `RoundedRectangleRuntime` | `RectangleRuntime` |

Generated color variables follow the mapping above: `Red`/`Green`/`Blue`/`Alpha` emit as the `Fill...` channel properties, or the `Stroke...` channels when the shape's `IsFilled` is false. Generated code preserves the legacy visual by assigning `IsFilled` and `StrokeWidth` (and `CornerRadius` for `RoundedRectangle`) explicitly, so a regenerated project renders the same as before.

After regenerating, update any hand-written code that references the generated fields:

* **Declared types** — code like `ColoredCircleRuntime circle = Screen.MyCircle;` no longer compiles, because the generated field is now typed `CircleRuntime`. The `GUM002` code fix updates these — use **Fix all in solution**.
* **Runtime casts** — code like `GetGraphicalUiElementByName("MyCircle") as ColoredCircleRuntime` still compiles but returns null at runtime, because the generated object is now a `CircleRuntime`. The analyzer cannot fix casts automatically; search for `as ColoredCircleRuntime` (and the rectangle equivalents) and change the cast to the collapsed type.

Projects using `FindByName` instantiation are unaffected — generated code keeps referencing the legacy types there, because runtime `.gumx` loading still creates them. Projects whose runtime reports a syntax version below 2 also keep the previous output. See [Syntax Versions](syntax-versions.md) for how the version is detected and how to override it.

### Legacy single-color members and `Radius` on `CircleRuntime` / `RectangleRuntime` are now `[Obsolete]`

`CircleRuntime` and `RectangleRuntime` are the new fill + stroke shapes, so their inherited single-color members — `Color`, `Red`, `Green`, `Blue`, `Alpha` — and `CircleRuntime.Radius` are superseded by the fill/stroke color API and by `Width` / `Height`. Each is now `[Obsolete]` on **every** backend (MonoGame, FNA, KNI, Raylib, Skia). Existing code keeps compiling, but each reference now produces a `CS0618` compiler warning.

{% hint style="info" %}
These members were already obsolete on the XNA-likes (MonoGame / FNA / KNI) earlier in this cycle; the May 2026 change extends the same deprecation to Raylib and Skia so the two shapes present one consistent API. If you build for Raylib or Skia you may see new `CS0618` warnings on code that compiled cleanly before.
{% endhint %}

The deprecation applies only to `CircleRuntime` / `RectangleRuntime`. The same members stay non-obsolete on the legacy single-color shapes (`ColoredCircleRuntime`, `RoundedRectangleRuntime`, `ArcRuntime`, etc.), where they remain the primary API — on Skia those shapes share the `SkiaShapeRuntime` base, which is deliberately left unchanged.

| Member | Replacement |
| --- | --- |
| `Color` | `StrokeColor` (the legacy `Color` painted the outline) — or `FillColor` for a filled shape |
| `Red` / `Green` / `Blue` / `Alpha` | `StrokeRed` / `StrokeGreen` / `StrokeBlue` / `StrokeAlpha` — or the matching `Fill…` channels for a filled shape |
| `CircleRuntime.Radius` | `Width` / `Height` (the setter already proxies `Width = Height = Radius * 2`) |

❌ Old:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Radius = 28;          // obsolete
circle.Color = Color.Yellow; // obsolete legacy single-color member (painted the outline)
circle.AddToRoot();
```

✅ New:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 56;
circle.Height = 56;
circle.StrokeColor = Color.Yellow; // or FillColor for a filled disc
circle.AddToRoot();
```

### Breaking: `UseGradient` no longer paints over a transparent fill

Issue #2956 — `UseGradient` is a *pattern* flag, not a *visibility* flag. A fill or outline whose effective color alpha is 0 (e.g. the default-transparent fill on a stroke-only plain `CircleRuntime` / `RectangleRuntime`) no longer paints its gradient. This brings the Apos.Shapes (MonoGame/FNA/KNI) and raylib backends in line with the SkiaGum backend, which has always enforced this naturally — `SKPaint.Color.alpha` modulates the shader output, so a transparent paint color suppresses the gradient.

Before the fix, the same code rendered differently across backends: Apos and raylib painted an opaque gradient on a fill the user had explicitly hidden via the documented default; Skia correctly suppressed it. After the fix, all three backends agree.

❌ Old (worked accidentally on Apos / raylib, silent no-op on Skia):

```csharp
var circle = new CircleRuntime();
circle.Width = 56;
circle.Height = 56;
circle.UseGradient = true;
circle.FillColor = Color.Black;
circle.Color2 = Color.White;
// Gradient appeared on the fill on Apos and raylib — even though FillColor
// defaults to transparent, which is supposed to hide the fill.
```

✅ New (paints the same on every backend):

```csharp
var circle = new CircleRuntime();
circle.Width = 56;
circle.Height = 56;
circle.FillColor = Color.Black;   // light the fill up and set the gradient start
circle.UseGradient = true;
circle.Color2 = Color.White;
```

Only the alpha of `FillColor` gates whether the fill paints at all. `StrokeColor` works the same way for the outline on backends that support gradient-on-stroke (Skia and Apos.Shapes; raylib's outline is solid only).

### Breaking: `Color1` removed from `CircleRuntime` / `RectangleRuntime` — gradient start is now the fill/stroke color

Issue #3009 — `CircleRuntime` and `RectangleRuntime` no longer have a standalone gradient **start** color. Previously the gradient ran between a dedicated `Color1` (`Red1` / `Green1` / `Blue1` / `Alpha1`) and `Color2`. Now the gradient **start** stop is the shape's active body color — `FillColor` when `IsFilled` is `true`, or `StrokeColor` when the shape is outline-only — and `Color2` (`Red2` / `Green2` / `Blue2` / `Alpha2`) remains the only standalone gradient color (the **end** stop). This removes the redundancy of having a fill color and a separate gradient-start color that had to be kept in sync.

On the XNA-likes (MonoGame / FNA / KNI) and raylib, `Color1` / `Red1` / `Green1` / `Blue1` / `Alpha1` are **removed** from `CircleRuntime` / `RectangleRuntime` — code referencing them no longer compiles. On Skia the same members are `[Obsolete(error: true)]`, so they also fail to compile.

To migrate, drop the `Color1` assignment and set the start color through `FillColor` (light up the fill so the gradient draws) — or through `StrokeColor` for an outline-only shape:

❌ Old:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 56;
circle.Height = 56;
circle.UseGradient = true;
circle.Color1 = Color.Black;   // gradient start
circle.Color2 = Color.White;   // gradient end
circle.AddToRoot();
```

✅ New:

```csharp
// Initialize
var circle = new CircleRuntime();
circle.Width = 56;
circle.Height = 56;
circle.IsFilled = true;          // ensure the fill (and thus the start color) draws
circle.FillColor = Color.Black;  // gradient start is now the fill color
circle.UseGradient = true;
circle.Color2 = Color.White;     // gradient end
circle.AddToRoot();
```

For an outline-only shape (`IsFilled = false`), set `StrokeColor` as the start color instead.

{% hint style="info" %}
**`ArcRuntime` keeps `Color1` as an obsolete alias.** Arc's gradient start is its primary `Color`, so `Color1` (`Red1` / `Green1` / `Blue1` / `Alpha1`) survives on `ArcRuntime` only as a `[Obsolete]` (warning) back-compat shim that maps onto `Color`. New Arc code should use `Color` for the gradient start. The alias is expected to be removed around November 2026.

The legacy `ColoredCircleRuntime` and `RoundedRectangleRuntime` are **unchanged** — they keep their real `Color1` gradient-start property.
{% endhint %}

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
| `ListBox` selection modifier keys (`ToggleSelectionModifierKey`, etc.) | XNA `Keys` | `Gum.Forms.Input.Keys` |

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

#### `ListBox` selection modifier keys

`ListBox.ToggleSelectionModifierKey`, `AlternateToggleSelectionModifierKey`, `RangeSelectionModifierKey`, and `AlternateRangeSelectionModifierKey` — the `public static` keys that gate multi-select (Ctrl / Shift by default) — are now typed `Gum.Forms.Input.Keys`. Only code that reassigns them needs the type swap.

❌ Old:

```csharp
ListBox.RangeSelectionModifierKey = Microsoft.Xna.Framework.Input.Keys.LeftAlt;
```

✅ New:

```csharp
ListBox.RangeSelectionModifierKey = Gum.Forms.Input.Keys.LeftAlt;
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