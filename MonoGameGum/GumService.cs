#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Wireframe;
using GumRuntime;
using Gum.Forms.Controls;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ToolsUtilities;
using Gum.Forms;
using Gum.Threading;
using Gum.Localization;

#if XNALIKE
using Gum.GueDeriving;
using MonoGameGum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace MonoGameGum;
#elif RAYLIB
using Gum.GueDeriving;
using Gum.Input;
using GameTime = double;
using Raylib_cs;
using RaylibGum.Renderables;
namespace RaylibGum;
#endif

public class GumService : IGumService
{
    IRenderer IGumService.Renderer => this.SystemManagers.Renderer;
    ICursor IGumService.Cursor => this.Cursor;

    void IGumService.Initialize()
    {
#if XNALIKE
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game) on the concrete GumService instead.");
#elif RAYLIB
        Initialize(DefaultVisualsVersion.Newest);
#endif
    }

    void IGumService.Initialize(string gumProjectFile)
    {
#if XNALIKE
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game, gumProjectFile) on the concrete GumService instead.");
#elif RAYLIB
        Initialize(gumProjectFile);
#endif
    }

    #region Default
    static GumService _default = default!;

    /// <summary>
    /// Gets the default instance of the GumService class.
    /// </summary>
    /// <remarks>This property provides a lazily initialized, shared GumService instance for general use. Use
    /// this instance when a custom configuration is not required.</remarks>
    public static GumService Default => _default ??= new GumService();

    #endregion

    /// <summary>
    /// The GameTime of the most recent Update call.
    /// </summary>
    public GameTime GameTime { get; private set; }

    /// <inheritdoc/>
    float? IGumService.GameTime =>
#if XNALIKE
        GameTime != null ? (float?)GameTime.TotalGameTime.TotalSeconds : null;
#else
        // On Raylib, GameTime is aliased to double and starts at 0; treat the pre-Update
        // state as null by also returning null when nothing has run Update yet.
        _hasReceivedUpdate ? (float?)GameTime : null;
#endif

#if !XNALIKE
    private bool _hasReceivedUpdate;
#endif

    /// <summary>
    /// Gets the default cursor, which represents either mouse or touch screen depending on hardware capabilities.
    /// </summary>
    public Cursor Cursor => FormsUtilities.Cursor;

    /// <summary>
    /// Gets the default keyboard.
    /// </summary>
    public Keyboard Keyboard => FormsUtilities.Keyboard;

    /// <summary>
    /// Gets the service used to provide localized strings and resources for the application.
    /// </summary>
    public ILocalizationService LocalizationService => CustomSetPropertyOnRenderable.LocalizationService!;

    /// <summary>
    /// Gets the collection of connected gamepads available to the application.
    /// </summary>
    public GamePad[] Gamepads => Gum.Forms.FormsUtilities.Gamepads;

    public Renderer Renderer => this.SystemManagers.Renderer;

    private SystemManagers? _systemManagers;
    public SystemManagers SystemManagers
    {
        get => _systemManagers ?? throw new InvalidOperationException(
            "GumService has not been initialized. Call GumService.Initialize() first.");
        private set => _systemManagers = value;
    }

    public DeferredActionQueue DeferredQueue { get; private set; }

    /// <inheritdoc/>
    public INativeTextInput? NativeTextInput { get; private set; }

    /// <inheritdoc/>
    public IGumClipboard? Clipboard { get; private set; }

    /// <inheritdoc/>
    IRenderable IGumService.CreateSpriteRenderable() =>
#if XNALIKE
        new global::RenderingLibrary.Graphics.Sprite(texture: null);
#elif RAYLIB
        new global::Gum.Renderables.Sprite();
#else
        throw new NotSupportedException("This runtime does not have a sprite renderable implementation.");
#endif

#if !IOS && !ANDROID
    private IGumHotReloadManager? _hotReloadManager;
