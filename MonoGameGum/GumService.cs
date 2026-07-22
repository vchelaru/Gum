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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;
using Gum.Forms;
using Gum.Threading;
using Gum.Localization;

using Gum.GueDeriving;
using Gum.Input;

// A few types this shared file references live in different namespaces per backend — Cursor
// (MonoGameGum.Input on XNALIKE vs Gum.Input on Raylib/Silk) and CustomSetPropertyOnRenderable
// (Gum.Wireframe on XNALIKE, RaylibGum.Renderables on Raylib, SkiaGum on Silk). Only the using set
// switches per platform; the method bodies stay backend-agnostic. This mirrors the per-platform using
// aliasing used across the unified GueDeriving runtimes (issue #3608). The renderable Sprite type also
// diverges, but its only use (CreateSpriteRenderable) lives in the per-platform partials, so no Sprite
// using is needed here.
#if MONOGAME || KNI || FNA
using MonoGameGum.Input;
#elif RAYLIB
using Gum.Renderables;
using RaylibGum.Renderables;
#elif SILK
using SkiaGum;
#endif

// The platform-agnostic home for GumService (issue #3119). The legacy
// MonoGameGum.GumService / RaylibGum.GumService names live on as permanent
// [Obsolete] subclass shims in GumServiceCompat.cs. Platform-divergent members live in the
// GumService.XnaLike.cs / GumService.Raylib.cs partials (issue #3608).
namespace Gum;

public partial class GumService : IGumService
{
    IRenderer IGumService.Renderer => this.SystemManagers.Renderer;
    ICursor IGumService.Cursor => this.Cursor;

    #region Default
    // The lazily-initialized singleton backing field. The Default property returns the
    // platform-specific back-compat subclass shim (MonoGameGum.GumService / RaylibGum.GumService)
    // and lives in the per-platform partials, as do the GameTime property and the explicit
    // IGumService.Initialize / IGumService.GameTime members (issue #3608).
    static GumService _default = default!;
    #endregion

    /// <summary>
    /// Gets the default cursor, which represents either mouse or touch screen depending on hardware capabilities.
    /// </summary>
    // 'as' (not a hard cast) preserves the prior null-on-mismatch behavior: tests may install a mock
    // ICursor/IInputReceiverKeyboard via FormsUtilities.SetCursor, and this forwarder returned null for
    // a non-Cursor before FormsUtilities.Cursor changed from 'Cursor?' to 'ICursor'. The '!' only
    // suppresses the nullable-return warning (runtime no-op) and does not reintroduce a throw.
    public Cursor Cursor => (FormsUtilities.Cursor as Cursor)!;

    /// <summary>
    /// Gets the default keyboard.
    /// </summary>
    public Keyboard Keyboard => (FormsUtilities.Keyboard as Keyboard)!;

    /// <summary>
    /// Gets the service used to provide localized strings and resources for the application.
    /// </summary>
    public ILocalizationService LocalizationService => CustomSetPropertyOnRenderable.LocalizationService!;

    /// <summary>
    /// Gets the collection of connected gamepads available to the application.
    /// </summary>
    public Gum.Input.GamePad[] Gamepads => Gum.Forms.FormsUtilities.Gamepads;

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

    // CreateSpriteRenderable / CreateCursor / CreateKeyboard / ApplyGamePadState are platform-specific
    // factories and live in the per-platform partials (issue #3608): the renderable Sprite type differs
    // per backend and only XNALIKE/Raylib expose the Sprite.CreateForCurrentPlatform factory (Skia news
    // one up directly), XNALIKE bakes the Game into the cursor/keyboard, and GamePadDriver is a
    // per-family type.

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
    /// <c>Update</c> whenever the window size changes. Call once at
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
    /// <c>Update</c> whenever the window size changes. Call once at
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

    // GetWindowSize() is platform-specific (XNA back-buffer vs raylib render size) and lives in the
    // per-platform partials (issue #3608).

    // The ContentLoader convenience property casts LoaderManager.Self.ContentLoader to the concrete
    // ContentLoader type, which exists on XNALIKE/Raylib but not on Skia (Silk), so it lives in the
    // per-platform partials (issue #3608).

