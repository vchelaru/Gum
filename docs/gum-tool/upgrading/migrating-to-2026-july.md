# Migrating to 2026 July

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 June` to `2026 July`.

{% hint style="warning" %}
**This release has not shipped yet.** There is no Gum tool download and no NuGet package version for `2026 July` at this time. The change described on this page currently applies **only** if you build Gum from source (the `main` branch). This page will be updated with the tool download link and NuGet package version once the release ships.
{% endhint %}

## What Changed at a Glance

`2026 July` marks the V1 and V2 Gum Forms default visuals as `[Obsolete]`. This is a **soft break**: the old default-visual classes and the `DefaultVisualsVersion.V1` / `DefaultVisualsVersion.V2` enum values still compile and work, but they now emit a `CS0618` warning and are slated for removal in a future release. `DefaultVisualsVersion.V3` (equivalently `DefaultVisualsVersion.Newest`) is the supported path going forward.

## Upgrading the Gum Tool and Runtime

There is nothing to upgrade yet — see the warning above. This page will list the tool download and the NuGet package version once `2026 July` ships.

## Breaking Changes and Migrations

### V1 and V2 Forms Default Visuals Are Now `[Obsolete]`

Gum Forms controls get their appearance from a *default visual* class per control. There have been three generations of these default visuals:

* **V1** — the `Default*Runtime` classes (`DefaultButtonRuntime`, `DefaultCheckboxRuntime`, and so on). These are the original Forms visuals, built on solid-color `ColoredRectangle` backgrounds.
* **V2** — the `*Visual` classes (`ButtonVisual`, `CheckBoxVisual`, and so on). These use nine-slice textured backgrounds with centralized styling.
* **V3** — the current generation, with color-driven styling. This is the only generation wired up completely across all platforms.

The V1 (`Default*Runtime`) and V2 (`*Visual`) classes, along with the `DefaultVisualsVersion.V1` and `DefaultVisualsVersion.V2` enum values, are now marked `[Obsolete]`. They still compile and work, but each use produces a `CS0618` compiler warning:

```
warning CS0618: 'DefaultVisualsVersion.V2' is obsolete
```

They are slated for removal in a future release. To migrate, pass `DefaultVisualsVersion.V3` (or `DefaultVisualsVersion.Newest`) to `GumService.Initialize` / `FormsUtilities.InitializeDefaults`:

❌ Old:
```csharp
// Initialize
GumService.Default.Initialize(
    this,
    defaultVisualsVersion: DefaultVisualsVersion.V2);
```

✅ New:
```csharp
// Initialize
GumService.Default.Initialize(
    this,
    defaultVisualsVersion: DefaultVisualsVersion.V3);
```

{% hint style="info" %}
**Switching visual versions is not purely cosmetic.** The different generations build **different visual trees** — different child structure and different named children. Projects that reach into a control's visual tree by name, or that customize the default visuals, may need adjustments beyond swapping the enum value. This is why V1 and V2 are only deprecated, not removed: you can keep compiling against them while you migrate the surrounding code, then move to V3 once your visual-tree customizations are updated.
{% endhint %}
