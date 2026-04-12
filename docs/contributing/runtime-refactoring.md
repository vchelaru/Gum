# Runtime Unification

## Overview

Gum supports multiple rendering backends: MonoGame (and its forks KNI and FNA), Raylib, and SkiaSharp. Each backend has its own set of runtime classes (`TextRuntime`, `SpriteRuntime`, `ContainerRuntime`, etc.) that wrap renderables and expose them through Gum's layout system. Historically these were developed and maintained independently, resulting in significant code duplication. A bug fix or new property added to one implementation had to be manually applied to the others — and that did not always happen.

The Runtime Unification effort exists to close the gaps between the implementations, standardize their behavior, and ultimately collapse each runtime into a single shared `.cs` file so future changes only need to be made once.

## Unification Strategy

The end goal is a **single shared file per runtime type** that all backend projects compile via file linking. The approach to get there is **additive convergence**:

1. Add `#if` / `#else` / `#endif` blocks to make each file structurally identical to the others, even if a given branch is unreachable in that particular project.
2. Once all files for a given runtime are byte-for-byte identical (modulo the `#if` guards themselves), they can be replaced with a single canonical file.

**Do not remove `#if` blocks that appear to be dead code in a specific project.** For example, the shared MonoGame+Raylib file contains `#if RAYLIB` blocks that are never true when compiled for MonoGame. These are intentional — they are the Raylib-specific counterpart that will be live in the eventual unified file. Removing them would require re-adding them later during the merge step.

When adding a new property or fixing a bug, include the appropriate `#if` guards in all files for that runtime so the structure stays in sync.

## Status At a Glance

| Runtime | MonoGame | Raylib | SkiaGum | XNA+Raylib Unified? |
|---|---|---|---|---|
| TextRuntime | yes | linked to MonoGame | yes | **Done** |
| ContainerRuntime | yes | linked to MonoGame | yes | **Done** |
| SpriteRuntime | yes | own file | yes | No |
| ColoredRectangleRuntime | yes | own file | yes | No |
| NineSliceRuntime | yes | own file | **not implemented** | No |
| CircleRuntime | yes | — | yes | N/A (no Raylib version) |
| PolygonRuntime | yes | — | yes | N/A (no Raylib version) |
| RectangleRuntime | yes | — | — | N/A (MonoGame only) |

### SkiaGum-only runtimes (no unification target)

These runtimes exist only in SkiaGum and have no MonoGame or Raylib counterpart. They are Skia-specific and are not candidates for unification:

- `ArcRuntime`
- `ColoredCircleRuntime`
- `LineGridRuntime`
- `LineRuntime`
- `LottieAnimationRuntime`
- `RoundedRectangleRuntime`
- `SkiaShapeRuntime` (base class for Circle, Polygon, and other shape runtimes in SkiaGum)
- `SolidRectangleRuntime`
- `SvgRuntime`

---

## TextRuntime

**Status:** XNA+Raylib unified. SkiaGum convergence in progress.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame + Raylib (shared) | `MonoGameGum/GueDeriving/TextRuntime.cs` |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/TextRuntime.cs` |

Raylib compiles the MonoGame file via file linking (`<Compile Include>` in `RaylibGum.csproj`), so there is no separate Raylib `TextRuntime.cs`. Platform differences within the shared file are handled by `#if RAYLIB` guards.

### What Has Been Done

- **MonoGame-Raylib unification complete** — Raylib no longer has its own `TextRuntime.cs`; it links to the MonoGame file.
- **`mContainedText` nullability** — changed to `Text?` to match actual usage.
- **`FontFamily` property** — added to all implementations, delegating to the underlying `Font` property.
- **`TextOverflowHorizontalMode` logic** — unified so behavior is consistent across backends.
- **Property getter/setter conventions** — standardized to call `NotifyPropertyChanged()` and `UpdateLayout()` correctly throughout.
- **`WrappedText` property** — added to all implementations.
- **Default static fields** — `DefaultFont`, `DefaultFontSize`, and `AssignFontInConstructor` are now present and consistent across both files.
- **Constructor initialization** — constructors now call `SuspendLayout()` before setup and `ResumeLayout()` at the end.
- **`SetTextNoTranslate` method** — added to both files with identical implementation.
- **`Text` property setter** — both files now share identical logic (width-before check, SetProperty call, conditional UpdateLayout).
- **Font properties** (`UseCustomFont`, `CustomFontFile`, `Font`/`FontFamily`, `IsItalic`, `UseFontSmoothing`, `OutlineThickness`) — present in both files with matching structure.

### Remaining Work (SkiaGum convergence)

