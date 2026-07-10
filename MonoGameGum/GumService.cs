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
using System.Collections.Specialized;
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
using Gum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif RAYLIB
using Gum.GueDeriving;
using Gum.Input;
using Gum.Renderables;
using GameTime = double;
using Raylib_cs;
using RaylibGum.Renderables;
#endif

// The platform-agnostic home for GumService (issue #3119). The legacy
// MonoGameGum.GumService / RaylibGum.GumService names live on as permanent
// [Obsolete] subclass shims in GumServiceCompat.cs.
namespace Gum;

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
#else
        throw new NotSupportedException(
            $"{nameof(GumService)}.Initialize() has no implementation for this backend. " +
            "A new backend must add an explicit XNALIKE/RAYLIB-style arm here rather than " +
            "relying on this fallback.");
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
#else
        throw new NotSupportedException(
            $"{nameof(GumService)}.Initialize(string) has no implementation for this backend. " +
            "A new backend must add an explicit XNALIKE/RAYLIB-style arm here rather than " +
            "relying on this fallback.");
#endif
    }

    #region Default
    static GumService _default = default!;

#pragma warning disable CS0618 // Type or member is obsolete — Default intentionally returns the
    // legacy-named subclass shim so existing code typed against it keeps compiling (soft migration,
    // issue #3119). Because static members are inherited, Gum.GumService.Default and the legacy
    // namespace's GumService.Default are the same single declaration and the same singleton.
    // This switch is intentionally left closed (no #else): a third backend must add its own
    // #elif arm here, with its own back-compat shim subclass, rather than assume a missing
    // #else is an oversight — there is no sensible default subclass to fall back to.
#if XNALIKE
    /// <summary>
    /// Gets the default instance of the GumService class.
    /// </summary>
    /// <remarks>This property provides a lazily initialized, shared GumService instance for general use. Use
    /// this instance when a custom configuration is not required. The declared and runtime type is the
    /// <see cref="MonoGameGum.GumService"/> back-compat subclass so legacy declarations keep compiling.</remarks>
    public static MonoGameGum.GumService Default =>
        (MonoGameGum.GumService)(_default ??= new MonoGameGum.GumService());
#elif RAYLIB
    /// <summary>
    /// Gets the default instance of the GumService class.
    /// </summary>
    /// <remarks>This property provides a lazily initialized, shared GumService instance for general use. Use
    /// this instance when a custom configuration is not required. The declared and runtime type is the
    /// <see cref="RaylibGum.GumService"/> back-compat subclass so legacy declarations keep compiling.</remarks>
    public static RaylibGum.GumService Default =>
        (RaylibGum.GumService)(_default ??= new RaylibGum.GumService());
#endif
#pragma warning restore CS0618

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

    /// <inheritdoc/>
    IRenderable IGumService.CreateSpriteRenderable() => Sprite.CreateForCurrentPlatform();

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor()
    {
#if XNALIKE
        // MonoGame/KNI/FNA bake the Game (for its GameWindow) into the cursor for mobile touch-offset.
        return Cursor.CreateForCurrentPlatform(_game);
#elif RAYLIB
        return Cursor.CreateForCurrentPlatform();
#endif
    }

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard()
    {
#if XNALIKE
        return Keyboard.CreateForCurrentPlatform(_game);
#elif RAYLIB
        return Keyboard.CreateForCurrentPlatform();
#endif
    }

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

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
        // BackBufferWidth/Height is always physical pixels - XNA has no separate logical/DPI-scaled size.
        var pp = Game.GraphicsDevice.PresentationParameters;
        return (pp.BackBufferWidth, pp.BackBufferHeight);
#elif RAYLIB
        // GetRenderWidth/Height (physical framebuffer pixels) rather than GetScreenWidth/Height
        // (logical/DPI-unaware size) to match XNALIKE's physical-pixel convention above (#3572).
        return (Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
#endif
    }

    public ContentLoader? ContentLoader => LoaderManager.Self.ContentLoader as ContentLoader;

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

    // Extracts textures the serializer captured but could not resolve to a file path -- embedded or
    // runtime-generated Texture2Ds (notably the Forms default visuals' shared UISpriteSheet) whose Name is
    // unset, so the snapshot has valid texture coordinates but no file to slice. Saves each unique texture
    // to a PNG next to the project and writes the relative path into its placeholder SourceFile variable so
    // the slices render in the tool. The actual Texture2D.SaveAsPng is XNALIKE-only; on other backends the
    // textures stay unresolved (no regression -- they were blank before this existed).
    private static void ExtractUnresolvedTextures(IRuntimeSnapshotSerializer serializer, string snapshotDirectory)
    {
#if XNALIKE
        if (serializer.UnresolvedTextureReferences.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(snapshotDirectory);

        FillUnresolvedTextureSourceFiles(serializer.UnresolvedTextureReferences, (texture, relativePath) =>
        {
            if (texture is not Texture2D texture2D)
            {
                return false;
            }
            try
            {
                using FileStream stream = File.Create(Path.Combine(snapshotDirectory, relativePath));
                texture2D.SaveAsPng(stream, texture2D.Width, texture2D.Height);
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Snapshot: failed to save embedded texture: {e.Message}");
                return false;
            }
        });
#endif
    }

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

    /// <summary>
    /// The <see cref="ProjectResolution"/> produced when the current project was loaded, or
    /// <c>null</c> if no project file has been loaded. Carries the project's
    /// <see cref="IGumFileProvider"/> — the canonical seam runtime code uses to read project
    /// content (e.g. <see cref="LoadAnimations"/> enumerates animation files through it). Cleared
    /// by <see cref="Uninitialize"/>.
    /// </summary>
    public ProjectResolution? CurrentProjectResolution { get; private set; }

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
        // NativeTextInput is the OS-provided *modal* text-entry dialog (the iOS soft-keyboard
        // popup / console prompt), wrapping XNA's KeyboardInput.Show. It is intentionally
        // MonoGame/KNI only. raylib, FNA, and Sokol are desktop-only (or don't ship the API),
        // so they deliberately leave this null and rely on inline typing via each Keyboard's
        // GetStringTyped instead — this is not an unfinished raylib port (see issue #3432).
        // The modal dialog only matters on true touch/console targets, which raylib-cs has no
        // natives for. See INativeTextInput and TextBoxBase.TryShowNativeKeyboard.
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

#if XNALIKE
        Renderer.TextureFilter = useLinearFiltering ? TextureFilter.Linear : TextureFilter.Point;
#elif RAYLIB
        // raylib has no global sampler state; the filter is a per-texture property applied at load
        // time, so ContentLoader reads this when creating sprite textures (see ContentLoader.cs).
        ContentLoader.DefaultTextureFilter = useLinearFiltering
            ? Raylib_cs.TextureFilter.Bilinear
            : Raylib_cs.TextureFilter.Point;
#endif
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
#elif RAYLIB
        // Raylib's Text has no Customizations/ContextCustomizations/DefaultBitmapFont
        // equivalents to the XNALIKE Text above - only DefaultFont. default(Font) has
        // BaseSize == 0, the constructor's documented uninitialized-Font sentinel (#3557).
        Text.DefaultFont = default;
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
