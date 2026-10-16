# Multiple GumService instances: state audit and refined design (#6066)

- **Date:** 2026-10-15
- **Base:** spike branch `6066-multi-service-spike` @ `c95eaee9c` (spike tests + `MultiGumServiceStaticSnapshot.cs`)
- **Status:** read-only audit. No production code was changed or run. Nothing here is a decision; open decisions are in section 8.

Evidence tags used throughout:

| Tag | Meaning |
|---|---|
| **V** | Read at the cited `file:line` in this checkout. |
| **I** | Inferred. The reason is given; not traced end to end. |
| **T** | Reported by the spike tests / issue comments. Not rerun for this audit. |

Verdict key:

| Verdict | Meaning |
|---|---|
| **P** | Per-service. Must live in the service's context. |
| **G** | Global. Shared by every service on one backend. Setup is idempotent; teardown runs only when the last service leaves. |
| **C** | Config or constant. Safe as a global. |
| **S** | Scratch buffer. Safe as a global, but not re-entrant and not thread-safe. |
| **H** | Bug or hazard today. Needs a code fix whichever design is chosen. |

Paths are repo-relative. `Gum/...` files are source-linked into MonoGameGum/KniGum/FnaGum (`MonoGameGum/MonoGameGum.csproj:215-263`).

---

## 1. Summary

Five findings that matter most:

1. **Two bugs exist with one service and need no design.** (a) `SetGraphicalUiElement` sets the thread-wide `GraphicalUiElement.IsAllLayoutSuspended = true` and clears it only on the success path, with no `try/finally` (`GumRuntime/ElementSaveExtensions.GumRuntime.cs:1478-1530`). An exception inside `ToGraphicalUiElement` leaves it stuck, so later layout and font work silently no-ops. This is the likely cause of the M0 "blank draws after an exception" item (read at the cited lines, **not reproduced**, which matches the issue's own guess); the same unprotected pattern is at `ComboBox.cs:388-397`, `FrameworkElementExt.cs:114-123`, `ItemsControl.cs:83-100`, `GraphicalUiElement.Lifecycle.cs:97-116`. (b) `Renderer` ignores its own `SystemManagers` in six places and `GumService.Draw()` draws `SystemManagers.Default` instead of its own (R2, L5).
2. **The element-type table captures `SystemManagers` in lambdas.** `RegisterComponentRuntimeInstantiations` registers `() => new TextRuntime(systemManagers: this)` (also Rectangle, Polygon) into a process-wide dictionary (`RenderingLibrary/SystemManagers.cs:482-494`). After service B initializes, project-built Text/Rectangle/Polygon of service A are created with B's managers. Not in the issue, not in the spike's lists. It forces the registration table to be per-context, with a global base layer for `[ModuleInitializer]` registrations (E1).
3. **Several per-service statics are in neither the spike's 27 slots nor its six "uncaptured" items.** Highest impact: `SystemManagers.Initialize` resets `FileManager.RelativeDirectory = "Content/"` on every call, even for a code-only service (`SystemManagers.cs:307`), so a code-only editor service silently breaks the game's lazy relative asset loads. Others: `Renderer.TextureFilter` is a static written from each project (C6), one shared `LocalizationService` receives both projects' strings (T1), `ElementSaveExtensions.CustomEvaluateExpression` is nulled by either `Uninitialize` (E3), `CustomEffectManager` and `PixelDataTextureApplier.Pool` are device-bound statics (R6, R8). Full list in section 3.
4. **Hard-wired `GumService.Default` calls break any non-default service.** `ShapeRenderer.Self.Initialize()` (`Runtimes/GumShapes/Renderables/ShapeRenderer.cs:248-262`), which every shape theme's `Apply()` reaches through `ThemeShapePlatform.InitializeShapeRenderer` (`Themes/Shared/ThemeShapePlatform.cs:23-31`), and `UseGamepadDefaults` (`MonoGameGum/GumService.cs:368-371`). The spike dodged this: it called `ShapeRenderer.Self.Initialize(GraphicsDevice, Content)` directly and loaded the themes as `.gumx` projects, so **code-only `Theme.Apply()` was never exercised** (T, `MultiGumServiceThemesSpikeTests.cs:656`).
5. **Refined design: the context owns the fields; statics become facades; `Initialize` and `Uninitialize` run inside the service's own activation.** The spike's snapshot is a hand-kept allowlist, the exact shape CLAUDE.md warns about. If a static no longer exists as a field, it cannot be forgotten. Running `Initialize` under a fresh context makes every existing static write inside it land in that context with few edits, which is why the issue's M1 ("`Initialize` stops clobbering") cannot be met without `ObjectFinder`, `RelativeDirectory`, registrations and `Styling`/templates (the issue's M3/M4 items). Details in section 4.

Changes to the issue's plan (section 5): insert a no-regret fix PR series before M1; fold the "written by `Initialize`" items of M3/M4 into M1; make the ambient pointer thread-static with a process "home" context that the first service adopts; add a static-inventory guard test; scope v1 to one `GraphicsDevice`.

---

## 2. Inventory

Counts are from `rg` over the runtime-compiled source (tool and tests excluded): about 55 code sites read or write `SystemManagers.Default`/`ISystemManagers.Default` in MonoGameGum, RenderingLibrary, GumRuntime and GumCommon; 87 sites use `ObjectFinder.Self` (MonoGameGum 10, GumRuntime 18, GumCommon 20, Runtimes 5, `Gum/DataTypes` 34); 58 use `FileManager.RelativeDirectory`. The issue's "about 15 runtime files" for `ObjectFinder.Self` undercounts: `Gum/DataTypes/*ExtensionMethods.cs` (shared with the tool) is the largest single reader.

