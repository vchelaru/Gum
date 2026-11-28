---
name: gum-runtime-binding
description: Gum runtime data binding — BindingContext, SetBinding on GraphicalUiElement visuals and FrameworkElement Forms controls (one shared engine), binding types (string, Binding, lambda), view model events bound to methods.
---

# Gum Runtime Binding

## One Engine, Two Kinds of Target

Visuals (`GraphicalUiElement`) and Forms controls (`FrameworkElement`) both implement `IBindingTarget` (`MonoGameGum/Forms/Data/IBindingTarget.cs`) and keep their bindings in a `PropertyRegistry`. A target property gets an `NpcBindingExpression`; a target method gets a `MethodBindingExpression`, which binds a view model event to it. `GumRuntime/GraphicalUiElement.Binding.cs` holds only BindingContext inheritance and thin `SetBinding` wrappers. FRB1 does not compile that file; it keeps its own visual engine in `../FlatRedBall/FRBDK/Glue/GumPlugin/GumPlugin/Embedded/GraphicalUiElement.Binding.cs`, and only the Forms side of this engine compiles there.

`FrameworkElement.BindingContext` delegates to its `Visual.BindingContext` — they share one context.

On a `Visual` swap, a context assigned on the old visual is copied to the new one (`CarryExplicitBindingContext`), so `SetBinding` bindings keep following the view model. A context the old visual only inherited from its parent is not copied; the new visual gets it when it is parented. After `ReactToVisualChanged`, every control-level binding re-evaluates its target (`PropertyRegistry.RefreshTargets`), so a bound property that lives on the visual (e.g. `Text`) shows the view-model value, not the new visual's authored one; it never writes to the source, and a binding with no source is left alone. Unbound properties keep what the new visual authors.

## BindingContext

Set on any `GraphicalUiElement` or `FrameworkElement`. Cascades automatically to all descendants unless overridden:

```csharp
root.BindingContext = viewModel;  // all children inherit it
child.BindingContext = other;     // explicit overrides inherited
```

Subscribes to `INotifyPropertyChanged` and updates bound UI properties on change.

**Leaving the root clears the element's own context.** Any path off the GumService `Root` (Forms or visual `RemoveFromRoot`, `Root.Children.Remove`/`Clear`, `RemoveChild`, `Parent = null`, assigning another parent) sets `BindingContext = null` on the element that left (`HandleBindingParentChanged`, when the old parent has `ClearsBindingContextOfRemovedChildren`, which GumService sets on its `Root`). Descendants that inherited it lose it with it; a descendant given its own keeps it, which is what keeps the item contexts ListBox/ComboBox/Menu/TabControl assign to their rows. A re-added screen needs its context assigned again. A bound `Items` (any ItemsControl, or ComboBox) is set to null when its context becomes null, so the control drops its rows and lets go of the view model's collection; scalar targets keep their last value. Moving between other parents keeps it, and so does leaving `PopupRoot`/`ModalRoot`: popups such as a ComboBox drop-down leave and rejoin those roots on every open and close. `RemoveFromManagers` follows the same rule for an element added with `AddToManagers`: only the element it was called on loses its own context, and the contained children it walks keep theirs.

## GraphicalUiElement Binding (Visuals)

`SetBinding(string, Binding)` takes every `Binding` option. The string and lambda overloads build a `Binding`; their `toStringFormat` is a numeric specifier that becomes `StringFormat = "{0:" + spec + "}"`:

```csharp
element.SetBinding("X", nameof(vm.Position));           // basic
element.SetBinding("Text", nameof(vm.Gold), "N0");      // numeric format specifier, not "{0:N0}"
element.SetBinding("Width", new Binding(nameof(vm.W)) { Mode = BindingMode.TwoWay });
```

**A visual binding that does not set `Mode` is VM to UI only** (`IBindingTarget.DefaultBindingMode`, OneWay on a visual, TwoWay on a Forms control). States, `SetProperty`, localization and variable references set visual properties and raise `PropertyChanged`, so a TwoWay default would write them into the view model (a localized `Text` would overwrite its string ID). A derived visual writes back with `PushValueToViewModel()` (`NpcBindingExpression.PushTargetToSource`), which an unset mode allows and an explicit `OneWay` blocks. `Binding.IsModeSet` tells unset from an explicit TwoWay. Tests: `VisualBindingEnginePinTests`.

## FrameworkElement Binding (Forms)

Three binding styles; visuals take the same three.

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

## Index-Based Binding

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

## Where Visuals and Forms Controls Differ

