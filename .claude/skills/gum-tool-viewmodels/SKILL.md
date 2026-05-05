---
name: gum-tool-viewmodels
description: Reference guide for Gum tool ViewModel conventions. Load this when working on ViewModels, XAML views, data binding, DependsOn, or visibility properties in the Gum tool.
---

# Gum Tool ViewModel Conventions

## Base Classes

- **`ViewModel`** (`Gum/Mvvm/ViewModel.cs`) — base for all view models. Provides `Get<T>()`/`Set()` property storage, `NotifyPropertyChanged`, and `[DependsOn]` propagation.
- **`DialogViewModel`** (`Gum/Services/Dialogs/DialogViewModel.cs`) — extends `ViewModel` for dialogs. Adds `AffirmativeCommand`/`NegativeCommand`, `RequestClose` event, and `AffirmativeText`/`NegativeText`.

## Property Patterns

**Stored properties** use `Get<T>()`/`Set()`:

```csharp
public string Name
{
    get => Get<string>() ?? string.Empty;
    set => Set(value);
}
```

**Derived properties** must use `[DependsOn]` so changes to the source property automatically raise `PropertyChanged` for the derived property. Without this, the UI will not update.

```csharp
[DependsOn(nameof(SourceType))]
public bool IsLocalFile => SourceType == SourceType.LocalFile;
```

Multiple dependencies are expressed with multiple attributes:

```csharp
[DependsOn(nameof(IsPreviewLoaded))]
[DependsOn(nameof(IsLoading))]
public bool CanImport => IsPreviewLoaded && !IsLoading;
```

## View Logic on the VM, Not in XAML

The boundary is **logic vs. theming**:

- **Logic** — what to show, when, in what state — lives on the VM as `[DependsOn]` computed properties. Examples: `Visibility`, `FontStyle`, display strings, enabled/disabled flags. XAML binds directly. Unit-testable.
- **Theming** — which brush, which font size — stays in XAML so `{DynamicResource ...}` can repaint on a runtime theme switch. Not testable, by necessity (DynamicResource only resolves through a `FrameworkElement`).

Do **not** use `IValueConverter` or `DataTrigger` for logic. The one exception is themed brushes (see below).

Visibility example:

```csharp
[DependsOn(nameof(ErrorMessage))]
public Visibility ErrorMessageVisibility =>
    string.IsNullOrEmpty(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
```

XAML then binds directly:

```xml
<TextBlock Visibility="{Binding ErrorMessageVisibility}" />
```

VMs in `GumCommon` or runtime projects must stay UI-agnostic — this rule is for tool code only.

### Exception: themed brushes

Light/dark theming uses brushes defined in `Gum/Themes/Frb.Brushes.{Light,Dark}.xaml` (e.g. `Frb.Brushes.Error`). These must be resolved with `{DynamicResource ...}` so a runtime theme switch repaints — that only works from a `FrameworkElement`, not from a VM-side `Brush` property. The right pattern:

- Keep the *logical* state on the VM (`IsOrphaned`, `IsInvalid`, etc., still `[DependsOn]`-driven and unit-testable).
- Apply the brush via a `Style` with a `DataTrigger` keyed off that VM bool, e.g. `<Setter Property="Foreground" Value="{DynamicResource Frb.Brushes.Error}" />`.

This is the only situation where `DataTrigger` is preferred over a VM-side property.

## Common Pitfalls

**Missing `[DependsOn]`**: If a getter computes from another property but lacks the attribute, the UI will show stale values. The `ViewModel` constructor scans for `[DependsOn]` via reflection at construction time — it only works if the attribute is present.

**Two-way derived properties**: Properties like `IsLocalFile` that both read from and write to a backing property need `[DependsOn]` for the read direction. The write direction (setter updating `SourceType`) works normally through `Set()`.
