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
   - **Drain a blocking singleton on the spot — don't ask about timing.** There is a dedicated later phase for draining `.Self` singletons wholesale, but when a singleton is *in the way of the current task* — most often because the class can't be unit-tested until it takes its dependencies via the constructor — convert it to constructor injection as part of the current PR. Do **not** stop to ask whether this conflicts with the dedicated drain phase; it doesn't, and that question is noise. Make the judgment call yourself; only pause to ask if the drain is genuinely *dangerous* (a construction cycle you can't break cleanly) or its call-site blast radius is large enough to deserve its own PR. Expanding an interface (adding a method) or switching a concrete dependency to its interface in service of the drain is fine.
   - **Breaking the DI cycle the `Self`+`Initialize`+`Locator` pattern was hiding.** A class kept as a `Self` singleton that resolves its own dependencies inside an `Initialize()` via `Locator` is often doing that to dodge a *construction cycle* (a dependency's constructor needs the class back). When you move it to real constructor injection, that cycle resurfaces as a DI exception at startup. Break it by injecting the back-edge dependency as `Lazy<T>` — the tool's DI already registers `Lazy<>` (see `Builder.cs`), so `Lazy<IFoo>` resolves and you access `.Value` at call time, past graph construction. Inject only the cycling dependencies lazily; keep the acyclic ones direct. **When you can choose which edge to lazy, prefer lazying the *consumer's* edge to the drained class** (`Lazy<IDrained>` in the higher-level class) rather than the drained class's back-edge: that defers the *entire* drained subtree off the consumer's construction path, which stays correct even when the cycle travels through a third hop (`A → B → drained → A`) — whereas lazying only the direct back-edge does not break such multi-hop cycles. Example: `CommandLineManager` (drained, #3277) ↔ `ProjectManager`; the `Lazy<ICommandLineManager>` went into `ProjectManager` (the consumer), keeping `CommandLineManager`'s own deps direct.

## When testability is the driver

If the reason you're tempted to make something `static` is "so a test can call it without constructing the owner," the right answer is the opposite: keep it instance, and either (a) construct the owner in the test with stub dependencies, or (b) extract the logic into a new small class that's cheap to construct. The test's friction is a signal that the owner has too many responsibilities, not that the method should be static.

**An extraction-for-testability is not done until the extracted unit has a test.** When you pull logic into a new class/service/ViewModel/utility specifically to make it testable, add the test before considering the refactor complete — test-first if the move also changes behavior, or a *characterization (pinning) test* capturing current behavior if the move preserves it. Do not let the [tdd](../tdd/SKILL.md) skill's refactor/rename exemption talk you out of it: extracting a new unit is not a pure rename, and an extracted-but-untested seam wastes the entire point of the extraction.

**Drains are usually behavior-preserving — calibrate the test to that.** Most singleton/`Locator` drains change only *how* a dependency is obtained (static `Locator` → injected field), not what the code does, so test-first TDD has nothing to specify — the fitting tool is a *characterization (pinning) test*. But the compiler and DI container already verify most of a drain: injection wiring is type-checked, construction cycles throw at startup, and a `static`→instance conversion makes the compiler flag every stale call site. So a pinning test's marginal value is modest — add one where the injected seam is clean and the assertion is meaningful, and skip it where the harness would have to contort (e.g. hand-rolling a delegate to mock an `out` parameter) in favor of the compiler plus a manual check. The drain's real payoff is *enabling* later behavior tests, not the test written during the drain itself. This does **not** loosen the extraction rule above: pulling out a *new unit* still requires its own test — a `static`→instance conversion is not an extraction.

## Before refactoring across runtimes

If a refactor touches shared runtime/rendering code (`GumCommon`, `RenderingLibrary`, anything under `Runtimes/`, or `MonoGameGum`), read [gum-runtime-topology](../gum-runtime-topology/SKILL.md) first. The same source is compiled into many assemblies and into FlatRedBall via shared projects, so "builds clean in `AllLibraries.sln`" does not mean "didn't break a consumer" (the WPF runtime and FRB are not in that solution).

## Converging per-platform duplicate files

A recurring Gum refactor is driving two (or more) per-platform copies of the same file toward byte-for-byte identical content, so they can eventually collapse to one `#if`-gated linked source. This is done incrementally — block by block, mirroring `#if RAYLIB` / `#if !RAYLIB` in *both* copies wherever a difference is genuinely platform-specific, so the cross-file diff shrinks toward empty. If you're touching code that exists as duplicated per-platform copies (e.g. `CustomSetPropertyOnRenderable.cs`, `GueDeriving/*Runtime.cs`), read [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md) for the full technique before editing.

## Why this matters in Gum

Gum's tool code is mid-migration from static singletons (`PluginManager.Self`, `*Manager.Self`, etc.) to constructor-injected services. Every new `static` is a step backward and undoes that migration's payoff. The runtime libraries are similar: `GraphicalUiElement` is already bloated, and the project memory explicitly calls out "avoid adding new properties/methods directly to this class — prefer separate controller/manager classes." The direction in this skill is the same direction the rest of the codebase is moving.
