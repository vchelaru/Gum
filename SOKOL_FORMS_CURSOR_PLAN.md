# SOKOL Forms Support — Cursor & Keyboard Plan

## Scope

This document describes the work required to make `Gum.Forms` controls usable under the SokolGum runtime. It focuses on input (Cursor + Keyboard) because that is the last remaining gap — Forms source is already shared via `<Compile Include="..\..\MonoGameGum\Forms\Controls\**\*.cs" />` in the other runtime csproj files, and `ICursor` / `IInputReceiverKeyboard` are already the Forms-facing abstractions.

**Out of scope:** writing the code. This document is a plan only.

## Current state

### What's already in place

- **`ICursor` interface** (in `GumRuntime/InteractiveGue.cs`) — Forms controls already consume only this, never the concrete `Cursor` class.
- **`IInputReceiverKeyboard`** — unified interface post-branch `KeyboardUnification2`. Forms controls use `FrameworkElement.MainKeyboard` (interface-typed).
- **`Gum.Forms.Input.Keys`** — platform-neutral key enum introduced during keyboard unification. Per-platform `Keyboard` implementations translate native keys → this enum.
- **Shared Forms source** — the Raylib csproj already globs in `MonoGameGum/Forms/Controls/**/*.cs`. SokolGum will do the same.

### What SokolGum has today

- `Runtimes/SokolGum/` — no `Input/` folder, no `Cursor`, no `Keyboard`, no Forms link.
- `Runtimes/SokolGum/GumService.cs` — has Initialize/Uninitialize/Update/Draw but nothing wiring input.
- `SokolGum.csproj` defines `SOKOL` and `FULL_DIAGNOSTICS` constants; does **not** define `RAYLIB` or `MONOGAME`/`KNI`/`FNA`/`XNA4`.

### The wrinkle (flagged as a problem)

My earlier research summary implied `Cursor.cs` was already set up to accommodate any new runtime. It is not quite that clean. Looking at `MonoGameGum/Input/Cursor.cs`:

- The file assumes a binary world: `#if RAYLIB` vs. everything-else-is-XNA-family.
- Top of file:
  ```csharp
  #if MONOGAME || KNI || XNA4 || FNA
  #define XNALIKE
  #endif
  ```
  and later `#if RAYLIB` picks Raylib namespaces/usings, `#else` falls through to `Microsoft.Xna.Framework.*`.
- `CustomCursor` setter branches on `#if MONOGAME || KNI` and `#if RAYLIB`.
- The Raylib build sidesteps the XNA dependency by shimming lookalike types (`MouseState`, `ButtonState`, `TouchCollection`, `TouchLocation`) inside `Runtimes/RaylibGum/Input/` in the `RaylibGum.Input` namespace — so when the shared file compiles under `RAYLIB`, `MouseState` resolves to the shim, not the XNA type.

**Implication for SOKOL:** simply adding a partial is not enough. The shared `Cursor.cs` needs a third branch (`#if SOKOL`) for namespace + usings, and the `CustomCursor` implementation needs a SOKOL arm (probably a no-op or a call to `sapp_set_mouse_cursor` if Sokol exposes it). Sokol will also need its own set of XNA-lookalike shim types — or we reference the Raylib shims via source linking.

This is not a blocker, but it is more invasive than "write two new files." It means touching a file shared by every XNA-family runtime (MonoGame, KNI, FNA) and by Raylib. Every runtime will recompile. Worth keeping the diff tight.

## Work items

### 1. Shim types for Sokol

Create `Runtimes/SokolGum/Input/` and add XNA-lookalike shims so the shared `Cursor.cs` can compile under `SOKOL`:

- `MouseState.cs` — struct with `X`, `Y`, `LeftButton`, `MiddleButton`, `RightButton`, `ScrollWheelValue` (the XNA arm of `ScrollWheelChange` reads last-minus-current `ScrollWheelValue`).
- `ButtonState.cs` — `Released` / `Pressed` enum.
- `TouchCollection.cs` / `TouchLocation.cs` — minimal stubs (`Count`, indexer returning a struct with `.Position`). Touch is out of scope for v1 but the shared file references these types.

