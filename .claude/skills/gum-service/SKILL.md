---
name: gum-service
description: GumService — runtime entry point for MonoGame/Raylib/KNI/FNA. Triggers: GumService init, Uninitialize, DeferredQueue, hot reload, Root/PopupRoot/ModalRoot containers.
---

# GumService Reference

## What It Is

`GumService` is the runtime-facing API that game developers use to initialize, update, and draw Gum UI. It lives in `MonoGameGum/GumService.cs` (compiled for XNALIKE, RAYLIB via `#if`) under the `Gum` namespace (since issue #3119 / syntax version 3). Legacy `MonoGameGum.GumService` / `RaylibGum.GumService` names are permanent `[Obsolete]` subclass shims in `MonoGameGum/GumServiceCompat.cs` (linked into `RaylibGum.csproj`). `WindowZoomMode`, `GumHotReloadManager`, and related hot-reload types also live in `namespace Gum`.

**The soft migration is intentional and finished — do not flag it as incomplete.** What matters is what the **user types**: new code uses `using Gum;` and types the service as `GumService` (= `Gum.GumService`) with no `using MonoGameGum;` / `using RaylibGum;` — that is 100% future-proof. `Gum.GumService.Default` is *deliberately* typed as the derived `[Obsolete]` shim so legacy `using MonoGameGum;` / `using RaylibGum;` declarations keep compiling; the user never names the obsolete type, so the modern path is warning-free. The `GumService` shim stays and keeps warning; the other shims in that file are removed no earlier than November 2027. One part is planned for later: retyping `Default` to `Gum.GumService`, because the typed setter cannot hold a plain `Gum.GumService` (multi-service code uses `IGumService.Default`). It is a breaking change held in the Future milestone for a release the maintainer picks, so do not do it or open new issues about it until asked. The issue is titled "GumService.Default should be typed as Gum.GumService so it can hold any service".

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

`Initialize` throws if called twice without an intervening `Uninitialize`. A failed `Initialize` (missing `.gumx`) rolls itself back, so the service can be initialized again. `Draw()`/`Update()` on an uninitialized service throw `InvalidOperationException`; `Draw()` on a service that was never initialized draws `SystemManagers.Default`.

`Draw()` draws everything through `SystemManagers.Default` in one call — for manual control over what's drawn when/to-which-target (e.g. compositing onto multiple `RenderTarget2D`s in one frame), use `GumBatch`'s immediate-mode `Begin`/`Draw`/`End` instead (`docs/code/rendering/gumbatch.md`; internals in the **gum-monogame-rendering** skill).

**Only the `gumProjectFile` overload loads a project.** `Initialize(game, gumProjectFile)` loads the `.gumx` and runs the project-load path — which applies project-level settings (standard-element defaults, localization, and the project's `TextureFilter` → `Renderer.TextureFilter`, issue #3199). The `Initialize(game, DefaultVisualsVersion)` / parameterless overloads are **code-only** and never touch that path. So when manually verifying anything that depends on a loaded project, you must pass the `.gumx` path or the behavior under test never runs. The cheapest from-file smoke test is `Initialize(game, "GumProject/GumProject.gumx")` plus a `SpriteRuntime` (`SourceFileName` set, scaled up) added to `Root`.

## GumService vs IGumService

Both stay supported long term. `GumService` is strongly typed and the more convenient choice for most games. `IGumService` (`RenderingLibrary/IGumService.cs`) is for code that should be truly cross-platform, for users who expect to migrate or support several runtimes, and it exposes interfaces. Neither replaces the other; each should be as fully featured as it can be, so a platform-neutral capability a game gets from `GumService` should also be reachable through `IGumService`.

**Cursor and keyboard accessors.** `GumService.Cursor` / `Keyboard` return the concrete `Cursor` / `Keyboard` and stay that way, so they are null while a custom `ICursor` / `IInputReceiverKeyboard` is installed with `FormsUtilities.SetCursor` / `SetKeyboard`. `IGumService.Cursor` / `Keyboard` and `FormsUtilities.Cursor` / `Keyboard` return whatever is installed. `FormsUtilities.SetCursor` / `SetKeyboard` only assign `FrameworkElement.MainCursor` / `MainKeyboard` (no event is raised), and the MonoGame service reads them per service (process statics for the first service, `GumContext` for later ones). A new `IGumService` implementation reads `FormsUtilities.Cursor` / `Keyboard`; never forward to the typed property.

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
- Skips the teardown when this service is not initialized and a different service is live, so a stray or repeated `Uninitialize` cannot reset the live service's statics
- Clears `SystemManagers.Default`/`ISystemManagers.Default`/`IGumService.Default` only when they belong to this service; restores `FileManager.CustomGetStreamFromFile` only if nobody replaced the bundle hook since; drops the language-change subscription
- A service given its own `SystemManagers` (`Initialize(game, managers)`) adopts them as `SystemManagers.Default` only when none exists

`FormsUtilities.Uninitialize()` is `internal`; tests access it via `InternalsVisibleTo`.

**Skia differs:** `GumServiceSkiaBase` (`Runtimes/SkiaGum/GumServiceSkiaBase.cs`) wires the runtime-type registrations and property/renderable delegates once per process, so its `Uninitialize` keeps them and removes only what the loaded project added. Clearing them the way MonoGame does leaves a later `Initialize` without them.

## Roots

| Property | Purpose |
|----------|---------|
| `Root` | Main scene container; sized to canvas on each `Update` |
| `PopupRoot` | Overlaid above Root; for non-modal popups |
| `ModalRoot` | Topmost layer; blocks input to everything below |

`PopupRoot` and `ModalRoot` are `FrameworkElement` statics, not instance fields — they are shared across all `GumService` instances.

## Multiple GumService Instances

The constructor is public (`new GumService()`) and each XNA-like instance owns its project lookup, cursor, canvas and element registrations through a `GumContext` (`GumRuntime/GumContext.cs`). `IGumService.Default` is the current service: `Initialize` sets it only when none exists, `Update`/`Draw` set it for the call and restore it, and code that builds or changes another service's elements outside those calls must assign it first (a sample doing this: `Samples/MultiGumServiceSample/`). Raylib, Skia and Silk stay single-service. User docs: the "More Than One GumService" section of `docs/code/gum-code-reference/gumservice-gumui/README.md`; the remaining shared state is listed in `Direction/multi-gum-service-audit.md`.

## Key Files

| File | Purpose |
|------|---------|
| `MonoGameGum/GumService.cs` | Main class (`namespace Gum`) |
| `MonoGameGum/GumServiceCompat.cs` | `[Obsolete]` subclass shims for `MonoGameGum.GumService` / `RaylibGum.GumService` (permanent); also holds the `AddChild` and `ToGraphicalUiElement` forwarders (obsolete, dated retention, not permanent) |
| `MonoGameGum/Forms/FormsUtilities.cs` | Input/cursor/gamepad setup; `Uninitialize()` lives here |
| `GumRuntime/ElementSaveExtensions.GumRuntime.cs` | `ClearRegistrations()` called during Uninitialize |
| `RenderingLibrary/Content/LoaderManager.cs` | `DisposeAndClear()` called during Uninitialize |
| `Tests/MonoGameGum.Tests.V3/GumServiceUninitializeTests.cs` | Tests for GPU-accessible Uninitialize behavior |
| `Tests/Gum.ProjectServices.Tests/UninitializeTests.cs` | Tests for non-GPU Uninitialize behavior |

## Testing Split

Uninitialize tests are split across two projects because `FormsUtilities`, `LoaderManager`, and `ElementSaveExtensions` don't all require a GPU:

- `Gum.ProjectServices.Tests` — `LoaderManager`, `ElementSaveExtensions`, `ObjectFinder` (no GPU needed)
- `MonoGameGum.Tests.V3` — `FormsUtilities`, root containers, `FrameworkElement` statics (require test setup with a mock `SystemManagers`)

## Hot Reload

`GumService.EnableHotReload(absoluteGumxSourcePath)` wires up a `GumHotReloadManager` that watches the source project directory and rebuilds `Root.Children` when `.gumx`/`.gusx`/`.gucx`/`.gutx`/`.fnt` files change. `GumService.Update` ticks it each frame; `Uninitialize` stops and nulls it. For details on the reload pipeline, debounce, font cache eviction, and gotchas, load the **gum-runtime-hot-reload** skill. Public docs: `docs/code/hot-reload.md`.