    InteractiveGue _root = null!;
    bool _rootOwnedByGumService;
    /// <summary>
    /// The root container that owns top-level elements added via <c>AddToRoot</c>. Can be
    /// reassigned to a custom container (for example, one an embedding host already draws
    /// through its own render pass) so that <c>AddToRoot</c> and the host's own root become
    /// the same object. Reassigning moves the focus-cleanup subscription (see
    /// <see cref="Gum.Forms.FormsUtilities.HandleRootCollectionChanged"/>) from the old root
    /// onto the new one. Unlike <see cref="Gum.Forms.Controls.FrameworkElement.PopupRoot"/> and
    /// <see cref="Gum.Forms.Controls.FrameworkElement.ModalRoot"/>, <see cref="Uninitialize"/>
    /// does not clear or detach a reassigned Root — it only tears down the default Root that
    /// GumService itself created, since a host-supplied Root is the host's own object.
    /// </summary>
    public InteractiveGue Root
    {
        get => _root;
        set
        {
            if (_root == value)
            {
                return;
            }

            if (_root != null)
            {
                _root.Children.CollectionChanged -= HandleRootChildrenCollectionChanged;
            }

            _root = value;
            _rootOwnedByGumService = false;
            _root.Children.CollectionChanged += HandleRootChildrenCollectionChanged;
        }
    }

