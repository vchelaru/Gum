# Hot Reload

## Introduction

Gum supports hot reload for rapid UI iteration during development. When hot reload is enabled, changes saved in the Gum tool are automatically reflected in your running game without restarting. This lets you adjust layouts, resize elements, change colors, and modify component structure while seeing results immediately.

Hot reload watches for changes to Gum project files (`.gumj`, `.gusj`, `.gucj`, `.gutj`, or their XML forms `.gumx`, `.gusx`, `.gucx`, `.gutx`) and font files (`.fnt`) and reconstructs the element tree when changes are detected.

## Enabling Hot Reload

Call `EnableHotReload` after initializing Gum, passing the absolute path to your **source** `.gumj` file — not the copy in your `bin/Content` folder.

```csharp
// Initialize
var gumProject = GumUI.Initialize(this, "GumProject/GumProject.gumj");

GumUI.EnableHotReload(
    @"c:\Users\YourName\source\YourGame\Content\GumProject\GumProject.gumj");
```

{% hint style="info" %}
The path must point to the original `.gumj` file that the Gum tool edits. If you point to the `bin/Content` copy, changes saved in the Gum tool will not be detected because that copy is only updated on build.
{% endhint %}

Once enabled, hot reload is fully automatic. `GumService` processes pending reloads during its `Update` call each frame, so no additional code is needed.

<figure><img src="../../.gitbook/assets/26_06 11 12.gif" alt=""><figcaption><p>Gum at runtime (top) updating in realtime to changes in the tool (bottom)</p></figcaption></figure>

## What Gets Reloaded

Hot reload monitors the following Gum file types:

| Extension | Description                  |
| --------- | ---------------------------- |
| `.gumj` / `.gumx` | Gum project file |
| `.gusj` / `.gusx` | Screen definitions |
| `.gucj` / `.gucx` | Component definitions |
| `.gutj` / `.gutx` | Standard element definitions |
| `.fnt`    | Bitmap font definitions      |

When any of these files change, Gum reloads the project in place: it diffs the new definitions against the live visual tree and updates only what changed. This covers variable edits (move, resize, color, state changes) as well as structural changes — instances added by copy/paste, instances deleted, instances whose `BaseType` was changed, and instance reorders.

Children added at runtime by your game code are left untouched. Hot reload identifies "design-time" children by their `Tag` — when Gum creates a visual from an `InstanceSave`, it stores the `InstanceSave` in `Tag`. Anything without an `InstanceSave` `Tag` is treated as runtime-owned and skipped.

When a reload is triggered, Gum automatically copies all `.fnt` and `.png` files from the source project's `FontCache/` directory to the bin-side `FontCache/` directory, and evicts cached fonts so they are reloaded from disk. Every `Text` then resolves its font again, so a font regenerated under the same name, or one that is written after the project was saved, shows on `Text` that is already on screen.

### Renamed or Deleted Screens and Components

Hot reload matches each screen or component your game is showing to the project by name. If you rename one, move it to another folder, or delete it in the tool, the running visual has nothing to match. It stays on screen as it was, and Gum reports it once to `PropertyAssignmentError` and the console, for example `Hot reload: the screen 'MainScreen' the game is showing is not in the project any more`. A component reads `the component 'Name'` instead. Edits to it, or to what replaced it, do not reach the game until you restart it. If you undo the rename, Gum matches it again and edits arrive.

An instance of a deleted component inside a screen or another component is treated differently. The tool keeps the instance in the file, and a restarted game would not create it, so Gum removes the instance and its children from the running UI to match. It reports this once as well, for example `Hot reload: 'Card1' in 'Main' is a 'Card', which is not in the project any more`.

