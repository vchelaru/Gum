# Finding Elements

## Introduction

When you have a reference to a parent — a screen, a Forms control's `Visual`, or any `GraphicalUiElement` — you often need to grab a specific child somewhere underneath it. For example, a Forms `TextBox` exposes its `Visual` property, but to set a property on the inner `TextRuntime` you first have to find it inside the visual.

Gum provides extension methods on `GraphicalUiElement` that traverse the visual tree and return the elements you ask for. They live in the `Gum.Wireframe` namespace, the same namespace as `GraphicalUiElement` itself, so they show up automatically in IntelliSense as long as you have a `using Gum.Wireframe;` directive.

## Quick Example

The most common case: find a named child of a known type underneath a Forms control's `Visual`.

```csharp
// Initialize
TextBox textBox = new();
TextRuntime textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;
textInstance.Color = Microsoft.Xna.Framework.Color.Red;
```

`Find<T>(name)` walks the descendants of `textBox.Visual` and returns the first one that is both a `TextRuntime` and named `"TextInstance"`. If no match is found, it returns null.

## Available Methods

The full set of traversal extensions:

| Method | Returns | Description |
|---|---|---|
| `Find<T>()` | `T?` | First descendant assignable to `T`. |
| `FindByName(string name)` | `GraphicalUiElement?` | First descendant whose `Name` equals `name`. |
| `Find<T>(string name)` | `T?` | First descendant that is both `T` and named `name`. |
| `Descendants()` | `IEnumerable<GraphicalUiElement>` | Every descendant (excluding self). |
| `DescendantsAndSelf()` | `IEnumerable<GraphicalUiElement>` | The element followed by every descendant. |
| `Ancestors()` | `IEnumerable<GraphicalUiElement>` | Every ancestor walking up `Parent` (excluding self). |
| `AncestorsAndSelf()` | `IEnumerable<GraphicalUiElement>` | The element followed by every ancestor. |

Type matching uses `is T` semantics, so `Find<ContainerRuntime>()` will match any class that derives from `ContainerRuntime`, not just `ContainerRuntime` itself.

## How Matches Are Ordered

`Descendants()` and the `Find*` methods enumerate **shallowest-first** — direct children before grandchildren, grandchildren before great-grandchildren, and so on. There are two reasons:

* **Closest match wins.** When you call `Find<T>()` and there are multiple matching descendants at different depths, you almost always want the one nearest the root you searched from. A `ListBox`, for example, has a `FocusedIndicator` near its root and another `FocusedIndicator` inside each `ListBoxItem`; shallowest-first returns the outer one, which is what callers expect.
* **Performance.** Lookups short-circuit as soon as they find a match. With shallowest-first ordering, common queries (where the target is near the top of the tree) finish quickly without descending into deep, populated subtrees like a `ListBox`'s item panel.

`Ancestors()` enumerates nearest-first, walking `Parent` once per step.

## Composing With LINQ

`Find<T>()` and `FindByName` cover the common cases. For anything more specific, the `Descendants()` and `Ancestors()` primitives compose with LINQ:

```csharp
// Initialize
using System.Linq;
using Gum.Wireframe;

// Every TextRuntime under root, materialized as a list:
List<TextRuntime> texts = root.Descendants().OfType<TextRuntime>().ToList();

// First TextRuntime whose Text starts with "Hello":
TextRuntime? greeting = root.Descendants()
    .OfType<TextRuntime>()
    .FirstOrDefault(t => t.Text?.StartsWith("Hello") == true);

// Walk up from a child to find the containing ListBoxItem visual:
GraphicalUiElement? listBoxItemVisual = child.Ancestors()
    .FirstOrDefault(a => a.Name == "ListBoxItemRoot");
```

Because `Descendants()` and `Ancestors()` return `IEnumerable<GraphicalUiElement>` and use deferred execution, they don't allocate a list up-front and they stop walking the tree as soon as the LINQ pipeline has what it needs.
