---
name: gum-architecture-layers
description: "The three layers of Gum: Forms (Button, FrameworkElement), the visual (Runtime: GueDeriving *Runtime, GraphicalUiElement) and Renderable, and which owns what. Triggers: what layer is this, Forms vs Visual vs Renderable, button.Visual, FormsControlAsObject, GueDeriving, confusing a Runtime with a Renderable, why the tool shows visual variables for a Forms control, behavior variables."
---

# Gum's Three Layers

Gum's visual stack is three layers, each owned by a different degree of sharing. The Runtime layer is what the docs, the tool and most other skills call the visual (`GraphicalUiElement` and the `GueDeriving` `*Runtime` classes):

| Layer | Example | Owns | Shared how |
|---|---|---|---|
| **Forms** | `Button`, `TextBox` | Lookless interaction/state logic | Same `.cs` file for every backend — `FrameworkElement` and a growing set of controls arrive via the `GumCommon` project reference; the rest are still source-linked per backend from `MonoGameGum/Forms/` |
| **Runtime (the visual)** | `SpriteRuntime`, `TextRuntime`, `RectangleRuntime` (`GueDeriving`) | Gum layout + property forwarding to a Renderable | *Can* be linked across backends; some backends keep their own copy instead |
| **Renderable** | MonoGame `Sprite`, raylib draw calls | Actual per-backend drawing code | Effectively never shared — except XNA-likes (MonoGame/KNI/FNA), which are close enough to share |

Forms are lookless — the same `Button.cs` works identically whether its `Visual` is a MonoGame, Skia, or raylib tree, because Forms only ever talks to a Runtime through Gum's property/layout API, never to backend drawing code directly. See **gum-forms-controls** for the Forms↔Visual split (a Visual is a Runtime tree rooted in an `InteractiveGue`).

Runtime-level sharing across backends is real but partial — see **gum-runtime-topology** for which Runtimes are actually linked vs. forked per backend, and why (`#if` gates for texture/blend differences). Renderable-level drawing specifics (batching, matrices, effects) are backend-specific; see **gum-monogame-rendering** for the XNA-family renderer. The Runtime→Renderable property bridge (`SetProperty` → `CustomSetPropertyOnRenderable` → renderable) is in **gum-property-assignment**.

## The tool shows the visual

In Gum everything is a visual, so selecting a Button instance in the tool lists every visual variable (X, XUnits, origins, ...), not only button variables. In code a `Button` is only the Forms control and the visual is `button.Visual`. A behavior's `FormsProperties` appear in the instance's "Behavior" category, which puts Forms-only properties next to the visual variables; a `VariableReferences` line can read them, though references otherwise act on the visual. That case is in **gum-forms-behaviors** ("Two Layers in One Variable List"). The user docs cover the same split: [Controls vs Visuals: What Goes Where](../../../docs/code/about/for-wpf-users.md#controls-vs-visuals-what-goes-where) and [Visual/Logical Tree Separation](../../../docs/code/about/for-wpf-users.md#visuallogical-tree-separation).

## Why "Runtime" types exist

Originally there were only Renderables and `GraphicalUiElement` — no `SpriteRuntime` class existed; a "sprite" was just a `GraphicalUiElement` whose contained renderable happened to be a `Sprite`, manually assigned. Concrete `*Runtime` classes (`GueDeriving`) were added because that manual-assignment pattern was boilerplate-heavy for code-only usage (users just wanted `new SpriteRuntime()`); they're a convenience layer over the same GUE+renderable pattern, not a structural change to it. Manually building a GUE and assigning it a raw renderable is now rare.

## Why `CustomSetPropertyOnRenderable` exists, and its two dispatch styles

All state/deserialized-project values are applied to a GUE through the string-based `SetProperty` path (see **gum-property-assignment**), never set directly on a renderable. But some properties — a texture reference, for example — have no home on `GraphicalUiElement` itself; they only make sense on the renderable (or, now, the Runtime). `CustomSetPropertyOnRenderable` exists purely to redirect that string-path value to wherever it actually lives.

Its dispatch tree currently mixes two strategies, and you can't assume which one a given property uses without checking:

- **Renderable-type dispatch** (`renderableIpso is LineCircle`, `is LinePolygon`, etc.) — the original style, predating Runtime types, that reaches straight into the renderable.
- **Runtime-type dispatch** (`graphicalUiElement is CircleRuntime`, `is SpriteRuntime`, routing into `TrySetPropertyOn*Runtime`) — sets the typed Runtime property instead, added where a renderable-type-only dispatch would be ambiguous or wrong (e.g. `CircleRuntime`/`RectangleRuntime` own two renderable slots — fill and stroke — so dispatching on the contained renderable's type can land a value on the wrong slot).

**Direction:** prefer Runtime-type dispatch for new branches. Runtimes are the layer that's actually portable across backends (see the table above); renderables mostly aren't. Dispatching on renderable type bets that backend divergence stays local to that `#if`; dispatching on Runtime type bets the Runtime's property surface is unifiable — and pays off further as more Runtimes get shared across backends, since a Runtime-typed branch becomes shared code too. Where a property is genuinely backend-specific, Runtime-type dispatch doesn't eliminate the `#if` but does relocate it into the Runtime class, concentrating backend variance in one place instead of scattering it through this dispatch tree.

This direction relies on the GUE actually *being* the concrete Runtime type at dispatch time, which it is for the common case: `ElementSaveExtensions.CreateGueForElement` tries `RegisterGueInstantiation<T>`'s strongly-typed factories first — the doc comment on that method calls it "the primary way Gum builds the visual tree" — and only falls back to the renderable-only `CustomCreateGraphicalComponentFunc` path (`Gum.Wireframe.FallbackRenderableFactory`) when no typed wrapper is registered for that base type.

For the concrete decision and the phased order to converge the two `CustomSetPropertyOnRenderable` files onto this direction (runtime property parity → redispatch → diff-converge → conditionally link), see `Direction/decisions/0007-converge-skia-property-dispatch.md` and `0008-sequence-runtime-dispatch-convergence.md`.
