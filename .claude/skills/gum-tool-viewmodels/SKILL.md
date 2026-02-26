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

## Visibility: ViewModel Properties, Not Converters

Do **not** use `IValueConverter` in XAML for visibility or other transformations. Instead, expose a `System.Windows.Visibility` property on the ViewModel with `[DependsOn]`:

```csharp
[DependsOn(nameof(ErrorMessage))]
public Visibility ErrorMessageVisibility =>
    string.IsNullOrEmpty(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
```

XAML then binds directly:

```xml
<TextBlock Visibility="{Binding ErrorMessageVisibility}" />
```

This keeps XAML simple and makes the logic unit-testable.

## Common Pitfalls

**Missing `[DependsOn]`**: If a getter computes from another property but lacks the attribute, the UI will show stale values. The `ViewModel` constructor scans for `[DependsOn]` via reflection at construction time — it only works if the attribute is present.

**Two-way derived properties**: Properties like `IsLocalFile` that both read from and write to a backing property need `[DependsOn]` for the read direction. The write direction (setter updating `SourceType`) works normally through `Set()`.