    void HandleRootChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root, e);

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
        // Resolve to an absolute path up front. A bare/relative file name (e.g. "MyTestSnapshot.gumx", as
        // the samples pass) would otherwise make Path.GetDirectoryName below return "", skipping the whole
        // directory block that extracts embedded textures and copies referenced files -- leaving those
        // textures unresolved (blank in the tool). project.Save resolves relative paths against the current
        // directory anyway, so this changes only the directory computation, not where the project is written.
        filePath = Path.GetFullPath(filePath);

        // A code-only game may never have triggered standards population; ensure the catalog exists
        // before reading it (as the serializer's baseline) and writing it (as the project's standards).
        if (StandardElementsManager.Self.DefaultStates == null)
        {
            StandardElementsManager.Self.Initialize();
        }

        string screenName = Path.GetFileNameWithoutExtension(filePath);

        // Non-null here: the guard above initializes the catalog when it was missing. The baseline provider
        // lets the serializer collapse Forms-control subtrees (Button, CheckBox, ...) into synthesized
        // components by diffing each against the control type's pristine default-template visual.
        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates!,
            type => FrameworkElement.GetGraphicalUiElementForFrameworkElement(type));
        ScreenSave screen = serializer.CreateScreenSave(Root, screenName, shake);

        GumProjectSave project = new();
        // A snapshot seeds the full default standards (the current native variable surface), so it
        // genuinely uses native-version features. Stamp NativeVersion explicitly -- the ctor default is
        // the older fallback for legacy files lacking a <Version>, which would make the tool's
        // variable-grid version gate hide the newer-only (v3 shape) variables. Matches the new-project
        // factories (ProjectManager.CreateNewProject, ProjectCreator.Create).
        project.Version = GumProjectSave.NativeVersion;
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

        // Forms-control subtrees collapse into reusable components (one per control type) plus thin instances.
        foreach (ComponentSave component in serializer.SynthesizedComponents)
        {
            project.Components.Add(component);
            project.ComponentReferences.Add(
                new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }

        EnsureReferencedStandardsExist(project, screen, serializer.SynthesizedComponents);

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.StandardSubfolder));
            if (serializer.SynthesizedComponents.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(directory, ElementReference.ComponentSubfolder));
            }

            // Save embedded/generated textures (e.g. the Forms default visuals' shared sheet) to files and
            // fill their SourceFile paths BEFORE Save, so the written XML carries the resolved paths.
            ExtractUnresolvedTextures(serializer, directory);
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
    // Synthesized components carry instances too, so their base types are checked alongside the screen's.
    private static void EnsureReferencedStandardsExist(GumProjectSave project, ScreenSave screen,
        IReadOnlyList<ComponentSave> components)
    {
        HashSet<string> existing = new(project.StandardElements.Select(standard => standard.Name));

        EnsureStandardsForInstances(project, screen.Instances, existing);
        foreach (ComponentSave component in components)
        {
            EnsureStandardsForInstances(project, component.Instances, existing);
        }
    }

    private static void EnsureStandardsForInstances(GumProjectSave project, IEnumerable<InstanceSave> instances,
        HashSet<string> existing)
    {
        foreach (InstanceSave instance in instances)
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
        // Textures extracted from embedded/generated sources are already written next to the project by
        // ExtractUnresolvedTextures, yet their now-filled SourceFile paths also surface in GetReferencedFiles.
        // Skip them here: they don't exist under the content directory (so the copy would just log a miss),
        // and a coincidentally same-named content file must not clobber the extracted PNG.
        HashSet<string> extractedPaths = new();
        foreach (UnresolvedTextureReference reference in serializer.UnresolvedTextureReferences)
        {
            if (reference.SourceFileVariable.Value is string extractedPath && !string.IsNullOrEmpty(extractedPath))
            {
                extractedPaths.Add(extractedPath);
            }
        }

        foreach (string referencedPath in serializer.GetReferencedFiles(screen))
        {
            if (extractedPaths.Contains(referencedPath))
            {
                continue;
            }

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

    // Extracts embedded/generated textures the serializer could not resolve to a file path, saving
    // each next to the project so the tool can slice it. XNALIKE-only (needs Texture2D.SaveAsPng);
    // the seam is elided on other backends -- they were blank before this existed. Implemented in
    // GumService.XnaLike.cs (issue #3608).
    static partial void ExtractUnresolvedTextures(IRuntimeSnapshotSerializer serializer, string snapshotDirectory);

    // Pure orchestration over the serializer's unresolved textures: dedupe by texture instance, give each a
    // relative file name, persist it via the supplied saver, and write the resulting path into the placeholder
    // SourceFile variable. The saver seam keeps the GPU-bound Texture2D.SaveAsPng out of this method so the
    // dedup/path-fill logic is testable headlessly. A texture the saver declines (returns false) is left
    // unresolved -- its placeholder keeps its null value, rendering blank rather than dangling on a bad path.
    internal static void FillUnresolvedTextureSourceFiles(
        IReadOnlyList<UnresolvedTextureReference> references, Func<object, string, bool> trySaveTexture)
    {
        // Dedupe by texture instance (not value): the one shared sheet -> one file, many filled placeholders.
        Dictionary<object, string> savedRelativePaths = new(ReferenceEqualityComparer.Instance);
        int index = 0;
        foreach (UnresolvedTextureReference reference in references)
        {
            if (!savedRelativePaths.TryGetValue(reference.Texture, out string? relativePath))
            {
                string candidate = $"EmbeddedTexture{index}.png";
                if (!trySaveTexture(reference.Texture, candidate))
                {
                    continue;
                }
                relativePath = candidate;
                index++;
                savedRelativePaths[reference.Texture] = relativePath;
            }
            reference.SourceFileVariable.Value = relativePath;
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

    // Off by default; opt in via UseSingleThreadedAsync. Formerly #if !FRB-guarded, but GumService
    // is not compiled into FRB (it is absent from GumCoreShared.projitems), so the guard was
    // always-true dead code and was removed for clarity (issue #3608).
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


    /// <summary>
    /// Gets whether GumService has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Result of the most recent project load performed by <c>Initialize</c>
    /// (or platform-equivalent overloads). Null if no project file has been loaded.
    /// Inspect <see cref="GumLoadResult.Warnings"/> for non-fatal issues such as
    /// localization string-ID collisions across multi-file RESX projects or
    /// misconfigured mixed CSV/RESX localization lists.
    /// </summary>
    public GumLoadResult? LastLoadResult { get; private set; }

    /// <summary>
    /// The <see cref="ProjectResolution"/> produced when the current project was loaded, or
    /// <c>null</c> if no project file has been loaded. Carries the project's
    /// <see cref="IGumFileProvider"/> — the canonical seam runtime code uses to read project
    /// content (e.g. <see cref="LoadAnimations"/> enumerates animation files through it). Cleared
    /// by <see cref="Uninitialize"/>.
    /// </summary>
    public ProjectResolution? CurrentProjectResolution { get; private set; }

    // The XNA Game host (_game / Game) is XNALIKE-only and lives in GumService.XnaLike.cs (issue #3608).

    #region Initialize

    /// <summary>
    /// Instantiates a new GumService. This is usually not called directly, since
    /// the Default is the most common way to access GumService.
    /// </summary>
    public GumService()
    {
        Root = new ContainerRuntime();
        _rootOwnedByGumService = true;
        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";
        Root.HasEvents = false;

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
        // NativeTextInput (the OS modal text-entry dialog, wrapping XNA's KeyboardInput.Show) is
        // MonoGame/KNI only; raylib/FNA/Sokol are desktop-only or don't ship the API, so they leave
        // it null and type inline via each Keyboard's GetStringTyped (issue #3432). The seam is
        // implemented in GumService.XnaLike.cs and elided elsewhere (issue #3608).
        AssignNativeTextInput();
        // Clipboard is the MonoGameGumClipboard on XNALIKE/Raylib; the Skia host (Silk) has no
        // clipboard implementation and leaves it null. The concrete type isn't even linked into
        // SilkNetGum, so the assignment lives in the AssignClipboard seam (implemented on XNALIKE/Raylib,
        // elided on Silk) rather than a shared #if (issue #3608).
        AssignClipboard();

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

    // The public Initialize(...) overloads are platform-specific (Game-based on XNALIKE, canvas/no-arg
    // on Raylib). Each stores its platform init args, runs the platform SystemManagers/renderer/forms
    // bootstrap in its own InitializeInternal, and hands off to the shared FinishInitialize tail. They
    // live in the per-platform partials (issue #3608).

    /// <summary>
    /// Loads animations for all elements in the project by enumerating the project's
    /// <c>*Animations.ganx</c> files through the loaded project's <see cref="IGumFileProvider"/>.
    /// </summary>
    /// <remarks>
    /// This enumerates once instead of probing <see cref="FileManager.FileExists"/> per element.
    /// In bundle mode the enumeration is an in-memory dictionary scan — zero I/O, and crucially
    /// zero cosmetic 404s on browser/streaming platforms (Blazor WASM), where every per-element
    /// probe was previously a guaranteed-miss HTTP request. In loose mode on a real filesystem it
    /// is a single directory walk; loose mode on a streaming platform cannot enumerate a directory
    /// over HTTP, so no animations load there — package the project as a <c>.gumpkg</c> to ship
    /// animations to those platforms.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a Gum project hasn't been loaded first, or if no project file provider is available
    /// (e.g. the project was injected directly rather than loaded via <c>Initialize(...gumProjectFile...)</c>).
    /// </exception>
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

        ProjectResolution? resolution = CurrentProjectResolution;
        if (resolution == null)
        {
            throw new InvalidOperationException(
                "No project file provider is available. LoadAnimations enumerates animation files " +
                "through the provider produced when the project is loaded — load the project via " +
                "Initialize(...gumProjectFile...) before calling LoadAnimations.");
        }

        int loaded = LoadAnimationsFromProvider(project, resolution.FileProvider);

        // Loose mode relies on directory enumeration, which streaming platforms (Blazor WASM)
        // can't do over HTTP — there the enumeration silently returns nothing. Surface that once
        // so a developer who expected animations isn't left guessing. Bundle mode never has this
        // problem (the in-memory entry list is the manifest), so it stays quiet.
        if (!resolution.UsedBundle && loaded == 0)
        {
            Console.WriteLine(
                "[Gum] No animation (*Animations.ganx) files were found for this loosely-loaded project. " +
                "Loose-mode animation loading enumerates the project directory, which is unavailable on " +
                "browser/streaming platforms (e.g. Blazor WASM) — package the project as a .gumpkg to " +
                "load animations on those platforms.");
        }
    }

    /// <summary>
    /// Enumerates <c>*Animations.ganx</c> files from <paramref name="provider"/>, deserializes each,
    /// and adds the result to <paramref name="project"/>'s <see cref="GumProjectSave.ElementAnimations"/>.
    /// The element name is derived from the file's path, not from the (stale) value serialized inside
    /// the file. Returns the number of animation files loaded.
    /// </summary>
    internal static int LoadAnimationsFromProvider(GumProjectSave project, IGumFileProvider provider)
    {
        // Filename-only pattern (no '/'): GlobMatcher matches it against the file name regardless of
        // directory depth, so nested component folders (Components/Buttons/MyButtonAnimations.ganx)
        // are found. A "**/*Animations.ganx" pattern would NOT work — GlobMatcher has no recursive
        // '**' support and would only match files exactly one folder deep.
        int loaded = 0;
        foreach (string path in provider.EnumerateFiles("*Animations.ganx"))
        {
            using Stream stream = provider.OpenRead(path);
            ElementAnimationsSave animation = FileManager.XmlDeserializeFromStream<ElementAnimationsSave>(stream);
            animation.ElementName = ElementNameFromPath(path);
            project.ElementAnimations.Add(animation);
            loaded++;
        }
        return loaded;
    }

    /// <summary>
    /// Maps an animation file path back to its element name — the inverse of the
    /// <c>{categoryFolder}/{element.Name}Animations.ganx</c> convention. Strips the
    /// <c>Animations.ganx</c> suffix and any leading category folder so a nested component path like
    /// <c>Components/Buttons/MyButtonAnimations.ganx</c> resolves to <c>Buttons/MyButton</c>.
    /// </summary>
    internal static string ElementNameFromPath(string path)
    {
        const string suffix = "Animations.ganx";
        string normalized = path.Replace('\\', '/');

        string withoutSuffix = normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(0, normalized.Length - suffix.Length)
            : normalized;

        foreach (string categoryFolder in AnimationCategoryFolders)
        {
            if (withoutSuffix.StartsWith(categoryFolder, StringComparison.OrdinalIgnoreCase))
            {
                return withoutSuffix.Substring(categoryFolder.Length);
            }
        }

        return withoutSuffix;
    }

    private static readonly string[] AnimationCategoryFolders =
        { "Screens/", "Components/", "StandardElements/" };

    // Shared tail of every platform's InitializeInternal: adds Root to managers, reinserts it at the
    // bottom of the main layer, and (when a project path is given) loads the project, its
    // localization, extended standard states, texture filter, and standard-element defaults. The
    // per-platform guard + SystemManagers/renderer/forms bootstrap lives in the per-platform
    // InitializeInternal in GumService.XnaLike.cs / GumService.Raylib.cs (issue #3608).
    private GumProjectSave? FinishInitialize(string? gumProjectFile)
    {
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
            ProjectResolution projectResolution = GumBundleLoader.Resolve(gumProjectFile);
            CurrentProjectResolution = projectResolution;
            gumProject = GumProjectSave.Load(projectResolution.ResolvedGumxPath, out GumLoadResult loadResult);
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

            // A loaded project may declare Skia-only standards (Canvas/Svg/LottieAnimation) or the
            // shape standards (Arc/ColoredCircle/RoundedRectangle/Line). This runtime has no Skia
            // plugin to resolve those, so register their default states here; otherwise
            // gumProject.Initialize() throws "Could not get the default state for type ..." for a
            // declared-but-unrendered standard. #3507 removed the shape runtime's Container fallback
            // that used to mask this. Headless Gum.ProjectServices / gumcli register the same states
            // for the same reason (no Skia plugin). Idempotent, and composes with the shape runtime's
            // own resolver, so it is safe to call unconditionally on every load.
            StandardElementsManager.Self.RegisterExtendedDefaultStates();

            gumProject.Initialize();
            ApplyProjectTextureFilter(gumProject);
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

    /// <summary>
    /// Applies the project's <see cref="GumProjectSave.TextureFilter"/> setting (configured in the
    /// editor's Project Properties) to the runtime so sprites render with the same filtering the
    /// editor previewed (issue #3199). Called automatically during project load.
    /// </summary>
    /// <remarks>
    /// Precedence: this runs during <c>Initialize</c>, so a per-layer
    /// <c>Layer.IsLinearFilteringEnabled</c> (when non-null) still wins, and
    /// code that assigns <see cref="Renderer.TextureFilter"/> after <c>Initialize</c> returns
    /// overrides the project value. <c>"Linear"</c> is the string the editor stores for linear
    /// filtering; any other value (including null) maps to point filtering.
    /// </remarks>
    internal static void ApplyProjectTextureFilter(GumProjectSave gumProject)
    {
        bool useLinearFiltering = string.Equals(gumProject?.TextureFilter, "Linear", StringComparison.Ordinal);

        ApplyTextureFilterPlatform(useLinearFiltering);
    }

    private void ApplyStandardElementDefaults(GumProjectSave gumProject)
    {
        var current = gumProject.StandardElements.Find(item => item.Name == "ColoredRectangle");
        ColoredRectangleRuntime.DefaultWidth = GetFloat("Width");
        ColoredRectangleRuntime.DefaultHeight = GetFloat("Height");

        current = gumProject.StandardElements.Find(item => item.Name == "NineSlice");

        float GetFloat(string variableName) => current?.DefaultState.GetValueOrDefault<float>(variableName) ?? 0;
    }

    // RegisterRuntimeTypesThroughReflection (the codegen/module-initializer fallback registration)
    // is XNALIKE-only and lives in GumService.XnaLike.cs (issue #3608).
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

        // Only tear down Root if GumService itself created it. A reassigned Root
        // (see the Root property doc) is the host's own object — clearing its
        // children or detaching its Managers would silently corrupt state the host
        // still owns and expects to keep using.
        if (_rootOwnedByGumService)
        {
            Root.Children.Clear();
            Root.RemoveFromManagers();
        }

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
        CurrentProjectResolution = null;

        LoaderManager.Self.DisposeAndClear();

        // Platform-specific teardown: XNALIKE clears RenderableRegistry/Text/Sprite state, uninits
        // the Renderer, and nulls the Game; raylib resets Text.DefaultFont. Implemented in the
        // per-platform partials (issue #3608).
        UninitializePlatform();

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

        _default = null;
    }

    #endregion

    #region Update

    // Scratch list reused by the platform Update(GameTime, GraphicalUiElement) overloads so a single
    // root can be forwarded to the IEnumerable overload without allocating each frame.
    List<GraphicalUiElement> roots = new List<GraphicalUiElement>();

    // Platform-agnostic front of every frame's Update, run before the platform pumps Forms input:
    // drain the sync context, process deferred actions, and tick hot reload. The public
    // Update(GameTime ...) family is platform-typed (XNA GameTime object vs double seconds) and lives
    // in the per-platform partials, which call this and AnimateRoots around their own
    // FormsUtilities.Update (issue #3608).
    private void UpdatePreamble(IEnumerable<GraphicalUiElement> roots)
    {
        _syncContext?.Update();
        DeferredQueue.ProcessPending();
#if !IOS && !ANDROID
        _hotReloadManager?.Update(roots);
#endif
    }

    // Platform-agnostic tail of every frame's Update: advance AnimationChain playback on each root.
    private void AnimateRoots(double difference, IEnumerable<GraphicalUiElement> roots)
    {
        // Internal callers pass the reused List field; take an index-based fast path so we don't box
        // the List enumerator each frame that foreach over the IEnumerable parameter would (#1934).
        if (roots is IList<GraphicalUiElement> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].AnimateSelf(difference);
            }
        }
        else
        {
            foreach (var item in roots)
            {
                item.AnimateSelf(difference);
            }
        }
    }

    #endregion

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

    // Draw(Camera2D) is raylib-only and lives in GumService.Raylib.cs (issue #3608).

    // ---- Platform seams (implemented in GumService.XnaLike.cs / GumService.Raylib.cs / GumService.Silk.cs) ----

    // Assigns NativeTextInput on MonoGame/KNI (elided on FNA/Raylib/Silk), called from the constructor.
    partial void AssignNativeTextInput();

    // Assigns Clipboard to the MonoGameGumClipboard on XNALIKE/Raylib (elided on Silk), from the ctor.
    partial void AssignClipboard();

    // Per-platform teardown inside Uninitialize().
    partial void UninitializePlatform();

    // Applies the resolved point/linear filtering choice to the platform's renderer, from
    // ApplyProjectTextureFilter.
    static partial void ApplyTextureFilterPlatform(bool useLinearFiltering);
}

// AddToRoot/RemoveFromRoot and AddChild/RemoveChild are all instance methods on
// GraphicalUiElement (the latter pair added in GumCommon/Forms/GraphicalUiElement.Forms.cs),
// so gue.AddToRoot()/gue.AddChild(formsChild) work under just `using Gum;` — no
// Gum.Forms.Controls or legacy MonoGameGum/RaylibGum import needed, hence no collision with
// user components named Label/StackPanel/etc. Instance methods also win overload resolution
// over Gum.Forms.Controls.FrameworkElementExt.AddChild, so importing both namespaces is never
// ambiguous (CS0121). The AddChild extension in GumServiceCompat.cs remains only so older
// generated code (syntax versions 0-2) that imports the legacy namespace keeps compiling.
// See issues #3119 and #3226.

/// <summary>
/// Convenience extensions for creating runtime visuals from loaded project elements.
/// </summary>
public static class ElementSaveExtensionMethods
{
    /// <summary>
    /// Instantiates a GraphicalUiElement from the supplied ElementSave using the given
    /// SystemManagers, or <see cref="SystemManagers.Default"/> when omitted.
    /// </summary>
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        systemManagers = systemManagers ?? SystemManagers.Default;
        return elementSave.ToGraphicalUiElement(systemManagers, addToManagers: false);
    }
}