#endif

    private int? _zoomReferenceWidth;
    private int? _zoomReferenceHeight;

    private enum FitPolicy
    {
        None,
        Zoom,
        Expand,
    }

    private FitPolicy _fitPolicy;
    private WindowZoomMode _fitZoomMode;
    private float _fitDefaultZoom;
    // Update() polls these against the current window size each frame so resize
    // detection is platform-agnostic — no resize-event subscription on either backend.
    private int _lastSeenWindowWidth;
    private int _lastSeenWindowHeight;

    /// <summary>
    /// Gets or sets the width of the canvas, which acts as the root-most coordiante space. This value
    /// represents the "internal coordinates" which can be adjusted by Camera zoom.
    /// </summary>
    public float CanvasWidth
    {
        get => GraphicalUiElement.CanvasWidth;
        set => GraphicalUiElement.CanvasWidth = value;
    }

    /// <summary>
    /// Gets or sets the height of the canvas, which acts as the root-most coordiante space. This value
    /// represents the "internal coordinates" which can be adjusted by Camera zoom.
    /// </summary>
    public float CanvasHeight
    {
        get => GraphicalUiElement.CanvasHeight;
        set => GraphicalUiElement.CanvasHeight = value;
    }

    /// <summary>
    /// Enables a zoom-based fit policy: the camera scales so the Gum canvas tracks the
    /// current window size, using the window dimensions at the first call as the 1:1
    /// reference. The fit is applied immediately and then re-applied automatically inside
    /// <see cref="Update(GameTime)"/> whenever the window size changes. Call once at
    /// startup — no resize-handler boilerplate required.
    /// </summary>
    /// <param name="mode">
    /// Whether window height or window width drives the zoom factor. The dominant axis
    /// fully fills the window; the other axis gets extra space or is cropped depending on
    /// the window's aspect ratio relative to the reference. Defaults to height-dominant.
    /// </param>
    /// <param name="defaultZoom">
    /// A multiplier applied on top of the computed zoom — i.e., the zoom factor at the
    /// reference resolution. Pass <c>2f</c> to make everything render at 2× the authored
    /// size at the reference resolution, scaling proportionally as the window resizes.
    /// </param>
    /// <remarks>
    /// Calling this replaces any previously enabled fit policy (including one set by
    /// <see cref="EnableExpandToWindow(float)"/>). The reference resolution is captured on
    /// the first call to <c>EnableZoomToWindow</c> and persists for the lifetime of this
    /// instance.
    /// </remarks>
    public void EnableZoomToWindow(WindowZoomMode mode = WindowZoomMode.HeightDominant, float defaultZoom = 1f)
    {
        _fitPolicy = FitPolicy.Zoom;
        _fitZoomMode = mode;
        _fitDefaultZoom = defaultZoom;
        ApplyCurrentFit();
    }

    /// <summary>
    /// Enables an expand-based fit policy: the Gum canvas is resized to match the current
    /// window so authored UI gets more (or less) space rather than scaling. The fit is
    /// applied immediately and then re-applied automatically inside
    /// <see cref="Update(GameTime)"/> whenever the window size changes. Call once at
    /// startup — no resize-handler boilerplate required.
    /// </summary>
    /// <param name="defaultZoom">
    /// A camera zoom multiplier. With <c>1f</c> the canvas matches the window pixel-for-pixel.
    /// With <c>2f</c> the camera zooms in 2× and the canvas covers half as many internal
    /// coordinate units — useful for authoring at a smaller virtual resolution while still
    /// filling the window.
    /// </param>
    /// <remarks>
    /// Calling this replaces any previously enabled fit policy (including one set by
    /// <see cref="EnableZoomToWindow(WindowZoomMode, float)"/>).
    /// </remarks>
    public void EnableExpandToWindow(float defaultZoom = 1f)
    {
        _fitPolicy = FitPolicy.Expand;
        _fitDefaultZoom = defaultZoom;
        ApplyCurrentFit();
    }

    private void ApplyCurrentFit()
    {
        if (_fitPolicy == FitPolicy.None)
        {
            return;
        }

        var (windowWidth, windowHeight) = GetWindowSize();
        _lastSeenWindowWidth = windowWidth;
        _lastSeenWindowHeight = windowHeight;
        ApplyFitForSize(windowWidth, windowHeight);
    }

    private void PollWindowSizeAndApplyFit()
    {
        if (_fitPolicy == FitPolicy.None)
        {
            return;
        }

        var (windowWidth, windowHeight) = GetWindowSize();
        if (windowWidth == _lastSeenWindowWidth && windowHeight == _lastSeenWindowHeight)
        {
            return;
        }

        _lastSeenWindowWidth = windowWidth;
        _lastSeenWindowHeight = windowHeight;
        ApplyFitForSize(windowWidth, windowHeight);
    }

    internal void ApplyFitForSize(int windowWidth, int windowHeight)
    {
        switch (_fitPolicy)
        {
            case FitPolicy.Zoom:
                _zoomReferenceWidth ??= windowWidth;
                _zoomReferenceHeight ??= windowHeight;
                var (zoom, zoomCanvasW, zoomCanvasH) = WindowFitMath.ComputeZoom(
                    windowWidth, windowHeight,
                    _zoomReferenceWidth.Value, _zoomReferenceHeight.Value,
                    _fitZoomMode, _fitDefaultZoom);
                SystemManagers.Renderer.Camera.Zoom = zoom;
                CanvasWidth = zoomCanvasW;
                CanvasHeight = zoomCanvasH;
                Root.UpdateLayout();
                break;
            case FitPolicy.Expand:
                var (expandZoom, expandCanvasW, expandCanvasH) = WindowFitMath.ComputeExpand(
                    windowWidth, windowHeight, _fitDefaultZoom);
                SystemManagers.Renderer.Camera.Zoom = expandZoom;
                CanvasWidth = expandCanvasW;
                CanvasHeight = expandCanvasH;
                Root.UpdateLayout();
                break;
        }
    }

    private (int width, int height) GetWindowSize()
    {
#if XNALIKE
        var pp = Game.GraphicsDevice.PresentationParameters;
        return (pp.BackBufferWidth, pp.BackBufferHeight);
#elif RAYLIB
        return (Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
#endif
    }

    public ContentLoader? ContentLoader => LoaderManager.Self.ContentLoader as ContentLoader;

    public InteractiveGue Root { get; private set; } = new ContainerRuntime();
    /// <inheritdoc/>
    public InteractiveGue PopupRoot => FrameworkElement.PopupRoot;
    /// <inheritdoc/>
    public InteractiveGue ModalRoot => FrameworkElement.ModalRoot;

    /// <summary>
    /// Exports the live UI tree under <see cref="Root"/> to a Gum project at <paramref name="filePath"/>,
    /// so it can be opened and inspected in the Gum tool. This is the headline path for code-only games,
    /// which have no design-time .gumx to open. Each runtime element is written as a standard-element
    /// instance and the screen is named after the file.
    /// </summary>
    /// <param name="filePath">
    /// Destination project (.gumx) path. Its directory receives the Screens/ and Standards/ subfolders.
    /// </param>
    /// <param name="shake">
    /// When true (default), values equal to the standard-element default are pruned so the artifact is
    /// light and reads as "unedited" in the tool. When false, every value is written — heavier, but the
    /// always-correct baseline-free form.
    /// </param>
    public void ExportSnapshot(string filePath, bool shake = true)
    {
        // A code-only game may never have triggered standards population; ensure the catalog exists
        // before reading it (as the serializer's baseline) and writing it (as the project's standards).
        if (StandardElementsManager.Self.DefaultStates == null)
        {
            StandardElementsManager.Self.Initialize();
        }

        string screenName = Path.GetFileNameWithoutExtension(filePath);

        // Non-null here: the guard above initializes the catalog when it was missing.
        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates!);
        ScreenSave screen = serializer.CreateScreenSave(Root, screenName, shake);

        GumProjectSave project = new();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        // Match the project's canvas resolution to the live canvas (the game's resolution) so the
        // snapshot lays out in the tool exactly as it did at runtime, rather than the 800x600 default.
        if (GraphicalUiElement.CanvasWidth > 0)
        {
            project.DefaultCanvasWidth = (int)GraphicalUiElement.CanvasWidth;
        }
        if (GraphicalUiElement.CanvasHeight > 0)
        {
            project.DefaultCanvasHeight = (int)GraphicalUiElement.CanvasHeight;
        }

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = screenName, ElementType = ElementType.Screen });

        EnsureReferencedStandardsExist(project, screen);

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.StandardSubfolder));
        }

        project.Save(filePath, saveElements: true);

        if (!string.IsNullOrEmpty(directory))
        {
            CopyReferencedFiles(serializer, screen, directory);
        }
    }

    // Instances may reference standard types the default seed omits -- notably deprecated ones like
    // ColoredRectangle, which new (v3) projects no longer include but an old/live tree may still contain.
    // Add any such referenced standard so the snapshot's instances don't dangle on a missing base type.
    private static void EnsureReferencedStandardsExist(GumProjectSave project, ScreenSave screen)
    {
        HashSet<string> existing = new(project.StandardElements.Select(standard => standard.Name));
        foreach (InstanceSave instance in screen.Instances)
        {
            string baseType = instance.BaseType;
            if (string.IsNullOrEmpty(baseType) || existing.Contains(baseType))
            {
                continue;
            }

            if (StandardElementsManager.Self.IsDefaultType(baseType))
            {
                StandardElementsManager.Self.AddStandardElementSaveInstance(project, baseType);
                existing.Add(baseType);
            }
        }
    }

    // Bundles the files referenced by the snapshot (Sprite/NineSlice textures, ...) next to the project
    // so it opens self-contained in the tool. Relative references are copied preserving their relative
    // path; absolute references already resolve on their own, and missing files are skipped (logged).
    private static void CopyReferencedFiles(IRuntimeSnapshotSerializer serializer, ScreenSave screen, string snapshotDirectory)
    {
        foreach (string referencedPath in serializer.GetReferencedFiles(screen))
        {
            if (!FileManager.IsRelative(referencedPath))
            {
                continue;
            }

            string absoluteSource;
            try
            {
                absoluteSource = FileManager.MakeAbsolute(referencedPath);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (!File.Exists(absoluteSource))
            {
                System.Diagnostics.Debug.WriteLine($"Snapshot: referenced file not found, skipping: {referencedPath}");
                continue;
            }

            string destination = Path.Combine(snapshotDirectory,
                referencedPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            string? destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Copy(absoluteSource, destination, overwrite: true);
        }
    }

    /// <summary>
    /// Re-applies all styles on Root, PopupRoot, and ModalRoot. Call after
    /// <see cref="GumRuntime.ElementSaveExtensions.ApplyAllVariableReferences"/>
    /// to push variable reference changes to all live visuals.
    /// </summary>
    public void RefreshStyles()
    {
        Root?.RefreshStyles();
        PopupRoot?.RefreshStyles();
        ModalRoot?.RefreshStyles();
    }

    /// <summary>
    /// Re-translates all live text on Root, PopupRoot, and ModalRoot using the
    /// current language on <see cref="LocalizationService"/>. Call this after
    /// changing <see cref="ILocalizationService.CurrentLanguage"/> if you have
    /// disabled the automatic refresh by replacing the service. Otherwise this
    /// is invoked automatically on language change.
    /// </summary>
    /// <remarks>
    /// Text assigned via <c>SetTextNoTranslate</c> (such as user input in a
    /// <c>TextBox</c>) is skipped. Programmatic strings assigned via the
    /// localized <c>Text</c> property are re-translated, so dynamic values
    /// (e.g. <c>"Score: " + score</c>) will receive the "(loc)" missing-key
    /// suffix on language change unless they are assigned via the no-translate API.
    /// </remarks>
    public void RefreshLocalization()
    {
        Root?.RefreshLocalization();
        PopupRoot?.RefreshLocalization();
        ModalRoot?.RefreshLocalization();
    }

    private ILocalizationService? _subscribedLocalizationService;

    private void HandleLocalizationServiceChanged(ILocalizationService? previous, ILocalizationService? current)
    {
        if (_subscribedLocalizationService != null)
        {
            _subscribedLocalizationService.CurrentLanguageChanged -= RefreshLocalization;
        }
        _subscribedLocalizationService = current;
        if (current != null)
        {
            current.CurrentLanguageChanged += RefreshLocalization;
        }
    }

    /// <summary>
    /// Re-applies all styles on the specified element and its children. Call after
    /// <see cref="GumRuntime.ElementSaveExtensions.ApplyAllVariableReferences"/>
    /// to push variable reference changes to live visuals in a specific subtree.
    /// </summary>
    /// <param name="target">The root of the subtree to refresh.</param>
    public void RefreshStyles(GraphicalUiElement target)
    {
        target?.RefreshStyles();
    }

#if !IOS && !ANDROID
    /// <summary>
    /// Starts watching the Gum project source files at the given path.
    /// When any .gumx, .gucx, .gusx, or .gutx file changes, the project
    /// is reloaded and active elements in Root have their state reapplied.
    /// </summary>
    /// <param name="absoluteGumxSourcePath">
    /// Absolute path to the source .gumx file (not the bin/Content copy).
    /// </param>
    public void EnableHotReload(string absoluteGumxSourcePath)
    {
        _hotReloadManager = new GumHotReloadManager();
        _hotReloadManager.ReloadCompleted += () => HotReloadCompleted?.Invoke();
        _hotReloadManager.Start(absoluteGumxSourcePath);
    }

    /// <summary>
    /// Raised after a hot-reload pass completes (Root.Children rebuilt from updated
    /// ElementSaves). Subscribe from your game to react to project changes — e.g. rebuild
    /// entity-attached Gum visuals that aren't part of Root.Children and therefore weren't
    /// touched by the in-place patch.
    /// </summary>
    public event Action? HotReloadCompleted;
#endif

    public void UseKeyboardDefaults()
    {
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
    }

    public void UseGamepadDefaults()
    {
        Gum.Forms.Controls.FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
    }

#if !FRB
    private Gum.Async.SingleThreadSynchronizationContext? _syncContext;

    /// <summary>
    /// The active single-threaded synchronization context, or <c>null</c> if
    /// <see cref="UseSingleThreadedAsync"/> has not been called.
    /// </summary>
    public Gum.Async.SingleThreadSynchronizationContext? SynchronizationContext => _syncContext;

    /// <summary>
    /// Installs a <see cref="Gum.Async.SingleThreadSynchronizationContext"/> on the calling
    /// thread so that <c>await</c> continuations (including
    /// <c>await dialogBox.ShowAsync(...)</c>) resume on the game's primary thread. Call once,
    /// after <c>Initialize</c>. Subsequent calls are no-ops.
    /// </summary>
    /// <remarks>
    /// Off by default. Skip this call if you've already installed your own
    /// <see cref="System.Threading.SynchronizationContext"/> — installing two would
    /// route continuations through the wrong queue.
    /// </remarks>
    public void UseSingleThreadedAsync()
    {
        if (_syncContext != null) return;
        _syncContext = new Gum.Async.SingleThreadSynchronizationContext();
    }
#endif


    /// <summary>
    /// Gets whether GumService has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Result of the most recent project load performed by <see cref="Initialize(Game,string,SystemManagers,DefaultVisualsVersion)"/>
    /// (or platform-equivalent overloads). Null if no project file has been loaded.
    /// Inspect <see cref="GumLoadResult.Warnings"/> for non-fatal issues such as
    /// localization string-ID collisions across multi-file RESX projects or
    /// misconfigured mixed CSV/RESX localization lists.
    /// </summary>
    public GumLoadResult? LastLoadResult { get; private set; }

#if XNALIKE
    private Game? _game;
    public Game Game
    {
        get => _game ?? throw new InvalidOperationException(
            "GumService has not been initialized. Call GumService.Initialize() first.");
        private set => _game = value;
    }
#endif

    #region Initialize

    /// <summary>
    /// Instantiates a new GumService. This is usually not called directly, since
    /// the Default is the most common way to access GumService.
    /// </summary>
    public GumService()
    {
        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";
        Root.HasEvents = false;

        Root.Children.CollectionChanged += (o, e) => Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root, e);

        CustomSetPropertyOnRenderable.LocalizationServiceChanged += HandleLocalizationServiceChanged;
        // Pick up any LocalizationService that was assigned before this GumService was constructed.
        HandleLocalizationServiceChanged(null, CustomSetPropertyOnRenderable.LocalizationService);

        GraphicalUiElement.RefreshLocalizationOnElementAction = element =>
        {
            string? key = CustomSetPropertyOnRenderable.TryGetLocalizationKey(element);
            if (key != null)
            {
                element.SetProperty("Text", key);
            }
        };

        DeferredQueue = new DeferredActionQueue();
#if MONOGAME || KNI
        NativeTextInput = new MonoGameNativeTextInput();
#endif
        Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();

        GraphicalUiElement.SaveFormsRuntimePropertiesAction = formsObject =>
        {
            if (formsObject is FrameworkElement frameworkElement)
            {
                frameworkElement.SaveRuntimeProperties();
            }
        };
        GraphicalUiElement.UpdateFormsStateAction = formsObject =>
        {
            if (formsObject is FrameworkElement frameworkElement)
            {
                frameworkElement.UpdateState();
                frameworkElement.ApplyRuntimeProperties();
            }
        };
    }

    /// <summary>
    /// Marks GumService as initialized without requiring a graphics device.
    /// Intended for use in unit tests only.
    /// </summary>
    public void InitializeForTesting()
    {
        if (!IsInitialized)
        {
            IsInitialized = true;
        }
        // Wire the platform-agnostic default too. Extensions in GumCommon (e.g.
        // FrameworkElementExt.AddToRoot) resolve the runtime via IGumService.Default,
        // so tests that bypass the full Initialize(Game, ...) path still need this set.
        IGumService.Default = this;
    }

#if XNALIKE
    /// <summary>
    /// Initializes Gum, optionally loading a Gum project.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="gumProjectFile">An optional project to load. If not specified, no project is loaded and Gum can be used "code only".</param>
    /// <returns>The loaded project, or null if no project is loaded</returns>
    public GumProjectSave? Initialize(Game game, string? gumProjectFile = null)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }
        return InitializeInternal(
            game, game.GraphicsDevice,
            gumProjectFile,
            defaultVisualsVersion: Gum.Forms.DefaultVisualsVersion.Newest);
    }
