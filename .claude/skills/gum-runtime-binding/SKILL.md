---
name: gum-runtime-binding
description: Reference guide for Gum's runtime data binding system — BindingContext, SetBinding on both GraphicalUiElement visuals and FrameworkElement Forms controls, binding types (string, Binding object, lambda), and how the two systems differ.
---

# Gum Runtime Binding

## Two Binding Systems

**GraphicalUiElement** (`GumRuntime/GraphicalUiElement.Binding.cs`) — basic binding available on all visuals.
**FrameworkElement** (`MonoGameGum/Forms/Controls/`) — richer binding on Forms controls, built on top of the GUE system.

`FrameworkElement.BindingContext` delegates to its `Visual.BindingContext` — they share one context.

## BindingContext

Set on any `GraphicalUiElement` or `FrameworkElement`. Cascades automatically to all descendants unless overridden:

```csharp
root.BindingContext = viewModel;  // all children inherit it
child.BindingContext = other;     // explicit overrides inherited
```

Subscribes to `INotifyPropertyChanged` and updates bound UI properties on change.

## GraphicalUiElement Binding (Visuals)

Simple string-only binding. No converters, no modes, no path traversal:

```csharp
element.SetBinding("X", nameof(vm.Position));           // basic
element.SetBinding("Text", nameof(vm.Name), "{0:N0}");  // with format string
```

`PushValueToViewModel()` is called from property setters to write back to the VM (always two-way implicitly).

## FrameworkElement Binding (Forms)

Three binding styles, all richer than the GUE version.

### 1. String-based

```csharp
textBox.SetBinding(nameof(TextBox.Text), nameof(vm.Name));
```

Shorthand — wraps the string in a default `Binding` object internally.

### 2. Explicit Binding object

```csharp
var binding = new Binding(nameof(vm.IsEnabled))
{
    Mode = BindingMode.OneWay,
    Converter = new BoolToVisibilityConverter(),
    FallbackValue = false
};
checkBox.SetBinding(nameof(CheckBox.IsChecked), binding);
```

`Binding` properties: `Path`, `Mode` (OneWay/TwoWay/OneWayToSource), `UpdateSourceTrigger` (Default/PropertyChanged/LostFocus), `Converter`, `ConverterParameter`, `StringFormat`, `FallbackValue`, `TargetNullValue`.

### 3. Lambda / expression tree

```csharp
// Typed (preferred — compiler-checked, extracts "Child.Text" path):
textBox.SetBinding<MyVm>(nameof(TextBox.Text), vm => vm.Child.Text);

// Parameterless closure:
textBox.SetBinding(nameof(TextBox.Text), () => vm.Child.Text);
```

Extension methods in `FrameworkElementExt.cs`. `BinderHelpers.ExtractPath()` walks the expression tree to produce a dotted path string, then creates a `Binding` normally. Nested paths (e.g. `vm => vm.A.B.C`) are fully supported via `PropertyPathObserver`.

## Index-Based Binding (Forms only)

Paths support integer indexer access via `[N]` syntax. Works in string paths, Binding objects, and lambdas:

```csharp
// String path
textBox.SetBinding(nameof(TextBox.Text), new Binding("Items[0].Text"));

// Lambda
textBox.SetBinding<MyVm>(nameof(TextBox.Text), vm => vm.Items[0].Text);

// Nested: index in the middle of a path
textBox.SetBinding(nameof(TextBox.Text), new Binding("Child.Items[1].Text"));
```

All binding features work with indexed paths: modes, converters, StringFormat, FallbackValue, LostFocus trigger.

**Collection change notification:** `PropertyPathObserver` subscribes to `INotifyCollectionChanged` on collections in indexed path segments. When items are added, removed, replaced, or cleared, the binding re-evaluates. Out-of-bounds indexes resolve to null (triggering `FallbackValue` if set). Currently reacts to ALL collection changes regardless of whether the specific bound index is affected — this is intentionally broad for correctness; a future optimization could filter by index relevance.

**Limitations:** Dictionary/string key indexing is not supported.

**Implementation:** `BinderHelpers.ParseSegments()` splits paths into `PathSegment` structs (name + optional int index). `BuildGetter`/`BuildSetter` emit indexer calls via `Expression.MakeIndex` or `Expression.ArrayIndex`. `ExtractPath` handles `MethodCallExpression` (`get_Item`) and `IndexExpression` nodes from lambdas. `PropertyPathObserver` uses `GetIndexedValue()` after property resolution for indexed segments.

## Feature Comparison

| Feature | GraphicalUiElement | FrameworkElement |
|---|---|---|
| String binding | ✓ | ✓ |
| Explicit Binding object | ✗ | ✓ |
| Lambda binding | ✗ | ✓ |
| Nested paths (`A.B.C`) | ✗ | ✓ |
| Index paths (`Items[0].Text`) | ✗ | ✓ |
| Binding modes | Implicit TwoWay | Configurable |
| Converters | ✗ | ✓ |
| FallbackValue / TargetNullValue | ✗ | ✓ |
| UpdateSourceTrigger | Always PropertyChanged | Configurable |

## Key Files

| File | Purpose |
|---|---|
| `GumRuntime/GraphicalUiElement.Binding.cs` | GUE binding — BindingContext, SetBinding, PushValueToViewModel |
| `MonoGameGum/Forms/Data/Binding.cs` | Binding config class + BindingMode + UpdateSourceTrigger + IValueConverter |
| `MonoGameGum/Forms/Data/NpcBindingExpression.cs` | Forms binding engine — UpdateTarget, UpdateSource |
| `MonoGameGum/Forms/Data/PropertyPathObserver.cs` | Watches dotted paths, re-hooks on intermediate changes, weak listeners |
| `MonoGameGum/Forms/Data/BinderHelpers.cs` | Lambda path extraction, compiled getter/setter delegates |
| `MonoGameGum/Forms/Controls/FrameworkElementExt.cs` | Lambda SetBinding extension methods |
| `GumRuntime/BindableGue.cs` | Deprecated alias for GraphicalUiElement — do not use |

## Non-Obvious Behaviors

**BindableGue is deprecated.** `GraphicalUiElement` now owns all binding logic. `BindableGue` exists only as a legacy alias.

**Weak listeners in PropertyPathObserver.** Forms binding uses weak references to avoid memory leaks on deep paths. GUE binding does not — callers should unsubscribe when disposing.

**Lambda extracts path at call time, not at update time.** `vm => vm.Child.Text` becomes the static path `"Child.Text"`. If `Child` is replaced, `PropertyPathObserver` re-hooks the listener chain automatically.

**ListBox `Items` is bindable.** `listBox.SetBinding(nameof(ListBox.Items), nameof(vm.Items))` works and keeps the list in sync with an `ObservableCollection` on the VM.
