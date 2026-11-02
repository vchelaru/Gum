# Migrating to 2026 November

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 October` to `2026 November`.

{% hint style="info" %}
Changes on this page are available in November 2026, or now if you build Gum from source.
{% endhint %}

## Moved, Renamed, and Obsolete APIs

Old names keep compiling as obsolete shims with a `CS0618` warning. The `GUM001` analyzer flags the old `using` lines, and its code fix updates them. See [Obsolete Shims Are Removed No Earlier Than November 2027](#obsolete-shims-are-removed-no-earlier-than-november-2027) for dates.

### Cursor and CursorExtensions Moved to Gum.Input

`Cursor` and `CursorExtensions` moved from `MonoGameGum.Input` to `Gum.Input` in MonoGame, KNI and FNA projects. Replace `using MonoGameGum.Input;` with `using Gum.Input;`, or change `MonoGameGum.Input.Cursor` to `Gum.Input.Cursor` where the full name is written.

The shim cannot cover these cases, which break:

* A variable declared with the old `Cursor` type no longer accepts `GumService.Default.Cursor` or `FrameworkElement.MainCursor` (`CS0266`).
* An override such as `UpdateThumbPositionToCursorDrag(Cursor cursor)` on a custom `Slider` or `ScrollBar` no longer matches the base signature (`CS0115`).
* `CursorExtensions` has no shim, so `cursor.GetEventFailureReason(...)` fails until `Gum.Input` is imported.
* A file importing both namespaces reports `CS0104` on `Cursor`. Remove the old `using`.

❌ Old:

```csharp
using MonoGameGum.Input;

// Initialize
Cursor cursor = GumService.Default.Cursor;
```

✅ New:

```csharp
using Gum.Input;