#else
    /// <summary>
    /// Initializes Gum, optionally loading a Gum project.
    /// </summary>
    /// <param name="gumProjectFile">An optional project to load. If not specified, no project is loaded and Gum can be used "code only".</param>
    /// <returns>The loaded project, or null if no project is loaded</returns>
    public GumProjectSave Initialize(string gumProjectFile)
    {
        return InitializeInternal(
            gumProjectFile,
            defaultVisualsVersion: DefaultVisualsVersion.Newest)!;
    }
#endif

#if XNALIKE
    public void Initialize(Game game, Gum.Forms.DefaultVisualsVersion defaultVisualsVersion)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }

        InitializeInternal(game, game.GraphicsDevice, defaultVisualsVersion: defaultVisualsVersion);
    }
    public void Initialize(Game game, SystemManagers systemManagers)
    {
        InitializeInternal(game, game.GraphicsDevice, systemManagers: systemManagers);
    }
#else
    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }
#endif

    /// <summary>
    /// Loads animations for all elements in the project.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if a Gum project hasn't been loaded first</exception>
    [Obsolete("Experimental - this API may change in future versions")]
    public void LoadAnimations()
    {
        var project = ObjectFinder.Self.GumProjectSave;

        if(project == null)
        {
            throw new InvalidOperationException(
                "You must first load a project before attempting to load its animations. " +
                "Did you call GumUI.Initialize with a valid .gumx first?");
        }

        // Probe each element for a sibling .ganx (animation) file. Elements without
        // animations are the common case, so most probes legitimately miss. On real
        // filesystems the miss is silent; on browser/streaming platforms (Blazor WASM)
        // each miss is logged by the browser as a 404 — that noise is expected and
        // harmless. Surfaced here once so developers seeing the 404s in DevTools can
        // connect them to this probe loop instead of chasing them as real failures.
        Console.WriteLine(
            "[Gum] Probing each project element for an optional sibling Animations.ganx file. " +
            "On browser/streaming platforms (e.g. Blazor WASM) elements without animations " +
            "will appear as 404s in the network/console log — those are expected and benign.");

        foreach (var element in project.AllElements)
        {
            var animation = TryLoadAnimation(element);

            if (animation != null)
            {
                project.ElementAnimations.Add(animation);
            }
        }
    }

    internal static ElementAnimationsSave? TryLoadAnimation(ElementSave element)
    {
        string prefix = element is ScreenSave ? "Screens/" :
            element is ComponentSave ? "Components/" :
            element is StandardElementSave ? "StandardElements/" : string.Empty;

        var fileName = prefix + element.Name + "Animations.ganx";

        if (FileManager.FileExists(fileName))
        {
            var animation = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
            animation.ElementName = element.Name;
            return animation;
        }
        return null;
    }

