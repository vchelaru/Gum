# 0004. ViewModels expose resolved display state in neutral types (Presentation Model)

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** Victor Chelaru, Claude

## Context

Conventional MVVM pushes display-formatting decisions (when is something visible? what color is
it?) into XAML value converters, leaving ViewModels holding only raw data. That makes the
formatting *decisions* — which are real logic — effectively untestable.

Gum's ViewModels instead **resolve display state directly** (a deliberate choice, good for
testability). Today they do so in **WPF types** (`Visibility`, `Color`, `Brush`,
`WriteableBitmap`). That pins those ~35 ViewModels to the `net8.0-windows` / `UseWPF` assembly and
would **defeat the compiler-enforced boundary** established in ADR-0003 — they could not move into
the headless assembly.

A common assumption is that keeping WPF types is fine because porting them to Avalonia later is
"mechanical." That holds for **`Color`** (near-identical struct) but **not** for the rest:
Avalonia has **no `Visibility` enum** (it uses a `bool IsVisible` — a semantic 3-state→2-state
change, per binding site), and `Brush` / `WriteableBitmap` are not namespace swaps. So the
expensive, common cases are exactly where "mechanical" fails.

## Decision

We will **keep resolving display state in ViewModels** (the **Presentation Model** pattern,
deliberately chosen over converter-based MVVM for testability) — but express it in
**framework-neutral types** so the ViewModels remain eligible for the headless assembly:

- **visibility → `bool`** (also Avalonia's native model, and *less* code than the WPF enum)
- **color → Gum's native color type** (`RenderingLibrary` already has one)
- **brush → resolve to a color** in the ViewModel; the view builds the brush
- **pixel / image data → `byte[]` or a Gum image type**; the view builds the bitmap

Framework types (`System.Windows.*`, later `Avalonia.*`) appear **only in a thin converter / view
layer**. A one-off, single-consumer ViewModel that is permanently a view concern may keep a
framework type **by explicit, documented exception** — never as the default.

## Consequences

- **Easier:** full testability of the formatting decisions is retained; the ViewModels become
  movable into the headless assembly; a future UI swap touches **one converter layer, not ~35
  ViewModels**. `bool` visibility is a strict improvement *today* (cleaner asserts, stock
  `BoolToVisibilityConverter` for WPF).
- **Harder / cost:** a small neutral-type + converter layer to maintain; marginally more ceremony
  than using WPF types directly. Accepted because the common types are cheap or already exist
  (`bool` is free; Gum owns a color type).
- This refines Phase 3 of `ui-decoupling-plan.md` from "remove `System.Windows` from VMs" to
  "expose resolved state in neutral types."

## Alternatives considered

- **Keep WPF types in ViewModels** (the original proposal). Rejected: pins ~35 ViewModels out of
  the headless assembly and rests on a "mechanical port" assumption that holds only for `Color`.
- **Pure converter-based MVVM** (no resolved state in the VM at all). Rejected: sacrifices the
  testability of the formatting decisions, which is the entire reason for resolving in the VM.
