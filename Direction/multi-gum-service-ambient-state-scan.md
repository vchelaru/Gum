# Multiple GumService instances: ambient-state scan (#6066)

- **Date:** 2026-10-16
- **Base:** `origin/6066-proto-context` @ `841e4e0b6`. Read-only scan, no production code changed, nothing was run.
- **Companion to:** `multi-gum-service-audit.md`, which inventories Gum's own statics. This file covers state that is not a Gum static field but that two services in one process share, fight over, or that Gum code assigns or reads implicitly (thread-level, process-level, third-party, event hookups).
- **Focus:** MonoGame / KNI / FNA path (one source, `XNALIKE`). Raylib, Skia/Silk, Sokol are in section 7.

Tags: **V** = read at the cited `file:line`. **I** = inferred from code that was read, not traced end to end or run.
Verdicts: **COLLIDE** (two services break each other or leak into each other), **FINE**, **GUESS** (plausible, needs a test or a decision).

## 1. Ranked findings

| # | Verdict | Finding | Where | Writer | Tag |
|---|---|---|---|---|---|
| 1 | COLLIDE | `SynchronizationContext`: last service to call `UseSingleThreadedAsync()` wins the thread, and nobody restores it | see 2.1 | each service's `UseSingleThreadedAsync` | V |
| 2 | COLLIDE | `IGumService.Default` is a plain static that is read at call time in code that can run outside any `Update`/`Draw` scope (after an `await`, thread-pool callbacks, host code between frames) | see 2.2 | `GumContext.EnterDefault`, `Initialize`, `Uninitialize`, user code | V |
| 3 | COLLIDE | Forms refresh hooks are installed in the `GumService` constructor but nulled by the last service's `Uninitialize`, so a service built before that teardown never gets them back | `MonoGameGum/GumService.cs:542` vs `:793-794`; Skia installs in `Initialize` (`GumServiceSkiaBase.cs:449`) | ctor / last `Uninitialize` | V |
| 4 | COLLIDE (I) | Base-layer registrations re-run on every service `Initialize` get a newer stamp than earlier services' own-layer entries and shadow them. Generated-code registrations always land in the one shared Base | `GumRuntime/ElementTypeRegistry.cs:78-105`; `GumService.XnaLike.cs:235-242,323,329`; `ElementSaveExtensions.GumRuntime.cs:30-32` | reflection hooks (`RegisterRuntimeType[s]`), `[ModuleInitializer]` | V/I |
| 5 | COLLIDE (I) | Language change calls each service's `RefreshLocalization()` with no scope entered, so with two live services the variable-reference refresh resolves `ObjectFinder.Self` against whoever is Default | `GumService.cs:381-386`, `LanguageChangeSubscription.cs:23-33`, `GraphicalUiElement.Lifecycle.cs:202-214` | `ILocalizationService.CurrentLanguageChanged` | V/I |
| 6 | COLLIDE (leak) | Static-event subscriptions that are never undone: ctor `LocalizationServiceChanged +=`, per-service `CurrentLanguageChanged` handler, `Window.TextInput` / FNA `TextInputEXT.TextInput` | `GumService.cs:517`; `LanguageChangeSubscription.cs:28-31`; `MonoGameGum/Input/Keyboard.cs:200-201,217` | ctor / `Keyboard` ctor | V |
| 7 | COLLIDE (Raylib) | `Raylib.GetCharPressed()` drains a global queue; the first service updated each frame eats the typed characters | `Runtimes/RaylibGum/Input/Keyboard.cs:250-256,281` | each service's keyboard | V |
| 8 | GUESS | `Mouse.SetCursor` is process-global and written on hover transitions of each service | `MonoGameGum/Input/Cursor.cs:50-66`, `FormsUtilities.cs:527-534` | each service's cursor | V |
| 9 | GUESS | `ThemePlatform.WireInMemoryFontCreator` assigns the static `InMemoryFontCreator` bound to the `GraphicsDevice` read from `SystemManagers.Default` at `Apply` time. Last theme applied wins | `Themes/Shared/ThemePlatform.cs:87` | each theme `Apply()` | V |
| 10 | GUESS | `ClipboardImplementation` has one static async slot (`_injectedClipboardTask`); `ContinueWith` without a scheduler uses `TaskScheduler.Current` | `MonoGameGum/Clipboard/ClipboardImplementation.cs:14-18,32-33`; `TextBoxBase.cs:1537` | any `TextBox` paste | V |
| 11 | GUESS | `EnableHotReload` called twice overwrites the manager without `Stop()`, leaking a `FileSystemWatcher` | `GumService.cs:410-415` | `EnableHotReload` | V |
| 12 | GUESS | `EvaluatedSyntax.enumTypeCache` caches negative lookups for the process, keyed by short enum name | `Runtimes/GumExpressions/EvaluatedSyntax.cs:1282-1318` | expression evaluation | V |
| 13 | GUESS | `GumContext.GetState<T>()` is keyed by type; `Sprite.InvalidTexture` uses `GetState<Texture2D>()`, so any second `Texture2D` stored there would collide | `GumContext.cs:98-112`, `Sprite.cs:36` | trap, not a bug today | V |