#if XNALIKE
    /// <summary>
    /// Initializes Gum without a <see cref="Microsoft.Xna.Framework.Game"/> instance.
    /// <para>
    /// This overload is intended for non-interactive scenarios such as CLI tools, screenshot
    /// generation, and headless rendering pipelines where a <c>Game</c> object is not available.
    /// </para>
    /// <para>
    /// Input handling is NOT supported in this mode. This includes keyboard input, cursor/mouse
    /// input, gamepad input, non-EN-US keyboard layouts, and ALT+numeric key codes for accented
    /// characters in <c>TextBox</c> controls.
    /// </para>
    /// <para>
    /// Interactive games should use <see cref="Initialize(Microsoft.Xna.Framework.Game, string?)"/>
    /// instead, which wires up full input support.
    /// </para>
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/> to use for rendering.</param>
    /// <param name="gumProjectFile">
    /// Optional path to a <c>.gumx</c> project file to load. Pass <c>null</c> to skip project loading.
    /// </param>
    /// <returns>The loaded <see cref="GumProjectSave"/>, or <c>null</c> if no project file was specified.</returns>
    public GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        return InitializeInternal(null, graphicsDevice, gumProjectFile);
    }
#endif


    GumProjectSave? InitializeInternal(
#if XNALIKE
        Game game, GraphicsDevice graphicsDevice,
#endif
        string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

#if XNALIKE
        Game = game;
        RegisterRuntimeTypesThroughReflection();
#endif

        this.SystemManagers = systemManagers ?? new SystemManagers();
        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
            ISystemManagers.Default = this.SystemManagers;
        }

        IGumService.Default = this;