Only the default mode (above) and `UpdateSourceTrigger.LostFocus`, which never fires on a visual because it has no focus. Everything else, including conversions, `FallbackValue` on a broken path and the read-only target check, is the same engine.

## Key Files

| File | Purpose |
|---|---|
| `GumRuntime/GraphicalUiElement.Binding.cs` | Visual BindingContext inheritance, `SetBinding` wrappers, `PushValueToViewModel` |
| `MonoGameGum/Forms/Data/IBindingTarget.cs` | What a binding needs from its target; `FrameworkElement` and `GraphicalUiElement` implement it |
| `MonoGameGum/Forms/Data/PropertyRegistry.cs` | One target's bindings; picks a value binding (property) or an event binding (method) |
| `MonoGameGum/Forms/Data/MethodBindingExpression.cs` | View model event bound to a target method of any visibility, re-subscribed on context changes |
| `MonoGameGum/Forms/Data/Binding.cs` | Binding config class + BindingMode + UpdateSourceTrigger + IValueConverter |
| `MonoGameGum/Forms/Data/NpcBindingExpression.cs` | Value binding — UpdateTarget, UpdateSource, PushTargetToSource |
| `GumCommon/Mvvm/PropertyPathObserver.cs` | Watches dotted paths, re-hooks on intermediate changes, weak listeners |
| `MonoGameGum/Forms/Data/BinderHelpers.cs` | Lambda path extraction, compiled getter/setter delegates |
| `MonoGameGum/Forms/Controls/FrameworkElementExt.cs` | Lambda SetBinding extension methods |
| `GumRuntime/BindableGue.cs` | Deprecated alias for GraphicalUiElement — do not use |

## Non-Obvious Behaviors

**A string path that misses a member reports once, never throws.** Every binding builds the message with `BinderHelpers.GetMissingPathMessage` and send it through `ElementSaveExtensions.ReportDiagnostic` (the `[Gum]` `PropertyAssignmentError` channel), once per binding, when a non-null context is attached. The check walks declared member types; past the root, a member the declared type lacks is judged on the object held there, and when that object is null it reports only for a sealed declared type.

**BindableGue is deprecated.** `GraphicalUiElement` now owns all binding logic. `BindableGue` exists only as a legacy alias.

**A null or empty `PropertyChanged` name means every property changed.** `PropertyPathObserver` re-reads the path from the level that raised it, for every binding on either kind of target. Tests: `BindingSourceNotificationTests`.

**Weak listeners in PropertyPathObserver.** Bindings use weak references to avoid memory leaks on deep paths.

**Compiled getters and setters are cached per (type, path).** `BinderHelpers.BuildGetter`/`BuildSetter` compile an expression tree once per pair; attaching a BindingContext only looks them up. A per-attach compile costs several KB and most of a millisecond per binding (`BindingAllocationTests`).

**A view model field is read but never written.** `PropertyPathObserver` resolves a public field at the leaf (`IsLeafField`), and `UntypedBindingExpression.SetSourceValue` skips it.

**What the visual engine used to throw on is reported once per binding.** A value the target cannot take (`NpcBindingExpression.ReportFailedConversionOnce`) and a `PushValueToViewModel` to a source with no setter (`PushTargetToSource`) or to a field write one `[Gum]` warning; a null into a value type stays silent.

**Forms `StringFormat` is a composite format for one value and must contain `{0}`.** `SetBinding` throws (`BinderHelpers.GetStringFormatErrorMessage`) on a format `string.Format` rejects with one argument (`{1}`, an unclosed brace) and on one without `{0}`, which is what a C# `$"..."` interpolated format leaves behind; a specifier the value type rejects (`{0:Q}`) is left to `FallbackValue`. The visual overloads' `toStringFormat` is a numeric specifier (`"N0"`) wrapped as `"{0:N0}"`, and a composite format passed there throws `ArgumentException`.

**A converter that throws is caught and reported once per direction per binding** (`UntypedBindingExpression.Convert`/`ConvertBack`). `Convert` shows `FallbackValue` or the default; `ConvertBack` leaves the source unchanged, as does `UnsetValue` from it. On the way back a target value equal to `TargetNullValue` writes null and skips the converter (`NpcBindingExpression.SetValueToSource`). Text written to an integral source parses with `NumberStyles.Number` in the current culture, so `"{0:N0}"` text round-trips; text that still fails (`"$1,235"`, `""`) leaves the source unchanged and is reported once (`ReportFailedSourceConversionOnce`); during a focused TextBox edit the report waits for the edit to end (`OnTextEditEnded`).

