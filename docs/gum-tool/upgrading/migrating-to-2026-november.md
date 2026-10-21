# Migrating to 2026 November

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 October` to `2026 November`.

## Breaking Changes and Migrations

### Cursor and CursorExtensions Moved to Gum.Input

`Cursor` and `CursorExtensions` moved from `MonoGameGum.Input` to `Gum.Input` in MonoGame, KNI and FNA projects. This finishes the move that started in `2026 June`, so `Gum.Input` now holds every input type, matching raylib and the other runtimes.

`MonoGameGum.Input.Cursor` remains as an obsolete shim that derives from `Gum.Input.Cursor`. Code that creates a `Cursor` or derives from it keeps compiling, with a `CS0618` warning. The shim cannot cover everything, so these cases break:

* A variable declared with the old type no longer accepts a cursor that Gum returns. `GumService.Default.Cursor` and `FrameworkElement.MainCursor` are `Gum.Input.Cursor`, and C# cannot assign a base class value to a derived class variable (`CS0266`).
* A method that overrides a Gum method taking a `Cursor`, such as `UpdateThumbPositionToCursorDrag(Cursor cursor)` on a custom `Slider` or `ScrollBar`, no longer matches the base signature (`CS0115`).
* `CursorExtensions` has no shim, so calls such as `cursor.GetEventFailureReason(...)` fail to compile until `Gum.Input` is imported. A second copy of the extension methods would make every call ambiguous in files that import both namespaces.
* A file that imports both `MonoGameGum.Input` and `Gum.Input` reports `CS0104` on `Cursor`, because both namespaces now contain one. This is the state the `2026 June` code fix left behind.

To migrate, replace `using MonoGameGum.Input;` with `using Gum.Input;`, or remove the old directive if `using Gum.Input;` is already there. The `GUM001` analyzer flags the old directive, and its code fix makes this change. In code that names the type with a full path, change `MonoGameGum.Input.Cursor` to `Gum.Input.Cursor`.

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

The `Gum.Shapes.MonoGame` and `Gum.Shapes.KNI` packages now put their public types in the unified `Gum` namespaces:

| Old | New |
|---|---|
| `MonoGameAndGum.Renderables.ShapeRenderer` | `Gum.Renderables.ShapeRenderer` |
| `MonoGameAndGum.Renderables.RenderableShapeBase` | `Gum.Renderables.RenderableShapeBase` |
| `MonoGameAndGum.Renderables.Circle` | `Gum.Renderables.Circle` |
| `MonoGameAndGum.Renderables.RoundedRectangle` | `Gum.Renderables.RoundedRectangle` |
| `MonoGameAndGum.Content.ShapeSvgLoader` | `Gum.Content.ShapeSvgLoader` |

The old names remain as obsolete shims, so existing code keeps compiling with a `CS0618` warning. `ShapeRenderer`, `RenderableShapeBase`, `Circle` and `RoundedRectangle` are derived classes, and `ShapeRenderer.Self` is inherited, so it returns the same renderer. `ShapeSvgLoader` is a static class, so its shim forwards `Load` to the new loader and shares its cache. The `Arc`, `Line` and `Svg` renderables are internal and moved without a shim.

The shims cannot cover everything, so these cases break:

* A variable declared with an old renderable type no longer accepts a shape that Gum creates. `RenderableComponent` and the renderables inside a shape runtime are `Gum.Renderables` instances, and C# cannot assign a base class value to a derived class variable (`CS0266`). A cast such as `as MonoGameAndGum.Renderables.Circle` returns `null` for the same reason.
* A file that imports both `MonoGameAndGum.Renderables` and `Gum.Renderables` reports `CS0104` on `ShapeRenderer`, `RenderableShapeBase`, `Circle` and `RoundedRectangle`, because both namespaces now contain them.

To migrate, replace `using MonoGameAndGum.Renderables;` with `using Gum.Renderables;` and `using MonoGameAndGum.Content;` with `using Gum.Content;`. The `GUM001` analyzer flags the old directives, and its code fix makes this change. In code that names the type with a full path, as the setup docs do, change the namespace in the path.

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

`RenderableCreator` moved from `MonoGameGum.Renderables` to `Gum.Renderables` in MonoGame, KNI and FNA projects, where raylib already had it. `MonoGameGum.Renderables.RenderableCreator` remains as an obsolete shim that forwards `HandleCreateGraphicalComponent` to the new class, so code that assigns it to `ElementSaveExtensions.CustomCreateGraphicalComponentFunc` keeps compiling with a `CS0618` warning.

To migrate, replace `using MonoGameGum.Renderables;` with `using Gum.Renderables;`. A file that imports both namespaces reports `CS0104` on `RenderableCreator`. The `GUM001` analyzer flags the old directive, and its code fix makes this change.

### MonoGameGum AddChild Extension Is Obsolete

The `AddChild` extension method in the `MonoGameGum` and `RaylibGum` namespaces is now `[Obsolete]`. Code generated at syntax versions 0 to 2 imports that namespace to call `AddChild` on a visual. The call `gue.AddChild(formsControl)` resolves to the instance method on `GraphicalUiElement`, which needs no `using` directive, so it produces no warning. Only a call that names `GraphicalUiElementExtensionMethods.AddChild` as a static method reports `CS0618`.

To migrate, call the instance method, or regenerate your code so the generated files no longer import the old namespace.

### XNA Keys Overloads on Keyboard Are Obsolete

In MonoGame, KNI and FNA projects, the key methods that take `Microsoft.Xna.Framework.Input.Keys` are now `[Obsolete]`:

* `KeyDown`, `KeyPushed`, `KeyReleased`, `KeyTyped` and `IsKeyLetter` on `Keyboard`
* `KeyDown`, `KeyPushed`, `KeyReleased` and `KeyTyped` on `IInputReceiverKeyboardMonoGame`
* The `KeyDown`, `KeyPushed`, `KeyReleased` and `KeyTyped` extension methods in `XnaKeyboardExtensions`, which add the XNA overloads to `IInputReceiverKeyboard`

Each one still works and reports `CS0618`. They become `[Obsolete(error: true)]` no earlier than March 2027 and are removed no earlier than July 2027. Every method has an overload that takes `Gum.Forms.Input.Keys`, the enum Forms already uses for `KeyCombo` and `KeyEventArgs.Key`.

This affects you only if you pass an XNA `Keys` value to one of these methods. In a file with `using Microsoft.Xna.Framework.Input;`, a bare `Keys.Enter` picks the XNA overload.

To migrate, pass `Gum.Forms.Input.Keys` instead. The enum mirrors the XNA values, so the names stay the same. `Keyboard.KeysTyped` and `KeyboardStateProcessor` still use XNA `Keys` and are not obsolete.

The same change adds four members to `ICursor` with default implementations: `VisualPushed` (the same value as `WindowPushed`), `LastX`, `LastY` and `ClearInputValues()`. A class that implements `ICursor` keeps compiling without them.

❌ Old:

```csharp
// Update
if (GumUI.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
{
    SubmitForm();
}
```

✅ New:

```csharp
// Update
if (GumUI.Keyboard.KeyPushed(Gum.Forms.Input.Keys.Enter))
{
    SubmitForm();
}
```

### ICursor Has FrameworkElement Members and TransformMatrix

`ICursor` gains `FrameworkElementOver`, `FrameworkElementPushed`, `FrameworkElementRightPushed` and a read-only `TransformMatrix`, so code that holds an `ICursor` can read what `Cursor` already offered. The `FrameworkElement` members return the control of the matching visual. `TransformMatrix` is a `System.Numerics.Matrix3x2` that defaults to the identity matrix. On MonoGame, KNI and FNA, the interface `TransformMatrix` is a 2D conversion of the XNA matrix, so Z and perspective are not part of it. `Cursor.TransformMatrix` keeps its type and setter (an XNA `Matrix` in those runtimes). A class that implements `ICursor` keeps compiling without these members.

### IGumService Cursor Is Not Null Under a Custom Cursor

`IGumService.Default.Cursor` now returns the cursor Forms is using, including one installed with `FormsUtilities.SetCursor`. It used to return `null` while a custom `ICursor` was installed, and in Skia-based hosts (Silk.NET, Stride, KNI Skia, Unity) it was always `null`. `IGumService` also has a new `Keyboard` member that returns the installed `IInputReceiverKeyboard`, with a default implementation that returns `null`, so a class that implements `IGumService` keeps compiling.

`GumService.Default.Cursor` and `GumService.Default.Keyboard` are unchanged. They return the concrete `Cursor` and `Keyboard` types, and they are `null` while a custom cursor or keyboard is installed.

### SkiaResourceManager and EmbeddedResourceContentLoader Are Obsolete

In SkiaSharp projects (including .NET MAUI), `SkiaGum.Content.SkiaResourceManager` and `SkiaGum.Content.EmbeddedResourceContentLoader` are now `[Obsolete]`. Use `RenderingLibrary.Content.ContentLoader`, which has the same name as the loader on MonoGame, KNI, FNA and raylib. Gum installs it as the default loader, and `GumService.ContentLoader` returns it.

Both old types keep working and report `CS0618`. Gum keeps them until at least April 2027. A cast such as `LoaderManager.Self.ContentLoader as EmbeddedResourceContentLoader` now returns `null`, because the default loader is a `ContentLoader`.

To migrate:

* Replace `SkiaResourceManager.CustomResourceAssembly` and `SkiaResourceManager.AdjustContentName` with the same properties on `ContentLoader`. They are the same setting, so code that still sets the old ones keeps working.
* Replace `SkiaResourceManager` loading calls with `LoadContent<T>` on the loader, where `T` is `SKBitmap`, `SKSvg`, `Animation` or `SKTypeface`.
* Replace `new EmbeddedResourceContentLoader()` with `new ContentLoader()`.

`GetSKBitmapFromUrl`, `CacheSvg` and the other `SkiaResourceManager` cache helpers have no `ContentLoader` equivalent. They keep working until the type is removed.

An `Svg` element's `SourceFile` now goes through `AdjustContentName`, as bitmaps, animations and typefaces already did. If your `SourceFile` values already include the prefix, remove it.

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

For more information see the [Content Using Embedded Resources](../../code/files-and-fonts/content-using-embedded-resources.md) page.

### OutputLibrary.MonoGameForms Renamed to GumForms

`Gum.ProjectServices.CodeGeneration.OutputLibrary.MonoGameForms` is now `GumForms`, the name the Gum tool already shows as **Gum Forms**. `MonoGameForms` remains as an obsolete alias with the same value, so it reports `CS0618`. It becomes `[Obsolete(error: true)]` no earlier than May 2027 and is removed no earlier than November 2027.

This affects you only if you wrote a plugin or tool that references `OutputLibrary.MonoGameForms`. Saved settings need no change. `ProjectCodeSettings.codsj` stores the value as `5`, and a file that spells out `MonoGameForms` still loads. `gumcli codegen-init` now prints `GumForms`.

To migrate, rename `OutputLibrary.MonoGameForms` to `OutputLibrary.GumForms`.

### Code Generation Updates Legacy Using Statements

Code generation copies the **Project-wide Using Statements** and an element's **Using Statements** into generated files. A project that saved `using MonoGameGum.GueDeriving;` there used to get the old namespace back in every generated file, even when the rest of the generated code used the unified names.

Code generation now rewrites these lines in the generated output, based on the project's [syntax version](syntax-versions.md):

| Saved line | Generated line | From syntax version |
|---|---|---|
| `using MonoGameGum.GueDeriving;` or `using SkiaGum.GueDeriving;` | `using Gum.GueDeriving;` | 1 |
| `using MonoGameGum;` | `using Gum;` | 3 |

Gum drops a rewritten line if the same list already imports that namespace. Your saved settings are not changed, and a syntax version below the one in the table keeps the line as written.

This affects you only if your using statements name the old namespaces. To migrate, regenerate your code. To remove the old names from your saved settings as well, edit the using statements in the **Code** tab.

### Obsolete Shims Are Removed No Earlier Than November 2027

The obsolete shims that keep the old `MonoGameGum`, `SkiaGum` and `MonoGameAndGum` names compiling are not permanent. Unless a row below says otherwise, each one is a `CS0618` warning today, becomes `[Obsolete(error: true)]` no earlier than May 2027, and is removed no earlier than November 2027. The message on each shim states these dates. Gum announces the actual release in the migration page for that month.

| Old name | Replacement | Obsoleted | Error no earlier than | Removed no earlier than |
|---|---|---|---|---|
| `MonoGameGum.GueDeriving` runtimes (`TextRuntime`, `ContainerRuntime`, `SpriteRuntime`, `ColoredRectangleRuntime`, `NineSliceRuntime`, `CircleRuntime`, `PolygonRuntime`, `RectangleRuntime`, and the shape runtimes) | `Gum.GueDeriving` | May 7, 2026 | May 2027 | November 2027 |
| `SkiaGum.GueDeriving` runtimes | `Gum.GueDeriving` | May 7, 2026 | May 2027 | November 2027 |
| `MonoGameGum.ElementSaveExtensionMethods.ToGraphicalUiElement` | `Gum.ElementSaveExtensionMethods` | June 12, 2026 | May 2027 | November 2027 |
| `MonoGameGum.Renderables` default renderables | `Gum.Renderables` | June 13, 2026 | May 2027 | November 2027 |
| `MonoGameGum.ExtensionMethods.IReadOnlyListExtensionMethods` | `Gum.ExtensionMethods` | June 13, 2026 | May 2027 | November 2027 |
| `MonoGameGum.Input.Cursor` | `Gum.Input.Cursor` | October 14, 2026 | May 2027 | November 2027 |
| `MonoGameGum.GraphicalUiElementExtensionMethods.AddChild` | `GraphicalUiElement.AddChild` | October 20, 2026 | May 2027 | November 2027 |
| `MonoGameGum.Renderables.RenderableCreator` | `Gum.Renderables.RenderableCreator` | October 20, 2026 | May 2027 | November 2027 |
| `MonoGameAndGum.Renderables` and `MonoGameAndGum.Content` types | `Gum.Renderables` and `Gum.Content` | October 20, 2026 | May 2027 | November 2027 |
| `Keyboard`, `IInputReceiverKeyboardMonoGame` and `XnaKeyboardExtensions` methods that take XNA `Keys` | The same methods taking `Gum.Forms.Input.Keys` | October 20, 2026 | March 2027 | July 2027 |
| `OutputLibrary.MonoGameForms` | `OutputLibrary.GumForms` | October 20, 2026 | May 2027 | November 2027 |
| `SkiaGum.Content.SkiaResourceManager` | `RenderingLibrary.Content.ContentLoader` | October 20, 2026 | Not scheduled | April 2027 |
| `SkiaGum.Content.EmbeddedResourceContentLoader` | `RenderingLibrary.Content.ContentLoader` | October 20, 2026 | Not scheduled | April 2027 |
| `MonoGameGum.GumService` and `RaylibGum.GumService` | `Gum.GumService` | June 12, 2026 | Never | Never |

The `GumService` shim stays because `GumService.Default` returns it. Declarations typed with the old name keep compiling with a `CS0618` warning, and you can change them to `Gum.GumService` whenever you like.

{% hint style="info" %}
The properties on the default Forms visuals (`TextInstance`, `Background`, and similar) are still typed as the obsolete `MonoGameGum.GueDeriving` shims. Gum changes them to the unified types in the release that removes the shims, so a variable declared with a shim type to hold one of them needs the new type by then.
{% endhint %}

To check what still uses an old name, build with the bundled `Gum.Analyzers` package and look for `GUM001` and `CS0618`.

### Stacked Children Ignore Their Origin on the Stacking Axis

A child after the first in a stack now ignores its origin on the stacking axis:

* In a `Top to Bottom Stack`, later children treat `Y Origin` as `Top`.
* In a `Left to Right Stack`, later children treat `X Origin` as `Left`.

Before this version, the origin still applied after the stack positioned the child. A `Center` origin pulled the child back over its previous sibling by half its size, and a `Bottom`, `Baseline`, or `Right` origin pulled it back by its full size, covering the previous sibling.

Stacks already ignored a later child's units on the stacking axis, so a child with `Y Units` of `Pixels From Center` still stacked below its previous sibling. The origin now follows the same rule, and a child's units and origin on the stacking axis have no effect unless it is the first child.

These cases are unchanged:

* The first child in a stack uses its units and origin on both axes.
* The origin on the other axis works normally. A child in a `Top to Bottom Stack` can still use `X Origin` of `Center` to center itself horizontally.
* `X` and `Y` values still add space between a child and its previous sibling.

This affects you only if a stacked child other than the first uses a `Y Origin` other than `Top` in a `Top to Bottom Stack`, or an `X Origin` other than `Left` in a `Left to Right Stack`. Calling `Anchor` with a value such as `Center` or `Bottom` sets these origins, so code that anchors a stacked child is affected too. Such a child now sits right after its previous sibling instead of overlapping it.

To migrate, open your screens and components in the Gum tool and check any stacks whose children use a non-default origin. If you relied on the overlap, for example to pull a child up so it overlaps the one before it, give the child a negative `Y` (or `X`) value instead. A negative value moves the child back toward its previous sibling by that many pixels.

❌ Old (child overlaps the previous sibling by half its height):

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

If the overlap was how you placed children side by side, the stack points the wrong way, and a negative value is not the fix. For example, a `Top to Bottom Stack` holds two children that both have a `Y Units` of `Pixels From Bottom` and a `Y Origin` of `Bottom`. Before this version they shared a row, because the second child's origin pulled it back over the first. Now the second child sits below the first, which can move it off screen. Set the stack to the direction you want instead. In the Gum tool, set the stack's `Children Layout` to `Left to Right Stack`. In code, set `Orientation` on a `StackPanel`, or `ChildrenLayout` on a container.

✅ New (children sit side by side, and each keeps its bottom origin):

```csharp
// Initialize
stackPanel.Orientation = Gum.Forms.Controls.Orientation.Horizontal;
```

```csharp
// Initialize
container.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
```

For more information see the [Children Layout](../gum-elements/container/children-layout.md#stacking-and-children-origin) page.

### Stacks Without Wrapping Grow to Their Max

A stack that sizes itself to its children and has a max on its stacking axis now grows all the way to that max when its children need more space. This applies to a `Top to Bottom Stack` with `Height Units` of `Relative to Children` (or `Relative to Max of Children or Parent`) and a `Max Height`, and to a `Left to Right Stack` with the matching `Width Units` and a `Max Width`.

Before this version, such a stack stopped growing at the last child that fit under its max. For example, a `Top to Bottom Stack` with a `Max Height` of `120` and three children that are each `50` tall measured `100`, and the third child hung below it. It now measures `120`, so it holds as much of the third child as it can. The children stay in the same positions.

These cases are unchanged:

* A stack with `Wraps Children` checked still moves the next child to a new row or column. Its size is its widest row or column, as described in the next section.
* A container with a `Children Layout` of `Regular` already grew to its max.
* A stack whose children fit under its max still sizes to its children.

This affects you only if a stack that does not wrap has more content than its `Max Height` or `Max Width` allows. Such a stack is now larger.

To migrate, open your screens and components in the Gum tool and check any stacks that use a max. If you want the old size, lower the max to the size of the children that fit, or check `Wraps Children` if the extra children should move to a new row or column.

❌ Old (with three children that are each 50 tall, measured 100 tall and now measures 120):

```csharp
// Initialize
var stackPanel = new ContainerRuntime();
stackPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
stackPanel.Height = 0;
stackPanel.MaxHeight = 120;
```

✅ New (keeps the old 100 tall size):

```csharp
// Initialize
var stackPanel = new ContainerRuntime();
stackPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
stackPanel.Height = 0;
stackPanel.MaxHeight = 100;
```

For more information see the [Children Layout](../gum-elements/container/children-layout.md#stacking-and-container-height-units-and-width-units) page.

### Wrapping Stacks Sized to Their Children Measure Their Widest Row

A stack with `Wraps Children` checked that sizes itself to its children and has a max on its stacking axis is now as large as its widest row or column. This applies to a `Left to Right Stack` with `Width Units` of `Relative to Children` (or `Relative to Max of Children or Parent`) and a `Max Width`, and to a `Top to Bottom Stack` with the matching `Height Units` and a `Max Height`. Rows break where the next child would pass the max, and padding and the max apply after the widest row is measured.

Before this version, such a stack measured only its first row. The rows after it then wrapped against that narrower size, so they used more rows than needed, and a child wider than the first row extended past the stack. For example, a `Left to Right Stack` with a `Max Width` of `200` holding children `100`, `120`, and `60` wide measured `100` wide and placed each child on its own row, with the `120` wide child extending `20` past the stack. It now measures `180` wide, with the `120` and `60` wide children sharing the second row.

A stack whose first child is wider than the max is now as wide as its max. Before this version, such a stack measured `0` wide when it had no `Width` padding.

These cases are unchanged:

* A stack whose first row is its widest row.
* A stack whose children fit in one row.
* Stacks with a size that does not depend on their children.

This affects you only if a wrapping stack sized to its children with a max has a later row wider than its first. Such a stack is now wider and may have fewer rows.

To migrate, open your screens and components in the Gum tool and check any wrapping stacks that use a max. If you want the old size, lower the max to the width of the first row.

For more information see the [Wraps Children](../gum-elements/container/wraps-children.md#relative-to-children-and-max-width-and-max-height) page.

### Wrapping Stacks Position Children Within Their Row or Column

A stack with `Wraps Children` checked that is sized to its children and has a max now measures its widest row or column, not only its first. In a stack with `Wraps Children` checked, each row or column is now the parent of its children on the other axis for positioning:

* In a `Left to Right Stack`, a child's `Y Units` and `Y Origin` position it within its row.
* In a `Top to Bottom Stack`, a child's `X Units` and `X Origin` position it within its column.

Before this version, these values positioned the child within the whole stack. For example, in a `Left to Right Stack` that is `300` tall with rows that are `40`, `80`, and `20` tall, a child with `Y Units` of `Pixels From Center` and `Y Origin` of `Center` sat at the stack's center whichever row it was in, outside its own row. It is now centered in its row. A child with `Y Units` of `Pixels From Bottom` now aligns to the bottom of its row instead of the bottom of the stack, and a `Percentage` value is now a percentage of the row's height.

A row or column is as large as its largest child, and rows and columns stay packed at the start of the stack. A child alone in its row is the same size as the row, so centering it leaves it at the top of the row.

Rows and columns now count their children the same way a container sized to its children does. A child's `Y` (or `X`) value counts from the edge it is measured from, a portion of a child outside its row does not count, and a child positioned with `Percentage` does not count. A child whose size depends on its parent (for example, `Height Units` of `Percentage of Parent`) still counts, unless the stack itself is sized to its children on that axis. Before this version, a row added each child's value to its size whatever its units.

These cases are unchanged:

* Children using `Pixels From Top` in a `Left to Right Stack` (or `Pixels From Left` in a `Top to Bottom Stack`), which is the default.
* Size units. A child with `Height Units` of `Percentage of Parent` still sizes itself from the whole stack.
* Stacks without `Wraps Children` checked.

This affects you only if a child of a wrapping stack uses units other than the default on the axis that does not stack, or a `Percentage` value there. Such a child now sits inside its row or column.

To migrate, open your screens and components in the Gum tool and check any wrapping stacks whose children use these units. If you want a child placed relative to the whole stack, move it out of the stack into a container that does not stack.

For more information see the [Wraps Children](../gum-elements/container/wraps-children.md#positioning-children-within-a-row-or-column) page.

### Containers Count a Child's Min When It Uses Relative to Max of Children or Parent

A container with `Height Units` of `Relative to Children` now counts a child that uses `Relative to Max of Children or Parent` at no less than the child's `Min Height`. The same applies to `Width Units` and `Min Width`.

Before this version, the container measured such a child by its children only. For example, a child with a `Min Height` of `80` whose children are `30` tall was `80` tall, but its container measured `30`, so the child extended past the container. The container now measures `80`.

These cases are unchanged:

* A child whose children are already larger than its `Min Height`.
* Children using other `Height Units`. Their min was already counted.

This affects you only if a container sized to its children holds a `Relative to Max of Children or Parent` child with a `Min Height` or `Min Width` larger than its content. Such a container is now larger.

To migrate, open your screens and components in the Gum tool and check these containers. If you want the old size, lower or clear the child's `Min Height` or `Min Width`.

For more information see the [Height Units](../gum-elements/general-properties/height-units.md#relative-to-max-of-children-or-parent) page.

### Dragging Is Raised Every Frame While the Button Is Held

`InteractiveGue.Dragging` is now raised every frame that the primary button is held after pushing on the visual, whether or not the cursor moved. It is still raised when the cursor is outside the bounds of the visual.

Before this version, `Dragging` was raised only on frames where the cursor moved. A visual that was pushed and held still raised nothing, so code that updated something every frame during a drag, such as a timer or a readout, stopped while the cursor was stationary.

These cases are unchanged:

* `Dragging` is not raised when the primary button is not down.
* `RollOver` is still raised only on frames where the cursor moved.
* Forms controls that use `Dragging` internally, such as `Slider`, `ScrollBar`, `Splitter`, `Window`, `ColorPicker`, `ListBox`, and `TextBox`, behave the same because they compute their result from the cursor position.

This affects you only if your code subscribes to `Dragging` on a visual and does something other than read the cursor position, such as accumulating a value on every call. That handler now runs on frames where the cursor did not move.

To migrate, check the cursor's `XChange` and `YChange` in the handler if you want to run only when the cursor moves.

❌ Old (`Dragging` ran only when the cursor moved):

```csharp
// Initialize
button.Visual.Dragging += (_, _) => movesCounted++;
```

✅ New (same behavior, stated explicitly):

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

For more information see the [Visual Events](../../code/events-and-interactivity/visual-events.md) page.

### AnimationChainFinished Replaces AnimationChainCycled at the End of a Non-Looping Chain

`AnimationChainCycled` is now raised only when a looping animation chain wraps around. A chain that does not loop raises the new `AnimationChainFinished` event once when it reaches its end and `Animate` becomes `false`. `SpriteRuntime`, `Sprite`, `NineSlice`, and `AnimationChainLogic` all have the new event.

Before this version, a chain that did not loop raised `AnimationChainCycled` when it reached its end, even though it never started over. It also returned to its first frame instead of holding its last.

A chain that does not loop now also finishes when it plays backward. With a negative `AnimationChainSpeed`, it stops on its first frame, sets `Animate` to `false`, and raises `AnimationChainFinished`. Before this version, it held the first frame, kept `Animate` set to `true`, and raised no event.

This affects you only if your code subscribes to `AnimationChainCycled` on a sprite or nine slice whose chain does not loop, for example to remove a sprite after its death animation plays. That handler no longer runs.

To migrate, subscribe to `AnimationChainFinished` instead.

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

For more information see the [Animation Chains](../../code/files-and-fonts/animation-chains.md#looping-and-end-of-chain-events) page.


### SvgRuntime.Document Renamed to Texture in MonoGame and KNI

The `SvgRuntime` in MonoGame and KNI projects that use `Gum.Shapes.MonoGame` or `Gum.Shapes.KNI` now names its document property `Texture`, the same name SkiaGum's `SvgRuntime` has always used. The property was `Document` before this version. The type is still the Apos.Shapes `ShapeSvg`, and `SourceFile` is unchanged.

This affects you only if your code reads or assigns `SvgRuntime.Document`, for example to draw a `ShapeSvg` you loaded yourself. The old name now fails to compile with `CS1061`.

To migrate, rename `Document` to `Texture`.

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

### RefreshStyles Restores Only Text Set in Code

`RefreshStyles` now restores a control's text only when code supplied it. Text counts as supplied by code when you set it through the property, call `SetTextNoTranslate` (or the `SetHeaderNoTranslate` and `SetPlaceholderNoTranslate` equivalents), bind it, or when the user types it into a `TextBox`. This applies to:

* `Text` on `Button`, `Label`, `ToggleButton` (and so `CheckBox` and `RadioButton`), `TextBox`, and `PasswordBox`
* `Header` on `Expander` and `MenuItem`
* `Placeholder` on `TextBox` and `PasswordBox`

Before this version, `Button`, `Label`, `TextBox`, and `PasswordBox` restored the text they had before the refresh whether or not code set it. A text authored on the Visual and then changed, for example through a variable reference, was overwritten with the old text. Text authored on the Visual is now re-applied from the Visual, so the changed text shows.

Text set in code also no longer carries over when you assign a new Visual to a control. The control shows the text the new Visual authors.

These cases are unchanged:

* Text set in code, through `SetTextNoTranslate`, or typed by the user still survives `RefreshStyles`.
* `ComboBox` still restores its displayed text.

This affects you only if a Visual authors text that you expected `RefreshStyles` to overwrite, or if you assign a new Visual to a control after setting its text in code.

To migrate, set the text through the control's property if you want it to survive `RefreshStyles`. Set the text again after assigning a new Visual.

For more information see the [Runtime Variable References](../../code/styling/runtime-variable-references.md#text-after-refreshstyles) page.

### Slider, ScrollBar, and ScrollViewer Step Defaults Changed in the Gum Tool

The behaviors that the Gum tool adds for new Forms controls now declare these step sizes:

| Control | Property | Before | Now |
|---|---|---|---|
| `Slider` | `SmallChange` | 1 | 5 |
| `Slider` | `LargeChange` | 10 | 25 |
| `ScrollBar` | `SmallChange` | 1 | 10 |
| `ScrollViewer` | `SmallChange` | 1 | 10 |

The tool writes a behavior's declared defaults onto controls that do not set their own value. A `Slider` placed in the tool therefore stepped by 1 and 10 while a `Slider` created in code steps by 5 and 25. The tool now matches code.

This affects you only if you rely on the step size of a `Slider`, `ScrollBar`, or `ScrollViewer` that you add in the tool. To migrate, set `SmallChange` or `LargeChange` in the **Behavior** category to the value you want.

### ScrollBar Orientation Is a Behavior Property

A ScrollBar instance now has an `Orientation` setting in the **Behavior** category, and the `OrientationCategoryState` variable is hidden. Before this version, setting the orientation state in the tool changed how the bar looked, but the control in your game never read it, so the bar stayed vertical.

When the tool or your game loads a project, Gum migrates a ScrollBar whose default state sets `OrientationCategoryState` to `Horizontal` by also setting its `Orientation` to `Horizontal`. The bar keeps its look and the control now follows it. The project does not need to be saved again. A project whose `ScrollBarBehavior` does not have the `Orientation` property yet is not migrated, since there is nothing to read the value.

This affects you only if you set the orientation of a ScrollBar in the tool. To migrate, open the project in the tool and check that the bars you made horizontal show `Orientation` set to `Horizontal`.

For more information see the [Behaviors](../gum-elements/behaviors/README.md#scrollbar-orientation) page.

### Code Generation Emits Variables Hidden From Instances

Code generation now emits a variable that a control hides from its instances, unless a behavior reference drives that variable. For example, a `Window` instance that sets `ClipsChildren` now gets that line in generated code.

Before this version, code generation skipped every hidden variable. The Gum tool still showed such a value and your game still loaded it, but generated code left it out.

Variables driven by a behavior reference, such as a category state that follows `IsEnabled`, are still skipped, because the control sets them itself.

This affects you only if an instance sets a variable that its control now hides. A project from before the control hid the variable can hold one without you knowing. Such a variable appears in generated code the next time you generate.

To migrate, regenerate your code and look at the changes. To drop a stale value, remove it from the instance in the tool.
