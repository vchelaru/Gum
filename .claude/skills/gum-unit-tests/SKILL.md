---
name: gum-unit-tests
description: Reference guide for writing unit tests in the Gum repository. Load this when writing or modifying tests in Gum.ProjectServices.Tests, Gum.Cli.Tests, or any other Gum test project.
---

# Gum Unit Test Reference

## Test Projects

| Project | Location | What it tests |
|---------|----------|---------------|
| `Gum.ProjectServices.Tests` | `Tests/Gum.ProjectServices.Tests/` | Headless services: error checking, codegen, font generation, project loading |
| `Gum.Cli.Tests` | `Tests/Gum.Cli.Tests/` | CLI command exit codes and output |

## Assertions

Always use **Shouldly** — never xUnit `Assert`. Alphabetize test methods within a class.

## Singletons and Parallelism

Gum uses global singletons (`ObjectFinder.Self`, `StandardElementsManager.Self`). All test projects must disable parallel execution:

```csharp
// TestAssemblyInitialize.cs
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

## BaseTestClass

All test classes that need a project context inherit from `BaseTestClass` (`Tests/Gum.ProjectServices.Tests/BaseTestClass.cs`). **Read it before adding setup to a subclass** — it already handles:

- `StandardElementsManager.Self.Initialize()` — required before any code that traverses element inheritance or resolves component defaults; omitting it causes "You must first call Initialize" exceptions.
- A `GumProjectSave` with all five standard elements (`Container`, `NineSlice`, `Sprite`, `Text`, `ColoredRectangle`), each with a Default state and `ParentContainer` set. Without Default states, `DefaultState` returns null and inheritance traversal crashes.
- `Dispose()` that clears `ObjectFinder.Self.GumProjectSave` so state doesn't leak between tests.

**Do not repeat these in subclass constructors.** Only add subclass-specific setup — for example, adding font variables to the Text standard element's existing Default state, or setting `ObjectFinder.Self.GumProjectSave = Project` (not done in `BaseTestClass` itself since not every test needs it).

## Critical Rule: States Must Have a ParentContainer

**Every `StateSave` used in a test must have `ParentContainer` set to the element it belongs to.** Never use `new StateSave()` standalone and pass it directly to methods that call `GetValueRecursive` or other extension methods on `StateSave`.

`GetValueRecursive` and `GetVariableRecursive` traverse the element hierarchy via `stateSave.ParentContainer`. When `ParentContainer` is null, the traversal skips most logic silently — but some code paths still proceed and call methods like `IsState(elementContainingState, ...)` with a null argument, causing `ArgumentNullException`.

**Wrong:**
```csharp
StateSave state = new StateSave();
state.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Arial" });
_sut.TryGetBmfcSaveFor(null, state, ...); // crashes in GetValueRecursive
```

**Right:**
```csharp
ScreenSave screen = new ScreenSave { Name = "TestScreen" };
StateSave state = AddState(screen); // sets ParentContainer, adds to screen.States
SetVar(state, "Font", "Arial");
_sut.TryGetBmfcSaveFor(null, state, ...);
```

Use `ScreenSave` as the element for standalone state tests — it has no base type and doesn't trigger the `ComponentSave`-specific `StandardElementsManager` fallback.

The `forcedValues` parameter in `TryGetBmfcSaveFor` is queried only via `GetValue()` (not `GetValueRecursive`), so it is safe to pass as a bare `new StateSave()`.

## Standard Helper Pattern

```csharp
private static StateSave AddState(ElementSave element, string name = "Default")
{
    StateSave state = new StateSave { Name = name };
    state.ParentContainer = element;
    element.States.Add(state);
    return state;
}

private static StateSave AddCategoryState(ElementSave element, string categoryName, string stateName)
{
    StateSaveCategory category = element.Categories.FirstOrDefault(c => c.Name == categoryName)
        ?? new StateSaveCategory { Name = categoryName };
    if (!element.Categories.Contains(category))
        element.Categories.Add(category);
    StateSave state = new StateSave { Name = stateName };
    state.ParentContainer = element;
    category.States.Add(state);
    return state;
}

private static void SetVar(StateSave state, string name, object value)
{
    state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value });
}
```

## ComponentSave vs ScreenSave in Tests

- **`ScreenSave`**: no base type by default → safe for isolated tests; traversal stops at the element boundary.
- **`ComponentSave` with `BaseType = "Container"`**: traversal goes into the Container standard element, then falls back to `StandardElementsManager.Self.GetDefaultStateFor("Component")`. Only use `ComponentSave` when the test specifically needs component behavior (e.g. testing instance inheritance). `BaseTestClass` already handles the required `StandardElementsManager` initialization.

## ObjectFinder Setup

When testing code that calls `ObjectFinder.Self.GetElementSave(...)` (e.g. inheritance traversal, instance type lookup):

```csharp
ObjectFinder.Self.GumProjectSave = Project;
```

Elements must be in `Project.Screens`, `Project.Components`, or `Project.StandardElements` for `ObjectFinder` to find them.

## InternalsVisibleTo

`Gum.ProjectServices.csproj` has `<InternalsVisibleTo Include="Gum.ProjectServices.Tests" />`. Tests can call `internal` methods directly — no reflection needed.
