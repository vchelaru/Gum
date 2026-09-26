# Dogfooding the Variables tab headlessly

`VariableGridHarness` hosts the tool's own Variables tab (the head's singleton `PropertyGridManager`,
plugin and view) over a temp project and drives it the way a user does. The find-a-bug, pin-it,
fix-it loop is the one in `Animations/README.md`; the shared pieces are in `../Harness/README.md`.

```
dotnet test Tests/Gum.Avalonia.Tests --filter "FullyQualifiedName~Gum.Avalonia.Tests.VariableGrid"
```

## Writing a scenario

```csharp
using VariableGridHarness grid = new VariableGridHarness();
ComponentSave button = grid.Project.AddComponent("Button");
InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
grid.Select(label);

grid.TypeAndEnter("X", "42");

VariableGridHarness.StoredValue(button, "Label.X").ShouldBe(42f);
```

- Select through `grid.Select(...)` (element, instance, several instances, state); it raises the
  same events a tree or States-tab click does.
- Gestures: `TypeAndEnter`, `TypeAndLeave`, `PressToggle`, `PickComboItem`, `PickRowMenuItem`,
  `TypeLinesAndApply` (VariableReferences), plus `grid.Input` for anything else.
- Rows are found by name; an instance row also answers to its unqualified name (`"X"` for `"Label.X"`).
- Undo and redo go through `grid.Undo()` / `grid.Redo()`. Undo replaces the element with a copy,
  so read results from `grid.SelectedState.SelectedElement`, not the reference you built.
- Assert on the element (`StoredValue`), the grid (`FieldText`, `Member(...)`), and the saved file
  (`ReadSaved`) where they apply.

## Gotchas

- A value equal to the inherited one stores nothing visible to `StoredValue` checks that expect a
  change (Container's default Width is 150, Text's default text is "Hello", its color is white). Pick
  values that differ.
- Text is a multi-line field: Enter adds a line, so commit it with `TypeAndLeave`.
- A combo's drop-down is its own top level that window input does not reach; `PickComboItem` makes
  the selection a click would make while it is open.
