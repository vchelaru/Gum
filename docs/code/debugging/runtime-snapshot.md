# Runtime Snapshot

## Introduction

A runtime snapshot exports your live Gum UI — the visual tree exactly as it exists in the running game — to a Gum project you can open and inspect in the Gum tool. It is the reverse of [Hot Reload](hot-reload.md): hot reload pushes changes from the tool into the running game, while a snapshot captures the running game back into a project.

Snapshots are especially useful for **code-only** UIs (built entirely in C# with no `.gumx` loaded), which otherwise have no design-time artifact to open. A snapshot is also a shareable file — attach it to a bug report so someone else can open your exact runtime layout in the tool.

{% hint style="info" %}
A snapshot is a one-way capture. Editing it in the Gum tool does **not** affect the running game — it is a photograph of the tree at the moment you exported it, with no channel back to the game.
{% endhint %}

## Exporting a Snapshot

Call `ExportSnapshot`, passing the path to the `.gumx` file you want to write:

```csharp
// Initialize
GumUI.ExportSnapshot(@"C:\Temp\GumSnapshot\Snapshot.gumx");
```

Gum serializes the live tree under its root and writes a complete project at that path: the `.gumx`, a `Screens/` folder, a `Standards/` folder, and copies of any referenced texture files. Open the `.gumx` in the Gum tool to inspect it.

Exporting is on-demand and has no per-frame cost, so a common pattern is to trigger it from a debug key while the game runs:

```csharp
// Update
if (GumUI.Keyboard.KeyPushed(Gum.Forms.Input.Keys.F1))
{
    GumUI.ExportSnapshot(@"C:\Temp\GumSnapshot\Snapshot.gumx");
}
```

Each export overwrites the same location, so you can adjust your running UI and re-export to capture the new state.

## What the Snapshot Contains

A snapshot reproduces the running tree as standard-element instances:

* **The visual tree**, flattened into standard elements — `Container`, `Text`, `Sprite`, `NineSlice`, and so on — parented exactly as they are at runtime.
* **The screen** — your top-level screen maps to the Gum `Screen`. When you add a single screen (such as a Forms screen) to the root, that screen's contents become the screen's instances directly, rather than nesting under an extra wrapper container.
* **Canvas size** matching your game's current resolution, so the layout in the tool matches what you see at runtime.
* **Referenced textures** — files referenced by `Sprite` and `NineSlice` source files are copied next to the project, preserving their relative path, so the snapshot opens self-contained.

## Shaken and Unshaken Snapshots

By default a snapshot is **shaken**: values equal to a standard element's default are omitted, so the file is smaller and only the values you actually changed appear as set in the tool. Pass `shake: false` to write every value instead:

```csharp
// Initialize
GumUI.ExportSnapshot(path, shake: false);
```

An unshaken snapshot is larger and shows every variable as set, but is otherwise equivalent. Shaken snapshots are recommended for inspection; unshaken is available when you want the fully explicit form.