An instance whose component was renamed, moved to another folder, or changed to another type is rebuilt, since its visual is made from the new component. The running tree is right, but code references and Forms handlers you held on the old visual are stale. Gum reports this once per pair of components in each reload, for example `Hot reload: 'Card1' in 'Main' is now a 'Card2' (was 'Card')`. The removal and the rebuild are also issues in the result of [`HotReloadFinished`](#reacting-to-a-reload).

### What Is Not Reloaded

Hot reload is focused on Gum element definitions. The following are **not** automatically reloaded:

* **Shader files** — When a file that a container's `SourceShaderFile` points at changes (any extension), the shader is compiled again and assigned to every container using it. The element tree is not rebuilt. A file the shader imports or includes is not tracked.
* **Textures and images** — If you change a `.png` file, the cached texture is still used. Restart the game to pick up texture changes.
* **Animation files** — `.ganx` animation definitions are not reloaded.
* **Runtime state on existing instances** — A reload applies the variables the project changed and leaves the rest alone, so a value your code set (e.g., `Text` or `Width`) stays unless the project changed that variable. See [Preserving Runtime State](#preserving-runtime-state) below.


### Known Limitation: Cleared Tags

If your code clears (`= null`) or replaces the `Tag` on a visual that was created from an `InstanceSave`, hot reload can no longer recognize it as a design-time child. It is treated as runtime-owned: not removed, not retyped, and not duplicated when the corresponding `InstanceSave` is still present in the project. The parent's qualified-name variables (`MyInstance.X`, etc.) still flow through to it by `Name`, so visible variables continue to update.

## Platform Support

{% hint style="info" %}
Hot reload requires loose project files on disk; it does not watch inside `.gumpkg` bundles. Initializing with a `.gumj` path loads loose files and watchers fire normally, even when a `.gumpkg` sits next to them (see [pack](../../cli/pack.md)). Initializing with a `.gumpkg` path loads at startup but cannot hot-reload.

If hot reload from a `.gumpkg` (e.g. for a hot-swap-on-deploy workflow) would be useful to you, let us know on [Discord](https://discord.gg/EvqwmSQuBz) or file an issue on [GitHub](https://github.com/vchelaru/Gum/issues) — real usage reports help us prioritize.
{% endhint %}

Hot reload is available on desktop platforms:

* MonoGame
* KNI
* FNA
* Raylib

Hot reload is **not available** on iOS or Android. Calls to `EnableHotReload` are excluded at compile time on these platforms.

## How It Works

When `EnableHotReload` is called, Gum creates a `FileSystemWatcher` on the directory containing your `.gumj` file (including subdirectories). When a watched file is saved:

1. The change is detected and a 200ms debounce timer starts. Additional changes within that window reset the timer, so rapid saves (such as the Gum tool writing multiple files at once) are coalesced into a single reload.
2. On the next `GumService.Update` call after the debounce window, Gum reloads the project from disk.
3. Gum walks the live visual tree and, for each visual that matches a project element by name, reconciles its design-time children with the new `Instances` list (adding, removing, retyping, and reordering as needed) and applies the default-state variables the project changed. Runtime-added children are left in place.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

A variable the reload does not find changed keeps the value your code gave it. Changing a variable in the Gum tool, or the default of the component an instance is built from, applies the new value over a value your code set. A variable the control's current state also sets is applied again with the state, so a value your code set on it is reset.

A project file that cannot be read during a reload (half written or locked by the editor) leaves the running UI as it was, and the file name and reason are reported through `CustomSetPropertyOnRenderable.PropertyAssignmentError` and the console. Gum retries once after the debounce, and the next complete save reloads normally. An element file that was deleted while the project still lists it is reported and left out, and the rest reloads. A failure while applying a file that loaded is reported the same way and the next complete save reloads.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Preserving Runtime State

Values your code set stay as long as the project does not change those variables. When the project does change a variable, the reload applies it, including to state your game derives from it, such as text computed from a variable. Subscribe to the `HotReloadCompleted` event to set that state again.

`HotReloadCompleted` fires after Gum has finished rebuilding the element tree but before the next frame draws, so changes made in the handler are visible immediately.

```csharp
// Initialize
GumUI.HotReloadCompleted += ReapplyRuntimeState;

void ReapplyRuntimeState()
{
    // Hide UI that should not be visible at this point in the game.
    _winOverlay.IsVisible = false;

    // Re-populate dynamic content driven by game state.
    _scoreLabel.Text = _player.Score.ToString();

    // Re-bind anything that was wired up after element creation.
    _inventoryListBox.ItemsSource = _player.Inventory;
}
```

{% hint style="info" %}
Only re-apply state that originated in your code. Values authored in the Gum tool are already restored by the reload itself.
{% endhint %}

Common things to re-apply in a `HotReloadCompleted` handler:

* Visibility of overlays, modals, or panels that were hidden or shown by game code.
* Text values that reflect game state (scores, names, timers, currency).
* Data bindings or `ItemsSource` assignments on `ListBox`, `ComboBox`, etc.
* Event handler subscriptions on buttons or other interactive elements, if they were attached to specific instances rather than registered through Forms.

If your game has a single screen-level "apply current state to UI" method, calling it from `HotReloadCompleted` is usually the simplest approach.

## Reacting to a Reload

`HotReloadFinished` fires after every reload, including one that failed, and gives you a `HotReloadResult` listing what the reload could not do exactly. `HotReloadCompleted` still fires after a reload that was applied.

```csharp
// Initialize
GumUI.HotReloadFinished += result =>
{
    if (result.IsClean)
    {
        return;
    }

    foreach (HotReloadIssue issue in result.Issues)
    {
        Console.WriteLine($"{issue.Kind}: {issue.Message}");
    }

    if (!result.MatchesRestart)
    {
        Console.WriteLine("The running UI differs from what a restart would show.");
    }
};
```

Every reload's result lists every issue that reload had, including one an earlier reload had too. Only the console output is once-only. A project that could not be read is retried once after the debounce, and only the final outcome raises the event. An exception thrown by a handler of `HotReloadCompleted` or `HotReloadFinished` is printed and does not stop the reload or the other handlers.

| `HotReloadIssueKind`  | Meaning                                                                                             | `MatchesRestart` |
| --------------------- | --------------------------------------------------------------------------------------------------- | ---------------- |
| `MissingFile`         | The project lists a file that is not on disk. The reload went on without it.                        | `false`          |
| `FontFailure`         | A changed font file could not be copied. It is tried again on the next reload.                      | `false`          |
| `ReloadFailed`        | The project could not be read, or applying it threw. The UI is unchanged or partly updated.         | `false`          |
| `ShownElementGone`    | The screen or component the game shows was renamed, moved or deleted. It stays on screen as it was. | `false`          |
| `InstanceRemoved`     | An instance of a deleted component was removed, as a restart would.                                 | `true`           |
| `InstanceRebuilt`     | An instance's component changed or was renamed, so its visual was rebuilt. References and handlers on it are stale. | `true` |
| `ComponentBaseTypeChanged` | A screen or component changed its base type. Visuals already on screen keep what they inherited from the old base. | `false` |
| `BaseElementMissing` | A screen or component derives from an element that is not in the project. Visuals already on screen keep what they inherited from it. | `false` |
| `ExposedVariableChanged` | A component changed which variables it exposes. Instances already on screen keep the exposure they were built with. | `false` |
| `StandardElementChanged` | The default state of a standard element changed. Instances already on screen keep their old values. | `false` |

An issue that makes `MatchesRestart` `false` because the running tree is stale, such as `ShownElementGone` or `ComponentBaseTypeChanged`, is listed in every result until a restart or until an edit puts the definition back.

`MatchesRestart` is `true` when the running tree equals what restarting the game with the same project would show. On the result it is `true` only if every issue's `MatchesRestart` is `true`, so a clean result matches. An issue with `MatchesRestart` set to `true` can still need attention from your code, for example `InstanceRebuilt`.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Disabling Hot Reload

Hot reload is automatically stopped when `GumService.Uninitialize()` is called. There is no separate method to disable it without uninitializing the entire Gum service. Since hot reload is a development-time feature, the simplest approach is to only call `EnableHotReload` in debug builds:

```csharp
// Initialize
var gumProject = GumUI.Initialize(this, "GumProject/GumProject.gumj");

#if DEBUG
GumUI.EnableHotReload(
    @"c:\Users\YourName\source\YourGame\Content\GumProject\GumProject.gumj");
#endif
```
