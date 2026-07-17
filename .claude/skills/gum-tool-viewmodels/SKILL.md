---
name: gum-tool-viewmodels
description: Gum tool ViewModel conventions. Triggers: ViewModels, XAML views, data binding, DependsOn, visibility properties.
---

# Gum Tool ViewModel Conventions

## Base Classes

- **`ViewModel`** (`Gum/Mvvm/ViewModel.cs`) — base for all view models. Provides `Get<T>()`/`Set()` property storage, `NotifyPropertyChanged`, and `[DependsOn]` propagation.
- **`DialogViewModel`** (`Tools/Gum.Presentation/Dialogs/DialogViewModel.cs`) — extends `ViewModel` for dialogs. Adds `AffirmativeCommand`/`NegativeCommand`, `RequestClose` event, and `AffirmativeText`/`NegativeText`. Lives in the headless `Gum.Presentation` assembly (ADR-0005). Any dialog VM deriving from it is safe to relocate there: `DialogViewResolver` falls back to scanning other loaded assemblies (`IDialogViewAssemblyProvider`) when the VM's own assembly has no View — but that fallback only pairs via `[Dialog(typeof(VM))]` on the View, not naming convention, so attribute the View before (or alongside) moving its VM. See `gum-tool-dialogs`.

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

- **Logic** — what to show, when, in what state — lives on the VM as `[DependsOn]` computed properties, in **framework-neutral types** (ADR-0004). Examples: `bool` visibility flags, `FontStyle`, display strings, enabled/disabled flags. XAML binds directly (a `bool` visibility flag through a stock `BooleanToVisibilityConverter`). Unit-testable, and the VM stays eligible for the headless assembly.
- **Theming** — which brush, which font size — stays in XAML so `{DynamicResource ...}` can repaint on a runtime theme switch. Not testable, by necessity (DynamicResource only resolves through a `FrameworkElement`).

Do **not** use `IValueConverter` or `DataTrigger` for logic. The one exception is themed brushes (see below).

Visibility example — expose a **`bool`**, not WPF `Visibility` (ADR-0004), so the VM can move into the headless assembly:

```csharp
[DependsOn(nameof(ErrorMessage))]
public bool IsErrorMessageVisible => !string.IsNullOrEmpty(ErrorMessage);
```

XAML binds through the stock `BooleanToVisibilityConverter` (a pure type-adapter, not logic):

```xml
<TextBlock Visibility="{Binding IsErrorMessageVisible, Converter={StaticResource BoolToVisibilityConverter}}" />
```

**Never expose `System.Windows` types from a VM** — `Visibility`, `Color`, `Brush`, `WriteableBitmap`. They pin the VM to the WPF assembly and defeat the compiler-enforced logic↔view boundary. Resolve display state in neutral types instead: `bool` for visibility, Gum's color type (`RenderingLibrary` has one) for colors, a color in place of a `Brush`, `byte[]`/a Gum image for pixels. Framework types live only in the view/converter layer. See `Direction/decisions/0004-viewmodels-expose-neutral-presentation-state.md`.

VMs in `GumCommon` or runtime projects must stay UI-agnostic — this rule is for tool code only.

### Exception: themed brushes

Light/dark theming uses brushes defined in `Gum/Themes/Frb.Brushes.{Light,Dark}.xaml` (e.g. `Frb.Brushes.Error`). These must be resolved with `{DynamicResource ...}` so a runtime theme switch repaints — that only works from a `FrameworkElement`, not from a VM-side `Brush` property. The right pattern:

- Keep the *logical* state on the VM (`IsOrphaned`, `IsInvalid`, etc., still `[DependsOn]`-driven and unit-testable).
- Apply the brush via a `Style` with a `DataTrigger` keyed off that VM bool, e.g. `<Setter Property="Foreground" Value="{DynamicResource Frb.Brushes.Error}" />`.

This is the only situation where `DataTrigger` is preferred over a VM-side property.

## Common Pitfalls

**Missing `[DependsOn]`**: If a getter computes from another property but lacks the attribute, the UI will show stale values. The `ViewModel` constructor scans for `[DependsOn]` via reflection at construction time — it only works if the attribute is present.

**Two-way derived properties**: Properties like `IsLocalFile` that both read from and write to a backing property need `[DependsOn]` for the read direction. The write direction (setter updating `SourceType`) works normally through `Set()`.