## 2. The two headline items

### 2.1 SynchronizationContext (item 1)

- `SingleThreadSynchronizationContext`'s **constructor** calls `SetSynchronizationContext(this)` (`GumCommon/HostServices/Async/SingleThreadSynchronizationContext.cs:23-26`). It saves nothing, so the previous context is lost.
- `UseSingleThreadedAsync()` guards only with the instance field (`GumService.cs:466-470`). Two services on one thread: last call wins. Same code in `GumServiceSkiaBase.cs:379-380` and `SokolGum/GumService.cs:155-156`.
- Each queue is drained only by its own service: `_syncContext?.Update()` in `UpdatePreamble` (`GumService.cs:841`), which runs inside `EnterDefault(_context)` (`GumService.XnaLike.cs:570`).
- Consequence A (wrong service): an `await dialogBox.ShowAsync(...)` in a handler of service A captures the thread's context. If B installed it last, the continuation runs in B's `Update`, with `IGumService.Default == B`, while mutating A's tree. Per-service facades (`ObjectFinder.Self`, `LoaderManager.Self`, `PopupRoot`, templates, `RelativeDirectory`, `Styling.ActiveStyle`) then resolve B. `DialogBox.EnsureVisualInRoot` adds to B's `Root` (`DialogBox.cs:236`). `CheckSameService` may throw for A-built elements.
- Consequence B (lost work): `Uninitialize` does not clear `_syncContext`, call `Clear()`, or restore the thread's previous context (`GumService.cs:~700-815`, no mention). After the service whose queue is installed uninitializes, or if that service is never `Update`d again, every later `await` continuation posted to the thread hangs. `Send` throws `NotImplementedException` (`:28-29`).
- Consequence C (single service, same root): `RunContinuationsAsynchronously` is used for `PlayAnimationAsync` (`AnimationController.cs:112`), so without a context the continuation goes to the thread pool and UI is mutated off-thread. That is why the context exists, and why a per-thread slot cannot be the answer with two services.
- No Gum code reads `SynchronizationContext.Current` or `TaskScheduler.Current`; the capture is the C# compiler's `await`.
- Direction (not a decision): make `Post` capture `IGumService.Default` and re-enter it with `EnterDefault` when draining, and make install/restore explicit instead of a constructor side effect. Open decision 1 in the audit (`ThreadStatic` vs `AsyncLocal`) cannot cover this, because the resume happens in a later frame, after the scope is gone.

### 2.2 `IGumService.Default` read at call time (item 2)

`DefaultScope` restores the previous default when `Update`/`Draw` return (`GumContext.cs:328-356`), so by the time an `await` resumes in a later frame the default is whatever it is then (null, which throws with two live services, or another service). The class doc on `GumContext` (lines 22-30) still says entry points "never put the previous value back"; the code does restore. Sites that read `IGumService.Default` instead of the owning service of the element:

- `FrameworkElementExt.cs:101-107`, `GraphicalUiElement.Hierarchy.cs:63-69` (`AddToRoot`)
- `DialogBox.cs:236` (Root), `:394`, `:688` (GameTime)
- `MenuItem.cs:244-246` (`DeferredQueue.Enqueue`: item drains in the other service's `Update`)
- `TextBox.cs:348,362,374`, `PasswordBox.cs:283` (clipboard), `TextBoxBase.cs:1522` (native input), `Image.cs:47`
- `FormsUtilities.cs:211,543`

Also: `IGumService.Default`, `GumContext.RegisteringShared` (`GumContext.cs:203`) and `_live` are plain statics, not `[ThreadStatic]`/`AsyncLocal`. The class doc says all UI work is on one thread. Two services on two threads (parallel test classes, a host with one thread per window) trample each other. GUESS, nothing enforces the rule.

## 3. Findings by requested category

### (1) BCL ambient state

- `SynchronizationContext`: item 1. `TaskScheduler.Current`: `ContinueWith` without a scheduler at `ClipboardImplementation.cs:33` and `TextBoxBase.cs:1537` (item 10). `TaskScheduler.Default` is explicit at `AnimationController.cs:141` (FINE).
- `Thread.CurrentThread.ManagedThreadId` stored per `SystemManagers` instance (`SystemManagers.cs:41,136,253`), no reader besides the property: FINE.
- `[ThreadStatic]`: `GraphicalUiElement.cs:269` (`IsAllLayoutSuspended`), `GraphicalUiElement.SetValuesAndStates.cs:372,448,451`, `GraphicalUiElement.UpdateLayout.cs:99,103`, `VariableReferenceGraph.cs:99`, `IWrappedText.cs:76`. Counters and scratch. Services on one thread share them, which is correct for nested counters. Stuck-flag bug is audit E9. FINE.
- `AppDomain.CurrentDomain.GetAssemblies()` read-only: `GumService.XnaLike.cs:348`, `EvaluatedSyntax.cs:1300`. FINE.
- Not found anywhere in the runtimes: `ThreadPool` settings, thread culture/name/priority, `CultureInfo.DefaultThreadCurrent*`, `AppDomain.UnhandledException`/`ProcessExit`/`AssemblyResolve`, `AssemblyLoadContext`, `AppContext.SetSwitch`, `Trace`/`Debug` listeners, `Console.Set*`, `AsyncLocal`, `ThreadLocal`, GC callbacks, timers. (`AppContext.BaseDirectory` reads only.)
- Write-only sinks: `Console.Error` (`CustomSetPropertyOnRenderable.cs:223`), `Console.WriteLine` (`Renderer.cs:2289`, `DeferredActionQueue.cs:50`), `Debug.WriteLine`. FINE.

### (2) Static events and delegates a service subscribes to

- `GumService` ctor: `CustomSetPropertyOnRenderable.LocalizationServiceChanged += _languageChangeSubscription.Track` (`GumService.cs:517`), never removed. Every `new GumService()`, including the lazy `GumService.Default` getter (`XnaLike.cs:44`), is rooted for the process lifetime. `Track` also hooks `ILocalizationService.CurrentLanguageChanged` (`LanguageChangeSubscription.cs:28-31`), never unhooked on `Uninitialize`, so a dead service keeps refreshing (item 6). With two live services both refresh on a language change, which is intended, but see item 5 for the missing scope.
- `Game.Disposed`: per service, removed in `DetachGameDisposedHandler` (`XnaLike.cs:216-222,524`). FINE.
- `Window.TextInput` (`Keyboard.cs:217`) and FNA `TextInputEXT.TextInput` plus `StartTextInput()` (`:200-201`): subscribed in the `Keyboard` ctor, no unsubscribe or `StopTextInput`. Two services on one `Game`: both keyboards buffer every typed character, so two focused `TextBox`es (one per service) both receive it. After `Uninitialize` the `Keyboard` stays rooted by the window/static event. `Window.ClientSizeChanged` is not used; window fit polls (`PollWindowSizeAndApplyFit`). FINE for size, COLLIDE for text.
- Hot reload: `FileSystemWatcher` is per `GumHotReloadManager` instance (`GumHotReloadManager.cs:74,133-141`), debounced and applied from the owning service's `Update`. FINE, except item 11. `Start` reads `FileManager.RelativeDirectory` through the facade (`:128`), so call it while the right service is Default (GUESS).
- `AposShapeRuntime.RegisterRuntimeTypes` does `CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable +=` and wraps `StandardElementsManager.Self.CustomGetDefaultState` (`AposShapeRuntime.cs:168-172`) once per `_registered` reset; `UninitializeRuntimeTypes` resets it (`:190-193`). Teardown is now last-service only (`GumService.XnaLike.cs:491-495`), so duplicates arise only across full single-service Init/Uninit/Init cycles. Audit S2 already has this.
- Other static delegates (`GraphicalUiElement.cs:344-388`, `Lifecycle.cs:78-188`, `ElementSaveExtensions.GumRuntime.cs:41,51,430,551,1584`, `Text`/`Renderer` hooks): see section 6 for the ones the audit did not list.

### (3) MonoGame / XNA / KNI / FNA / Raylib process singletons

- `Mouse.GetState`, `Keyboard.GetState`, `GamePad.GetState`, `TouchPanel.GetState`: read-only snapshots, FINE (`Cursor.MonoGame.cs:32,45`, `Keyboard.cs:749`, `GamePadDriver.cs:33,35`).
- `Mouse.SetCursor`: process-global write (`Cursor.cs:50-66`). Edge-triggered by hover change (`FormsUtilities.cs:525-534`), so two services only fight when their hovered elements overlap on screen. GUESS.
- `Mouse.WindowHandle`, `Game.Exiting`, `GraphicsDeviceManager`, `Game.Services` (Android keyboard only, `Keyboard.Android.cs:67,91`): not touched on desktop. FINE.
- `KeyboardInput.Show` (`MonoGameNativeTextInput.cs:32`) is a process-modal dialog; the in-progress flag is per `TextBox` (`TextBoxBase.cs:1522-1545`). Two services each showing one at once is unguarded. GUESS, mobile only.
- `GraphicsDevice`: one `Game` means one device, so the device-bound statics (`CustomEffectManager` `mContentManager`/`Effect`, `Renderer.cs:2248-2266,459`; `ShapeRenderer.Self`) work. Audit R6/S1/R8 already say this.
- `ContentLoader.XnaContentManager = game.Content` (`XnaLike.cs:~262`): two services on one `Game` share its `ContentManager` and its cached `Texture2D`s (`ContentLoader.cs:102-104` does not dispose those). FINE.
- Clipboard: `GumService.Clipboard` is per instance but the implementation is static (`ClipboardImplementation.cs:14-18`) plus the OS clipboard. Item 10.

### (4) Global registrations

- `[ModuleInitializer]`: core `DefaultFilledRectangleRenderable.cs:29`, `DefaultStrokedRectangleRenderable.cs:27`, `DefaultStrokedCircleRenderable.cs:38`, `AposShapeRuntime.cs:63`, and codegen (`CodeGenerator.cs:1859,1956,3877`). They run before any service, so they write the Base layer / `RenderableRegistry`. FINE for shared defaults. For generated project code this is item 4: two projects whose generated classes share an element name end up with one Base entry, last registered wins for both services.
- Reflection scan: `RegisterRuntimeTypesThroughReflection` runs on every `Initialize` inside `RegisteringShared = true` (`XnaLike.cs:235-242,323,329`). Legacy entry-assembly `RegisterRuntimeType` (singular) has no idempotence guard; each call re-stamps Base (`ElementTypeRegistry.cs:78-105` picks the newest stamp across context layer and Base). A later service's `Initialize` therefore re-stamps Base entries and can shadow an earlier service's own-layer registration for the same name (for example a user override made after init). I; needs a test.
- Teardown: `UninitializeRuntimeTypesThroughReflection`, `RenderableRegistry.Reset`, `Text.Customizations.Clear`, `ElementTypeRegistry.Base.Clear` run only when no other service is live (`XnaLike.cs:491-500`, `ElementSaveExtensions.GumRuntime.cs:132-143`). This fixes audit T5/E6. Side effect: after the last service leaves, `[ModuleInitializer]` and generated registrations are gone and only the reflection hooks bring them back (known gap #2761, `XnaLike.cs:~488`).
- Static constructors with side effects outside their own type: only `GumContext` (`GumContext.cs:52-56`) installs `FileManager.RelativeDirectoryReader/Writer`, never removed. FINE. `DialogBox.cs:148`, `VisualTemplate.cs:23`, `NineSlice.cs:1240`, `NineSliceExtensions.cs:26`, `Text.cs:888`, `FileManager.cs:148`, `BlendState.cs:142` only fill constants.

### (5) Third-party static singletons

- KernSmith: `BmFont.RegisterFont` is a process-wide registry (`Integrations/KernSmith/KernSmith.MonoGameGum/KernSmithFontCreator.cs:62`). Each theme registers distinct family names (`HazardTheme.cs:125`, `BubblegumTheme.cs:86-92`: "Nunito", "Nunito Icons", Saira etc.), so different themes do not collide; the same theme in two services is an idempotent re-register. Gum never calls `ClearRegisteredFonts`/`UnregisterFont`. FINE. The creator itself is per `Apply` and holds one `GraphicsDevice` (item 9). Growth state is per creator instance (`:36,41`).
- Apos.Shapes: `ShapeRenderer.Self` single `ShapeBatch` per process; `Initialize()` reads `Gum.GumService.Default` (`ShapeRenderer.cs:250`), which is the concrete static `_default` and can lazily create a new uninitialized service (audit S1/L1).
- FontStashSharp, StbImage, HttpClient: no usages in `MonoGameGum`, `RenderingLibrary`, `GumRuntime`, `GumCommon`, `Themes` (the SkiaSharp hits in those folders are Skia-build `using` aliases and doc comments; the StbTrueType reference is only the reflection lookup in `ThemePlatform`).
- `ThemePlatform.ForceStaticConstructor` (`ThemePlatform.cs:31-32`) runs `RunClassConstructor` on KernSmith's StbTrueType backend, once per process. FINE.

### (6) Caches two services could poison

- `EvaluatedSyntax.enumTypeCache` (item 12), `TypeResolver` static (`:1280`).
- `SystemManagers.StreamByteDictionary` (`SystemManagers.cs:155`, hook `:181-203`, mobile/browser only): public static, path-keyed, unsynchronized; the byte arrays are the host's. FINE (GUESS if two services load different bundles with the same relative name).
- `GraphicalUiElementPropertyReadExtensions._propertyCache` (`:244`): `ConcurrentDictionary<(Type,string),PropertyInfo>`, pure reflection. FINE.
- `RadioButton.RadioButtonDictionary` (`RadioButton.cs:21`) is keyed by parent object; `ToolTipService._tooltipsByHost` (`ToolTipService.cs:42`) and `CustomSetPropertyOnRenderable._localizationKeys` (`:145`) are `ConditionalWeakTable`s keyed by element. FINE.
- `BitmapFont._reportedPageLoadFailures` (`BitmapFont.cs:288`): static log dedupe. FINE.
- `Renderer.SiblingOrdering` (`Renderer.cs:52`), `HierarchicalOrderer.Instance`, `BatchKeyGroupedOrderer.Instance`: no instance fields, so stateless singletons. The choice is process-wide (config). FINE.
- `GumContext._builtBy` (`GumContext.cs:207`): CWT keyed by element. FINE.

## 4. Static event / delegate sweep: rows the audit inventory did not list

Grep for `static` events, delegates, lists, dictionaries in `MonoGameGum`, `GumCommon`, `RenderingLibrary`, `GumRuntime`, `GumShapes`, `GumExpressions`, `Themes`, `Gum/Wireframe`, `ToolsUtilities`. Not in audit rows L/R/C/E/F/T/S/X:

| Member | Where | Verdict |
|---|---|---|
| `ElementSaveExtensions.VariableChangedThroughReference` | `ElementSaveExtensions.GumRuntime.cs:551` | FINE (host hook; writer not traced, I) |
| `VariableSaveExtensionMethods.CustomFixEnumerations` | `Gum/DataTypes/VariableSaveExtensionMethods.cs:232` | FINE (project load, host hook) |
| `EvaluatedSyntax.TypeResolver`, `enumTypeCache` | `EvaluatedSyntax.cs:1280,1282` | GUESS, item 12 |
| `FileManager.RelativeDirectoryReader/Writer` | `FileManager.cs:91,97` | FINE, installed by `GumContext` static ctor |
| `ElementTypeRegistry.Base` and `RegisteringShared` | `ElementTypeRegistry.cs:21`, `GumContext.cs:203` | see items 2, 4 |
| `SystemManagers.StreamByteDictionary` | `SystemManagers.cs:155` | FINE |
| `CustomSetPropertyOnRenderable` font/tag stacks (`fontSizeStack`...`allTags`) | `Gum/Wireframe/CustomSetPropertyOnRenderable.cs:1532-1542,1640` | S (scratch, not reentrant, audit T7 lists only part) |
| `StateBbCodeAllowlist`, `BbCodeParser.KnownTags` | `:1649`, `BbCodeParser.cs:66` | FINE (config) |
| `GraphicalUiElement` list scratch `listOfListsForReducingAllocInInterpolation` | `GraphicalUiElement.SetValuesAndStates.cs:104` | S |
| `Renderer.LockObject` | `Renderer.cs:121` | FINE |
| `Binding.DefaultUpdateTriggers` | `MonoGameGum/Forms/Data/Binding.cs:147` | FINE (config) |
| `FrameworkElement` key-combo lists `TabKeyCombos` etc. | `FrameworkElement.cs:1369-1418` | C (audit F14) |
| `Silk GamePadDriver._forceUpdatedGamepads` | `SilkNetGum/Input/GamePadDriver.cs:40` | FINE (keyed by instance, unsynchronized) |
| `ThemePlatform` test seams | `ThemePlatform.cs:28-32` | FINE |
| `AposShapeRuntime.IsSvgRuntimeEnabled` | `AposShapeRuntime.cs:49` | FINE (config) |

## 5. Inventory rows I think are wrong or stale

- **L7** says `_syncContext` is "instance-owned and correct". The field is per instance, but what it installs is thread-level (item 1). `Clipboard` and `NativeTextInput` are likewise instance-owned wrappers over a static implementation (item 10).
- **E5** and **L4** mark the hook delegates **G**. True for the lambdas, but `SaveFormsRuntimePropertiesAction` / `UpdateFormsStateAction` are installed in the MonoGame ctor (`GumService.cs:542`) and nulled by the last service's `Uninitialize` (`:793-794`). Item 3. Skia installs in `Initialize`, so it is not affected.
- **E4** says the notifier is cleared by `UnregisterFromFileFormRuntimeDefaults`. That method has no callers (`FormsUtilities.cs:597` is the only match), so `Uninitialize` never clears it and it stays set after teardown. Harmless (capture-free) but stale.
- **L9** says hook registration is "G, idempotent by contract". The singular entry-assembly hook has no guard, and re-running hooks re-stamps Base (item 4).
- **T5** (H) and **E6** (H) are already fixed on this branch by the last-service guard (`XnaLike.cs:491-500`).
- **T2 / F7** call `FontService` / `InMemoryFontCreator` global. That holds for one `GraphicsDevice` only; `ThemePlatform.cs:87` binds the creator to the device at `Apply` time (item 9).
- `GumContext` class doc (lines 22-30) says entry points never restore the previous default; the code restores it (`GumContext.cs:328-356`).

## 6. Related items already in the audit

- `GumService.cs:441` (`UseGamepadDefaults`) and `ShapeRenderer.cs:250` read the concrete `GumService.Default`, which creates an uninitialized service on first read (audit L1/L6/S1; the fix branch `6066-fix-default-hardwire` exists).
- `Sprite.InvalidTexture`, `Text.DefaultFont`, `Text.DefaultBitmapFont`, `Styling.ActiveStyle` are per-service through `GetState<T>()` keyed by type (item 13).

## 7. Other runtimes (brief)

- **Skia / Silk / WPF / MAUI** (`GumServiceSkiaBase`): same `SynchronizationContext` bug (`GumServiceSkiaBase.cs:358-380`, `:615`). `Topten.RichTextKit.FontMapper.Default` is a process-wide assignment done once behind `HasInitializedGlobal` (`Runtimes/SkiaGum/RenderingLibrary/SystemManagers.cs:79-120`); `GumFontMapper` and `SkiaResourceManager` hold static path-keyed caches (`GumFontMapper.cs:27,37`, `SkiaResourceManager.cs:26,76,109,222`). Skia `SystemManagers.Default = new SystemManagers()` unconditionally (`GumServiceSkiaBase.cs:411`). Silk subscribes `Scroll`/`KeyChar` per window device (`Cursor.Silk.cs:32`, `Keyboard.Silk.cs:24`), never removed. Silk's `GamePadDriver` static set is FINE.
- **Raylib:** one OS window and GL context per process. `GetCharPressed` drain (item 7). `Raylib.SetMouseCursor` global (`Cursor.cs`). `GumService.Raylib.cs:118` assigns `IGumService.Default` without a `GumContext` (no context yet, per the issue). `BatchDrawCallCounter` swaps the active `Rlgl` batch around each pass and restores it (`BatchDrawCallCounter.cs:93,291`), FINE sequentially.
- **Sokol:** same `UseSingleThreadedAsync` (`SokolGum/GumService.cs:155-156,358`). Not in any solution, unverified.
- **FNA:** same source as MonoGame/KNI. The only FNA-specific ambient state found is the static `TextInputEXT` event (item 6).

## 8. Limits

Read-only. Nothing here was run, and the three **I** items (4, 5, and the stale-notifier claim in section 5) were read, not traced to an observed failure. A test for each of items 1, 3 and 4 is cheap (two services, one thread; construct both, uninitialize one; initialize both with an overriding registration). The scan looked in the runtimes and optional packages, not in `Tool/`, `Tools/`, samples, or test projects.