#if XNALIKE
        this.SystemManagers.Initialize(graphicsDevice, fullInstantiation: true);

        if (game != null && ContentLoader != null && ContentLoader.XnaContentManager == null)
        {
            ContentLoader.XnaContentManager = game.Content;
        }

        FormsUtilities.InitializeDefaults(game:game, systemManagers: this.SystemManagers,
            defaultVisualsVersion: defaultVisualsVersion);
#elif RAYLIB
        // SystemManagers.Initialize must come first because it assigns the
        // GraphicalUiElement.AddRenderableToManagers delegate. InitializeDefaults
        // creates PopupRoot/ModalRoot and calls AddToManagers on them — that call
        // silently no-ops if the delegate is still null, so the roots would never
        // be added to MainLayer.Renderables and would not draw.
        this.SystemManagers.Initialize();
        FormsUtilities.InitializeDefaults(systemManagers: this.SystemManagers,
            defaultVisualsVersion: defaultVisualsVersion);
#endif


        Root.AddToManagers(SystemManagers);
        Root.UpdateLayout();

        var mainLayer = SystemManagers.Renderer.MainLayer;
        if (Root.RenderableComponent is IRenderableIpso rootRenderable)
        {
            mainLayer.Remove(rootRenderable);
            mainLayer.Insert(0, rootRenderable);
        }

        GumProjectSave? gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {
            // Resolve loose-vs-bundle off the file extension: ".gumx" = loose, ".gumpkg" = bundle.
            // In bundle mode, installs a CustomGetStreamFromFile hook so runtime asset loads
            // (textures/fonts) also resolve from the bundle.
            BundleResolution bundleResolution = GumBundleLoader.Resolve(gumProjectFile);
            gumProject = GumProjectSave.Load(bundleResolution.ResolvedGumxPath, out GumLoadResult loadResult);
            LastLoadResult = loadResult;

            if (gumProject == null || !string.IsNullOrEmpty(loadResult.ErrorMessage) || loadResult.MissingFiles.Count > 0)
            {
                var stringBuilder = new StringBuilder();
                if (!string.IsNullOrEmpty(loadResult.ErrorMessage))
                {
                    stringBuilder.AppendLine(loadResult.ErrorMessage);
                }
                foreach (var missingFile in loadResult.MissingFiles)
                {
                    stringBuilder.AppendLine($"Missing file: {missingFile}");
                }
                throw new Exception(stringBuilder.ToString());
            }

            var localizationFiles = gumProject?.LocalizationFiles;
            if (localizationFiles != null && localizationFiles.Count > 0)
            {
                var projectDirectory = FileManager.GetDirectory(gumProject!.FullFileName);
                var localizationService = CustomSetPropertyOnRenderable.LocalizationService;

                var resolvedPaths = new List<string>();
                foreach (var relative in localizationFiles)
                {
                    if (!string.IsNullOrEmpty(relative))
                    {
                        resolvedPaths.Add(projectDirectory + relative);
                    }
                }

                // Policy mirrors the tool's FileCommands.LoadLocalizationFile:
                //   0 paths -> no-op
                //   1 path  -> dispatch by extension (single-file overloads)
                //   2+ paths -> require all .resx; call the multi-file RESX overload.
                //              Mixed CSV+RESX or multi-CSV is rejected because the
                //              runtime LocalizationService has no merge API for them.
                if (resolvedPaths.Count == 1 && localizationService != null)
                {
                    var fileName = resolvedPaths[0];
                    var extension = FileManager.GetExtension(fileName);

                    if (string.Equals(extension, "resx", StringComparison.OrdinalIgnoreCase))
                    {
                        // RESX satellite discovery requires enumerating the directory
                        // (e.g. Strings.es.resx alongside Strings.resx). On desktop platforms
                        // the path-based overload handles this via Directory.GetFiles.
                        // Bundled-content platforms (Android/iOS/TitleContainer) cannot
                        // enumerate sibling files from a stream, so this auto-load path
                        // assumes real filesystem access - matching the existing CSV behavior.
                        localizationService.AddResxDatabase(fileName);
                    }
                    else
                    {
                        using var stream = FileManager.GetStreamForFile(fileName);
                        localizationService.AddCsvDatabase(stream);
                    }
                }
                else if (resolvedPaths.Count > 1 && localizationService != null)
                {
                    var allResx = true;
                    foreach (var path in resolvedPaths)
                    {
                        if (!string.Equals(FileManager.GetExtension(path), "resx", StringComparison.OrdinalIgnoreCase))
                        {
                            allResx = false;
                            break;
                        }
                    }

                    if (!allResx)
                    {
                        loadResult.Warnings.Add(
                            "Localization: multiple files configured but not all are .resx. " +
                            "Mixed CSV/RESX and multi-CSV loading are not supported. Loading was skipped.");
                    }
                    else
                    {
                        var existingPaths = new List<string>();
                        foreach (var path in resolvedPaths)
                        {
                            if (System.IO.File.Exists(path))
                            {
                                existingPaths.Add(path);
                            }
                            else
                            {
                                loadResult.Warnings.Add($"Localization: file not found, skipping: {path}");
                            }
                        }

                        if (existingPaths.Count > 0)
                        {
                            localizationService.AddResxDatabase(
                                existingPaths,
                                onWarning: message => loadResult.Warnings.Add("Localization warning: " + message));
                        }
                    }
                }
            }

            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absoluteFile = gumProjectFile;
            if (FileManager.IsRelative(absoluteFile))
            {
                absoluteFile = FileManager.MakeAbsolute(gumProjectFile);
            }

            var gumDirectory = FileManager.GetDirectory(absoluteFile);

            FileManager.RelativeDirectory = gumDirectory;

            ApplyStandardElementDefaults(gumProject);
        }

        return gumProject;
    }

    private void ApplyStandardElementDefaults(GumProjectSave gumProject)
    {
        var current = gumProject.StandardElements.Find(item => item.Name == "ColoredRectangle");
        ColoredRectangleRuntime.DefaultWidth = GetFloat("Width");
        ColoredRectangleRuntime.DefaultHeight = GetFloat("Height");

        current = gumProject.StandardElements.Find(item => item.Name == "NineSlice");

        float GetFloat(string variableName) => current?.DefaultState.GetValueOrDefault<float>(variableName) ?? 0;
    }