// Initialize
Cursor cursor = GumService.Default.Cursor;
```

### Shapes Package Types Moved to Gum.Renderables and Gum.Content

The `Gum.Shapes.MonoGame` and `Gum.Shapes.KNI` packages moved their public types:

| Old | New |
|---|---|
| `MonoGameAndGum.Renderables.ShapeRenderer` | `Gum.Renderables.ShapeRenderer` |
| `MonoGameAndGum.Renderables.RenderableShapeBase` | `Gum.Renderables.RenderableShapeBase` |
| `MonoGameAndGum.Renderables.Circle` | `Gum.Renderables.Circle` |
| `MonoGameAndGum.Renderables.RoundedRectangle` | `Gum.Renderables.RoundedRectangle` |
| `MonoGameAndGum.Content.ShapeSvgLoader` | `Gum.Content.ShapeSvgLoader` |

Replace `using MonoGameAndGum.Renderables;` with `using Gum.Renderables;` and `using MonoGameAndGum.Content;` with `using Gum.Content;`, or change the namespace where the full name is written, as the setup docs do.

The shims cannot cover these cases, which break:

* A variable declared with an old renderable type no longer accepts a shape Gum creates (`CS0266`), and `as MonoGameAndGum.Renderables.Circle` returns `null`.
* A file importing both namespaces reports `CS0104`. Remove the old `using`.

❌ Old:

```csharp
// Initialize
MonoGameAndGum.Renderables.ShapeRenderer.Self.Initialize();
```

✅ New:

```csharp
// Initialize
Gum.Renderables.ShapeRenderer.Self.Initialize();
```

### RenderableCreator Moved to Gum.Renderables

`RenderableCreator` moved from `MonoGameGum.Renderables` to `Gum.Renderables` in MonoGame, KNI and FNA, where raylib already had it. Replace `using MonoGameGum.Renderables;` with `using Gum.Renderables;`. A file importing both reports `CS0104`.

### OutputLibrary.MonoGameForms Renamed to GumForms

Rename `OutputLibrary.MonoGameForms` to `OutputLibrary.GumForms`. This affects only plugins or tools that reference it. Saved settings need no change.

### SvgRuntime.Document Renamed to Texture in MonoGame and KNI

In projects using `Gum.Shapes.MonoGame` or `Gum.Shapes.KNI`, `SvgRuntime.Document` is now `Texture`, the name SkiaGum already used. The old name fails with `CS1061`.

❌ Old:

```csharp
// Initialize
svg.Document = shapeSvg;
```

✅ New:

```csharp
// Initialize
svg.Texture = shapeSvg;
```

### SkiaResourceManager and EmbeddedResourceContentLoader Are Obsolete

In SkiaSharp projects (including .NET MAUI), use `RenderingLibrary.Content.ContentLoader`, the same loader MonoGame, KNI, FNA and raylib use. Gum installs it as the default, so a cast such as `LoaderManager.Self.ContentLoader as EmbeddedResourceContentLoader` now returns `null`.

* Move `CustomResourceAssembly` and `AdjustContentName` from `SkiaResourceManager` to `ContentLoader`.
* Replace `SkiaResourceManager` loading calls with `LoadContent<T>`, where `T` is `SKBitmap`, `SKSvg`, `Animation` or `SKTypeface`.
* Replace `new EmbeddedResourceContentLoader()` with `new ContentLoader()`.
* `GetSKBitmapFromUrl`, `CacheSvg` and the other cache helpers have no replacement and keep working until the type is removed.
* An `Svg` element's `SourceFile` now goes through `AdjustContentName`. If your `SourceFile` values already include the prefix, remove it.

❌ Old:

```csharp
// Initialize
SkiaGum.Content.SkiaResourceManager.CustomResourceAssembly = typeof(MauiProgram).Assembly;
SkiaGum.Content.SkiaResourceManager.AdjustContentName = (contentName) => "MyGame.Assets." + contentName;
```

✅ New:

```csharp
// Initialize
RenderingLibrary.Content.ContentLoader.CustomResourceAssembly = typeof(MauiProgram).Assembly;
RenderingLibrary.Content.ContentLoader.AdjustContentName = (contentName) => "MyGame.Assets." + contentName;
```

See [Content Using Embedded Resources](../../code/files-and-fonts/content-using-embedded-resources.md).

### MonoGameGum AddChild Extension Is Obsolete

The `AddChild` extension in the `MonoGameGum` and `RaylibGum` namespaces is obsolete. `gue.AddChild(formsControl)` resolves to the instance method and gives no warning. Only a call that names `GraphicalUiElementExtensionMethods.AddChild` as a static method reports `CS0618`. Call the instance method, or regenerate your code.

### XNA Keys Keyboard Overloads Are Obsolete

On MonoGame, KNI and FNA, the `KeyDown`, `KeyPushed`, `KeyReleased` and `KeyTyped` overloads that take the XNA `Keys` are obsolete, on `Keyboard`, `IInputReceiverKeyboardMonoGame` and `XnaKeyboardExtensions`. They become errors no earlier than March 2027 and are removed no earlier than July 2027. Call the overload that takes `Gum.Forms.Input.Keys`, written with its full name because `Keys` is ambiguous in a file that imports `Microsoft.Xna.Framework.Input`.

❌ Old:

```csharp
// Update
if (GumService.Default.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
{
    Jump();
}
```

✅ New:

```csharp
// Update
if (GumService.Default.Keyboard.KeyPushed(Gum.Forms.Input.Keys.Space))
{
    Jump();
}
```

### ViewModel.ChangeAndNotify Is Obsolete

Use `Set(ref field, value)` instead. It also watches the contents of a collection property. `ChangeAndNotify` stays until at least October 2027.

❌ Old:

```csharp
// Class scope
string _name;
public string Name
{
    get => _name;
    set => ChangeAndNotify(ref _name, value);
}
```

✅ New:

```csharp
// Class scope
string _name;
public string Name
{
    get => _name;
    set => Set(ref _name, value);
}
```

### Code Generation Updates Legacy Using Statements

Code generation now rewrites old namespaces in the **Project-wide Using Statements** and an element's **Using Statements** when it writes generated files, based on the project's [syntax version](syntax-versions.md):

| Saved line | Generated line | From syntax version |
|---|---|---|
| `using MonoGameGum.GueDeriving;` or `using SkiaGum.GueDeriving;` | `using Gum.GueDeriving;` | 1 |
| `using MonoGameGum;` | `using Gum;` | 3 |

A rewritten line is dropped if the list already imports that namespace. Saved settings are unchanged. Regenerate your code, and edit the **Code** tab if you also want the old names gone from your settings.

### Obsolete Shims Are Removed No Earlier Than November 2027

Unless a row says otherwise, each shim is a `CS0618` warning today, becomes `[Obsolete(error: true)]` no earlier than May 2027, and is removed no earlier than November 2027. Gum announces the actual release in that month's migration page.

| Old name | Replacement | Error no earlier than |
|---|---|---|
| `MonoGameGum.GueDeriving` and `SkiaGum.GueDeriving` runtimes (`TextRuntime`, `ContainerRuntime`, `SpriteRuntime`, the shape runtimes, and so on) | `Gum.GueDeriving` | May 2027 |
| `MonoGameGum.ElementSaveExtensionMethods.ToGraphicalUiElement` | `Gum.ElementSaveExtensionMethods` | May 2027 |
| `MonoGameGum.Renderables` default renderables | `Gum.Renderables` | May 2027 |
| `MonoGameGum.ExtensionMethods.IReadOnlyListExtensionMethods` | `Gum.ExtensionMethods` | May 2027 |
| `MonoGameGum.Input.Cursor` | `Gum.Input.Cursor` | May 2027 |
| `MonoGameGum.GraphicalUiElementExtensionMethods.AddChild` | `GraphicalUiElement.AddChild` | May 2027 |
| `MonoGameGum.Renderables.RenderableCreator` | `Gum.Renderables.RenderableCreator` | May 2027 |
| `MonoGameAndGum.Renderables` and `MonoGameAndGum.Content` types | `Gum.Renderables` and `Gum.Content` | May 2027 |
| `OutputLibrary.MonoGameForms` | `OutputLibrary.GumForms` | May 2027 |
| `SkiaGum.Content.SkiaResourceManager` and `EmbeddedResourceContentLoader` | `RenderingLibrary.Content.ContentLoader` | April 2027 |
| `MonoGameGum.GumService` and `RaylibGum.GumService` | `Gum.GumService` | Never |

The `GumService` shim stays because `GumService.Default` returns it. Change declarations to `Gum.GumService` whenever you like.

{% hint style="info" %}
The properties on the default Forms visuals (`TextInstance`, `Background`, and similar) are still typed as the obsolete `MonoGameGum.GueDeriving` shims. Gum changes them to the unified types in the release that removes the shims, so a variable holding one of them needs the new type by then.
{% endhint %}

To find what still uses an old name, build with the bundled `Gum.Analyzers` package and look for `GUM001` and `CS0618`.

## Missing Files and Load Errors

Gum now fails loudly instead of drawing nothing in these cases. Fix the file or path. To keep the old silent behavior, set `ConsumeSilently` after Gum initializes. Gum then raises `PropertyAssignmentError` with the file name.

```csharp
// Initialize
Gum.Wireframe.GraphicalUiElement.MissingFileBehavior =
    Gum.Wireframe.MissingFileBehavior.ConsumeSilently;
```

### raylib Throws When an Image Cannot Be Decoded

raylib treats an image it cannot decode like a missing file. A `.jpg` Source File, for example, loaded as an empty texture before and now throws, because the stock raylib native has no JPEG decoder. Convert the image to PNG, or register an `IImageDecoder` for its extension before Gum loads the project. See [Custom Image Formats](../../code/files-and-fonts/custom-image-formats.md).

### SkiaGum Throws When a File Is Missing or an Image Cannot Be Decoded

SkiaGum now defaults `MissingFileBehavior` to `ThrowException`, like the other runtimes. A missing Sprite or NineSlice image, font file, Svg or LottieAnimation file, or an image SkiaSharp cannot decode, throws a `FileNotFoundException` naming the file when the element is created. Under `ConsumeSilently`, a Sprite or `SvgRuntime` has a null `Texture` and a `LottieAnimationRuntime` a null `Animation`.

### SvgRuntime Throws When Its SVG Cannot Be Loaded

On MonoGame and KNI, an `SvgRuntime` whose `SourceFile` is missing or not a readable SVG now follows `MissingFileBehavior`, as does `ShapeSvgLoader.Load`. Before, it drew nothing and reported nothing. Under `ConsumeSilently`, `Texture` is null.

### An .achx Frame Texture That Fails to Load Is a Blank Frame

A frame texture that is missing or cannot be decoded follows `MissingFileBehavior` on every runtime. On MonoGame, KNI and FNA, a missing frame texture used to be silent under the default and now throws when the `.achx` loads.

Under `ConsumeSilently`, that frame has a null `Texture` and draws nothing, `PropertyAssignmentError` is raised once per texture, and `AnimationChains` is no longer null. Check `Texture` on the frames instead. Under `ThrowException`, the `FileNotFoundException` is the `InnerException`.

### SourceShaderFile Needs a Shader Resolver

A container with a `SourceShaderFile` now fails to load unless a shader resolver handles the file. Before, MonoGame, KNI, FNA and raylib rendered it without the shader and reported nothing. This includes a container with `IsRenderTarget` set to `false`.

Add the ShadowDusk package for your runtime (`Gum.MonoGame.ShadowDusk`, `Gum.KNI.ShadowDusk`, `Gum.FNA.ShadowDusk`, or `Gum.raylib.ShadowDusk`) and call `UseShadowDusk()` once at startup, after `Initialize` and before any element that uses a `SourceShaderFile` is created. A later call does not retry a file that already failed. See [Loading Shader Files With SourceShaderFile](../../code/standard-visuals/containerruntime.md#loading-shader-files-with-sourceshaderfile).

❌ Old:

```csharp
// Initialize
GumUI.Initialize(this);
// The screen's SourceShaderFile silently rendered unshaded.
```

✅ New:

```csharp
// Initialize
GumUI.Initialize(this);
GumUI.UseShadowDusk();
```

### Generated Visuals Throw When Their Element Is Not Loaded

At [syntax version 7](syntax-versions.md), a generated MonoGame, raylib, or Silk.NET runtime class throws from its constructor when the loaded project has no element by its name. Before, it created an empty visual. This matters if you create generated visuals before loading the Gum project, such as in unit tests. Load the project first.

### References That Cannot Be Applied Are Reported

A reference whose result does not fit the variable on the left is now reported, and the variable keeps its value. This covers `NaN` or infinity into a `float` or `double`, and with `Gum.Expressions` a `bool` or text into a number, or a number outside the range of an `int`. `MinWidth`, `MaxWidth`, `MinHeight` and `MaxHeight` are the exception: `NaN` or infinity now means no limit.

See [Casting and Type Considerations](../../code/styling/runtime-variable-references.md#casting-and-type-considerations).

## Layout

Open your screens and components in the Gum tool and check the stacks and containers below.

### Stacked Children Ignore Their Origin on the Stacking Axis

A child after the first in a stack now ignores its origin on the stacking axis: `Y Origin` is treated as `Top` in a `Top to Bottom Stack`, and `X Origin` as `Left` in a `Left to Right Stack`. Before, a `Center` origin pulled the child back over its previous sibling by half its size, and `Bottom`, `Baseline` or `Right` by its full size. The first child, the origin on the other axis, and `X`/`Y` spacing are unchanged.

This affects stacked children (not the first) with those origins, including children set with `Anchor`. To keep an overlap, give the child a negative `Y` (or `X`).

❌ Old (overlaps the previous sibling by half its height):

```csharp
// Initialize
var child = new ContainerRuntime();
child.Height = 50;
child.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
stackPanel.AddChild(child);
```

✅ New (same overlap, stated explicitly):

```csharp
// Initialize
var child = new ContainerRuntime();
child.Height = 50;
child.Y = -25;
stackPanel.AddChild(child);
```

If you used the overlap to place children side by side, point the stack the right way instead. For example, two children with `Y Units` of `Pixels From Bottom` and `Y Origin` of `Bottom` used to share a row. Set `Children Layout` to `Left to Right Stack` in the tool, or in code:

```csharp
// Initialize
stackPanel.Orientation = Gum.Forms.Controls.Orientation.Horizontal;
```

See [Children Layout](../gum-elements/container/children-layout.md#stacking-and-children-origin).

### Stacks Without Wrapping Grow to Their Max

A non-wrapping stack sized to its children (`Relative to Children` or `Relative to Max of Children or Parent`) with a `Max Height` or `Max Width` now grows all the way to that max when its children need more. Before, it stopped at the last child that fit. Three 50 tall children in a stack with a `Max Height` of `120` used to measure `100` and now measure `120`. Child positions are unchanged.

If you want the old size, lower the max to the size of the children that fit, or check `Wraps Children`. See [Children Layout](../gum-elements/container/children-layout.md#stacking-and-container-height-units-and-width-units).

### Wrapping Stacks Sized to Their Children Measure Their Widest Row

A wrapping stack sized to its children with a max is now as large as its widest row or column. Before, it measured only its first row, so later rows wrapped against that narrower size and a wider child extended past the stack. Children `100`, `120` and `60` wide in a `Max Width` of `200` used to measure `100`, and now measure `180` with the last two sharing a row. A first child wider than the max now measures as wide as the max, not `0`.

If you want the old size, lower the max to the width of the first row. See [Wraps Children](../gum-elements/container/wraps-children.md#relative-to-children-and-max-width-and-max-height).

### Wrapping Stacks Position Children Within Their Row or Column

In a wrapping stack, each row or column is now the parent of its children on the other axis. In a `Left to Right Stack`, a child's `Y Units` and `Y Origin` position it within its row, and `Percentage` is a percentage of the row. The same applies to `X` in a `Top to Bottom Stack`. Before, they positioned the child within the whole stack, so `Pixels From Center` centered a child on the stack whichever row it was in.

Rows and columns also count children the way a container sized to its children does: a `Percentage`-positioned child and any part outside the row do not count. The default units (`Pixels From Top`, or `Pixels From Left`) and size units are unchanged.

If you want a child placed relative to the whole stack, move it out of the stack into a container that does not stack. See [Wraps Children](../gum-elements/container/wraps-children.md#positioning-children-within-a-row-or-column).

### Wrapping Stacks With Negative Padding Keep Their Measured Rows

A wrapping stack sized to its children now breaks rows where it measured them, so with negative padding a row hangs past the edge instead of wrapping again. Children `100`, `90` and `50` wide with a `Width` of `-20` and a `Max Width` of `200` used to lay out as `[100]` and `[90, 50]`, and now as `[100, 90]` and `[50]`. With no max, nothing wraps. To get the old rows, raise the padding to `0` or lower the max.

### Fixed Size AutoGrids Overflow Instead of Packing Extra Rows

An `Auto Grid Horizontal` or `Auto Grid Vertical` with a fixed size on its growing axis now lets extra children spill outside the grid. Before, the extra rows were squeezed into the grid and the children overlapped. Raise the cell counts to hold every child, or set `Relative to Children` on the growing axis.

### Containers Count a Child's Min When It Uses Relative to Max of Children or Parent

A container with `Height Units` of `Relative to Children` now counts a `Relative to Max of Children or Parent` child at no less than that child's `Min Height` (and likewise for `Width Units` and `Min Width`). Before, a child with a `Min Height` of `80` and 30 tall content was measured as `30`, so it extended past its container. If you want the old size, lower or clear the child's `Min Height` or `Min Width`. See [Height Units](../gum-elements/general-properties/height-units.md#relative-to-max-of-children-or-parent).

## Input and Events

### Dragging Is Raised Every Frame While the Button Is Held

`InteractiveGue.Dragging` is now raised every frame the primary button is held after pushing on the visual, even when the cursor did not move. Before, it was raised only on frames where the cursor moved. This matters only if a handler does more than read the cursor position, such as accumulating a value each call. To run only on movement, check the cursor's `XChange` and `YChange`. `RollOver` and the Forms controls are unchanged.

```csharp
// Initialize
button.Visual.Dragging += (_, _) =>
{
    if (GumUI.Cursor.XChange != 0 || GumUI.Cursor.YChange != 0)
    {
        movesCounted++;
    }
};
```

See [Visual Events](../../code/events-and-interactivity/visual-events.md).

### RollOff Is Raised Before RollOn

When the cursor moves straight from one `InteractiveGue` to another, `RollOff` now runs on the old element before `RollOn` on the new one. Before, `RollOn` came first. A `RollOn` handler in the same frame now sees a `cursor.VisualOver` assigned by a `RollOff` handler, so check any code that relied on `RollOn` running first.

### AnimationChainFinished Replaces AnimationChainCycled at the End of a Non-Looping Chain

`AnimationChainCycled` is now raised only when a looping chain wraps around. A chain that does not loop raises the new `AnimationChainFinished` once at its end, when `Animate` becomes `false`. `SpriteRuntime`, `Sprite`, `NineSlice` and `AnimationChainLogic` all have it. A non-looping chain also stops on its first frame when it plays backward (negative `AnimationChainSpeed`), where before it held the frame and raised nothing.

If you subscribed to `AnimationChainCycled` on a non-looping chain, such as to remove a sprite after a death animation, subscribe to `AnimationChainFinished` instead.

❌ Old:

```csharp
// Initialize
sprite.IsAnimationChainLooping = false;
sprite.AnimationChainCycled += () => sprite.RemoveFromRoot();
```

✅ New:

```csharp
// Initialize
sprite.IsAnimationChainLooping = false;
sprite.AnimationChainFinished += () => sprite.RemoveFromRoot();
```

See [Animation Chains](../../code/files-and-fonts/animation-chains.md#looping-and-end-of-chain-events).

### A Hidden Control Refuses Focus

Setting `IsFocused` to `true` on a control that is not visible (it or a parent has `IsVisible` set to `false`) now leaves it `false`, the same as a disabled control. Show the control first, then focus it.

❌ Old:

```csharp
// Initialize
okButton.IsFocused = true;
dialog.IsVisible = true;
```

✅ New:

```csharp
// Initialize
dialog.IsVisible = true;
okButton.IsFocused = true;
```

### ListBox Left and Right No Longer Step a Vertical List

When a `ListBox` has item-level focus (`DoListItemsHaveFocus` is `true`), the axis the list does not use now leaves the list, like the perpendicular axis of a `Slider`. Left and right no longer step a vertical list, and up and down no longer step a horizontal one. Focus moves as it does at top-level focus, honoring any `SpatialNavigationLeft`, `SpatialNavigationRight`, `SpatialNavigationUp` or `SpatialNavigationDown` on the `ListBox`, and `SelectedIndex` is kept. Arrow keys on the list's own axis, wrapping lists, and the `ComboBox` drop-down are unchanged.

### raylib and Sokol Repeat Held Keys With Gum's Timing

On raylib and Sokol, held navigation keys (arrows, Backspace, Delete) now repeat after `RepeatDelay` (500 ms) at `RepeatRate` (70 ms), as on the other runtimes, instead of using the operating system's repeat for `KeyTyped` and `KeysTyped`. Typed characters, including Enter in a multi-line `TextBox` on raylib, still use the operating system's repeat.

On Windows and macOS, `Initialize` starts from the system keyboard settings (read once). On macOS they apply only after the user has changed them in System Settings. To use fixed values on every machine, set `RepeatDelay` and `RepeatRate` after `Initialize`.

`IInputReceiverKeyboard` now declares `RepeatDelay` and `RepeatRate` as default members, so existing implementers keep compiling.

## Forms

### RefreshStyles Restores Only Text Set in Code

`RefreshStyles` now restores a control's text only when code supplied it: through the property, `SetTextNoTranslate` (or the `SetHeaderNoTranslate` and `SetPlaceholderNoTranslate` equivalents), a binding, or typing in a `TextBox`. This covers `Text` on `Button`, `Label`, `ToggleButton` (so `CheckBox` and `RadioButton`), `TextBox` and `PasswordBox`, `Header` on `Expander` and `MenuItem`, and `Placeholder` on `TextBox` and `PasswordBox`.

Before, those controls restored their old text even if the Visual authored it, which overwrote text changed on the Visual, for example through a variable reference. Text set in code also no longer carries over to a new Visual, so set it again after assigning one. `ComboBox` still restores its displayed text.

See [Runtime Variable References](../../code/styling/runtime-variable-references.md#text-after-refreshstyles).

### Slider, ScrollBar, and ScrollViewer Step Defaults Changed in the Gum Tool

New Forms controls added in the tool now get the same step sizes as controls created in code:

| Control | Property | Before | Now |
|---|---|---|---|
| `Slider` | `SmallChange` | 1 | 5 |
| `Slider` | `LargeChange` | 10 | 25 |
| `ScrollBar` | `SmallChange` | 1 | 10 |
| `ScrollViewer` | `SmallChange` | 1 | 10 |

If you rely on a step size, set `SmallChange` or `LargeChange` in the **Behavior** category.

### Step Sizes Survive a Visual Swap

Assigning a new Visual to a `ScrollBar`, `ScrollViewer` or `Slider` no longer replaces `SmallChange` and `LargeChange`. A value set in code, authored on the Visual, or applied from a behavior is kept, and `LargeChange` on a `ScrollBar` and `ScrollViewer` has no default, so it follows the viewport until you assign it. A project from before this version still declares a `LargeChange` of 10 on its scroll controls, and that value now stays at 10. Set the step sizes you want after assigning the Visual. See [Properties Set by Forms Controls](../../code/styling/properties-set-by-forms-controls.md#scroll-step-sizes-survive-a-visual-swap).

### ScrollBar Orientation Is a Behavior Property

A ScrollBar instance now has an `Orientation` setting in the **Behavior** category, and `OrientationCategoryState` is hidden. Before, setting the orientation state in the tool changed the look but the control stayed vertical in your game.

On load, Gum sets `Orientation` to `Horizontal` on any ScrollBar whose default state sets `OrientationCategoryState` to `Horizontal`. You do not need to save again. A project whose `ScrollBarBehavior` lacks the `Orientation` property is not migrated. Open the project in the tool and check that your horizontal bars show `Horizontal`. See [Behaviors](../gum-elements/behaviors/README.md#scrollbar-orientation).

### Hot Reload Keeps Values Your Code Set

A [hot reload](../../code/debugging/hot-reload.md) now applies only the variables the project changed. Before, it applied the whole default state of a component instance, so a `Width` your code set on a tool-authored `Button` went back to the component's value on every reload. If your game relied on a reload resetting a code-set value, set it again in `HotReloadCompleted` or change the variable in the Gum project.

## Rendering and Defaults

### Default Font Is Liberation Sans

On MonoGame, KNI and FNA, text that never sets a font now draws with Liberation Sans instead of Arial. Glyph shapes differ slightly, a few characters are 1 pixel wider or narrower, and text is about 1 pixel shorter. Set an explicit font to keep a specific look.

### MonoGame Gradients Blend in Straight sRGB

Gradients on MonoGame, KNI and FNA now blend in straight sRGB to match Skia and raylib. Mid-tones look darker than before, and no setting restores the old look.

### Shape DropshadowBlur Defaults to 3

A shape whose project never sets `DropshadowBlur` now uses `3` on every runtime, the value the tool authors. It was `0` for the Arc, RoundedRectangle, ColoredCircle and Line on MonoGame, KNI and FNA, and for the Circle and Rectangle on raylib, so their dropshadows were hard-edged. To keep the hard edge, set it to `0`:

```csharp
// Initialize
var rectangle = new RectangleRuntime();
rectangle.HasDropshadow = true;
rectangle.DropshadowBlur = 0;
```

### Skia Circle and Rectangle Obsolete Colors Set the Outline

In SkiaSharp projects, the obsolete `Color`, `Red`, `Green`, `Blue` and `Alpha` on `CircleRuntime` and `RectangleRuntime` now color the outline, as on MonoGame and Raylib. Before, they colored the fill. Use `FillColor` (or `FillRed`, `FillGreen`, `FillBlue`, `FillAlpha`) for the fill and `StrokeColor` for the outline.

❌ Old:

```csharp
// Initialize
circle.Color = SkiaSharp.SKColors.Red;
```

✅ New:

```csharp
// Initialize
circle.FillColor = SkiaSharp.SKColors.Red;
```

### Raylib TTF Text Draws at Its FontSize

On Raylib, text using a `.ttf` font without an `InMemoryFontCreator` such as KernSmith now draws with its em equal to `FontSize`, as on MonoGame and Skia. Before, Gum loaded these fonts at a fixed size of 24. Most `.ttf` text now draws larger, and text with a `FontSize` other than 24 changes size. KernSmith and `.fnt` text is unchanged. Run your game and lower `FontSize` where text is now too large.

### Styled Text Reports Its Drawn Height

Styled text that combines a [FontScale] or [FontSize] run with a `LineHeightMultiplier` other than 1 now reports its drawn height. On MonoGame, KNI, FNA and Raylib it gets shorter with a multiplier above 1 and taller below 1. On Skia, lines with a size run use the same spacing at any multiplier, a trailing newline adds its empty line to the height, and text without a size or font run measures exactly `LineHeightMultiplier` times its line height. Anything laid out around such text can shift.

### Skia Render Target Shaders Read SpriteTexture

A Skia `SourceShaderFile` in a `.sksl` file must now name its input `SpriteTexture`, the name MonoGame shaders use, instead of `inputImage`. A shader that still declares `inputImage` throws an `ArgumentOutOfRangeException` saying `SpriteTexture` was not found. Rename the input and every use of it.

❌ Old:

```glsl
uniform shader inputImage;
half4 main(float2 coord) {
    return inputImage.eval(coord);
}
```

✅ New:

```glsl
uniform shader SpriteTexture;
half4 main(float2 coord) {
    return SpriteTexture.eval(coord);
}
```

See [Container Runtime](../../code/standard-visuals/containerruntime.md).

## Code Generation

### Code Generation Emits Variables Hidden From Instances

Code generation now emits a variable a control hides from its instances, unless a behavior reference drives it. For example, a `Window` instance that sets `ClipsChildren` now gets that line in generated code. Before, hidden variables were skipped, though the tool still showed the value and your game still loaded it. A project from before the control hid the variable can hold one you do not know about. Regenerate and review the changes, and remove stale values from the instance in the tool.

### FullyInCode Forms Code Generation

After you regenerate Gum Forms code generated with **FullyInCode**, fix these errors if they appear:

* `CS0111` on `ReactToVisualChanged`: your partial class overrides it and generated code now does too. Move your code into `CustomInitialize`, which runs once. Code that must also run when `Visual` is replaced belongs in a class that derives from the component and overrides `ReactToVisualChanged`.
* `CS0114` on `ApplyDefaultVariables` or `InitializeInstancesFromVisual`: a class you derived from a generated component declares a method with that name. Add `override` and call the base method first, or rename yours.

### Unity Startup Hook Is Skipped When GUM_NO_UNITY_ENGINE Is Defined

If you define `GUM_NO_UNITY_ENGINE` (for an assembly with **No Engine References**), generated code omits the `UnityEngine.RuntimeInitializeOnLoadMethod` hook and uses `[ModuleInitializer]` instead. Regenerate your code. Unity does not run `[ModuleInitializer]` methods, so call the generated `RegisterRuntimeType()` methods and `StandardElementsCodeGenRegistration.RegisterFallbackStandardElements()` yourself at startup. See [Unity](../../code/getting-started/setup/adding-initializing-gum/unity.md#generated-code-in-an-assembly-with-no-engine-references).

## Packages

### Skia Packages Require SkiaSharp 4

`Gum.SkiaSharp`, `Gum.SkiaSharp.Maui`, and the Silk.NET and Stride Skia packages now depend on SkiaSharp `4.153.1`, and Gum uses the stock `Topten.RichTextKit` instead of the retired `Gum.Topten.RichTextKit` fork. If your project references SkiaSharp:

1. Update `SkiaSharp` and related packages (for example `SkiaSharp.Views`) to `4.153.1`.
2. Remove any reference to `Gum.Topten.RichTextKit`.
3. Move your drawing code off members SkiaSharp 4 marks `[Obsolete]`: the `SKPath` mutators (use `SKPathBuilder`) and the `DrawBitmap` and `DrawImage` overloads without `SKSamplingOptions`.

### KernSmith.MonoGameGum No Longer References Desktop MonoGame

`KernSmith.MonoGameGum` no longer brings in `MonoGame.Framework.DesktopGL`. If your app got MonoGame only through this package, add the MonoGame package for your platform.

`KernSmith.GumCommon` and `KernSmith.MonoGameGum` also add iOS and Android builds, which use the `StbTrueType` rasterizer by default. Pass a backend to `GumFontGenerator` to choose another. To include the mobile builds when building from source, pass `-p:IncludeIOS=true` or `-p:IncludeAndroid=true`.
