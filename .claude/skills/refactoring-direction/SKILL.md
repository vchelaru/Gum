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

## Why this matters in Gum

Gum's tool code is mid-migration from static singletons (`PluginManager.Self`, `*Manager.Self`, etc.) to constructor-injected services. Every new `static` is a step backward and undoes that migration's payoff. The runtime libraries are similar: `GraphicalUiElement` is already bloated, and the project memory explicitly calls out "avoid adding new properties/methods directly to this class — prefer separate controller/manager classes." The direction in this skill is the same direction the rest of the codebase is moving.