#if XNALIKE
    // Originally added so codegen-emitted user types could self-register. Module initializers
    // replaced that path for newer Gum (https://github.com/vchelaru/Gum/issues/275), but we
    // still need a reflection-based hook for two reasons:
    //   1. Older projects emit a static "RegisterRuntimeType" (singular) in the entry assembly.
    //   2. Mono/WASM (Blazor) does not fire [ModuleInitializer] until a type in the module is
    //      touched. Extension packages like Gum.Shapes.KNI expose a public static
    //      "RegisterRuntimeTypes" (plural) we can call directly to force registration before
    //      .gumx load. RegisterRuntimeTypes is idempotent (guarded), so calling it on top of an
    //      already-fired ModuleInitializer is a no-op.
    private void RegisterRuntimeTypesThroughReflection()
    {
        // (1) Legacy entry-assembly hook (singular method name).
        Assembly? executingAssembly = Assembly.GetEntryAssembly();
        var types = executingAssembly?.GetTypes();
        if (types != null)
        {
            foreach (Type type in types)
            {
                var method = type.GetMethod("RegisterRuntimeType", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, null);
            }
        }

        // (2) Extension-package hook (plural method name) across all loaded assemblies. This is
        // what unblocks Apos shapes on Blazor/WASM.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            Type[] assemblyTypes;
            try { assemblyTypes = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { assemblyTypes = ex.Types.Where(t => t != null).ToArray()!; }
            catch { continue; }

            foreach (var type in assemblyTypes)
            {
                if (type == null) continue;
                var method = type.GetMethod("RegisterRuntimeTypes", BindingFlags.Static | BindingFlags.Public);
                if (method != null && method.GetParameters().Length == 0)
                {
                    try { method.Invoke(null, null); }
                    catch { /* a misbehaving extension shouldn't break Gum init */ }
                }
            }
        }
    }
