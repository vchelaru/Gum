---
name: gum-runtime-binding
description: Gum runtime data binding — BindingContext, SetBinding on GraphicalUiElement visuals and FrameworkElement Forms controls, binding types (string, Binding, lambda), differences between the two systems.
---

# Gum Runtime Binding

## Two Binding Systems

**GraphicalUiElement** (`GumRuntime/GraphicalUiElement.Binding.cs`) — basic binding available on all visuals.
**FrameworkElement** (`MonoGameGum/Forms/Controls/`) — richer binding on Forms controls, built on top of the GUE system.

`FrameworkElement.BindingContext` delegates to its `Visual.BindingContext` — they share one context.

On a `Visual` swap, a context assigned on the old visual is copied to the new one (`CarryExplicitBindingContext`), so `SetBinding` bindings keep following the view model. A context the old visual only inherited from its parent is not copied; the new visual gets it when it is parented. After `ReactToVisualChanged`, every control-level binding re-evaluates its target (`PropertyRegistry.RefreshTargets`), so a bound property that lives on the visual (e.g. `Text`) shows the view-model value, not the new visual's authored one; it never writes to the source, and a binding with no source is left alone. Unbound properties keep what the new visual authors.

## BindingContext

Set on any `GraphicalUiElement` or `FrameworkElement`. Cascades automatically to all descendants unless overridden:

```csharp
root.BindingContext = viewModel;  // all children inherit it
child.BindingContext = other;     // explicit overrides inherited
```

Subscribes to `INotifyPropertyChanged` and updates bound UI properties on change.

**Leaving the root clears the element's own context.** Any path off the GumService `Root` (Forms or visual `RemoveFromRoot`, `Root.Children.Remove`/`Clear`, `RemoveChild`, `Parent = null`, assigning another parent) sets `BindingContext = null` on the element that left (`HandleBindingParentChanged`, when the old parent has `ClearsBindingContextOfRemovedChildren`, which GumService sets on its `Root`). Descendants that inherited it lose it with it; a descendant given its own keeps it, which is what keeps the item contexts ListBox/ComboBox/Menu/TabControl assign to their rows. A re-added screen needs its context assigned again. Moving between other parents keeps it, and so does leaving `PopupRoot`/`ModalRoot`: popups such as a ComboBox drop-down leave and rejoin those roots on every open and close. `RemoveFromManagers` follows the same rule for an element added with `AddToManagers`: only the element it was called on loses its own context, and the contained children it walks keep theirs.

## GraphicalUiElement Binding (Visuals)

String-path binding (plus a `SetBinding<TVm>` lambda overload that extracts the path). No converters or modes; dotted and indexed paths go through `PropertyPathObserver`:

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
| Lambda binding | `SetBinding<TVm>` only | ✓ |
| Nested paths (`A.B.C`) | ✓ | ✓ |
| Index paths (`Items[0].Text`) | ✓ | ✓ |
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
| `GumCommon/Mvvm/PropertyPathObserver.cs` | Watches dotted paths, re-hooks on intermediate changes, weak listeners |
| `MonoGameGum/Forms/Data/BinderHelpers.cs` | Lambda path extraction, compiled getter/setter delegates |
| `MonoGameGum/Forms/Controls/FrameworkElementExt.cs` | Lambda SetBinding extension methods |
| `GumRuntime/BindableGue.cs` | Deprecated alias for GraphicalUiElement — do not use |

## Non-Obvious Behaviors

**A string path that misses a member reports once, never throws.** Both systems build the message with `BinderHelpers.GetMissingPathMessage` and send it through `ElementSaveExtensions.ReportDiagnostic` (the `[Gum]` `PropertyAssignmentError` channel), once per binding, when a non-null context is attached. The check walks declared member types; past the root, a member the declared type lacks is judged on the object held there, and when that object is null it reports only for a sealed declared type.

**BindableGue is deprecated.** `GraphicalUiElement` now owns all binding logic. `BindableGue` exists only as a legacy alias.

**A null or empty `PropertyChanged` name means every property changed.** `PropertyPathObserver` re-reads the path from the level that raised it, and the GUE root handler refreshes every flat binding and every descendant's `BindingContext` binding. Tests: `BindingSourceNotificationTests`.

**Weak listeners in PropertyPathObserver.** Forms binding uses weak references to avoid memory leaks on deep paths. GUE binding does not — callers should unsubscribe when disposing.

**A setter-only target property binds OneWay on both systems.** Nothing may read the target without checking `CanRead` (`NpcBindingExpression.ShouldUpdateSource`, `GraphicalUiElement.PushValueToViewModel`); a default TwoWay binding only pushes source values, and `OneWayToSource` on a setter-only target throws.

**A view model member with no setter still feeds a TwoWay binding; only `OneWayToSource` refuses it.** TwoWay is the default mode, so a computed `FullName => ...` shown in a Label must keep working; `OneWayToSource` throws at attach (`BinderHelpers.GetReadOnlySourceMessage`). A private setter counts as a setter on both sides, as `PropertyInfo.SetValue` reaches it; on the source side one a base class declares is found through `BinderHelpers.AsDeclared`, since a `PropertyInfo` reflected from the derived type hides it.

**A Forms `OneWayToSource` binding writes the control's current value to each source it attaches to** (`UntypedBindingExpression.AttachToSource`), as WPF does. This is how a view model bound to `ListBox.SelectedItems` receives the live selection collection, which never raises `PropertyChanged` itself. `ClearBinding` resets the target only when it has a setter of any visibility (`NpcBindingExpression.HasTargetSetter`). Tests: `OneWayToSourceAttachTests`.

**`[DependsOn("Game.Gold")]` follows a dotted path.** `ViewModel` handles the root segment in `NotifyPropertyChanged` and watches the rest with a `PropertyPathObserver` on the root's value (weak, so the referenced VM never keeps the dependent alive); it attaches when the root is notified or on the first `PropertyChanged` subscription, and reports an unknown segment once through the `[Gum]` channel. Undotted `[DependsOn]` is unchanged. Tests: `ViewModelDottedDependsOnTests`, `DottedDependsOnBindingTests`.

**Lambda extracts path at call time, not at update time.** `vm => vm.Child.Text` becomes the static path `"Child.Text"`. If `Child` is replaced, `PropertyPathObserver` re-hooks the listener chain automatically.

**ListBox `Items` is bindable.** `listBox.SetBinding(nameof(ListBox.Items), nameof(vm.Items))` works and keeps the list in sync with an `ObservableCollection` on the VM.

**`UpdateSourceTrigger.Default` is project-configurable.** `Binding.DefaultUpdateTriggers` lets a project register a default trigger per control type + property, resolved in `NpcBindingExpression.HookUpdateSource` before falling back to `PropertyChanged`. See [Default Update Triggers](../../../docs/code/binding-viewmodels/advanced-binding-options.md#default-update-triggers).

**Forms getters tell a null leaf from a broken path.** `BinderHelpers.BuildGetter` returns null for a null leaf (the target shows `TargetNullValue`) and `GumProperty.UnsetValue` for a null intermediate or a missing member (`FallbackValue`, also for a missing root member). Past the root, a segment the declared type lacks (an intermediate typed `object` or a base class) is resolved on the held object's runtime type at every read and write (`GetOnRuntimeType`/`SetOnRuntimeType`); GUE bindings already walk runtime types through `PropertyPathObserver`.
