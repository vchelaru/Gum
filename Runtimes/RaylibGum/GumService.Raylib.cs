// Raylib arm of the partial GumService (issue #3608). The shared ~90% lives in MonoGameGum's
// GumService.cs (file-linked into RaylibGum.csproj); this file holds only the RAYLIB-divergent
// members (Initialize signature, renderer/forms bootstrap, GameTime-typed Update family,
// texture-filter/teardown seams) plus Draw(Camera2D). It lives in the RaylibGum project so
// raylib-only code sits with the raylib runtime rather than under MonoGameGum. The #if RAYLIB
// wrap is belt-and-suspenders: only RaylibGum compiles this file, and RAYLIB is always defined there.
#if RAYLIB
using Gum.DataTypes;
using Gum.Forms;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Raylib_cs;
using RaylibGum.Renderables;
using System;
using System.Collections.Generic;
using GameTime = double;

namespace Gum;

public partial class GumService
{
    #region Default

#pragma warning disable CS0618 // Type or member is obsolete — Default intentionally returns the
    // legacy-named subclass shim so existing code typed against it keeps compiling (soft migration,
    // issue #3119). Because static members are inherited, Gum.GumService.Default and the legacy
    // namespace's GumService.Default are the same single declaration and the same singleton.
    /// <summary>
    /// Gets the default instance of the GumService class.
    /// </summary>
    /// <remarks>This property provides a lazily initialized, shared GumService instance for general use. Use
    /// this instance when a custom configuration is not required. The declared and runtime type is the
    /// <see cref="RaylibGum.GumService"/> back-compat subclass so legacy declarations keep compiling.</remarks>
    public static RaylibGum.GumService Default =>
        (RaylibGum.GumService)(_default ??= new RaylibGum.GumService());
#pragma warning restore CS0618

    #endregion

    /// <summary>
    /// The GameTime of the most recent Update call.
    /// </summary>
    public GameTime GameTime { get; private set; }

    /// <inheritdoc/>
    float? IGumService.GameTime =>
        // On Raylib, GameTime is aliased to double and starts at 0; treat the pre-Update
        // state as null by also returning null when nothing has run Update yet.
        _hasReceivedUpdate ? (float?)GameTime : null;

    private bool _hasReceivedUpdate;

    void IGumService.Initialize() => Initialize(DefaultVisualsVersion.Newest);

    void IGumService.Initialize(string gumProjectFile) => Initialize(gumProjectFile);

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor() => Cursor.CreateForCurrentPlatform();

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard() => Keyboard.CreateForCurrentPlatform();

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

    /// <inheritdoc/>
    IRenderable IGumService.CreateSpriteRenderable() => Sprite.CreateForCurrentPlatform();

    public ContentLoader? ContentLoader => LoaderManager.Self.ContentLoader as ContentLoader;

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

    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }

    // Verbatim copy of the RAYLIB live path from the pre-split InitializeInternal: the guard and
    // SystemManagers bootstrap are shared in shape but the renderer/forms init diverges, so each
    // platform owns its InitializeInternal and hands off to the shared FinishInitialize tail.
    GumProjectSave? InitializeInternal(
        string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

        this.SystemManagers = systemManagers ?? new SystemManagers();
        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
            ISystemManagers.Default = this.SystemManagers;
        }

        IGumService.Default = this;

        // SystemManagers.Initialize must come first because it assigns the
        // GraphicalUiElement.AddRenderableToManagers delegate. InitializeDefaults
        // creates PopupRoot/ModalRoot and calls AddToManagers on them — that call
        // silently no-ops if the delegate is still null, so the roots would never
        // be added to MainLayer.Renderables and would not draw.
        this.SystemManagers.Initialize();
        FormsUtilities.InitializeDefaults(systemManagers: this.SystemManagers,
            defaultVisualsVersion: defaultVisualsVersion);

        return FinishInitialize(gumProjectFile);
    }

    private (int width, int height) GetWindowSize()
    {
        // GetRenderWidth/Height (physical framebuffer pixels) rather than GetScreenWidth/Height
        // (logical/DPI-unaware size) to match XNALIKE's physical-pixel convention (#3572).
        return (Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
    }

    static partial void ApplyTextureFilterPlatform(bool useLinearFiltering)
    {
        // raylib has no global sampler state; the filter is a per-texture property applied at load
        // time, so ContentLoader reads this when creating sprite textures (see ContentLoader.cs).
        ContentLoader.DefaultTextureFilter = useLinearFiltering
            ? Raylib_cs.TextureFilter.Bilinear
            : Raylib_cs.TextureFilter.Point;
    }

    partial void UninitializePlatform()
    {
        // Raylib's Text has no Customizations/ContextCustomizations/DefaultBitmapFont
        // equivalents to the XNALIKE Text - only DefaultFont. default(Font) has
        // BaseSize == 0, the constructor's documented uninitialized-Font sentinel (#3557).
        Text.DefaultFont = default;
    }

    partial void AssignClipboard()
    {
        Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();
    }

    #region Update

    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen,
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The total number of seconds passed since the game has started.</param>
    public void Update(GameTime gameTime)
    {
        PollWindowSizeAndApplyFit();

        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(this.Root);

        Update(gameTime, this.Root);
    }

    public void Update(GameTime totalGameTime, GraphicalUiElement root)
    {
        roots.Clear();
        roots.Add(root);

        Update(totalGameTime, roots);
    }

    public void Update(GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        var difference = gameTime - GameTime;

        UpdatePreamble(roots);

        GameTime = gameTime;
        _hasReceivedUpdate = true;

        FormsUtilities.Update(gameTime, roots);

        AnimateRoots(difference, roots);
    }

    #endregion

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
}
#endif