#endif
    #endregion

    #region Uninitialize

    /// <summary>
    /// Tears down this GumService instance, releasing GPU resources, clearing registrations,
    /// and resetting all static state so that Initialize can be called again (e.g., between
    /// test runs or after a scene transition that requires full teardown).
    /// </summary>
    public void Uninitialize()
    {
#if !IOS && !ANDROID
        _hotReloadManager?.Stop();
        _hotReloadManager = null;
#endif

        DeferredQueue.Clear();

        InteractiveGue.CurrentInputReceiver = null;

        Root.Children.Clear();
        Root.RemoveFromManagers();

        if (FrameworkElement.PopupRoot != null)
        {
            FrameworkElement.PopupRoot.Children.Clear();
            FrameworkElement.PopupRoot.RemoveFromManagers();
            FrameworkElement.PopupRoot = null;
        }

        if (FrameworkElement.ModalRoot != null)
        {
            FrameworkElement.ModalRoot.Children.Clear();
            FrameworkElement.ModalRoot.RemoveFromManagers();
            FrameworkElement.ModalRoot = null;
        }

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.MainCursor = null;
        FrameworkElement.MainKeyboard = null;

        FormsUtilities.Uninitialize();

        ElementSaveExtensions.ClearRegistrations();

        FrameworkElement.DefaultFormsTemplates.Clear();
        FrameworkElement.DefaultFormsComponents.Clear();

        ObjectFinder.Self.GumProjectSave = null;

        LoaderManager.Self.DisposeAndClear();

#if XNALIKE
        // RenderableRegistry holds static per-capability factories. Clearing here
        // mirrors how Uninitialize treats the other extension points
        // (ElementSaveExtensions.ClearRegistrations, Text.Customizations.Clear,
        // FrameworkElement.DefaultFormsTemplates.Clear, etc.) so a subsequent
        // Initialize starts from a known empty state. Optional packages are
        // expected to expose a static RegisterRuntimeTypes method — Initialize's
        // reflection scan re-invokes it each cycle, so their registrations come
        // back. Packages that register only via [ModuleInitializer] won't, by
        // design — that's a known load-order contract gap tracked in issue #2761.
        RenderableRegistry.Reset();

        Text.Customizations.Clear();
        Text.ContextCustomizations.Clear();
        Text.DefaultBitmapFont = null;
        Text.DefaultFont = null;

        if (Sprite.InvalidTexture != null)
        {
            Sprite.InvalidTexture.Dispose();
            Sprite.InvalidTexture = null;
        }

        if (_systemManagers != null)
        {
            _systemManagers.Renderer.Uninitialize();
        }

        Gum.Forms.DefaultVisuals.Styling.ActiveStyle = null;
        Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null;
#endif

        GraphicalUiElement.SetPropertyOnRenderable = null!;
        GraphicalUiElement.UpdateFontFromProperties = null;
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = null;
        GraphicalUiElement.UpdateFormsStateAction = null;
        GraphicalUiElement.AddRenderableToManagers = null;
        GraphicalUiElement.RemoveRenderableFromManagers = null;

        GraphicalUiElement.CanvasWidth = 0;
        GraphicalUiElement.CanvasHeight = 0;

        _zoomReferenceWidth = null;
        _zoomReferenceHeight = null;
        _fitPolicy = FitPolicy.None;
        _lastSeenWindowWidth = 0;
        _lastSeenWindowHeight = 0;

        SystemManagers.Default = null;
        ISystemManagers.Default = null;
        IGumService.Default = null;

        // Only reset RelativeDirectory if a project was loaded (it gets set to the project directory).
        // Reset to the default value expected before initialization.
        FileManager.RelativeDirectory = "Content/";

        IsInitialized = false;

        _systemManagers = null;
#if XNALIKE
        _game = null;
#endif

        _default = null;
    }

    #endregion

    #region Update

#if XNALIKE
    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime, FrameworkElement root) => Update(gameTime, root.Visual);
    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime) => Update(gameTime);
    [Obsolete("Use the version which does not take a Game")]
    public void Update(Game game, GameTime gameTime, GraphicalUiElement root) => Update(gameTime, root);
    [Obsolete("Use the version of this method which does not take a Game")]
    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots) => Update(gameTime, roots);
#endif