**Namespace: `Gum.Input`** (not `SokolGum.Input`). All shim types and the `SOKOL` arm of the shared `Cursor.cs` live under this namespace. Rationale: SokolGum is positioned as the canonical modern runtime, so its input types get the unprefixed `Gum.*` namespace rather than a platform-prefixed one.

### 2. Extend shared `Cursor.cs` to know about SOKOL

In `MonoGameGum/Input/Cursor.cs`:

- **Top block (lines 9–23):** add a `#elif SOKOL` arm. Mirror Raylib's setup (`using System.Numerics; using Matrix = System.Numerics.Matrix3x2;`) but `namespace Gum.Input;`. Final shape: `#if RAYLIB … #elif SOKOL … #else (XNA) … #endif`.
- **`CustomCursor` setter (line 40):** add a `#if SOKOL` arm calling `SApp.sapp_set_mouse_cursor(sapp_mouse_cursor.SAPP_MOUSECURSOR_ARROW | RESIZE_NS | RESIZE_EW | RESIZE_NWSE | RESIZE_NESW)` — Sokol exposes all the shapes we need (confirmed: `sapp_mouse_cursor` enum in `Sokol.NET/src/sokol/generated/SApp.cs` lines 743–772).
- **`ScrollWheelChange` (line 186):** add a `#if SOKOL` arm. Sokol delivers scroll through `SAPP_EVENTTYPE_MOUSE_SCROLL` events with `scroll_y` floats (no live poll). Accumulate scroll in the partial's event handler and read it via `_mouseState.ScrollWheelValue`.
- **Activity touch-null arm (line 487):** change `#if RAYLIB` to `#if RAYLIB || SOKOL` so the null guard applies.
- Constructor (line 422/429) and `_gameWindow` field (line 412–414) are `#if XNALIKE`-guarded — SOKOL falls through to the parameterless `#else` arm correctly. No change needed there.

### 3. `Cursor.Sokol.cs` partial

New file `Runtimes/SokolGum/Input/Cursor.Sokol.cs`. Namespace `Gum.Input`. `partial class Cursor` providing:

- `private MouseState GetMouseState()` — returns the currently-accumulated state from an internal buffer (not a live poll).
- `private TouchCollection GetTouchCollection()` — return empty for v1.

**Confirmed: Sokol is event-only for input.** `Sokol.NET/src/sokol/generated/SApp.cs` exposes no `sapp_mouse_x()` / `sapp_mouse_y()` functions. Input arrives through the host's event callback as `sapp_event_t` with fields `mouse_button`, `mouse_x`, `mouse_y`, `mouse_dx`, `mouse_dy`, `scroll_x`, `scroll_y`. Relevant event types: `SAPP_EVENTTYPE_MOUSE_DOWN`, `MOUSE_UP`, `MOUSE_MOVE`, `MOUSE_SCROLL`.

**Design:** `Cursor.Sokol.cs` owns a static (or instance-static-accessible) buffer holding current `MouseState`. It exposes a static method `HandleSokolEvent(in sapp_event ev)` that:

- On `MOUSE_MOVE` / `MOUSE_DOWN` / `MOUSE_UP`: update `_pendingState.X/Y` from `mouse_x/mouse_y` and Left/Right/Middle button state from `mouse_button` + event type.
- On `MOUSE_SCROLL`: accumulate `scroll_y` into `_pendingState.ScrollWheelValue` (scaled by 120 to match XNA detent convention, so the existing `ScrollWheelChange` math in the shared file works unchanged).

`GetMouseState()` returns `_pendingState` — `Activity(...)` in the shared file already snapshots it into `_mouseState` once per frame, so this plays nicely with the existing Push/Click/Release derivation logic.

This ripples into task #3 (GumService): host apps pass Sokol events to `GumService.Default.HandleSokolEvent(ev)` from their `sokol_app` event callback, which forwards to the static buffer on `Cursor`.

### 4. `Keyboard.cs` for SokolGum

New file `Runtimes/SokolGum/Input/Keyboard.cs`. Implements `IInputReceiverKeyboard`.

