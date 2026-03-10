# TextRuntime Unification

## Overview

Gum supports multiple rendering backends: MonoGame (and its forks KNI and FNA), Raylib, and SkiaSharp. Each backend has its own `TextRuntime` class that exposes text rendering to the rest of the framework. Historically these three classes were developed and maintained independently, resulting in significant code duplication. A bug fix or new property added to one implementation had to be manually applied to the other two — and that did not always happen.

The TextRuntime Unification effort exists to close the gaps between the three implementations, standardize their behavior, and ultimately collapse all three files into a single shared `.cs` file so future changes only need to be made once.

## Unification Strategy

The end goal is a **single shared file** that all three backend projects compile via file linking or a shared project. The approach to get there is **additive convergence**:

1. Add `#if` / `#else` / `#endif` blocks to make each file structurally identical to the others, even if a given branch is unreachable in that particular project.
2. Once all three files are byte-for-byte identical (modulo the `#if` guards themselves), they can be replaced with a single canonical file.

**Do not remove `#if` blocks that appear to be dead code in a specific project.** For example, the MonoGame file contains `#if RAYLIB` blocks that are never true when compiled for MonoGame. These are intentional — they are the Raylib-specific counterpart that will be live in the eventual unified file. Removing them would require re-adding them later during the merge step.

When adding a new property or fixing a bug, include the appropriate `#if` guards in all three files so the structure stays in sync, even if some branches will only ever compile in one project.

## Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/TextRuntime.cs` |
| Raylib | `Runtimes/RaylibGum/GueDeriving/TextRuntime.cs` |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/TextRuntime.cs` |

MonoGame and Raylib both inherit from `InteractiveGue`. SkiaGum inherits from `GraphicalUiElement` directly, which is a legitimate platform difference (see below).

## What Has Been Done

The following changes have been applied across all three files to bring them into alignment:

- **`mContainedText` nullability** — changed to `Text?` to match actual usage.
- **`FontFamily` property** — added to all three implementations, delegating to the underlying `Font` property.
- **`TextOverflowHorizontalMode` logic** — unified so behavior is consistent across backends.
- **Property getter/setter conventions** — standardized to call `NotifyPropertyChanged()` and `UpdateLayout()` correctly throughout.
- **`WrappedText` property** — added to all three implementations.
- **Default static fields** — `DefaultFont`, `DefaultFontSize`, and `AssignFontInConstructor` are now present and consistent across all three files.
- **Constructor initialization** — all three constructors now call `SuspendLayout()` before setup and `ResumeLayout()` at the end.

## Remaining Work

The following items still need to be addressed:

- **Make all three files structurally identical.** Properties and methods that exist in one file but not the others need to be added (with appropriate `#if` guards) so all three files converge toward the same structure. Once identical, they can be collapsed into a single shared file.
- **Standardize bold/weight handling.** MonoGame and Raylib use a boolean `IsBold` property. SkiaGum uses a float `BoldWeight` property. These need a unified approach that satisfies both models, or the difference needs to be explicitly documented as intentional.
- **Continue narrowing the SkiaGum inheritance gap.** SkiaGum's `GraphicalUiElement` base means some `InteractiveGue`-specific behavior is unavailable. Opportunities to close this gap without breaking SkiaGum's design should be identified and addressed where practical.

## Known Legitimate Differences

The following differences across the three files reflect real platform constraints. Do not attempt to unify them away.

| Difference | Detail |
|---|---|
| Base class | MonoGame and Raylib inherit `InteractiveGue`; SkiaGum inherits `GraphicalUiElement` |
| Color type | MonoGame and Raylib use XNA `Color`; SkiaGum uses `SKColor` |
| `BitmapFont` property | Present in MonoGame and Raylib; not applicable to SkiaGum |
| `MaxLettersToShow` property | Present in MonoGame and SkiaGum; absent from Raylib |
| Bold representation | MonoGame and Raylib use `bool IsBold`; SkiaGum uses `float BoldWeight` |
| Obsolete color fields | SkiaGum has legacy `DefaultRed`, `DefaultGreen`, `DefaultBlue` static fields; the others do not |

When a difference is intentional, add a brief comment in the code explaining why that file diverges from the others.

## How to Contribute

**Fixing a bug or adding a property:** Apply the change to all three files. Verify the behavior is consistent. If a property does not apply to a specific backend (see the table above), skip that file and leave a comment in the other files noting the omission and why.

**Platform-specific changes:** If a change only makes sense for one backend, add a comment at the point of divergence so future contributors understand the intent. For example:

```csharp
// BitmapFont is not applicable to SkiaGum because Skia handles font
// loading through its own type system (SKTypeface).
```

**Working toward a single file:** The target state is one shared file, not a base class. Each incremental step should make the three files more identical, not more abstracted. Resist the urge to extract helpers or base classes — the payoff comes when all three files can simply be deleted and replaced with one.

**Verifying your change:** After touching any of the three files, build and do a quick smoke test on the affected backend if possible. At minimum, confirm the other two files still compile cleanly.
