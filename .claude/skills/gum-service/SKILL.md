---
name: gum-service
description: Reference guide for GumService — the runtime entry point for MonoGame/Raylib/KNI/FNA games. Load this when working on GumService initialization, Uninitialize, DeferredQueue, hot reload, or the Root/PopupRoot/ModalRoot containers.
---

# GumService Reference

## What It Is

`GumService` is the runtime-facing API that game developers use to initialize, update, and draw Gum UI. It lives in `MonoGameGum/GumService.cs` (compiled for XNALIKE, RAYLIB via `#if`).

**Not the CLI.** `Gum.Cli` / `Gum.ProjectServices` are separate tools for headless project validation and codegen.

## Lifecycle

```
GumService.Default.Initialize(game)   // one-time setup
  ↓ per frame:
GumService.Default.Update(gameTime)
GumService.Default.Draw()
  ↓ on teardown (optional):
GumService.Default.Uninitialize()
```

`Initialize` throws if called twice without an intervening `Uninitialize`.

## Singleton Pattern

`Default` is lazily initialized via `??=`. `Uninitialize()` sets `_default = null`, so after teardown `GumService.Default` creates a **fresh instance** — any stored reference to the old instance is now orphaned.

## Uninitialize — Non-Obvious Details

`Uninitialize()` resets a large amount of shared static state across multiple singletons. Key things it does that are surprising:

- Nulls `GraphicalUiElement.SetPropertyOnRenderable`, `AddRenderableToManagers`, etc. — delegates wired by `FormsUtilities.InitializeDefaults`
- Calls `ElementSaveExtensions.ClearRegistrations()` — clears `RegisterGueInstantiation` / `RegisterDefaultInstantiationType` callbacks
- Calls `LoaderManager.Self.DisposeAndClear()` — disposes GPU content and empties the cache
- Nulls and removes `FrameworkElement.PopupRoot` and `FrameworkElement.ModalRoot` from managers
- Resets `FileManager.RelativeDirectory` to `"Content/"` (only meaningful if a project was loaded)
- Calls `_systemManagers.Renderer.Uninitialize()` (XNALIKE only)
- Sets `_default = null` — next access to `Default` creates a new `GumService`

`FormsUtilities.Uninitialize()` is `internal`; tests access it via `InternalsVisibleTo`.

## Roots

| Property | Purpose |
|----------|---------|
| `Root` | Main scene container; sized to canvas on each `Update` |
| `PopupRoot` | Overlaid above Root; for non-modal popups |
| `ModalRoot` | Topmost layer; blocks input to everything below |

`PopupRoot` and `ModalRoot` are `FrameworkElement` statics, not instance fields — they are shared across all `GumService` instances.

## Key Files

| File | Purpose |
|------|---------|
| `MonoGameGum/GumService.cs` | Main class |
| `MonoGameGum/Forms/FormsUtilities.cs` | Input/cursor/gamepad setup; `Uninitialize()` lives here |
| `GumRuntime/ElementSaveExtensions.GumRuntime.cs` | `ClearRegistrations()` called during Uninitialize |
| `RenderingLibrary/Content/LoaderManager.cs` | `DisposeAndClear()` called during Uninitialize |
| `Tests/MonoGameGum.Tests.V2/GumServiceUninitializeTests.cs` | Tests for GPU-accessible Uninitialize behavior |
| `Tests/Gum.ProjectServices.Tests/UninitializeTests.cs` | Tests for non-GPU Uninitialize behavior |

## Testing Split

Uninitialize tests are split across two projects because `FormsUtilities`, `LoaderManager`, and `ElementSaveExtensions` don't all require a GPU:

- `Gum.ProjectServices.Tests` — `LoaderManager`, `ElementSaveExtensions`, `ObjectFinder` (no GPU needed)
- `MonoGameGum.Tests.V2` — `FormsUtilities`, root containers, `FrameworkElement` statics (require test setup with a mock `SystemManagers`)

## Hot Reload

`GumService.EnableHotReload(absoluteGumxSourcePath)` wires up a `GumHotReloadManager` that watches the source project directory and rebuilds `Root.Children` when `.gumx`/`.gusx`/`.gucx`/`.gutx`/`.fnt` files change. `GumService.Update` ticks it each frame; `Uninitialize` stops and nulls it. For details on the reload pipeline, debounce, font cache eviction, and gotchas, load the **gum-runtime-hot-reload** skill. Public docs: `docs/code/hot-reload.md`.
