---
name: refactoring-direction
description: "Refactoring direction rules for Gum. Trigger when proposing or performing refactors that change how code is shaped — extracting helpers, choosing between static and instance, deciding where new logic should live. Applies to all Gum source projects."
---

# Refactoring direction

When refactoring Gum code, **always move toward instances, interfaces, and dedicated single-responsibility classes**. Never move the other way.

## Specific rules

1. **Never promote an instance method, field, or class to `static`.** Even if the method "has no instance state right now," keep it as an instance member. Static methods are sticky — they grow callers everywhere, can't be substituted in tests, and can't be evolved without source-breaking every caller. Instance methods preserve future optionality.

2. **Never demote an interface to a concrete type.** If a parameter or field is currently typed as an interface (`ISelectedState`, `IDialogService`, `IUndoManager`, etc.), keep it that way. Adding a new dependency? Inject it as an interface, not a concrete class.

3. **Prefer extracting a new single-responsibility class over adding to a "god class."** `GraphicalUiElement`, `CodeGenerator`, and similar large classes should not grow. New behavior goes in a new controller/service class that the existing class composes with, even if the new class starts with one method.

4. **Prefer constructor injection over `*.Self` singletons.** Don't reverse the tool's migration off static singletons.
   - `ObjectFinder.Self` is the sanctioned exception — fine in new or refactored code; it won't be removed. No other singletons should be reintroduced.
   - Plugins are MEF-composed: `[ImportingConstructor]` can inject only interfaces registered in `PluginManager.LoadPlugins` via `batch.AddExportedValue<T>` (today just `ISelectedState`). To inject anything else, register it there first; otherwise keep `Locator.GetRequiredService<T>()` in the body (see `MainStatePlugin`).
   - Replacing a `Locator.GetRequiredService<T>()` call with an injected `T` (one already exported to MEF) is good drive-by cleanup.

## When testability is the driver

If the reason you're tempted to make something `static` is "so a test can call it without constructing the owner," the right answer is the opposite: keep it instance, and either (a) construct the owner in the test with stub dependencies, or (b) extract the logic into a new small class that's cheap to construct. The test's friction is a signal that the owner has too many responsibilities, not that the method should be static.

**An extraction-for-testability is not done until the extracted unit has a test.** When you pull logic into a new class/service/ViewModel/utility specifically to make it testable, add the test before considering the refactor complete — test-first if the move also changes behavior, or a *characterization (pinning) test* capturing current behavior if the move preserves it. Do not let the [tdd](../tdd/SKILL.md) skill's refactor/rename exemption talk you out of it: extracting a new unit is not a pure rename, and an extracted-but-untested seam wastes the entire point of the extraction.

## Before refactoring across runtimes

If a refactor touches shared runtime/rendering code (`GumCommon`, `RenderingLibrary`, anything under `Runtimes/`, or `MonoGameGum`), read [gum-runtime-topology](../gum-runtime-topology/SKILL.md) first. The same source is compiled into many assemblies and into FlatRedBall via shared projects, so "builds clean in `AllLibraries.sln`" does not mean "didn't break a consumer" (the WPF runtime and FRB are not in that solution).

## Converging per-platform duplicate files

A recurring Gum refactor is driving two (or more) per-platform copies of the same file toward byte-for-byte identical content, so they can eventually collapse to one `#if`-gated linked source. This is done incrementally — block by block, mirroring `#if RAYLIB` / `#if !RAYLIB` in *both* copies wherever a difference is genuinely platform-specific, so the cross-file diff shrinks toward empty. If you're touching code that exists as duplicated per-platform copies (e.g. `CustomSetPropertyOnRenderable.cs`, `GueDeriving/*Runtime.cs`), read [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md) for the full technique before editing.

## Why this matters in Gum

Gum's tool code is mid-migration from static singletons (`PluginManager.Self`, `*Manager.Self`, etc.) to constructor-injected services. Every new `static` is a step backward and undoes that migration's payoff. The runtime libraries are similar: `GraphicalUiElement` is already bloated, and the project memory explicitly calls out "avoid adding new properties/methods directly to this class — prefer separate controller/manager classes." The direction in this skill is the same direction the rest of the codebase is moving.
