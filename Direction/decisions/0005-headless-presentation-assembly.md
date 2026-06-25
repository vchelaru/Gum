# 0005. Home for the headless presentation layer: a dedicated `Gum.Presentation` assembly

- **Status:** Accepted
- **Date:** 2026-06-24
- **Deciders:** Victor Chelaru, Claude

## Context

ADR-0003 set the target end-state as a headless `net8.0` assembly enforcing the logic↔view
boundary, but deliberately left the *home* open: "grown from `Gum.ProjectServices`, or a new
`Gum.Core`." Phase 3 of `ui-decoupling-plan.md` (moving ViewModels and their service interfaces
headless) cannot start until this is settled — relocating interfaces into the wrong assembly means
moving them twice.

Today: all core UI-service interfaces (`ISelectedState`, `IDialogService`, `IUndoManager`,
`IFileCommands`, `IGuiCommands`, `IElementCommands`, …) live in the WPF tool assembly
(`Gum.csproj`) — the headless side cannot see any of them. `Gum.ProjectServices` is a
project-**operations** layer (load/save, error-check, codegen, font/SVG export, import) genuinely
shared by the **CLI** (`Gum.Cli`) and the tool. The CLI consumes operations; it does not consume
presentation.

## Decision

We will introduce a dedicated headless `net8.0` **`Gum.Presentation`** assembly holding the
ViewModels and the UI-service interface *contracts* (`IDialogService`, `ISelectedState`,
`IUndoManager`, …). Dependency stack: **`Gum.Presentation` → `Gum.ProjectServices` → `GumCommon`**.
Both the WPF tool and any future Avalonia shell reference `Gum.Presentation` and supply the
framework-specific interface *implementations* (e.g. the WPF `DialogService`). The **CLI continues
to reference only `Gum.ProjectServices`**. `Gum.ProjectServices` stays the operations layer and
takes on no presentation code.

## Consequences

- **Easier:** a clean acyclic layer stack; the CLI stays lean (no presentation cruft it never
  calls); the presentation↔operations boundary is **compiler-enforced**, consistent with ADR-0003's
  "compiler, not convention" thesis; both UI shells bind the *same* ViewModels, so there is no logic
  duplication across WPF/Avalonia (only view markup — XAML vs. AXAML — is reimplemented).
- **Harder / cost:** one more project; Phase 3 must relocate interfaces in **dependency-ordered
  clusters** (each interface drags the transitive closure of the types in its signatures);
  framework-specific implementations must be split out to the shell.
- **Watch-out:** the split only pays off if logic actually *leaves* the view code-behind (the
  view-wall classes) — otherwise it just re-duplicates into the Avalonia shell.
- **Watch-out (relocation mechanics):** when a relocated interface returns a value object (e.g.
  `KeyCombination`), that object's framework-coupled methods go to the shell as **extension methods** —
  *except* any method that is mocked **through the interface** in tests. Moq cannot intercept a static
  extension call, so such a method must instead be **promoted to an interface method** on the manager
  (e.g. `IHotkeyManager.IsPressedInControl(KeyCombination)`, PR #3345). Unmocked framework methods
  (`KeyCombination.IsPressed(...)`) stay extensions with no call-site churn when the namespace is kept.
- **Follow-up:** per-interface placement (which implementations move to `Gum.Presentation` vs. stay
  in the shell) is Phase 3 execution detail, not decided here.

## Alternatives considered

- **Grow `Gum.ProjectServices` to hold everything.** Rejected: conflates project-operations with
  presentation; forces the CLI to compile presentation interfaces it never uses; and nothing stops
  operations code from depending on a ViewModel (wrong direction) — the same convention-not-compiler
  weakness ADR-0003 rejected.
- **Folders within one assembly** (operations + presentation namespaces in one `net8.0` project).
  Rejected: a namespace is not a boundary; per ADR-0003 only a project reference durably enforces a
  layer.
- **Keep interfaces in the WPF tool** (status quo). Rejected: this is exactly what blocks Phase 3 —
  a ViewModel cannot go headless while its dependencies' interfaces are stranded in `Gum.csproj`.
