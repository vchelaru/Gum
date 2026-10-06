# Migrating to 2026 November

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 October` to `2026 November`.

## What Changed at a Glance

`2026 November` changes how stacks position their children. A child after the first in a stack now ignores its origin on the stacking axis, the same way it already ignored its units, so it no longer overlaps its previous sibling. This change affects layouts in both the Gum tool and the runtime, and it reaches you only if a stacked child (other than the first) uses a non-default origin on the stacking axis.

## Breaking Changes and Migrations

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

For more information see the [Children Layout](../gum-elements/container/children-layout.md#stacking-and-children-origin) page.