#if XNALIKE
    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen, 
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
#else
    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen, 
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The total number of seconds passed since the game has started.</param>
#endif
    public void Update(GameTime gameTime)
    {
        PollWindowSizeAndApplyFit();

        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(this.Root);

        Update(gameTime, this.Root);
    }
    List<GraphicalUiElement> roots = new List<GraphicalUiElement>();
    public void Update(GameTime totalGameTime, GraphicalUiElement root)
    {
        roots.Clear();
        roots.Add(root);

        Update(totalGameTime, roots);
    }


    public void Update(GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    { 
#if XNALIKE
        var difference = gameTime.ElapsedGameTime.TotalSeconds;
#else
        var difference = gameTime - GameTime;
#endif

#if !FRB
        _syncContext?.Update();
#endif
        DeferredQueue.ProcessPending();
#if !IOS && !ANDROID
        _hotReloadManager?.Update(roots);
#endif
        GameTime = gameTime;
#if !XNALIKE
        _hasReceivedUpdate = true;
#endif
#if XNALIKE
        FormsUtilities.Update(_game, gameTime, roots);
#else
        FormsUtilities.Update(gameTime, roots);
#endif
        // SystemManagers.Activity (as of Sept 13, 2025) only
        // performs Sprite animation internally. This is not a
        // critical system, but unit tests cannot initialize a SystemManagers
        // because these require a graphics device. Therefore, we can tolerate
        // a null SystemManagers to simplify unit tests.
#if XNALIKE
        _systemManagers?.Activity(gameTime.TotalGameTime.TotalSeconds);
#endif
        foreach (var item in roots)
        {
            item.AnimateSelf(difference);
        }
    }

    #endregion

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

#if RAYLIB
    /// <summary>
    /// Draws Gum's UI under the supplied raylib <see cref="Camera2D"/>. Copies the
    /// camera's <c>Target</c> and <c>Zoom</c> onto Gum's internal camera before drawing,
    /// so the UI renders with the same transform other content drawn under that
    /// <c>Camera2D</c> uses. This overwrites any previously-configured
    /// <c>SystemManagers.Default.Renderer.Camera.X/Y/Zoom</c> for the frame.
    ///
    /// Note: <c>Camera2D.Offset</c> and <c>Camera2D.Rotation</c> are intentionally NOT
    /// copied. Gum's render path derives offset from <see cref="CameraCenterOnScreen"/>
    /// on the camera; set that separately if you need non-center placement. Rotation is
    /// not modeled by Gum's camera and is ignored.
    ///
    /// A MonoGame/XNA <c>Draw(Matrix)</c> equivalent is not yet exposed — that path
    /// needs cross-platform validation work (see issue #2846 discussion). The underlying
    /// <c>Camera.SetFromMatrix</c> primitive exists and is unit-tested for when we add it.
    /// </summary>
    public void Draw(Camera2D camera)
    {
        Camera renderCamera = SystemManagers.Default.Renderer.Camera;
        renderCamera.X = camera.Target.X;
        renderCamera.Y = camera.Target.Y;
        renderCamera.Zoom = camera.Zoom;
        Draw();
    }
#endif
}

#region GraphicalUiElementExtensionMethods Class
public static class GraphicalUiElementExtensionMethods
{
    /// <summary>
    /// Adds this element as a child of the GumService root container, making it a top-level
    /// element that will be rendered and receive layout updates. This is the recommended way
    /// to display a root-level element — prefer this over the obsolete <c>AddToManagers</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if GumService has not been initialized.
    /// </exception>
    public static void AddToRoot(this GraphicalUiElement element)
    {
        if(GumService.Default.IsInitialized == false)
        {
            throw new InvalidOperationException("Cannot call AddToRoot because GumService.Default " +
                "is not initialized - did you remember to initialize Gum first (GumUI.Initialize)?");
        }
        GumService.Default.Root.Children.Add(element);
    }

    /// <summary>
    /// Removes this element from its parent, effectively removing it from the visual tree.
    /// This reverses a previous <see cref="AddToRoot(GraphicalUiElement)"/> call.
    /// </summary>
    public static void RemoveFromRoot(this GraphicalUiElement element)
    {
        element.Parent = null;
    }

    /// <summary>
    /// Parents the supplied Forms control under this <see cref="GraphicalUiElement"/>. Used by
    /// MonoGameForms-output codegen — the generated <c>AssignParents()</c> body routinely calls
    /// <c>someRuntime.AddChild(someFormsControl)</c> to attach a Forms child to a runtime visual,
    /// and resolves it through this extension because the canonical
    /// <see cref="Gum.Forms.Controls.FrameworkElementExt.AddChild"/> lives in <c>Gum.Forms.Controls</c>
    /// — a namespace generated code does not import (to avoid name collisions with user-authored
    /// components that share names with built-in Forms types: <c>Label</c>, <c>ListBox</c>, etc.).
    /// </summary>
    /// <remarks>
    /// Hand-written code that imports both <c>MonoGameGum</c> and <c>Gum.Forms.Controls</c> will
    /// see this overload and the canonical <see cref="Gum.Forms.Controls.FrameworkElementExt.AddChild"/>
    /// as ambiguous (CS0121) — drop <c>using MonoGameGum;</c> or fully-qualify the call site in
    /// that situation. See the 2026 May upgrade doc.
    /// </remarks>
    public static void AddChild(this GraphicalUiElement element, Gum.Forms.Controls.FrameworkElement child) =>
        Gum.Forms.Controls.FrameworkElementExt.AddChild(element, child);
}

#endregion

public static class ElementSaveExtensionMethods
{
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        systemManagers = systemManagers ?? SystemManagers.Default;
        return elementSave.ToGraphicalUiElement(systemManagers, addToManagers: false);
    }
}