**Forms bindings convert strings and integers to enum targets.** `BinderHelpers.ConvertToEnum` parses names ignoring case (as WPF's `EnumConverter` does) and maps numbers with `Enum.ToObject`; a value naming no member throws in `UpdateTarget`, while an unmatched edit written back to an enum source leaves the source unchanged.

**A bound target (Forms control or visual) resets when an attached BindingContext becomes null.** `NpcBindingExpression.ResetTarget` shows `FallbackValue`, else the value the property held before it was first bound (`UnboundTargetValue`, kept per property by `PropertyRegistry` across rebinds; `ClearBinding` restores it too); OneWayToSource and BindingContext targets are skipped, and binding before any context resets nothing. Visual (GUE) bindings keep their last value. Tests: `BindingContextNullResetTests`, whose expected values go through `ContextResetExpectation`.

**A focused TextBox/PasswordBox with the PropertyChanged trigger is not rewritten mid-typing.** A source value raised during its own edit is deferred (`_hasDeferredTargetUpdate`) and shown on `LostFocus` or `EditCommitted`. Tests: `TextBoxPropertyChangedEditTests`.

**A setter-only target property binds OneWay on every target.** Nothing may read the target without checking `CanRead` (`NpcBindingExpression.ShouldUpdateSource`, `NpcBindingExpression.PushTargetToSource`); a default TwoWay binding only pushes source values, and `OneWayToSource` on a setter-only target throws.

**A view model member with no setter still feeds a TwoWay binding; only `OneWayToSource` refuses it.** TwoWay is the Forms default mode, so a computed `FullName => ...` shown in a Label must keep working; `OneWayToSource` throws at attach (`BinderHelpers.GetReadOnlySourceMessage`). A private setter counts as a setter on both sides, as `PropertyInfo.SetValue` reaches it; on the source side one a base class declares is found through `BinderHelpers.AsDeclared`, since a `PropertyInfo` reflected from the derived type hides it. A value the user enters into such a binding is dropped and reported once (`NpcBindingExpression.ReportUnwritableSourceOnce`); "the user" means the change happened while `InteractiveGue.IsDispatchingInput` was true, so a value set from code stays silent (and FlatRedBall, which dispatches input itself, never reports).

**A Forms `OneWayToSource` binding writes the control's current value to each source it attaches to** (`UntypedBindingExpression.AttachToSource`), as WPF does. This is how a view model bound to `ListBox.SelectedItems` receives the live selection collection, which never raises `PropertyChanged` itself. `ClearBinding` resets the target only when it has a setter of any visibility (`NpcBindingExpression.HasTargetSetter`). Tests: `OneWayToSourceAttachTests`.

**`[DependsOn("Game.Gold")]` follows a dotted path.** `ViewModel` handles the root segment in `NotifyPropertyChanged` and watches the rest with a `PropertyPathObserver` on the root's value (weak, so the referenced VM never keeps the dependent alive); it attaches when the root is notified or on the first `PropertyChanged` subscription, and reports an unknown segment once through the `[Gum]` channel. Undotted `[DependsOn]` is unchanged. Tests: `ViewModelDottedDependsOnTests`, `DottedDependsOnBindingTests`.

**Lambda extracts path at call time, not at update time.** `vm => vm.Child.Text` becomes the static path `"Child.Text"`. If `Child` is replaced, `PropertyPathObserver` re-hooks the listener chain automatically.

**ListBox `Items` is bindable.** `listBox.SetBinding(nameof(ListBox.Items), nameof(vm.Items))` works and keeps the list in sync with an `ObservableCollection` on the VM.

**`UpdateSourceTrigger.Default` is project-configurable.** `Binding.DefaultUpdateTriggers` lets a project register a default trigger per control type + property, resolved in `NpcBindingExpression.HookUpdateSource` before falling back to `PropertyChanged`. See [Default Update Triggers](../../../docs/code/binding-viewmodels/advanced-binding-options.md#default-update-triggers).

**Forms getters tell a null leaf from a broken path.** `BinderHelpers.BuildGetter` returns null for a null leaf (the target shows `TargetNullValue`) and `GumProperty.UnsetValue` for a null intermediate or a missing member (`FallbackValue`, also for a missing root member). Past the root, a segment the declared type lacks (an intermediate typed `object` or a base class) is resolved on the held object's runtime type at every read and write (`GetOnRuntimeType`/`SetOnRuntimeType`).