- Mirror the Raylib `Keyboard.cs` (not a partial — a standalone file).
- Translate Sokol key codes → `Gum.Forms.Input.Keys`. Will need a lookup table similar to what Raylib has.
- Character/typed-text input: Sokol exposes `SAPP_EVENTTYPE_CHAR` for Unicode input — the implementation should buffer these for `GetKeysTyped`-equivalent queries. Same event-vs-poll concern as Cursor.

Reference: look at the recent commits `aff9eafd6` ("Fixed GetKeysTyped in raylib") and `861abda62` ("Forms Keyboard unification") for the shape of what the interface expects.

### 5. Wire input into `GumService`

In `Runtimes/SokolGum/GumService.cs`:

- Add `public Cursor Cursor { get; private set; }` and `public Keyboard Keyboard { get; private set; }`.
- In `Initialize`, construct both and assign:
  ```csharp
  FrameworkElement.MainCursor = Cursor;
  FrameworkElement.MainKeyboard = Keyboard;
  ```
- In `Update`, call `Cursor.Activity(totalSeconds)` and `Keyboard.Activity(totalSeconds)` (or the equivalent tick methods — check what Raylib's GumService does).
- If using an event-fed buffer (see risks above), `GumService` also needs an entry point for the host app's `sokol_app` event callback to forward events into.
- In `Uninitialize`, null out `MainCursor` / `MainKeyboard`.

### 6. Link Forms source in `SokolGum.csproj`

Mirror the Raylib csproj block starting at line 45:

- Forms controls glob: `<Compile Include="..\..\MonoGameGum\Forms\Controls\**\*.cs" ... />`
- All the `Forms/Data/*`, `Forms/DefaultVisuals/**`, `Forms/*` files currently linked by Raylib.
- The `<Compile Remove>` block for Image/Menu/MenuItem/PasswordBox (same as Raylib).
- `Input/Cursor.cs`, `Input/CursorExtensions.cs`, `Input/KeyCombo.cs` source-linked from MonoGameGum.
- `Clipboard/ClipboardImplementation.cs` if clipboard is in scope (TextBox needs this). If SokolGum can't support clipboard on day one, leave this out and expect TextBox to throw or no-op. Flag for the user.

### 7. Sample / smoke test

Add or extend a Sokol sample that instantiates a Button and a TextBox, proves clicks register, and proves typed characters appear in the TextBox. Not strictly required for the PR, but the coder agent should confirm the end-to-end path rather than stopping at "it compiles."

## Questions the coder agent should resolve before starting

1. ~~**Sokol input API shape**~~ — **Resolved: event-only.** See task 3.
2. ~~**Cursor shape control**~~ — **Resolved: `sapp_set_mouse_cursor` exists** with all needed shapes (Arrow, Resize_NS, Resize_EW, Resize_NWSE, Resize_NESW).
3. **Clipboard** — is TextBox in v1 scope? If yes, `ClipboardImplementation.cs` needs to work under SOKOL (currently uses `TextCopy` package, which may already be cross-platform enough).
4. **Touch** — ignore for v1? (recommended).

## Recommended order

1. (2) + (1) first — make the shared `Cursor.cs` compile under `SOKOL` with empty shims and a no-op partial. Build `AllLibraries.sln` and confirm nothing else broke.
2. (3) — fill in real mouse reading.
3. (4) — keyboard.
4. (5) — wire into `GumService`.
5. (6) — link Forms source, build, fix fallout.
6. (7) — smoke test.

Each step is independently verifiable with a build. Do not bundle.

## Risk summary

- **Low:** csproj linking (well-trodden path from Raylib).
- **Low:** `CustomCursor` wiring — Sokol's cursor shape enum is a 1:1 match.
- **Medium:** the `Cursor.cs` third-branch edit — touches a file compiled into every runtime. Keep the diff minimal and verify all other runtimes still build.
- **Medium:** the event-driven input model requires a static event buffer on `Cursor` (and later `Keyboard`) plus an `HandleSokolEvent` forwarding method on `GumService`. Well-understood now, but it's more code than the other runtimes need.
