# Hot Reload

## Introduction

Gum supports hot reload for rapid UI iteration during development. When hot reload is enabled, changes saved in the Gum tool are automatically reflected in your running game without restarting. This lets you adjust layouts, resize elements, change colors, and modify component structure while seeing results immediately.

Hot reload watches for changes to Gum project files (`.gumx`, `.gusx`, `.gucx`, `.gutx`) and font files (`.fnt`) and reconstructs the element tree when changes are detected.

## Enabling Hot Reload

Call `EnableHotReload` after initializing Gum, passing the absolute path to your **source** `.gumx` file — not the copy in your `bin/Content` folder.

```csharp
// Initialize
var gumProject = GumUI.Initialize(this, "GumProject/GumProject.gumx");

GumUI.EnableHotReload(
    @"c:\Users\YourName\source\YourGame\Content\GumProject\GumProject.gumx");
```

{% hint style="info" %}
The path must point to the original `.gumx` file that the Gum tool edits. If you point to the `bin/Content` copy, changes saved in the Gum tool will not be detected because that copy is only updated on build.
{% endhint %}

Once enabled, hot reload is fully automatic. `GumService` processes pending reloads during its `Update` call each frame, so no additional code is needed.

<figure><img src="../.gitbook/assets/26_06 11 12.gif" alt=""><figcaption><p>Gum at runtime (top) updating in realtime to changes in the tool (bottom)</p></figcaption></figure>

## What Gets Reloaded

Hot reload monitors the following Gum file types:

| Extension | Description                  |
| --------- | ---------------------------- |
| `.gumx`   | Gum project file             |
| `.gusx`   | Screen definitions           |
| `.gucx`   | Component definitions        |
| `.gutx`   | Standard element definitions |
| `.fnt`    | Bitmap font definitions      |

When any of these files change, Gum reloads the entire project and reconstructs all children of the root element from the updated definitions. This means changes to layout, styling, states, variables, component structure, and fonts are all picked up.

When a reload is triggered, Gum automatically copies all `.fnt` and `.png` files from the source project's `FontCache/` directory to the bin-side `FontCache/` directory, and evicts cached fonts so they are reloaded from disk.

### What Is Not Reloaded

Hot reload is focused on Gum element definitions. The following are **not** automatically reloaded:

* **Textures and images** — If you change a `.png` file, the cached texture is still used. Restart the game to pick up texture changes.
* **Animation files** — `.ganx` animation definitions are not reloaded.
* **Runtime state** — Any properties set programmatically at runtime (e.g., changing `Text` or `Width` in code) are lost when the element is recreated. Only values defined in the Gum project are restored.

## Platform Support

Hot reload is available on desktop platforms:

* MonoGame
* KNI
* FNA
* Raylib

Hot reload is **not available** on iOS or Android. Calls to `EnableHotReload` are excluded at compile time on these platforms.

## How It Works

When `EnableHotReload` is called, Gum creates a `FileSystemWatcher` on the directory containing your `.gumx` file (including subdirectories). When a watched file is saved:

1. The change is detected and a 200ms debounce timer starts. Additional changes within that window reset the timer, so rapid saves (such as the Gum tool writing multiple files at once) are coalesced into a single reload.
2. On the next `GumService.Update` call after the debounce window, Gum reloads the project from disk.
3. All children of the root element are removed and recreated from the updated element definitions, preserving their order.

{% hint style="warning" %}
Because elements are fully recreated during hot reload, any runtime state set in code is lost. If your game sets properties on UI elements after creation (e.g., populating a label with a score), that code needs to run again after a reload. For development purposes, this is usually acceptable — hot reload is a development-time feature, not intended for production builds.
{% endhint %}

## Disabling Hot Reload

Hot reload is automatically stopped when `GumService.Uninitialize()` is called. There is no separate method to disable it without uninitializing the entire Gum service. Since hot reload is a development-time feature, the simplest approach is to only call `EnableHotReload` in debug builds:

```csharp
// Initialize
var gumProject = GumUI.Initialize(this, "GumProject/GumProject.gumx");

#if DEBUG
GumUI.EnableHotReload(
    @"c:\Users\YourName\source\YourGame\Content\GumProject\GumProject.gumx");
#endif
```
