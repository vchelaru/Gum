# Migrating to 2026 April

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 March` to `2026 April`.

## Upgrading Gum Tool

{% hint style="info" %}
The 2026 April release of the Gum tool is not yet available. This page will be updated with download links when the release is published.
{% endhint %}

## Upgrading Runtime

{% hint style="info" %}
The 2026 April NuGet packages are not yet available. This page will be updated with version numbers when the packages are published.
{% endhint %}

See below for breaking changes and updates.

### MonoGameGum.Forms Compatibility Shims Removed

The `MonoGameGum.Forms` compatibility shims that were introduced in [2025 August](migrating-to-2025-august.md#removal-of-monogamegum.forms-namespace) have been deleted. These shims were marked `[Obsolete(error: true)]` since [2025 October](migrating-to-2025-october.md#monogamegum.forms-namespace-error), meaning any project referencing them was already a compile error.

If you migrated to `Gum.Forms` namespaces when the error was introduced, no action is needed. If your project still has `using MonoGameGum.Forms.Controls` or similar references, you must update them to the `Gum.Forms` equivalents:

| Old Namespace                         | New Namespace                 |
| ------------------------------------- | ----------------------------- |
| MonoGameGum.Forms                     | Gum.Forms                     |
| MonoGameGum.Forms.Controls            | Gum.Forms.Controls            |
| MonoGameGum.Forms.Controls.Primitives | Gum.Forms.Controls.Primitives |
| MonoGameGum.Forms.Data                | Gum.Forms.Data                |
| MonoGameGum.Forms.DefaultVisuals      | Gum.Forms.DefaultVisuals      |

### Raylib GumService.Initialize Defaults to V3

The parameterless `GumService.Initialize()` overload used by raylib projects previously defaulted to `DefaultVisualsVersion.V2`. It now defaults to `DefaultVisualsVersion.Newest` (V3), matching MonoGame's behavior.

If you need to keep V2 visuals, pass the version explicitly:

```csharp
GumUI.Initialize(DefaultVisualsVersion.V2);
```

### RaylibGum.Input Namespace Removed

Input types have been unified across non-XNA runtimes (raylib, sokol) under a single `Gum.Input` namespace. The `RaylibGum.Input` namespace has been deleted.

The following types moved from `RaylibGum.Input` to `Gum.Input`: `Cursor`, `Keyboard`, `GamePad`, `GamepadButton`, `AnalogStick`, `DPadDirection`, `MouseState`, `ButtonState`, `TouchCollection`, and `TouchLocation`.

Update any `using` directives or fully-qualified references:

| Old                          | New                      |
| ---------------------------- | ------------------------ |
| `using RaylibGum.Input;`     | `using Gum.Input;`       |
| `RaylibGum.Input.Cursor`     | `Gum.Input.Cursor`       |
| `RaylibGum.Input.GamePad`    | `Gum.Input.GamePad`      |

MonoGame projects are unaffected — the `MonoGameGum.Input` namespace has not changed. This change only impacts raylib-based projects.