- ~~**Reconcile `MaxLettersToShow` guard.**~~ Done — both files use `#if !SKIA`.
- ~~**Reconcile `BitmapFont` guard.**~~ Done — both files use `#if !RAYLIB && !SKIA`.
- ~~**Reconcile `LineHeightMultiplier` guard.**~~ Done — Raylib renderable now implements `LineHeightMultiplier`; property is unguarded in both `TextRuntime` files. See `Runtimes/RaylibGum/Renderables/Text.cs` (`Render` and `UpdatePreRenderDimensions`) and `Runtimes/RaylibGum/Renderables/CustomSetPropertyOnRenderable.cs`.
- ~~**Handle SkiaGum-only legacy code.**~~ Already removed. No action needed.
- ~~**Handle `WrappedText` and `OverlapDirection`.**~~ Done — `WrappedText` uses `#if !SKIA` in both files (Raylib and MonoGame both expose it; Skia's renderable does not). `OverlapDirection` uses `#if !RAYLIB && !SKIA` in both files with XML docs explaining the Raylib/Skia limitations. See `RenderingLibrary/Graphics/TextOverflowMode.cs` for the enum docs.
- ~~**Handle `Clone()` override.**~~ Done — `Clone()` promoted to the shared file. This fixed a latent bug: `base.Clone()` uses `MemberwiseClone`, which leaves `mContainedText` pointing at the original's renderable. Nulling it forces re-cache from the clone's own `RenderableComponent`.
- ~~**Standardize bold/weight handling.**~~ Done — both files use an identical `#if SKIA / #else` block. Skia path: `float _boldWeight` + `BoldWeight` property with `IsBold` derived as `_boldWeight > 1`. Non-Skia path: `bool isBold` backing field + simple `IsBold` property calling `UpdateToFontValues()`.
- ~~**Reconcile `FontSize` implementation.**~~ Done — both files use the backing-field pattern (`int fontSize` + `UpdateToFontValues()` in the setter).
- ~~**Reconcile constructors.**~~ Done — signatures and bodies are now byte-identical between the two files. Both accept `(bool fullInstantiation = true, SystemManagers? systemManagers = null)` with default text `"Hello World"`. Platform-specific lines are wrapped in `#if` guards: `RenderBoundary = false` uses `#if !RAYLIB && !SKIA`; the `DefaultCustomFont` assignment branch uses `#if !SKIA` (with an inner `#if !RAYLIB / #else` for BitmapFont vs CustomFont). The `DefaultCustomFont` field declaration itself uses `#if !RAYLIB && !SKIA / #elif RAYLIB`. Skia's `Text` gained a `Text(SystemManagers)` overload for API uniformity — see `Runtimes/SkiaGum/Renderables/Text.cs`.
- **Converge SkiaGum toward the shared file's structure and collapse into one file.** Remaining structural/ordering differences need to be resolved so the two files become byte-for-byte identical (modulo `#if` guards), at which point the SkiaGum file can be deleted and `SkiaGum.csproj` can file-link the shared `MonoGameGum/GueDeriving/TextRuntime.cs` — same pattern Raylib already uses. This is the final step.

### Known Legitimate Differences

| Difference | Detail |
|---|---|
| Base class | ~~Shared file inherits `InteractiveGue`; SkiaGum inherits `GraphicalUiElement`~~ **Resolved (April 2026)** — all runtimes now inherit `InteractiveGue` |
| Color type | Shared file uses XNA `Color` (MonoGame) or Raylib `Color`; SkiaGum uses `SKColor` |
| `BitmapFont` property | Present in MonoGame builds; not applicable to SkiaGum or Raylib |
| `CustomFont` property | Raylib-only (`Font` type); not applicable to others |
| `BlendState`/`Blend` properties | XNA-like only; not applicable to Raylib or SkiaGum |
| `TextRenderingPositionMode` | Present in MonoGame and SkiaGum; absent from Raylib |
| Bold representation | Both files use an `#if SKIA / #else` block: Skia exposes `float BoldWeight` (with `IsBold` derived as `_boldWeight > 1`); non-Skia uses `bool isBold`. |
| `RenderBoundary` | Set to `false` in the shared-file constructor for MonoGame only. Raylib and Skia `Text` renderables do not have this property. |
| `DefaultCustomFont` | MonoGame assigns `BitmapFont`; Raylib assigns `CustomFont` (a `Font` value); Skia has no equivalent constructor-time default custom font. |

---

## ContainerRuntime

**Status:** XNA+Raylib unified. SkiaGum version is minimal and needs convergence.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame + Raylib (shared) | `MonoGameGum/GueDeriving/ContainerRuntime.cs` (~127 lines) |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/ContainerRuntime.cs` (~30 lines) |

Raylib compiles the MonoGame file via file linking.

### Current State

The shared MonoGame+Raylib file has a full set of properties: `Alpha`, `IsRenderTarget`, `BlendState`, `Blend`, `AddToManagers()`, and batch rendering methods (`BeginRenderingAsBatch`/`EndRenderingAsBatch`). It inherits from `InteractiveGue`.

The SkiaGum version is almost empty — it inherits from `InteractiveGue` (migrated from `GraphicalUiElement` in April 2026) and only has a `Clone()` override. Most container behavior is inherited from the base class.

### Remaining Work

- **Determine which shared-file properties are applicable to SkiaGum.** `Alpha` likely applies. `IsRenderTarget`, `BlendState`, `Blend`, and batch methods are likely platform-specific and may need `#if` guards.
- **Add SkiaGum-applicable properties** behind appropriate guards.
- **Add `Clone()` override to the shared file** if it's needed across all backends.
- **Unify MonoGame and Raylib first, then converge with SkiaGum** — since Raylib is already linked, the remaining gap is entirely SkiaGum.

---

## SpriteRuntime

**Status:** Three separate files, all significantly different. Most divergent runtime.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/SpriteRuntime.cs` (~310 lines) |
| Raylib | `Runtimes/RaylibGum/GueDeriving/SpriteRuntime.cs` (~96 lines) |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/SpriteRuntime.cs` (~80 lines) |

### Current State

MonoGame is the most complete with 23+ properties: color channels (Red/Green/Blue/Alpha), `Color`, `BlendState`/`Blend`, texture/source rectangle handling, animation chains, `Animate`, and `FlipHorizontal`. It inherits from `InteractiveGue`.

Raylib has 6 properties: `Color`, `Texture`, `FlipVertical`. Animation support is stubbed out with "todo" comments but not implemented. It inherits from `InteractiveGue`.

SkiaGum has 5 properties: `SourceFile`, `Texture` (as `SKBitmap`), `Image` (as `SKImage`). It inherits from `GraphicalUiElement` and has a `Clone()` override. No color channel properties or animation support.

### Remaining Work

- **Unify MonoGame and Raylib first.** Add missing properties to Raylib (Red/Green/Blue/Alpha, animation chains) behind `#if` guards, converge the files, then switch Raylib to file-link the MonoGame file.
- **Decide on animation chain support.** Raylib has stubs but no implementation. Determine whether Raylib's renderable supports animation chains before adding the properties.
- **Converge SkiaGum.** SkiaGum uses `SKBitmap`/`SKImage` for textures vs. MonoGame's `Texture2D`. These are legitimate platform differences. Color channels, `FlipHorizontal`, and other non-platform-specific properties should be added.
- **Reconcile `Color` type differences.** Same pattern as TextRuntime — `#if` guards for XNA `Color`, Raylib `Color`, and `SKColor`.

> **Note:** SpriteRuntime is the most divergent of all the runtimes — Raylib's animation support is only stubs, SkiaGum uses entirely different texture types (`SKBitmap`/`SKImage`), and the three files share very little structure today. This will be the most work to unify.

---

## ColoredRectangleRuntime

**Status:** Three separate files with similar shape but different details.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/ColoredRectangleRuntime.cs` (~135 lines) |
| Raylib | `Runtimes/RaylibGum/GueDeriving/ColoredRectangleRuntime.cs` (~103 lines) |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/ColoredRectangleRuntime.cs` (~76 lines) |

### Current State

All three have Red/Green/Blue and Color properties. MonoGame adds `Alpha`, `BlendState`, and `Blend`. Raylib has `Alpha` commented out. SkiaGum has `Alpha` but no blend support.

Notable: SkiaGum wraps a `RoundedRectangle` renderable rather than a `SolidRectangle`, which is a significant design difference.

### Remaining Work

- **Unify MonoGame and Raylib first.** Uncomment/add `Alpha` to Raylib, add `#if` guards for `BlendState`/`Blend`, converge the files, then switch to file linking.
- **Converge SkiaGum.** The `RoundedRectangle` vs `SolidRectangle` renderable difference may be a legitimate platform distinction that needs `#if` guards in the unified file.
- **Reconcile `Color` type differences** with `#if` guards.

---

## NineSliceRuntime

**Status:** MonoGame and Raylib have separate files. SkiaGum has no implementation.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/NineSliceRuntime.cs` (~256 lines) |
| Raylib | `Runtimes/RaylibGum/GueDeriving/NineSliceRuntime.cs` (~149 lines) |
| SkiaGum | **Does not exist** |

### Current State

MonoGame is the most complete with `Alpha`, `BlendState`/`Blend`, animation chains, `CustomFrameTextureCoordinateWidth`, `IsTilingMiddleSections`, `BorderScale`, and static defaults. It inherits from `InteractiveGue`.

Raylib has `Alpha`, Red/Green/Blue, `Color`, `Texture`, `BorderScale`, and `SourceFileName`. Some properties are conditionally compiled with `XNALIKE`. It inherits from `InteractiveGue`.

SkiaGum has no `NineSliceRuntime` wrapper at all, though the underlying `NineSlice` renderable does exist in SkiaGum. This is a notable gap — the renderable is there but has no runtime wrapper exposing it through Gum's layout system.

### Remaining Work

- **Unify MonoGame and Raylib first.** Add missing properties to Raylib behind `#if` guards, converge, then switch to file linking.
- **Create SkiaGum implementation.** A `NineSliceRuntime` wrapping the existing Skia `NineSlice` renderable needs to be created to bring SkiaGum to parity.
- **Converge all three** once SkiaGum has an implementation.

---

## CircleRuntime

**Status:** MonoGame and SkiaGum have separate files. No Raylib version exists.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/CircleRuntime.cs` (~132 lines) |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/CircleRuntime.cs` (~57 lines) |

### Current State

MonoGame has Red/Green/Blue/Alpha and Color properties. It wraps a `LineCircle` renderable and inherits from `InteractiveGue`.

SkiaGum inherits from `SkiaShapeRuntime` (which now inherits `InteractiveGue` as of April 2026, a Skia-specific base class that provides color, stroke, and dropshadow properties) and has no public properties of its own beyond what it inherits. It has a `Clone()` override.

### Remaining Work

- **Determine unification feasibility.** The SkiaGum version uses a different inheritance hierarchy (`SkiaShapeRuntime`). Both chains now root at `InteractiveGue`, but the intermediate base class is still different. This may make true file-level unification impractical. The alternative is to document this as a legitimate structural difference.
- **If unifiable:** Add color properties to SkiaGum explicitly (rather than relying on inheritance), add `#if` guards, and converge.
- **If not unifiable:** Document why and keep as separate files.

> **Note:** CircleRuntime and PolygonRuntime may not be candidates for file-level unification — SkiaGum's `SkiaShapeRuntime` base class provides color, stroke, and dropshadow through inheritance rather than explicit properties, which is a fundamentally different design from the MonoGame approach.

---

## PolygonRuntime

**Status:** MonoGame and SkiaGum have separate files. No Raylib version exists.

### Affected Files

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/PolygonRuntime.cs` (~161 lines) |
| SkiaGum | `Runtimes/SkiaGum/GueDeriving/PolygonRuntime.cs` (~77 lines) |

### Current State

MonoGame has Red/Green/Blue/Alpha, Color, `SetPoints` (with `Vector2[]`), `LineWidth`, `IsDotted`, point manipulation methods (`InsertPointAt`, `RemovePointAt`, `SetPointAt`), and `IsPointInside`. It inherits from `InteractiveGue`.

SkiaGum inherits from `SkiaShapeRuntime` and has `IsClosed`, `Points` (as `List<SKPoint>`), `PointXUnits`, and `PointYUnits`. It has a `Clone()` override.

### Remaining Work

- **Same `SkiaShapeRuntime` challenge as CircleRuntime** (see note there). The different inheritance hierarchy may prevent file-level unification.
- **Point type differences.** MonoGame uses `Vector2`; SkiaGum uses `SKPoint`. This is a legitimate platform difference that would need `#if` guards.
- **API differences.** MonoGame has imperative point manipulation; SkiaGum has declarative point lists with unit types. These may reflect genuinely different design philosophies.

---

## RectangleRuntime

**Status:** MonoGame only. No unification needed.

### File

| Backend | Path |
|---|---|
| MonoGame | `MonoGameGum/GueDeriving/RectangleRuntime.cs` (~134 lines) |

This is a line-rectangle (outline) primitive. It has Red/Green/Blue/Alpha, Color, `LineWidth`, and `IsDotted`. No Raylib or SkiaGum equivalents exist. If a Raylib or SkiaGum version is created in the future, it should follow the unification strategy from the start.

---

## CustomSetPropertyOnRenderable

**Status:** MonoGame and Raylib have separate files with significant overlap. Candidate for unification.

### Files

| Backend | Path | Size |
|---|---|---|
| MonoGame | `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` | ~2118 lines |
| Raylib | `Runtimes/RaylibGum/Renderables/CustomSetPropertyOnRenderable.cs` | ~667 lines |
| SkiaGum | *(none — Skia uses a different property-assignment path)* | — |

Raylib's file is a reduced copy of MonoGame's, handling the subset of properties Raylib supports. Both dispatch by property name string to forward values onto renderables.

### Unification Approach

The same `#if RAYLIB` file-linking pattern used for `TextRuntime` and `ContainerRuntime` should work here. Differences are almost entirely additive (MonoGame supports more property names) rather than structurally divergent, so they can be guarded with `#if !RAYLIB` blocks.

### Remaining Work

- **Audit property-by-property** to confirm Raylib's file is a strict subset (no Raylib-specific branches that differ in behavior from MonoGame).
- **Reconcile branches where both exist** — any case where the two files handle the same property differently needs to be understood before merging (e.g. the recent `LineHeightMultiplier` case in Raylib was a commented-out stub that has since been activated).
- **Converge ordering and structure** so the shared file is byte-for-byte identical modulo `#if` guards, then collapse into one file and link from `RaylibGum.csproj`.
- **Reconcile type differences** — XNA `Color` vs Raylib `Color`, `Texture2D` from different namespaces, etc. Same pattern already used in the other unified files.

> **Note:** SkiaGum deliberately excluded. Its property-assignment goes through a different code path and is not a unification candidate at this time.

---

## Cross-Cutting Concerns

### FlatRedBall 1 Compatibility

**All namespace changes must be guarded with `#if !FRB`.**

Gum is used by both FlatRedBall 1 (FRB1) and the new FlatRedBall 2 engine. FRB1 has its own code generation and project structure; updating it for namespace changes is impractical. The `FRB` preprocessor symbol is already used throughout the codebase to guard FRB1-specific code.

For namespace unification, `[Obsolete]` deprecations, and eventual API removals:

- New namespaces go inside `#if !FRB` blocks.
- Old namespaces are preserved inside `#else` blocks for FRB1.
- FRB1 projects will never see the new namespaces — no migration needed for them.
- FRB2 uses the MonoGame runtime directly, so it gets the new namespaces with no special handling.
- The Roslyn analyzer only applies to non-FRB builds (FRB1 users won't have new namespaces to migrate to).
- The `GumSyntaxVersionAttribute` and code generator namespace logic only apply to non-FRB builds.

Example pattern for a moved enum:

```csharp
#if !FRB
namespace Gum.Layout;
#else
namespace Gum.DataTypes;
#endif

public enum DimensionUnitType
{
    // ...
}
```

---

### Runtime Namespace Unification

**This is a breaking change that requires planning.**

Currently, each backend uses a different namespace for its runtime classes:

| Backend | Namespace |
|---|---|
| MonoGame | `MonoGameGum.GueDeriving` |
| Raylib | `Gum.GueDeriving` |
| SkiaGum | `SkiaGum.GueDeriving` |

The two already-unified files (TextRuntime, ContainerRuntime) use `#if RAYLIB` to switch between `Gum.GueDeriving` and `MonoGameGum.GueDeriving`, which means even in the "unified" files there is no single namespace.

For documentation and code portability, all runtimes should live in the same namespace regardless of backend. A user following a tutorial or copying sample code should not need to change `using` statements based on which rendering backend they chose.

**Decision needed:** What should the unified namespace be? Candidates:

- `Gum.GueDeriving` — Raylib already uses this; neutral and backend-agnostic. But "GueDeriving" is an internal implementation term that may confuse users.
- `Gum.Runtimes` — Descriptive and user-facing. Would require renaming the folder or using a namespace that doesn't match the folder path.
- `MonoGameGum.GueDeriving` — Preserves backward compatibility for the largest user base (MonoGame) but is misleading for Raylib/SkiaGum users.

**Migration approach:** This will use the Gum.Analyzers project (see below). The runtime namespace change will be a simultaneous break + auto-fix: the namespaces move, and the analyzer provides one-click migration. Pre-warning users about the change happens through documentation and release notes, not compiler warnings.

---

### Layout Enum Namespace Unification

**This is a breaking change that requires planning.**

Gum's layout-related enums grew organically and are now scattered across 9 different namespaces with no consistency. A user configuring layout must add `using` statements from several unrelated namespaces, and some enums even have duplicate definitions in different namespaces.

**Current state — all layout enums and where they live:**

| Enum | Current Namespace | File |
|---|---|---|
| `DimensionUnitType` | `Gum.DataTypes` | `GumDataTypes/DimensionUnitType.cs` |
| `HierarchyDependencyType` | `Gum.DataTypes` | `GumDataTypes/DimensionUnitType.cs` |
| `PositionUnitType` | `Gum.Managers` | `GumDataTypes/UnitConverter.cs` |
| `GeneralUnitType` | `Gum.Converters` | `GumDataTypes/UnitConverter.cs` |
| `XOrY` | `Gum.Converters` | `GumDataTypes/UnitConverter.cs` |
| `ChildrenLayout` | `Gum.Managers` | `Gum/Managers/StandardElementsManager.cs` |
| `HorizontalAlignment` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/HorizontalAlignment.cs` |
| `VerticalAlignment` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/VerticalAlignment.cs` |
| `TextOverflowHorizontalMode` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/TextOverflowMode.cs` |
| `TextOverflowVerticalMode` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/TextOverflowMode.cs` |
| `OverlapDirection` | `Gum.Graphics` | `RenderingLibrary/Graphics/TextOverflowMode.cs` |
| `TextRenderingMode` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/Text.cs` |
| `TextRenderingPositionMode` | `RenderingLibrary.Graphics` | `RenderingLibrary/Graphics/Text.cs` |
| `TextRenderingPositionMode` | `Gum.Renderables` | `Runtimes/RaylibGum/Renderables/Text.cs` (duplicate) |
| `CircleOrigin` | `RenderingLibrary.Math.Geometry` | `RenderingLibrary/Math/Geometry/LineCircle.cs` |
| `CircleOrigin` | `Gum.Renderables` | `Runtimes/RaylibGum/Renderables/LineCircle.cs` (duplicate) |
| `Anchor` | `Gum.Wireframe` | `GumRuntime/GraphicalUiElement.cs` |
| `Dock` | `Gum.Wireframe` | `GumRuntime/GraphicalUiElement.cs` |
| `TextWrapping` | `Gum.Forms` | `MonoGameGum/Forms/TextWrapping.cs` |
| `GridUnitType` | `Gum.Forms.Controls` | `MonoGameGum/Forms/Controls/GridUnitType.cs` |

**Note on GumDataTypes vs GumCommon:** The `GumDataTypes` .csproj is only referenced by legacy projects (`GumRuntimeStandard`, `GumRuntimeNet6`, `GumRuntime`). All modern projects reference `GumCommon`, which file-links the source files from the `GumDataTypes/` folder. The source files in `GumDataTypes/` matter, but the assembly does not — everything is compiled into GumCommon.

**Problems:**

1. **Nine namespaces** for 19 enums. A user doing basic layout work needs `using` statements from `Gum.DataTypes`, `Gum.Managers`, `RenderingLibrary.Graphics`, and `Gum.Wireframe` at minimum.
2. **Duplicate definitions** of `TextRenderingPositionMode` and `CircleOrigin` in separate namespaces (Raylib defines its own copies in `Gum.Renderables`).
3. **Enums defined inside unrelated files.** `PositionUnitType` and `GeneralUnitType` live in `UnitConverter.cs`. `ChildrenLayout` lives in `StandardElementsManager.cs`. `Anchor` and `Dock` are defined inline in `GraphicalUiElement.cs`.
4. **Namespace names leak internal architecture.** `RenderingLibrary.Graphics` is an engine internal; `Gum.Managers` and `Gum.Converters` describe the code's role in the tool, not the user-facing concept.

**Decision needed:** What should the unified namespace be? One option:

- Move all layout enums to a single namespace like `Gum.Layout` or `Gum.DataTypes`. This gives users a single `using` statement for all layout configuration.
- Text-specific enums (`TextOverflowHorizontalMode`, `TextRenderingMode`, etc.) could optionally stay in a `Gum.Text` or similar namespace if grouping by domain is preferred.
- Forms-specific enums (`TextWrapping`, `GridUnitType`) may belong in `Gum.Forms` since they are only relevant to Forms users.

**Migration approach:** This will use the Gum.Analyzers project (see below). The enum namespace change will be a simultaneous break + auto-fix: the namespaces move, and the analyzer provides one-click migration.

**Duplicate elimination:** The Raylib-specific duplicates (`TextRenderingPositionMode`, `CircleOrigin` in `Gum.Renderables`) should be removed in favor of the canonical definitions once a unified namespace is chosen.

---

### Gum.Analyzers — Migration Tooling

Gum will include a Roslyn analyzer project (`Gum.Analyzers`) that provides automated migration assistance for breaking namespace changes. The analyzer ships with the Gum packages and provides compiler warnings with one-click code fixes when users reference types via old namespaces.

#### Why an analyzer

C# enums cannot have `[Obsolete]` attributes that redirect to a new namespace, and implicit conversion operators cannot be defined on enums. This means there is no way to have a graceful deprecation period where both old and new namespaces work simultaneously. The break and the fix must ship together. The analyzer makes that break painless — users see warnings with auto-fix suggestions in their IDE and can resolve everything with "Fix all in solution."

#### Design

The analyzer is **data-driven**: it reads from a mapping table of (old namespace, type name) -> (new namespace, type name). Adding future migrations (such as the runtime namespace unification) is just adding rows to the table. The analyzer infrastructure is written once.

The analyzer produces two diagnostics:
- **`GUM001: Type moved to new namespace`** — triggered when a `using` directive or fully-qualified reference uses an old namespace for a type that has moved. The code fix rewrites the `using` statement or qualified name.
- **`GUM002: Duplicate type definition removed`** — triggered if user code references a Raylib-specific duplicate that has been deleted in favor of the canonical definition.

#### Project setup

- **Target framework:** `netstandard2.0` (required — Roslyn analyzers must run in the compiler process, which uses netstandard2.0).
- **Project location:** `Tools/Gum.Analyzers/Gum.Analyzers.csproj`
- **Test project:** `Tests/Gum.Analyzers.Tests/` — analyzer tests use the `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` package.

#### How it reaches users

**NuGet consumers:** The analyzer DLL is placed in the `analyzers/dotnet/cs/` folder of the MonoGameGum, RaylibGum, and SkiaGum NuGet packages. NuGet loads it automatically at build time.

**Direct project reference (source linking):** The runtime projects reference the analyzer with special MSBuild attributes:

```xml
<ProjectReference Include="..\..\Tools\Gum.Analyzers\Gum.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

`OutputItemType="Analyzer"` tells the compiler to load the DLL as an analyzer rather than a library reference. `ReferenceOutputAssembly="false"` prevents it from appearing in the build output. This means anyone who references MonoGameGum, RaylibGum, or SkiaGum — whether via NuGet or direct ProjectReference — gets the analyzer automatically.

#### Rollout plan

**Pacing principle:** One refactor per monthly release, even if it's non-breaking. This keeps each change isolated for testing, makes regressions easy to trace to a specific release, and avoids overwhelming users with multiple simultaneous migrations.

**Phase 1 — Infrastructure (April 2026).** No breaking changes. Get the plumbing in place and shipping so it can be tested in the wild before it matters. **(Done)**

- `GumSyntaxVersionAttribute` class in `GumDataTypes/`, file-linked into GumCommon.
- `[assembly: GumSyntaxVersion(Version = 0)]` on MonoGameGum, RaylibGum, and SkiaGum.
- `SyntaxVersion` field in `CodeOutputProjectSettings` (`.codsj`), defaulting to `"*"` (auto-detect).
- Auto-detection logic: parse the game project's `.csproj` to find the Gum reference, then read the syntax version from the NuGet cache DLL (for PackageReference) or from `AssemblyAttributes.cs` source text (for ProjectReference).
- Syntax version displayed in the Gum tool's codegen UI.
- `docs/gum-tool/upgrading/syntax-versions.md` documenting the version table.
- `Gum.Analyzers` Roslyn project with data-driven namespace remapper and code fix provider. Empty mapping table — ships but does nothing yet. Wired into all three runtime projects.

**Phase 2 — Runtime namespace unification (target: TBD).** Non-breaking change using the compatibility shim pattern.

This uses the same approach that was used for the Forms controls migration: create the runtime class in the new unified namespace, make the old-namespace class inherit from it (adding nothing), and mark the old class `[Obsolete]`. Existing user code keeps working — they're using a derived class that's functionally identical. After a release cycle, promote to `[Obsolete(error: true)]`, then eventually delete the shims.

- Decide on the target namespace (e.g., `Gum.Runtimes`).
- Create runtime classes in the new namespace.
- Convert old-namespace classes to thin derived shims marked `[Obsolete]`.
- Update internal Gum code to use the new namespace.
- Update code generator to emit new-namespace `using` statements for syntax version >= 1.
- Bump syntax version to 1.

**Phase 3 — Enum namespace unification (target: TBD, after Phase 2).** Breaking change — requires the analyzer.

Unlike runtime classes, enums cannot use a base/derived compatibility shim because C# enums don't support inheritance. This is a hard break that ships simultaneously with the Roslyn analyzer auto-fix.

- Decide on the target namespace (e.g., `Gum.Layout`).
- Move the enums to the new namespace in source files. Guard with `#if !FRB` for FlatRedBall 1 compatibility.
- Update all internal Gum code.
- Add the migration mappings to the analyzer.
- Bump the syntax version to 2. Update the code generator to emit new-style `using` statements when it reads version >= 2.
- Eliminate Raylib duplicate enum definitions.
- Update the syntax versions doc.

**Future phases** follow the same pattern: one change per release, assess whether it can use compatibility shims (non-breaking) or requires the analyzer (breaking), update syntax version and codegen.

#### Code generation compatibility

The Gum tool includes a code generator that emits C# source files for game projects. When namespaces change, the code generator must emit the correct `using` statements for the version of Gum the target project actually references — otherwise the generated code won't compile.

The problem: the Gum tool and the Gum runtime can be at different versions. A user might update the Gum tool but not their runtime NuGet packages (or vice versa). The code generator cannot assume the target project understands the new namespaces. And because the code generator runs in the Gum tool (before compilation), there may be no compiled DLL in the user's `bin/` folder to reflect on.

**Solution: two-layer version detection.**

**Layer 1 — Assembly-level syntax version attribute.** Following the same pattern used by FlatRedBall, the Gum runtime assemblies carry a version attribute:

```csharp
public class GumSyntaxVersionAttribute : Attribute
{
    public int Version;
}

// In AssemblyAttributes.cs:
[assembly: GumSyntaxVersion(Version = 0)]
```

This attribute is the **source of truth** for what a Gum runtime assembly supports. It is read by the Roslyn analyzer (at compile time, when DLLs are always available) and by the code generator's auto-detection logic (see below).

**Layer 2 — `.codsj` setting with auto-detection.** The `CodeOutputProjectSettings` (stored in `ProjectCodeSettings.codsj`) gets a new `SyntaxVersion` field:

| Value | Behavior |
|---|---|
| `"*"` (default) | Auto-detect from the referenced Gum runtime. This is the default for new projects and for existing projects where the field is absent. |
| `"0"`, `"1"`, etc. | Explicit override. The code generator uses this value directly, skipping auto-detection. |

**Auto-detection** works by inspecting the game project's `.csproj` (located via `CodeProjectRoot`):

1. **NuGet PackageReference:** The code generator finds the Gum package reference, locates the DLL in the NuGet cache (`~/.nuget/packages/`), and reads the `GumSyntaxVersionAttribute` via reflection. The NuGet cache always has the DLL — no build required.
2. **Direct ProjectReference:** The code generator follows the project reference path and looks for `AssemblyAttributes.cs` in the referenced project. It parses the syntax version from the source text. No build required.
3. **Neither found / parse failure:** Falls back to version 0 (pre-unification) and displays a warning in the UI.

**Syntax version table:**

| Syntax Version | Meaning |
|---|---|
| 0 | Baseline. No breaking changes from the pre-attribute era. Emit old namespaces (`Gum.DataTypes`, `Gum.Managers`, `RenderingLibrary.Graphics`, etc.) |
| 1 | Enum namespace unification. Emit new unified namespace. |
| 2+ | Reserved for future changes (e.g., runtime namespace unification). |

**Why this works:**
- **Automatic by default.** With `"*"` as the default, upgrading the NuGet package is the only step needed — the code generator detects the new version and emits the correct namespaces immediately. No manual setting change.
- **Explicit override for edge cases.** Users with unusual project structures (monorepos, custom package feeds, etc.) can hardcode the version in `.codsj` to bypass auto-detection.
- **Backward compatible.** Old Gum assemblies lack the attribute, so auto-detection returns 0. Existing `.codsj` files without the field default to `"*"`. Nothing breaks.
- **Version-agnostic.** No need to parse NuGet version strings or maintain a version-to-feature mapping. The attribute directly declares what the assembly supports.
- **Familiar pattern.** FlatRedBall already uses `SyntaxVersionAttribute` for the same purpose, so the primary user base understands the concept.

**The UI** displays the resolved syntax version along with how it was determined: "Syntax Version: 0 (auto-detected from NuGet)", "Syntax Version: 1 (auto-detected from ProjectReference)", or "Syntax Version: 0 (manual override)". This makes it easy to diagnose issues.

#### Pre-warning philosophy

The analyzer does **not** pre-warn users about upcoming changes. A warning that says "this will change next release" but offers no actionable fix is noise — users cannot switch to the new namespace before it exists, so the warning just trains them to ignore warnings. Instead:

- **Before the breaking release:** Communicate through documentation (`docs/gum-tool/upgrading/`), release notes, and changelogs.
- **In the breaking release:** The break and the auto-fix arrive together. Users experience a 10-second migration, not a surprise.

---

## General Guidelines

### How to Contribute

**Fixing a bug or adding a property:** Apply the change to all files for that runtime. Verify the behavior is consistent. If a property does not apply to a specific backend (see the legitimate differences tables), skip that file and leave a comment noting the omission and why.

**Platform-specific changes:** If a change only makes sense for one backend, add a comment at the point of divergence so future contributors understand the intent. For example:

```csharp
// BitmapFont is not applicable to SkiaGum because Skia handles font
// loading through its own type system (SKTypeface).
```

**Working toward a single file:** The target state is one shared file per runtime, not a base class. Each incremental step should make the files more identical, not more abstracted. Resist the urge to extract helpers or base classes — the payoff comes when the files can simply be deleted and replaced with one.

**Verifying your change:** After touching a runtime file, build and do a quick smoke test on the affected backend if possible. At minimum, confirm the other files still compile cleanly.

### Common Legitimate Differences (all runtimes)

These patterns recur across most runtimes and are expected:

| Difference | Detail |
|---|---|
| Base class | All runtimes inherit `InteractiveGue` (or `SkiaShapeRuntime : InteractiveGue` for SkiaGum shapes). SkiaGum runtimes were migrated from `GraphicalUiElement` to `InteractiveGue` in April 2026 to enable future Forms/input support. |
| Color type | XNA `Color`, Raylib `Color`, or `SKColor` — use `#if` guards |
| `BlendState`/`Blend` | XNA-like only |
| `AddToManagers()` | Marked `[Obsolete]` in all non-FRB builds (April 2026). Users should use `AddToRoot()` instead. FRB keeps it permanently — guarded with `#if !FRB`. The two-parameter `AddToManagers(ISystemManagers, Layer?)` is NOT obsolete and is required for multi-instance scenarios (e.g. SkiaGum). Do not add the parameterless overload to new runtimes. Will be removed from non-FRB builds in a future release. |
| `Clone()` override | Present in some SkiaGum runtimes but not MonoGame/Raylib — should be unified if needed |
| Texture types | `Texture2D` (MonoGame), `Texture2D` (Raylib), `SKBitmap`/`SKImage` (SkiaGum) |