### 2.1 Service lifecycle: ctor, Initialize, Uninitialize, Update, Draw (`MonoGameGum/GumService*.cs`)

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| L1 | `GumService._default` (`GumService.cs:55`); `Default` getter lazily news one (`GumService.XnaLike.cs:44`) | `Uninitialize` sets `_default = null` **unconditionally** (`GumService.cs:688`) | `GumService.Default` everywhere | **H**: a non-default service's `Uninitialize` orphans the real default. Guard with `ReferenceEquals(_default, this)`. | V |
| L2 | `IGumService.Default` (`RenderingLibrary/IGumService.cs`, public get/set) | `InitializeInternal` always (`XnaLike.cs:190`, even when the caller passes its own managers); `InitializeForTesting` (`GumService.cs:487`); `Uninitialize` nulls it unconditionally (`GumService.cs:678`) | `FormsUtilities.cs:191,523`; `FrameworkElementExt.cs:101-107`; `GraphicalUiElement.Hierarchy.cs:63-69`; `TextBoxBase.cs:1522`; `TextBox.cs:348-374`; `PasswordBox.cs:283`; `MenuItem.cs:244`; `Image.cs:47`; `DialogBox.cs:236,394,688` | **P** (active service); unconditional null is **H** | V |
| L3 | `SystemManagers.Default` / `ISystemManagers.Default` (`SystemManagers.cs:56`, `ISystemManagers.cs:16`) | `XnaLike.cs:186-187` only when no managers were passed; nulled `GumService.cs:676-677` | ~55 sites; the non-trivial ones are R1-R5, F6 (the `Styling` ctor), F8, F9 | **P** (active service's managers) | V |
| L4 | Constructor touches statics: `CustomSetPropertyOnRenderable.LocalizationServiceChanged += ...` (never unsubscribed), `GraphicalUiElement.RefreshLocalizationOnElementAction = ...`, `FormsRefreshStylesHooks.Install()` (`GumService.cs:446,450,471`) | `new GumService()` | every localization change fires every constructed service's `RefreshLocalization`, which reads the ambient `PopupRoot`/`ModalRoot` (`GumService.cs:310-315`) | **H**: constructing a service has process-wide effects; the lambdas capture nothing, so the assignments themselves are **G** | V |
| L5 | `GumService.Draw()` | calls `SystemManagers.Default.Draw()` (`GumService.cs:750`) | host game | **H**: should call its own `SystemManagers.Draw()` (`SystemManagers.cs:438-441`, already per-instance) | V |
| L6 | `UseGamepadDefaults` adds `GumService.Default.Gamepads` (`GumService.cs:370`); `UseKeyboardDefaults` reads `FormsUtilities.Keyboard` (`GumService.cs:361-366`) | host game | | **H** for the first (wrong service, and creates an uninitialized `Default` as a side effect); second is ambient | V |
| L7 | Instance-owned and correct: `Root` (`GumService.cs:225`), `DeferredQueue`, `Clipboard`, `NativeTextInput`, `_syncContext`, `_windowFit`, `_hotReloadManager`, `_systemManagers`, `GameTime`, `LastLoadResult`, `CurrentProjectResolution` | | controls reach `DeferredQueue`/`Clipboard`/`GameTime`/`Root` **through `IGumService.Default`** (see L2) | **P**, already per-instance; the lookups are ambient | V |
| L8 | Forwarders that read statics: `Cursor` `:70`, `Keyboard` `:81`, `LocalizationService` `:86`, `Gamepads` `:91`, `CanvasWidth/Height` `:125-139`, `PopupRoot/ModalRoot` `:250-252`, `ContentLoader` (`XnaLike.cs:89`) | | user code (`GumService.Default.Cursor` etc.) | **P** through the facades: these keep working and mean "this service's" when called under its scope, "active" otherwise | V |
| L9 | `Initialize` runs `RegisterRuntimeTypesThroughReflection`: reflection scan of the entry assembly and every loaded assembly for static `RegisterRuntimeType(s)` hooks (`XnaLike.cs:181,229-245,261-284`) | each `Initialize` | the hooks write global tables (E1, S2) | **G**: idempotent by contract; scan cost is paid per service | V |
| L10 | `Uninitialize` global teardown: `ElementSaveExtensions.ClearRegistrations`, delegate nulling, `LoaderManager.DisposeAndClear`, `RenderableRegistry.Reset`, `UninitializeRuntimeTypes` reflection hook, `Text.Customizations.Clear`, `Sprite.InvalidTexture.Dispose`, `Styling.ActiveStyle = null` (`GumService.cs:629-689`, `XnaLike.cs:394-434`, `GumServiceTeardown.cs:17-48`, `FormsUtilities.cs:557-570`) | either service | everyone | **H**: all of it assumes one service. Reproduces the spike's red tests (`ShapeRenderer is null`, `CustomCreateGraphicalComponentFunc must be set`) | V, T |
| L11 | `Update` order: `PollWindowSizeAndApplyFit` → `SetDimensionsToCanvas(Root)` → `UpdatePreamble` → `FormsUtilities.Update` → `_systemManagers?.Activity` → `AnimateRoots` (`XnaLike.cs:452-487`, `GumService.cs:708-744`) | host game | writes/reads F-rows and R-rows below | the activation boundary | V |

### 2.2 Rendering and managers (`RenderingLibrary/**`, `GumRuntime`)

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| R1 | `Renderer.Self` => `SystemManagers.Default.Renderer`, throws if null (`Renderer.cs:220-237`) | derived | `ContentLoader.cs:158,235`, `FilledStrokedRectangle.cs:235`, `Line.cs:139,199`, `LinePrimitive.cs:288`, `LineRectangle.cs:165,295`, `LinePolygon.cs:261`, `LineCircle.cs:195`, `LineGrid.cs:257`, `Sprite.cs:837`, `Text.cs:770`, `SpriteManager.cs:46`, `TextManager.cs:38`, `ImageData.cs:564` | **P** via L3. Most are fallbacks when no managers were passed; the renderables carry `Text.mManagers` etc. (`Text.cs:290,903`) | V |
| R2 | `Renderer` instance reads `SystemManagers.Default` instead of its own managers: `Draw(managers)` null fallback `:580`, immediate `Draw` `:799`, `End` `:832` and `:852`, `PreRender` render-target bake `:908`, `GumBatch` ctor `:1828`. `Renderer.Initialize(graphicsDevice, managers)` (`:374`) never stores `managers` | n/a | any draw that uses immediate mode, deferred mode or render-target baking | **H**: `serviceB.SystemManagers.Draw()` bakes B's render targets through A's managers unless A is active. The spike's hand swap hides this | V |
| R3 | `SystemManagers` (Renderer, Camera, Layers, SpriteManager, ShapeManager, TextManager) | `GumService.InitializeInternal` (`XnaLike.cs:183`) | instance | **P** already per-instance | V |
| R4 | `GraphicalUiElement.CanvasWidth/Height` (auto properties, `GraphicalUiElement.Properties.cs:227,237`) | `SystemManagers.Initialize` (`:253-254`), `WindowFit` (`GumService.cs:146-152`), `GumService` setters (`:125-139`), `GumService.Uninitialize` zeroes (`:671-672`), Skia `GumServiceSkiaBase.cs:416-417`, Skia WPF `GumSKElement.cs:116` | `GetParentDimensions` (`GraphicalUiElement.RequiredSize.cs:219-220`), screen IPSO size (`Properties.cs:311,330`), `FormsUtilities.SetDimensionsToCanvas` (`:553-554`), `InteractiveGue.cs:1336-1337`, `CreateFullscreenContainer` (`FormsUtilities.cs:223-224`) | **P**. Reads happen for parentless elements only, so a per-context read is not on the per-element hot path (I; benchmark in section 7) | V |
| R5 | `Cursor.XRespectingGumZoomAndBounds/YRespecting...` and viewport offset read `SystemManagers.Default.Renderer` (`Input/Cursor.cs:184,202`, `Cursor.MonoGame.cs:55,58`, `CursorExtensions.cs:288`); `ScrollViewer.cs:552,558`, `TextBoxBase.cs:656` read `ISystemManagers.Default...Camera.Zoom`; `ListBox.cs:1719` falls back to it | n/a | per input event | **P** via L3; M1 "climb to `EffectiveManagers`" is right for the control sites, but `Cursor` has no element to climb from, so it needs a per-cursor managers reference or the ambient (see F1) | V |
| R6 | `Renderer.CustomEffectManager` is `static` (`Renderer.cs:329`) with its own `static ContentManager mContentManager` (`:2205`) and loaded `Effect`; `Renderer.Uninitialize` calls `CustomEffectManager.Reset()` (`:424`, defined `:2261`) which nulls the content manager (`:2358`) | `Renderer.Initialize` (`CustomEffectManager.Initialize(graphicsDevice)`) | every effect draw | **H**: device-bound global; one service's `Uninitialize` kills the other's custom shader. With one device it is **G** (refcount) | V |
| R7 | Static `Renderer` knobs: `TextureFilter` (`:350`), `LinearizeTextures` (`:340`), `ApplyCameraZoomOnWorldTranslation` (`:348`, set true by `SystemManagers.cs:288`), `BleedTransparentTextureEdgesOnLoad` (`:360`), `RenderUsingHierarchy` (`:43`), `SiblingOrdering` (`:52`), `CullOffscreenWhenClipped` (`:65` forwards to `CameraScissorExtensions`), `LockObject` (`:121`), `RendererSettings.Use*EffectRendering` (`RendererSettings.cs:11-13`) | host config, except `TextureFilter` (see C6) | `SetFilteringForLayer` (`:570`) per draw pass | **C**, except `TextureFilter` which is per project (C6) | V |
| R8 | `PixelDataTextureApplier.Pool` static; textures made on `SystemManagers.Default?.Renderer?.GraphicsDevice` (`SystemManagers.cs:522,535,555`) | `GraphicalUiElement.ApplyPooledTextureFromPixelData` (set `SystemManagers.cs:274-275`) | color-picker visuals | **G** for one device; **H** across devices | V |
| R9 | `Sprite.InvalidTexture` (`Sprite.cs:33-38`); `Text.DefaultFont`, `Text.DefaultBitmapFont` (`Text.cs:98-111`) | `SystemManagers.Initialize` sets `DefaultBitmapFont` from a freshly loaded embedded font (`:264`); `UninitializePlatform` disposes/nulls all three (`XnaLike.cs:414-421`) | `Text` fallback when a font is null; `Sprite` missing-texture draw | **P**: GPU resources created per `Initialize` | V |
| R10 | `Text.RenderBoundaryDefault` set false at init (`SystemManagers.cs:300`); `SystemManagers.GlobalFontScale` (`:118-125`) forwards to `GraphicalUiElement.GlobalFontScale` (`GraphicalUiElement.cs:128`); `Camera.PixelPerfectOffsetX/Y` (`Camera.cs:115-116`) written with the same value by every `Renderer.Initialize` (`Renderer.cs:386-390`) | | | **C** (identical values) | V |
| R11 | Draw leaves no render target bound (T, issue comment). A host compositing services into one target must rebind per service | n/a | | constraint, document | T |

### 2.3 Project, assets, content (`ObjectFinder`, `FileManager`, `LoaderManager`)

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| C1 | `ObjectFinder.Self { get; private set; } = new()` (`Gum/Managers/ObjectFinder.cs:93`); instance holds `GumProjectSave` (`:98`), `cachedDictionary` (`:85`), `_fallbackStandardElements` (`:91`), cache refcount (`:108`) | `GumProjectSave =` at `GumService.cs:556`, `GumServiceTeardown.cs:45`, `GumHotReloadManager.cs:287`, Skia `:484`, Sokol `:278,308` | 87 sites in runtime-compiled source; the tool has ~476 more and DI-binds the same instance (`refactoring-direction` skill). Includes element building (`ElementSaveExtensions.GumRuntime.cs:234,349,415,428,460,1496`), `VariableReferenceGraph.cs:202,261`, `CustomSetPropertyOnRenderable.cs:1599,1632,2203,2366,2405`, `FormsUtilities.cs:605,621,774,809` | **P**. It is already an instance class, so isolating it means swapping which instance `Self` returns, not rewriting call sites. `private set` is the only blocker | V |
| C2 | `FileManager.RelativeDirectory` (`ToolsUtilities/FileManager.cs:78-103`) | `SystemManagers.Initialize` resets to `"Content/"` on **every** call (XNA-like only, `SystemManagers.cs:307`); `FinishInitialize` sets the project dir (`GumService.cs:580`); `Uninitialize` resets (`:682`) | 58 sites: `ContentLoader.cs:147,203`, `BitmapFont.cs:253`, `NineSlice.cs:1251`, `AnimationChainList.cs:218`, `GraphicalUiElement.Text.cs`, `CustomSetPropertyOnRenderable.cs:467,686,3275`, `GumDataTypes/ElementReference.cs`, `GumHotReloadManager.cs:128` | **P**, and **H** today: a second service's `Initialize` resets it even with no project. Lives in `ToolsUtilities`, below GumCommon, so a facade onto the context needs a hook (section 4.3) | V |
| C3 | `FileManager.CustomGetStreamFromFile` (`FileManager.cs:121`) | mobile/browser default if null (`SystemManagers.cs:147-155`); each bundle load composes a new hook over the previous (`GumBundleLoader.cs:131-135`); only Skia restores it on teardown (`GumServiceSkiaBase.cs:507-516`) | `FileManager.GetStreamForFile/FileExists` | **G**. Composition by project root makes two bundles work (I: each hook tests `TryMakeRelative(path, projectRoot)`). **H**: MonoGame/KNI/FNA never restore it, so hooks stack across `Initialize` cycles (no `CustomGetStreamFromFile =` in `GumService*.cs`) | V |
| C4 | `LoaderManager.Self` singleton, `ContentLoader`, cache `mCachedDisposables` (`GumCommon/Content/LoaderManager.cs:44-60,75-77`) | `ContentLoader = new ContentLoader()` per `Initialize` (`SystemManagers.cs:260`); `XnaContentManager` set once (`XnaLike.cs:194-197`); `DisposeAndClear` (`LoaderManager.cs:207-215`) from `GumServiceTeardown.cs:47` | every texture/font/shader load | **G** with refcounted disposal. Cache keys are `RelativeDirectory + path` (`ContentLoader.cs:139-152`), absolute once a project is loaded, so identical files share and different projects do not collide. With the default relative `"Content/"` two code-only services share keys, which is correct (same folder) | V |
| C5 | Embedded resources cached under `EmbeddedResource.*` with `ExistingContentBehavior.Replace` (`SystemManagers.cs:335-341,355-358`); `GetOrLoadEmbeddedTexture2d` returns the cached one (`:381-392`); `Styling` takes `UISpriteSheet.png` from it (`Styling.cs:75`, `FormsUtilities.cs:139`) | each `Initialize` re-adds the embedded fonts (replace does not dispose the old entry, `LoaderManager.cs:183-199`) | | **G**. The shared sprite sheet is disposed by either service's `DisposeAndClear` while the other still uses it (Styling holds it). Refcount `DisposeAndClear` | V |
| C6 | Texture filter: `Renderer.TextureFilter` static, read per pass (`Renderer.cs:350,570`) | project load `ApplyProjectTextureFilter` → `ApplyTextureFilterPlatform` (`GumService.cs:569,600-603`, `XnaLike.cs:353-356`); hot reload passes the same callback (`GumService.cs:342`) | all `Renderer` instances | **P** (per project). Not in the issue or the spike lists. Make it an instance property on `Renderer` (keep the static as the default/forwarder) | V |
| C7 | `ColoredRectangleRuntime.DefaultWidth/Height` statics (`ColoredRectangleRuntime.cs:34-35`) | project load, from the project's ColoredRectangle standard (`GumService.cs:605-612`) | new `ColoredRectangleRuntime` | **P** (obsolete type; low priority). `NineSliceRuntime.Default*` (`NineSliceRuntime.cs:59-64`) have no writer in `GumService`; the `NineSlice` lookup at `GumService.cs:613` is dead code (assigned, never read) | V |
| C8 | `ProjectLocalizationLoader.Load(project, CustomSetPropertyOnRenderable.LocalizationService, ...)` (`GumService.cs:548-554`) | project load | shared service | see T1 | V |
| C9 | `GumHotReloadManager` instance state is per service; reads `FileManager.RelativeDirectory` (`:128`), writes `ObjectFinder.Self.GumProjectSave` (`:287`), applies diff to `ISystemManagers.Default` (`:303`) | `Update` ticks it (`GumService.cs:713`) | | **P** reads/writes ambient, so correct if hot reload runs inside the service's `Update` scope, which it does (`UpdatePreamble`) | V |
| C10 | `VariableReferenceGraph`: `ConditionalWeakTable<ElementSave, Graph> _cache` (`GumCommon/Runtime/VariableReferenceGraph.cs:86`), `[ThreadStatic] _suppressionDepth` (`:99-100`); built via `ObjectFinder.Self.GetBaseElements` (`:202`) | `GetFor` | element building | **G**: keyed by `ElementSave` identity, so safe **provided the graph is built while the element's own project is active**; building under the wrong `ObjectFinder` caches a wrong graph permanently (I) | V |

### 2.4 Element building: registrations, hooks, delegates

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| E1 | `ElementSaveExtensions.mElementToGueTypes`, `mElementToGueTypeFuncs`, `TemplateFunc` (`ElementSaveExtensions.GumRuntime.cs:25-27`), keyed by element **name** | `SystemManagers.RegisterComponentRuntimeInstantiations` (8 standards; **closures capture `this`** at `:482,486,494`); `FormsUtilities.RegisterFromFileFormRuntimeDefaults` registers every Forms-behavior component by name, `overwriteIfAlreadyExists: false` (`FormsUtilities.cs:593-775`, public statics, so codegen and `[ModuleInitializer]` code write here too); `AposShapeRuntime.RegisterRuntimeTypes` (`:119-171`); `ClearRegistrations` (`:131-139`) | `CreateGueForElement` → `TryCreateStrongTypeForElement` (`:158-247`) | **P** (project-derived entries and the capturing lambdas) over a **G** base layer (module-initializer / reflection-hook registrations, which run before any service exists) | V |
| E2 | `ElementSaveExtensions.CustomCreateGraphicalComponentFunc` (`:387`) | `SystemManagers.Initialize` (`SystemManagers.cs:293`), nulled by `ClearRegistrations` (`:136`) | `CreateGraphicalComponent` (`:389-396`), throws if null | **G**; **H**: teardown nulls it. Spike T: `B.Uninitialize()` breaks A's next build | V, T |
| E3 | `CustomEvaluateExpression`, `CustomEvaluateExpressionAllBranches` (`:36,46`) | `GumExpressionService.Initialize` (per the XML doc); nulled by `ClearRegistrations` (`:137-138`) | variable-reference evaluation | **G**; **H**: either service's `Uninitialize` disables expressions for the other. Writer not traced (I) | V |
| E4 | `ElementSaveExtensions.InitialStateAppliedNotifier` (field, `:1541`) | `RegisterFromFileFormRuntimeDefaults` (`FormsUtilities.cs:595-602`); cleared by `UnregisterFromFileFormRuntimeDefaults` (`:581`) | end of every element build (`:1519-1520`) | **G**: the lambda captures nothing | V |
| E5 | `GraphicalUiElement` hook delegates: `SetPropertyOnRenderable` (default is reflection, `GraphicalUiElement.cs:367`), `UpdateFontFromProperties`, `ThrowExceptionsForMissingFiles`, `RemoveRenderableFromManagers`, `AddRenderableToManagers`, `ApplyMarkup`, `ApplyCachedTextureFromPixelData`, `ApplyPooledTextureFromPixelData`, `CloneRenderableFunction` (`:344-372`); `SaveFormsRuntimePropertiesAction`, `UpdateFormsStateAction` (`GraphicalUiElement.Lifecycle.cs:78,87`); `RefreshLocalizationOnElementAction`, `TryGetLocalizationKey`, `SetLocalizationEnabled` (`:168-188`) | `SystemManagers.Initialize` (`SystemManagers.cs:273-281`), `FormsRefreshStylesHooks.Install`, `GumService` ctor; nulled in `Uninitialize` (`GumService.cs:664-669`) | hot path: every property set | **G**: every assignment is a method group of a static class or a capture-free lambda, identical across services of one backend. Keep as plain static fields (they are on the layout hot path; do not put them behind a context lookup). Refcount the teardown | V |
| E6 | `RenderableRegistry` four dictionaries + lock (`GumCommon/GueDeriving/RenderableRegistry.cs:47-51`) | core `[ModuleInitializer]`s (`DefaultFilledRectangleRenderable.cs:29`, `DefaultStrokedRectangleRenderable.cs:27`, `DefaultStrokedCircleRenderable.cs:38`), `AposShapeRuntime.RegisterRuntimeTypes` (`:67-117`); `Reset()` in `UninitializePlatform` (`XnaLike.cs:405`) | shape runtimes at construction (`RectangleRuntime.cs:1829-1834`) | **G**; **H**: `Reset` on any `Uninitialize` leaves the other service building shapes with the degraded default renderables (the runtime has a fallback, so it degrades instead of throwing) | V |
| E7 | `StandardElementsManager.Self` (`Gum/Managers/StandardElementsManager.cs:79`), `Initialize` idempotent (`:141-150`), `CustomGetDefaultState` chain (`:914`, `RegisterExtendedDefaultStates` `:1145-1151`, Apos wrapper) | `SystemManagers.Initialize` (`:295`), `FinishInitialize` (`GumService.cs:566`), `AposShapeRuntime` | project default states | **G** (identical content for every project) | V |
| E8 | `GraphicalUiElement.MissingFileBehavior` set to `ThrowException` at init (`SystemManagers.cs:312`, `GraphicalUiElement.Properties.cs:47`); `GlobalFontScale`, `SuppressFontRegeneration`, `SuppressLayoutFromFontChange`, `ShowLineRectangles`, `AreUpdatesAppliedWhenInvisible` (`GraphicalUiElement.cs:128-199`, `Lifecycle.cs:220`) | host | | **C** | V |
| E9 | Thread-wide layout flags: `[ThreadStatic] IsAllLayoutSuspended` (`GraphicalUiElement.cs:269-270`), `[ThreadStatic]` `s_session`, `s_layoutChangeCount` (`GraphicalUiElement.UpdateLayout.cs:99-103`), `_stateBatchDepth`, `_pendingReferenceChanges`, `_affectedRowsScratch` (`SetValuesAndStates.cs:372-452`) | build / `RefreshStyles` / Forms controls | layout | **S**/thread-local; not service state. `IsAllLayoutSuspended` is **H** (no `finally`, see summary 1a). Do not move into the context | V |

### 2.5 Forms, input, popups, theme (`MonoGameGum/Forms`, `GumRuntime/InteractiveGue.cs`)

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| F1 | `FormsUtilities.cursor` / `keyboard` (`FormsUtilities.cs:58,71`), mirrored in `FrameworkElement.MainCursor` / `MainKeyboard` (`FrameworkElement.cs:127-129`) | `InitializeDefaults` (`:192,198,208-209`); `SetCursor` / `SetKeyboard`; `Uninitialize` nulls (`:559-560`, `GumServiceTeardown.cs:37-38`) | 73 `MainCursor` + 11 `MainKeyboard` hits in 17 + 5 files; `FormsUtilities.Update` (`:347-349`) | **P**. The cursor and keyboard objects are per-instance (`Cursor.CreateForCurrentPlatform(_game)`, `XnaLike.cs:73`), so a per-service cursor ticks `Activity` once per service per frame, not twice. The issue's double-tick concern only applies if the two services **share** one cursor instance | V |
| F2 | `FormsUtilities.Gamepads { get; private set; }` array (`:114`); `InitializeDefaults` **mutates the live array in place** (`:200-203`); `UpdateGamepads` (`:517-524`) drives `Gamepads[i]` through `IGumService.Default!.ApplyGamePadState`; `Uninitialize` replaces the array (`:561`) | | `GumService.Gamepads` (`:91`); `UseGamepadDefaults` copies the elements into `GamePadsForUiControl` | **P**; **H**: B's `InitializeDefaults` replaces A's `GamePad` objects, orphaning the ones A registered | V |
| F3 | `FrameworkElement.KeyboardsForUiControl`, `GamePadsForUiControl` (`FrameworkElement.cs:135,139`), `private set` | `UseKeyboardDefaults`/`UseGamepadDefaults`; `GumServiceTeardown.cs:35-36` clears | 27 + 29 hits | **P** | V |
| F4 | `FrameworkElement.PopupRoot`, `ModalRoot` (`:152,158`), `AdditionalPopupRootPairs` (`:171`), `GraphicalUiElement.ResolvePopupRoots` (`GraphicalUiElement.cs:388`) | `InitializeDefaults` (`FormsUtilities.cs:212-213`); `GumServiceTeardown.cs:21-33` | `FormsUtilities.Update` (`:368-466`, `RefreshPopupModalRootPairs` `:539-544`), `ListBox.cs:1693-1694`, `Tooltip.cs:127-162`, `FrameworkElement.cs:1984` | **P** | V |
| F5 | `FrameworkElement.DefaultFormsTemplates`, `DefaultFormsComponents` (`FrameworkElement.cs:646,654`, `private set`) | `InitializeDefaults` `TryAdd` only when absent (`FormsUtilities.cs:143-183`), so a second service **inherits the first's templates**; each theme's `RegisterVisuals` overwrites (`HazardTheme.cs:168-189`, `EditorTheme.cs:90`); `GumServiceTeardown.cs:40-42` clears | `FrameworkElement.cs:660-700` (construction of every Forms control) | **P**. Themed visuals are factories; the theme data they read is F6/F7 | V |
| F6 | `Styling.ActiveStyle` (`Styling.cs:22-41`); the `Styling` ctor assigns itself to `_activeStyle` when null (`:77-83`) and reads `SystemManagers.Default?.GetOrLoadEmbeddedTexture2d` (`:75`) | `InitializeDefaults` (`:167`); theme `ConfigureStyling` mutates it (`HazardTheme.cs:132-134`); nulled by `FormsUtilities.Uninitialize` (`:568`) and `XnaLike.cs:429` | every V3 visual ctor (24 files in `DefaultVisuals/V3`, e.g. `ButtonVisual`) and every theme visual (164 `*Visual.cs` in `Themes/`) | **P**. Visual constructors bake the style at construction (`Themes/Gum.Themes.Hazard.MonoGame/ButtonVisual.cs:45-56`), so the ambient at **construction time** decides the look; this alone rules out explicit parameters (section 4.1) | V |
| F7 | Theme-owned statics: `HazardStyling.ActiveStyle` (`HazardStyling.cs:29`, `private set`), `EditorStyling.ActiveStyle` (`EditorStyling.cs:25`), the same per theme; `BmfcSave._additionalRanges` (`BmfcSave.cs:160,180`, set by `Apply`); KernSmith font registration (`ThemePlatform.WireInMemoryFontCreator`/`RegisterFont`, `Themes/Shared/ThemePlatform.cs:22-99`) | `Theme.Apply()` | the theme's visuals | **G** for fonts and glyph ranges (additive, keyed by family name). **P** for `XStyling.ActiveStyle` **only if two services use the same theme with different tokens**; different themes never collide because each has its own class. Not covered by the spike (it never called `Apply`) | V, I |
| F8 | Tooltip default template: `FormsUtilities._defaultTooltipTemplate` (`:584`); the from-file template resolves the component with `ObjectFinder.Self` and builds with `SystemManagers.Default` **at hover time** (`:807-823`) | `InitializeDefaults`, `RegisterFromFileTooltipTemplate` | `Tooltip` creation | **P** (a lazy ambient read at event time: needs the right scope at hover, which `Update` provides) | V |
| F9 | `FormsUtilities.Update` modal/popup z-raise loops read `SystemManagers.Default.Renderer.Layers` (`:406,450`) | | | **P** via L3; under an `Update` scope they hit the right service | V |
| F10 | `InteractiveGue.CurrentInputReceiver` (setter fires `OnLoseFocus/OnGainFocus`, `InteractiveGue.cs:86-111`), `LastVisualPushed` (`:85`), `CurrentGameTime` (`:82`) | `DoUiActivityRecursively` (`:1328`), controls; `HandleRootCollectionChanged` clears focus (`FormsUtilities.cs:232-261`); `Uninitialize` (`GumService.cs:638`) | 35 `CurrentInputReceiver` hits in 7 files | **P** if each service owns its focus; one shared input receiver is an owner decision (section 8, Q3) | V |
| F11 | `InteractiveGue` next-push / next-click action queues: `nextPushActions`, `nextClickActions` (`:74-75`), holding lists (`:900,927`); public `AddNextPushAction` (`:909`) | menu/popup code (`Menu.HandleNextPush`, per `BaseTestClass.cs` comment) | the next cursor push in **whichever pump runs next** | **P**: otherwise service A's pump fires service B's queued "close popup on next click" under A's scope | V, I |
| F12 | `ToolTipService` hover state: `_hoveredElement`, timings, `_currentOpenTooltip`, `ConditionalWeakTable` (`ToolTipService.cs:42-48`), called from `FormsUtilities.Update` (`:498`) | | | **P** (one hovered element per cursor) | V |
| F13 | Update scratch: `innerList`, `innerRootList`, `_lastEventRoots`, `popupModalRootPairs` (`FormsUtilities.cs:292-301,537`); `GueInteractiveExtensionMethods.internalList`, `previewList` (`InteractiveGue.cs:1309,1318`) | | | **S**: a nested `FormsUtilities.Update` (a handler of service A calling B's `Update`) clears the outer list mid-iteration. Forbid nesting, or move into the context | V |
| F14 | Input config: `FrameworkElement.TabKeyCombos`, `ClickCombos`, `Up/Down/Left/RightKeyCombos` (`:1333-1382`), `ListBox` modifier keys (`:433-451`), `RangeBase` repeat rates (`RangeBase.cs:37-38`), `ToolTipService.*Delay` (`:29-40`), `ICursor.VisualOverBehavior` (`InteractiveGue.cs:1142`, set to the same value by `InitializeDefaults:196`), `Binding.DefaultUpdateTriggers` (`Binding.cs:147`) | host | | **C** | V |
| F15 | `DialogBox.LastTimeDismissed`, `NoTextShownState` (`DialogBox.cs:77,83`); `RadioButton.RadioButtonDictionary` keyed by parent object (`RadioButton.cs:21`) | | | **C** (per-parent / per-game-clock; DialogBox reads `IGumService.Default.GameTime`) | V |

### 2.6 Text, fonts, localization

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| T1 | `CustomSetPropertyOnRenderable.LocalizationService` forwards to `LocalizationRuntimeState.Current` (`Gum/Wireframe/CustomSetPropertyOnRenderable.cs:124-143`, `GumCommon/Localization/LocalizationRuntimeState.cs:13`) | `SystemManagers.Initialize` `??= new LocalizationService()` (`:276`); project load merges **the project's strings into that one service** (`GumService.cs:548-554`) | text assignment (`:978,1007`), `GumExpressions/EvaluatedSyntax.cs:626` | **P**. Today project B's strings merge into project A's service, so same-key strings collide and language is shared. Per-service localization is an owner decision (Q5) | V |
| T2 | `FontService` (`:164`), `InMemoryFontCreator` (`:189`), `_knownUnresolvableFontKeys` negative cache (`:2438`, cleared by both setters `:173,198`), `RenderTargetEffectResolver` (`:242`), `AdditionalPropertyOnRenderable` (`:248`), `PropertyAssignmentError` (`:203`) | host / themes (`ThemePlatform`) | font and property application | **G**. The negative cache is keyed by full file name, which is project-dependent, so it is correct only while paths are absolute | V |
| T3 | Font resolution reads `ObjectFinder.Self.GumProjectSave` (`UsesPathHashedFontCacheNames` `:2366`, font path `:2405`, `:2203`) and `RelativeDirectory`; fonts cached in `LoaderManager` by file name (`:2118-2144,2243`) | | **any `Font`/`FontSize`/`CustomFontFile` property set at runtime**, not just during build | **P**. This is the argument for "elements remember their context": game code changing `editorLabel.FontSize` outside any scope must resolve in the editor's project (section 4.3, E-resolution) | V |
| T4 | `Text.mManagers` set at construction (`Text.cs:290,903`), used for `Renderer` (`:762-777`; null falls back to `Renderer.Self`), render-target text (`:1073`), bounds (`:1122-1126`) | | | **resolves the M0 "what does Text do with its managers" item**: it keeps them for life, so a `Text` is correct after construction **if** it was built with the right managers (see E1 for the case where it was not) | V |
| T5 | `Text.Customizations`, `ContextCustomizations` (`Text.cs:855,874`, user-registered BBCode effects), `BbCodeParser.KnownTags` (`BbCodeParser.cs:66`) | host code | text rendering | **C**; **H**: `UninitializePlatform` clears both (`XnaLike.cs:412-413`) so any service's teardown wipes user registrations. Clear only when the last service leaves, or not at all | V |
| T6 | `TextRuntime.DefaultFont`, `DefaultFontSize`, `UseFontOversampling`, `OversamplingRegenerateThresholdPixels`, `UseAutomaticFontGrowth`, `MaxInMemoryFontAtlasSize`, `AssignFontInConstructor`, `DefaultTypeface`/`DefaultCustomFont` (`TextRuntime.cs:1518-1596`) | host | | **C** | V |
| T7 | Scratch: `BitmapFont.letterInfos`, `sMeasuredLineWidths` (`BitmapFont.cs:930,935`), `Text.widths` (`Text.cs:1174`), `IWrappedText._pooledWordList` `[ThreadStatic]` (`IWrappedText.cs:76`), `ImageData.mStaticData` (`ImageData.cs:22`), `CustomSetPropertyOnRenderable.currentDropshadow*` (`:1548-1554`), `BitmapFont._reportedPageLoadFailures` (`:288`) | | | **S** | V |

### 2.7 Shapes and optional packages (`Runtimes/GumShapes`)

| ID | State / behavior | Written by | Read by | Verdict | Ev |
|---|---|---|---|---|---|
| S1 | `ShapeRenderer._self` / `Self` (`ShapeRenderer.cs:15,239`) owning one `ShapeBatch` and per-draw batch state (`_currentView`, `_isBatchBegun`, ... `:19-55`) | `Initialize()` reads **`Gum.GumService.Default`** (`:248-262`); `Initialize(device, content)` (`:265`); `Uninitialize` (`:288`), invoked from `AposShapeRuntime.UninitializeRuntimeTypes` (`:193`) via the `UninitializeRuntimeTypes` reflection scan | `RenderableShapeBase.ShapeRenderer => ShapeRenderer.Self` (`RenderableShapeBase.cs:29`), `AposShapeRuntime.cs:92-113` | **G** for one device (batch state is reset by each `StartBatch`, so sequential draws by two services are fine: T, spike green). **H**: `Initialize()` is hard-wired to `GumService.Default`; `Uninitialize` is a process-wide hook any service triggers | V, T |
| S2 | `AposShapeRuntime._registered` (`:28`), `IsSvgRuntimeEnabled` (`:49`); `RegisterRuntimeTypes` does `AdditionalPropertyOnRenderable += ...` (`:171`) every time `_registered` was reset, and `UninitializeRuntimeTypes` resets it (`:192`) without a matching `-=` | | | **H**: duplicate handlers accumulate across Initialize/Uninitialize cycles (harmless but a leak) | V |
| S3 | Other optional packages with a `RegisterRuntimeTypes` / `UninitializeRuntimeTypes` pair | scanned by L9/L10 | | **G** | I |

### 2.8 Other runtimes that share GumCommon

| ID | Runtime | Finding | Ev |
|---|---|---|---|
| X1 | MonoGame / KNI / FNA | One source (`XNALIKE`): `GumService.XnaLike.cs`, `SystemManagers.cs`. KNI/FNA csprojs link the same files (`MonoGameGum/KniGum/KniGum.csproj`, `FnaGum.csproj:43-47`), so everything above applies to all three at once | V |
| X2 | Raylib | Same pattern: `InitializeInternal` (`Runtimes/RaylibGum/GumService.Raylib.cs:100-118`), `SystemManagers.Default` read by `Draw(Camera2D)` (`:219`). One OS window / GL context per process, so multiple services share it. `SystemManagers.Initialize` does not reset `RelativeDirectory` there (`#if !RAYLIB`, `SystemManagers.cs:303-308`) | V |
| X3 | Skia (`GumServiceSkiaBase`, used by Silk.NET, Stride, Unity, WPF, MAUI) | Always `SystemManagers.Default = new SystemManagers()` (`GumServiceSkiaBase.cs:411`). Its `SystemManagers.Initialize` already guards one-time global setup with `HasInitializedGlobal` (`Runtimes/SkiaGum/RenderingLibrary/SystemManagers.cs:79,98-120`) so several windows can coexist, and `GumSKElement` sets `CanvasWidth` before each draw (`GumSKElement.cs:116`): **precedent for "set the ambient before drawing"**. Skia restores the bundle hook on teardown (`GumServiceSkiaBase.cs:507-516`); XNA-like does not | V |
| X4 | Sokol | Own `GumService`, writes `ObjectFinder.Self.GumProjectSave` (`Runtimes/SokolGum/GumService.cs:278,308`). Not in any solution; report as unverified | V |
| X5 | FlatRedBall, Gum tool | Both use `SystemManagers.Default`, `ObjectFinder.Self`, `GraphicalUiElement` statics directly and never construct a `GumService`. They must see no change (section 4.4). FRB compiles `FrameworkElement.cs`, `GraphicalUiElement*.cs`, `ElementSaveExtensions` through `GumCoreShared.projitems` (net6 floor), so a new GumCommon file needs projitems registration (`frb-build-verification` skill) | V, I |

---

## 3. Comparison with the spike

The spike's `StaticSnapshot` has 27 slots (18 in the first spike, 9 added for Forms/themes; `MultiGumServiceStaticSnapshot.cs:46-77`) and a six-item `UncapturedState` list (`:26-34`).

| Group | Items | Inventory rows |
|---|---|---|
| Captured (27 slots; templates and components are restored by copying their contents, since the reference has no setter) | Managers x3, Project, RelativeDirectory, Canvas x2, Popup/ModalRoot, Cursor, Keyboard, Style, ContentLoader, the 5 delegates/func, InitialStateNotifier, VisualOver, Text defaults x2, InvalidTexture, keyboard/gamepad lists, templates and components contents | L2, L3, C1, C2, R4, F4, F1, F6, C4, E2, E5, E4, F14, R9, F3, F5 |
| The spike's six "uncaptured" | registrations table; `FormsUtilities.Gamepads`; `ShapeRenderer.Self`; `DefaultFormsTemplates` (contents copied, reference not); `Text.Customizations`/`ContextCustomizations`; `RenderableRegistry` | E1, F2, S1, F5, T5, E6 |
| **In neither list** (new here) | `Renderer.TextureFilter` (C6); `RelativeDirectory` reset inside `SystemManagers.Initialize` (C2); registration closures that capture managers (E1); `CustomEvaluateExpression` (E3); `LocalizationService` merge (T1); `CustomEffectManager` + its static `ContentManager` (R6); `PixelDataTextureApplier.Pool` (R8); embedded resource cache entries replaced on every `Initialize` and the shared UI sprite sheet disposed by either teardown (C5); `FileManager.CustomGetStreamFromFile` chain (C3); `InteractiveGue` next-push/click queues (F11); `ToolTipService` state (F12); `CurrentInputReceiver`/`LastVisualPushed` (F10, the spike lists the issue's note but does not snapshot it); gamepad array mutated in place (F2); `_default` / `IGumService.Default` unconditional null on teardown (L1, L2); constructor-time statics (L4); `Renderer` instance reads of `Default` (R2); `UseGamepadDefaults`/`ShapeRenderer.Initialize()` on `GumService.Default` (L6, S1); theme-owned statics and `BmfcSave` ranges (F7); `ColoredRectangleRuntime.DefaultWidth/Height` (C7) | as listed |

Things the spike did not exercise (T plus reading): `Update` and the whole input pump; code-only `Theme.Apply()`; popups opened at runtime (F4, F8, F11); lazy loads after `Initialize` (C2, T3); separate asset folders; bundles; hot reload; a second `GraphicsDevice`; `Uninitialize` followed by re-`Initialize` of one service while the other lives; `GumService.Draw()` with both services alive beyond the single red test.

---

## 4. Refined design

### 4.1 Activation scope versus explicit passing

| Criterion | Ambient (activation scope) | Explicit (`new Button(service)`, pass managers/context everywhere) |
|---|---|---|
| Public API churn | None for single-service code. Adds `Activate()` and a small context type | 24 files in `DefaultVisuals/V3`, 164 theme `*Visual.cs` files across backends, every `*Runtime` ctor, every codegen'd `new FooRuntime()` and every user custom visual. Codegen output would break for existing projects |
| Visual constructors read style at construction (F6) | Works: construction happens under the scope | Needs the service threaded into every visual and every nested `new` |
| Later reads with no element in hand (`Cursor` zoom, `FormsUtilities.Update`, `TextBoxBase` paste) | Works under `Update` scope | Needs a back-pointer on each object (cursor to its service, etc.) |
| Property set long after build (T3) | Needs the **element's** context, not the ambient (see 4.3 "Resolution order") | Same need; passing does not help because the setter signature has no service |
| Failure mode | Silent wrong context if code runs under the wrong scope (an editor click handler building game UI) | Compile error for missing argument; but a wrong service can still be passed |
| Thread behavior | Ambient pointer must be thread-aware (background screen loads) | None |
| FRB / tool / other backends | Unchanged (home context behaves like today's statics) | Source-breaking for FRB-shared files |
| Perf | One thread-static read plus a field read on facade reads; none on the layout/property-set hot paths if E5 stays plain statics | None |

Verdict: ambient, as the issue decided, with four safeguards the issue only partly states: (1) resolve from the element wherever an element exists (`EffectiveManagers` already exists, `GraphicalUiElement.Properties.cs:64`); (2) explicit overloads only at public entry points that already have them (`ToGraphicalUiElement(systemManagers)`, `Initialize(game, systemManagers)`, `Update(gameTime, roots)`); (3) fail loudly: a mismatch check on `Root.Children.Add`/`AddToManagers`, and under `FULL_DIAGNOSTICS` an ambient read while two or more contexts are live and no scope is active throws or logs; (4) a documented `using (service.Activate())` escape hatch for cross-service callbacks.

### 4.2 The model

**`GumContext`** (new, GumCommon, backend-neutral): the per-service bundle. It composes with, and does not replace, `IGumService` (ADR-0006 already made `IGumService` the runtime-capability interface); the service owns exactly one context.

Ambient resolution:

- `GumContext.Current` is `[ThreadStatic] static GumContext? _current` with a process-wide `GumContext.Home` fallback. A thread that never activated sees Home. This keeps the existing background-load pattern working (the codebase already made `IsAllLayoutSuspended` thread-static for that reason, `GraphicalUiElement.cs:266-269`) and makes a background build under `Activate()` safe where a plain static would stomp the main thread.
- `Activate()` returns a `readonly struct ActivationScope : IDisposable` holding the previous value. No allocation, nesting is stack-disciplined, restored in `finally` by `using`.
- **Home adoption.** The first `GumService.Initialize` while no service is live adopts Home as its context; later services get fresh contexts. When the last live service uninitializes, Home is reset and free again. Result: with one service, the context is the same object that today's statics effectively are, so single-service apps, tests that poke statics before `Initialize` (e.g. `MonoGameGum.Tests/BaseTestClass.cs:20-37`), the Gum tool and FRB behave exactly as now.
- **Initialize and Uninitialize run inside their own scope.** `InitializeInternal` becomes `using (context.Activate()) { ...existing body... }`. Every static write in `SystemManagers.Initialize`, `InitializeDefaults`, `FinishInitialize` then lands in that service's context with no per-line edits. `Initialize` leaves the previous ambient in place when it returns. `Update(...)` and `Draw()` open the same scope. One implementation point on XNA-like and raylib (`GumService.cs` is shared); Skia, Sokol need the same three wrappers.

**Three tiers** (the verdict column above):

| Tier | Storage | Contents | Teardown |
|---|---|---|---|
| **P** per-service | field in `GumContext`; the old static becomes a property facade over `Current` (same name, same type, source- and binary-compatible for properties) | L2/L3 pointers, C1 `ObjectFinder`, C2 relative dir, R4 canvas, R9, C6, C7, E1 element tables, F1-F6, F8, F10-F12, T1, F7 (`XStyling`), shape-renderer-per-device if multi-device is ever wanted | disposed with the service |
| **G** global | stays a plain static (hot paths stay hot) | E2-E7 delegates and tables' base layer, C3, C4, C5, R6, R8, S1-S3, T2, StandardElementsManager | refcount: set up idempotently by every `Initialize`, torn down only when the **last** service leaves (a `LiveServiceCount` in the context type) |
| **C/S** | unchanged | the config and scratch rows | n/a |

This is also the class-level answer to "fix the class, not the repro": a hand-maintained swap list (the spike's snapshot) is a per-case allowlist that grows each time someone adds a static. With context-owned fields, a new per-service static is a field that must be placed in the context to exist at all; the guard test in section 7 enforces it for anything that slips in as a plain static.

**Registration tables are layered, not swapped.** Lookup is the active context's table, then the global base. Writes made while a service scope is active (`Initialize`, `RegisterFromFileFormRuntimeDefaults`, `Activate()`d user code) go to that context; writes made with no scope active (`[ModuleInitializer]`, reflection hooks, codegen static constructors, `ElementSaveExtensions.RegisterGueInstantiationType(...)` called from `Main`) go to the base. Without the base layer, a second service would lose every registration made before it existed. Removing the closure capture (E1) falls out: each context registers its own lambdas.

**Resolution order for work that happens later (T3, element context).** A property set or state application resolves its context as: the element's `EffectiveManagers` context (free for any element that has been added to managers; `Managers` is already set recursively, `GraphicalUiElement.Managers.cs:161,318,339,361`), else the context stamped on the element's `ElementSave` at project load (every project-built element has `.ElementSave`, assigned in `CreateGueForElement`, `ElementSaveExtensions.GumRuntime.cs:174`), else the ambient. This answers M0 "how cheaply can a GUE carry its context": **no new field on `GraphicalUiElement`**, which `refactoring-direction` asks to keep from growing. `ElementSave -> context` is a `ConditionalWeakTable` filled once per element at project load (hundreds of entries, not per live element); `ISystemManagers` gains a default interface member `GumContext? Context => null` (non-breaking; `IGumService` already uses default members, `IGumService.cs`), implemented by `SystemManagers` through a back-pointer set when the service creates it.

**Where the public API surface changes (needs a setter or owner object):**

| Static | Today | Needed |
|---|---|---|
| `ObjectFinder.Self` | `private set` (`ObjectFinder.cs:93`) | `internal`/`public` set, or facade over `Current.ObjectFinder`. The tool never sets it |
| `FormsUtilities.Gamepads` | `private set` (`:114`) | owned by context; facade |
| `FrameworkElement.DefaultFormsTemplates`, `DefaultFormsComponents`, `GamePadsForUiControl`, `KeyboardsForUiControl` | `private set` | facade returning the context's collection (the reference is never reassigned by user code, only mutated) |
| `Text.Customizations`/`ContextCustomizations` | `private set` | stay **C**; just stop clearing them while another service lives |
| `ElementSaveExtensions` tables | private fields | move into a registry object owned by the context + base |
| `ShapeRenderer.Self` | private `_self` | stays **G**; fix `Initialize()` to take the service it is called on |
| `Renderer.TextureFilter` | static | instance property on `Renderer`, static kept as default |
| `FileManager.RelativeDirectory`, `CustomGetStreamFromFile` | `ToolsUtilities` statics | `ToolsUtilities` cannot reference GumCommon, so either a tiny provider hook (`FileManager.RelativeDirectoryProvider`) or write-on-activate (swap these two values when a scope opens/closes). Prefer write-on-activate: a handful of assignments per scope, no new public API |
| `CanvasWidth/Height` | auto properties | facade over the context; reads are rare (R4) |
| `GraphicalUiElement` delegate fields (E5) | public static **fields** | unchanged; changing a public field to a property would break binary compatibility for compiled extension packages |

### 4.3 Per-subtree canvas and the input pump (what the context does not solve alone)

- Canvas size: the context holds one canvas; a service drawn into a sub-rectangle of the window (the editor in a panel) also needs its cursor mapped into that rectangle. #4096/#4098 already added `HitTestTransformMatrix`; wire it to the per-service `Cursor` (a cursor knowing its managers removes the R5 ambient read).
- Input exclusivity (editor on top of game): per-service cursors both see the same click. Needs a gate: the owner of the host loop decides which service receives input this frame (e.g. `service.IsInputEnabled`, or call only one `Update`). The `Update(gameTime, roots)` overload already drives several trees through one pump and remains the lowest-risk path for "UI over UI in the same theme". Q3.

### 4.4 Back-compat

| Consumer | Effect |
|---|---|
| Single service, `GumService.Default` | Identical. Home adoption: its context is the home context; `SystemManagers.Default`, `ObjectFinder.Self` etc. return the same objects as today. Must be enforced by a test (section 7) |
| `new GumService()` as the only service | Same as above (the first initialized adopts Home) |
| `GumService.Default` property | Unchanged. It is a lazily created instance, not "the active service". `IGumService.Default` is the active-service facade. Guard `Uninitialize` so it only nulls `_default` / `IGumService.Default` when they are `this` (L1, L2) |
| User code reading statics directly (`SystemManagers.Default`, `FrameworkElement.PopupRoot`, `ObjectFinder.Self`, `GraphicalUiElement.CanvasWidth`) | Source- and binary-compatible. Semantics become "the active context's" instead of "last `Initialize` wins". Only apps that initialized two services and relied on last-wins change, and they were already broken |
| Code assigning statics (`SystemManagers.Default = x`, tests, FRB, tool) | Writes go to the active context, which is Home outside any scope: same as today |
| Codegen output and `*Runtime` constructors | Unchanged (no new required parameters) |
| Raylib | Same code path (`GumService.cs` shared); raylib's `SystemManagers` is a different class with its own `Default`; apply the same facade there |
| Skia family (Silk.NET, Stride, Unity, WPF, MAUI) | `GumServiceSkiaBase` and `SystemManagers` are separate files; port the three scope wrappers and the facades. The existing `HasInitializedGlobal` and per-draw canvas assignment already match the model. M1 is scoped to XNA-like, with raylib and Skia as follow-ups (Q4) |
| Sokol | Separate `GumService`; unverified here |
| FRB | Home behaves as today. New GumCommon files must be added to `GumCoreShared.projitems` and the FRB shared projects and built on net6; run the FRB canary (`frb-build-verification`) |
| Binary compatibility | Convert only properties (auto or manual) to facades. Do not turn public static fields into properties (E5, `InitialStateAppliedNotifier`, `CustomEvaluateExpression`) |

### 4.5 What breaks for user code that is not updated

1. A handler running inside service X's scope that builds UI for service Y gets X's style and templates (F6) and X's registrations; `AddToRoot`/`Root.Children.Add` should flag the mismatch; fix is `using (y.Activate())`.
2. `Draw()` ordering: services drawing into one target must rebind the render target per service (R11).
3. Code that stores a `SystemManagers.Default` reference at startup keeps the home service's, which is right for the game and wrong for editor code that grabbed it after a scope; documented.
4. `FormsUtilities.Update`/`InteractiveGue.AddNextPushAction` called from a thread with no scope act on Home.
5. Nested `Update` across services (F13) is unsupported unless the scratch lists move into the context.
6. Two services on **different `GraphicsDevice`s** remain unsupported in v1: the content cache, `Sprite.InvalidTexture`, `Text.DefaultBitmapFont`, `PixelDataTextureApplier.Pool`, `ShapeRenderer.Self` and `CustomEffectManager` are device-bound (R6, R8, S1, C4).

---

## 5. Milestones (revised)

Land one change at a time and measure/verify before stacking (CLAUDE.md "one change at a time"). Each PR below is behavior-preserving for one service unless stated.

| Step | Content | Why here |
|---|---|---|
| **M0a** (3-5 tiny PRs, each independent, each fixes a one-service bug) | (1) `try/finally` around `IsAllLayoutSuspended` toggles; a small scoped helper (struct disposable) so the five sites share it. (2) `Renderer` stores its `SystemManagers` in `Initialize` and uses it in the six places (R2); `GumService.Draw()` calls `SystemManagers.Draw()` (L5). (3) `Uninitialize` clears `_default`, `IGumService.Default`, `SystemManagers.Default`, `ISystemManagers.Default` only if they are `this` (L1-L3). (4) `UseGamepadDefaults` uses `this`; `ShapeRenderer.Initialize()` takes the service it is called on (L6, S1). (5) MonoGame/KNI/FNA restore the bundle `CustomGetStreamFromFile` on teardown like Skia (C3). Delete the dead `NineSlice` lookup (`GumService.cs:613-615`) while there | Real bugs, no design dependency, shrink what the design has to explain. The spike's red tests do not change except `ServiceDraw_OnFirstService...`, which goes green with (2) |
| **M0b** | Finish the two M0 checks: Text (answered, T4) and GUE context (answered, 4.2). Decide backend scope and Q1-Q5 | Unblocks M1 |
| **M1a** | `GumContext` + `Activate()` + Home adoption + `LiveServiceCount`. No behavior change; the context holds only L2/L3 at first. Add the static-inventory guard test with every row of section 2 classified | Smallest reviewable step; the guard test prevents silent regressions of the class |
| **M1b** | Move the **Initialize-written** state into the context in one PR, because splitting it leaves `Initialize` clobbering half of it: `ObjectFinder` (C1), `RelativeDirectory` (C2), canvas (R4), element tables with base layer (E1), `Styling`/templates/popup roots/cursor/keyboard/gamepads (F1-F6), `Text`/`Sprite` defaults (R9), `TextureFilter` (C6). `Initialize` and `Uninitialize` run in scope; global teardown behind the refcount (L10, E2-E6, C4, C5, T5). This **absorbs the issue's M3 ("per-context templates and `Styling`") and the "per-context `ObjectFinder`" and "`RelativeDirectory`" items of M4**, because `Initialize` writes them | The issue's M1 acceptance ("`Initialize` no longer overwrites another service's state") cannot be met otherwise. Turns the spike's red tests green without the snapshot |
| **M1c** | `Update`/`Draw` scopes; `Cursor` and other controls resolve managers from the element where one exists (R5); mismatch check; `FULL_DIAGNOSTICS` ambient-read check | Needed for input and popups |
| **M2** | Input pump per service (F10-F13), cursor transform and viewport, focus/input gating, per-service popup roots through `ResolvePopupRoots` by default | As in the issue |
| **M3'** | Per-service localization (T1), shared-cache ownership on dispose (C4/C5 beyond refcount), hot reload scoping (C9), per-subtree canvas | The remainder of the issue's M3/M4 |
| **M4'** | Raylib, then Skia family ports of the same three wrappers and facades | Q4 |
| **M5** | Docs and sample, as in the issue; update the `gum-service` skill (its "Multiple GumService Instances" paragraph still says only #4096) and the `gum-runtime-systemmanagers-init` landmines | As in the issue |

Spike tests map: `SecondInitialize_...StaticState` and `...WithoutAnySwap` go green at M1b; `UninitializingSecond...` at M1b (refcount); `ServiceDraw_OnFirstService...` at M0a; themed no-swap at M1b.

---

## 6. Risks

| Risk | Mitigation |
|---|---|
| A static is missed, and the second service silently shares it | The guard test (section 7) classifies every mutable static; adding one without a classification fails CI. This is the main reason for facades over a swap list |
| Wrong-context construction inside callbacks (4.5 item 1) | Mismatch check, diagnostics-mode ambient check, docs, `Activate()` |
| Facade reads add cost on hot paths | Keep E5 delegates and layout flags as plain statics; canvas reads are parentless-only; run `Tests/Gum.Layout.Benchmarks` before/after M1b |
| `ToolsUtilities` cannot see GumCommon | Write-on-activate for the two `FileManager` values |
| Registration layering surprises: custom runtime registered after `Initialize` goes to base and is visible to every service | Intended for by-name registrations; document `Activate()` for service-specific ones |
| FRB (net6, `GumCoreShared.projitems`) and the Gum tool compile the same files | New files registered in projitems; FRB canary and `Gum.slnx`/`Gum.Wpf.sln` builds in the M1a PR |
| `Uninitialize` ordering with a live second service (assets disposed that the other still uses, C5) | Refcounted `DisposeAndClear`; M3' for per-service ownership |
| Reflection scan cost per `Initialize` (L9) | Cache which assemblies were scanned; low priority |
| Behavior change where `Initialize` currently "wins" | Only for apps that initialize two services; call out in release notes |
| Thread assumptions in scratch buffers (F13, T7) | Document single main thread per service; background loads open their own scope |

---

## 7. Test strategy

Use `Tests/MonoGameGum.IntegrationTests` (real `GraphicsDevice`, headless host `MainThreadTestHost.cs`) for pixel and lifecycle tests; `MonoGameGum.Tests` for logic that needs no device. Shouldly, no parallel execution (`gum-unit-tests` skill).

1. **Static-inventory guard test** (new, `MonoGameGum.Tests`): reflect over `GumCommon`, `MonoGameGum`, the shapes package and `Gum.Themes.*` for non-readonly public/internal static fields and static properties with a setter, excluding compiler-generated and `[ThreadStatic]` layout flags; compare to a checked-in classification file (P/G/C/S). A new mutable static without a classification fails. Replaces the hand-written lists in `BaseTestClass.cs` and the spike's `UncapturedState` as the source of truth.
2. **Single-service identity** (hard requirement from the issue): a characterization test, written before M1a, that records `SystemManagers.Default`, `ObjectFinder.Self`, `FrameworkElement.PopupRoot`, `Styling.ActiveStyle`, canvas and templates after `Initialize`/`Uninitialize`/`Initialize` and asserts identity after M1b. Plus the existing suites unchanged.
3. **Spike tests as acceptance**: remove `StaticSnapshot.Restore()` from the themed and plain tests; they must pass without it. Keep the snapshot only for the characterization (red) variants until M1b.
4. **New characterization tests, red first** (one per inventory finding that has a behavior): relative-dir reset by a code-only second service (C2) with a lazy relative texture load in A; element tables capture (E1): A's `TextRuntime` after B initializes has A's managers; `TextureFilter` per project (C6); localization strings merge (T1); `CustomEvaluateExpression` survives the other's teardown (E3); `UseGamepadDefaults` on a non-default service; code-only `Theme.Apply()` on service B with a shape theme (S1); `Renderer.Draw` of a non-active service with a render-target element (R2); exception inside `ToGraphicalUiElement` leaves `IsAllLayoutSuspended` false (M0a-1, no second service needed).
5. **Lifecycle matrix**: A init, B init, B uninit, A still builds and draws; A uninit then B still works; A uninit, A re-init while B lives; both orders; ref-count reaches zero then a fresh pair works (the existing `UninitializeTests.cs` and `GumServiceUnitTests.cs` stay green).
6. **Update/input** (M2): two services with a `Button` each under one scripted `Cursor` stub; click lands only in the gated service; popups open into the right root; next-push queue fires in the right pump; `Update` of both in one frame ticks each cursor once.
7. **Scope behavior**: nesting restores; exception inside a scope restores; a background thread without a scope sees Home; mismatch check fires; `FULL_DIAGNOSTICS` ambient check fires.
8. **Perf**: `Tests/Gum.Layout.Benchmarks` before/after M1b for layout and property-set; no regression expected (E5 and layout flags stay plain).
9. **Other backends**: `RaylibGum.Tests`, `SkiaGum.Tests` for each port step; FRB canary and `Gum.Wpf.sln` in the M1a/M1b PRs; run `Tools/verify.ps1` per CLAUDE.md, not whole suites.

---

## 8. Open decisions (owner)

1. **Which service is the default after a second `Initialize`?** Recommendation: the first one (Home adoption), so adding an editor never changes the game's behavior; add `service.MakeActive()` (non-scoped switch) for hosts that want to flip it. The issue lists this as open.
2. **Is a second `.gumx` required in v1?** The audit says the P-tier moves as one block (M1b); a code-only editor does not shrink the work much, because the same `Initialize` writes (`ObjectFinder`, `RelativeDirectory`, `Styling`) are involved either way. Recommendation: second `.gumx` is in v1 scope for XNA-like.
3. **One cursor/keyboard/focus or one per service?** Recommendation: one cursor/keyboard instance per service (matches `Cursor.CreateForCurrentPlatform`, avoids the double-tick), with an input gate chosen by the host; shared `CurrentInputReceiver` is a separate call (one focused TextBox across both UIs is plausible, but then `OnLoseFocus/OnGainFocus` cross services).
4. **Backends in v1.** Recommendation: MonoGame/KNI/FNA only (one source); raylib next (shared `GumService.cs`); Skia family after, since its init already guards global setup. FRB and the Gum tool unchanged.
5. **Per-service localization?** Today project strings merge into one service (T1). Recommendation: per-service by default for services after Home; Home keeps the shared service.
6. **Same device only in v1?** Recommendation: yes; document two devices as unsupported.
7. **Ambient thread model:** thread-static with Home fallback (recommended) versus plain static (matches today's statics, but a background build under `Activate()` would stomp the main thread).
8. **Where this doc lives.** Placed under `Direction/` (the repo's strategy/plan convention, listed in `Direction/README.md`), not under the tests folder.
9. **Thread model of the active context (decided; prototype branch `6066-proto-context`).** Decision: the active context is a **plain static**, not `[ThreadStatic]` and not `AsyncLocal`. All UI work for all services runs on one thread, and background building for a service is unsupported unless it runs on that thread. This supersedes the thread-static recommendation in section 4.2 and Q7. Consequences: a scope opened on one thread is visible to every thread while it lasts, and it persists across an `await` that resumes on the same synchronization context. `GumContextTests.Activate_ShouldBeVisibleToWorkRunOnAnotherThread` and `Activate_ShouldPersistAcrossAwaitOnTheSameSynchronizationContext` pin this.
10. **ADR.** A decision this size likely wants an ADR once Q1-Q7 are settled (`Direction/decisions/`), superseding nothing; ADR-0006 is its closest relative.

---

## 9. How this was made, and limits

- Method: read `GumService*.cs`, `FormsUtilities.cs`, `SystemManagers.cs`, `Renderer.cs`, `ElementSaveExtensions.GumRuntime.cs`, `ObjectFinder.cs`, `LoaderManager.cs`, `ContentLoader.cs`, `Styling.cs`, `ShapeRenderer.cs`, `AposShapeRuntime.cs`, `GumBundleLoader.cs`, the Hazard theme, the Skia/raylib services, and the spike files; enumerated statics with `rg` over `MonoGameGum`, `GumCommon`, `GumRuntime`, `RenderingLibrary`, `Runtimes`, `Gum/DataTypes`, `Gum/Wireframe`, `ToolsUtilities`, `Themes`.
- Not done: nothing was built or run, so **T** items are the spike author's results. Readers of rows marked I were not traced exhaustively (notably E3's writer, all `Gum/DataTypes` call sites, Sokol/Stride/Unity internals). The counts are from `rg` line matches, not a semantic tool (CLAUDE.md: no LSP here), so treat them as order-of-magnitude.
- The tool (`Tool/`, `Gum/` WPF head) was not audited beyond confirming it shares `ObjectFinder`, `StandardElementsManager` and `GraphicalUiElement` and must not change.
