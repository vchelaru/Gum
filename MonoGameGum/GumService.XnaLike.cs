#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
// XNA-like arm of the partial GumService (issue #3608) — compiled for MonoGame, KNI, and FNA.
// The shared ~90% lives in GumService.cs; this file holds only the XNALIKE-divergent members
// (the Game host, Initialize(Game ...) overloads, reflection type registration, the GameTime-typed
// Update family) plus the platform seam implementations. The whole file is wrapped in #if XNALIKE
// as a safety belt so a stray glob/link where XNALIKE is not defined stays inert.
#if XNALIKE
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Input;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

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
    /// <see cref="MonoGameGum.GumService"/> back-compat subclass so legacy declarations keep compiling.</remarks>
    public static MonoGameGum.GumService Default =>
        (MonoGameGum.GumService)(_default ??= new MonoGameGum.GumService());
#pragma warning restore CS0618

    #endregion

    /// <summary>
    /// The GameTime of the most recent Update call.
    /// </summary>
    public GameTime GameTime { get; private set; }

    /// <inheritdoc/>
    float? IGumService.GameTime =>
        GameTime != null ? (float?)GameTime.TotalGameTime.TotalSeconds : null;

    void IGumService.Initialize() =>
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game) on the concrete GumService instead.");

    void IGumService.Initialize(string gumProjectFile) =>
        throw new NotSupportedException(
            "This runtime requires a Game instance. Call " +
            "GumService.Default.Initialize(Game, gumProjectFile) on the concrete GumService instead.");

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor()
    {
        // MonoGame/KNI/FNA bake the Game (for its GameWindow) into the cursor for mobile touch-offset.
        return Cursor.CreateForCurrentPlatform(_game);
    }

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard()
    {
        return Keyboard.CreateForCurrentPlatform(_game);
    }

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) =>
        GamePadDriver.Apply(gamepad, index, time);

    /// <inheritdoc/>
    IRenderable IGumService.CreateSpriteRenderable() => Sprite.CreateForCurrentPlatform();

    public ContentLoader? ContentLoader => LoaderManager.Self.ContentLoader as ContentLoader;

    private Game? _game;
    public Game Game
    {
        get => _game ?? throw new InvalidOperationException(
            "GumService has not been initialized. Call GumService.Initialize() first.");
        private set => _game = value;
    }

    #region Initialize

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

    // Verbatim copy of the XNALIKE live path from the pre-split InitializeInternal: the guard and
    // SystemManagers bootstrap are shared in shape but the renderer/forms init diverges, so each
    // platform owns its InitializeInternal and hands off to the shared FinishInitialize tail.
    GumProjectSave? InitializeInternal(
        Game? game, GraphicsDevice graphicsDevice,
        string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

        _game = game;
        RegisterRuntimeTypesThroughReflection();

        this.SystemManagers = systemManagers ?? new SystemManagers();
        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
            ISystemManagers.Default = this.SystemManagers;
        }

        IGumService.Default = this;

        this.SystemManagers.Initialize(graphicsDevice, fullInstantiation: true);

        if (game != null && ContentLoader != null && ContentLoader.XnaContentManager == null)
        {
            ContentLoader.XnaContentManager = game.Content;
        }

        FormsUtilities.InitializeDefaults(systemManagers: this.SystemManagers,
            defaultVisualsVersion: defaultVisualsVersion);

        return FinishInitialize(gumProjectFile);
    }

    // Framework/BCL assemblies never define a Gum "RegisterRuntimeType(s)" hook. Skipping them by
    // name avoids wasted scanning and, under Native AOT, avoids GetTypes()/GetMethod() throwing
    // while walking their trimmed metadata (issue #4105).
    private static readonly string[] FrameworkAssemblyNamePrefixes =
    {
        "System", "Microsoft", "mscorlib", "netstandard", "WindowsBase",
        "PresentationCore", "PresentationFramework", "Accessibility",
    };

    // Originally added so codegen-emitted user types could self-register. Module initializers
    // replaced that path for newer Gum (https://github.com/vchelaru/Gum/issues/275), but we
    // still need a reflection-based hook for two reasons:
    //   1. Older projects emit a static "RegisterRuntimeType" (singular) in the entry assembly.
    //   2. Mono/WASM (Blazor) does not fire [ModuleInitializer] until a type in the module is
    //      touched. Extension packages like Gum.Shapes.KNI expose a public static
    //      "RegisterRuntimeTypes" (plural) we can call directly to force registration before
    //      .gumx load. RegisterRuntimeTypes is idempotent (guarded), so calling it on top of an
    //      already-fired ModuleInitializer is a no-op.
    // Best-effort by design: under trimming/Native AOT the [ModuleInitializer] hooks have already
    // run, so a type this scan can no longer see was registered by other means. Every call is
    // individually guarded (see InvokeRuntimeTypeHookIfPresent), so a trimmed-away hook degrades to
    // a no-op instead of a failure.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Scanning for optional self-registration hooks; module initializers cover the trimmed case and every call is guarded.")]
    internal void RegisterRuntimeTypesThroughReflection()
    {
        // (1) Legacy entry-assembly hook (singular method name).
        Assembly? executingAssembly = Assembly.GetEntryAssembly();
        var types = executingAssembly?.GetTypes();
        if (types != null)
        {
            foreach (Type type in types)
            {
                InvokeRuntimeTypeHookIfPresent(type, "RegisterRuntimeType");
            }
        }

        // (2) Extension-package hook (plural method name) across all loaded assemblies. This is
        // what unblocks Apos shapes on Blazor/WASM.
        InvokeHookOnAllLoadedAssemblies("RegisterRuntimeTypes");
    }

    // Mirror of the "RegisterRuntimeTypes" scan above (issue #4416), but for teardown: an optional
    // package that GumService cannot reference directly — e.g. Gum.Shapes.MonoGame/KNI, which
    // reference MonoGameGum rather than the other way around — exposes a public static parameterless
    // "UninitializeRuntimeTypes" method to reset its own static state when Uninitialize runs. A
    // package with no such hook is simply skipped (see InvokeRuntimeTypeHookIfPresent's guard).
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Scanning for optional self-uninitialize hooks; every call is guarded so a trimmed-away hook degrades to a no-op.")]
    internal void UninitializeRuntimeTypesThroughReflection()
    {
        InvokeHookOnAllLoadedAssemblies("UninitializeRuntimeTypes");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Scanning for optional self-registration/uninitialize hooks; every call is guarded so a trimmed-away hook degrades to a no-op.")]
    private void InvokeHookOnAllLoadedAssemblies(string methodName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || IsFrameworkAssembly(assembly.GetName()))
            {
                continue;
            }

            Type[] assemblyTypes;
            try { assemblyTypes = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { assemblyTypes = ex.Types.Where(t => t != null).ToArray()!; }
            catch { continue; }

            foreach (var type in assemblyTypes)
            {
                if (type == null)
                {
                    continue;
                }
                InvokeRuntimeTypeHookIfPresent(type, methodName);
            }
        }
    }

    internal bool IsFrameworkAssembly(AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (string prefix in FrameworkAssemblyNamePrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // GetMethod itself can throw - not just Invoke - for an ambiguous overload set
    // (AmbiguousMatchException) or, under Native AOT, while walking a type's trimmed metadata
    // (TypeLoadException, issue #4105). A misbehaving or ambiguous hook shouldn't break Gum init,
    // so the whole lookup+call is guarded together rather than just the Invoke.
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Looks for an optional self-registration hook; a trimmed-away hook degrades to a no-op because the lookup is guarded.")]
    private void InvokeRuntimeTypeHookIfPresent(Type type, string methodName)
    {
        try
        {
            MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method != null && method.GetParameters().Length == 0)
            {
                method.Invoke(null, null);
            }
        }
        catch
        {
            // a misbehaving or ambiguous extension shouldn't break Gum init
        }
    }

    #endregion

    private (int width, int height) GetWindowSize()
    {
        // BackBufferWidth/Height is always physical pixels - XNA has no separate logical/DPI-scaled size.
        var pp = Game.GraphicsDevice.PresentationParameters;
        return (pp.BackBufferWidth, pp.BackBufferHeight);
    }

    // MonoGame/KNI ship the OS-provided *modal* text-entry dialog (the iOS soft-keyboard popup /
    // console prompt), wrapping XNA's KeyboardInput.Show. raylib, FNA, and Sokol are desktop-only
    // (or don't ship the API), so they deliberately leave NativeTextInput null and rely on inline
    // typing via each Keyboard's GetStringTyped instead — this is not an unfinished port (issue
    // #3432). The seam is elided where no platform implements it (FNA/Raylib), leaving the property
    // null exactly as before.
#if MONOGAME || KNI
    partial void AssignNativeTextInput()
    {
        NativeTextInput = new MonoGameNativeTextInput();
    }
#endif

    partial void AssignClipboard()
    {
        Clipboard = new global::Gum.Clipboard.MonoGameGumClipboard();
    }

    static partial void ApplyTextureFilterPlatform(bool useLinearFiltering)
    {
        Renderer.TextureFilter = useLinearFiltering ? TextureFilter.Linear : TextureFilter.Point;
    }

    // Extracts textures the serializer captured but could not resolve to a file path -- embedded or
    // runtime-generated Texture2Ds (notably the Forms default visuals' shared UISpriteSheet) whose Name is
    // unset, so the snapshot has valid texture coordinates but no file to slice. Saves each unique texture
    // to a PNG next to the project and writes the relative path into its placeholder SourceFile variable so
    // the slices render in the tool. The actual Texture2D.SaveAsPng is XNALIKE-only; other backends pass
    // GumSnapshotExporter a null seam instead of this method, leaving those textures unresolved (the same
    // as before this existed).
    private static void ExtractUnresolvedTextures(IRuntimeSnapshotSerializer serializer, string snapshotDirectory)
    {
        if (serializer.UnresolvedTextureReferences.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(snapshotDirectory);

        GumSnapshotExporter.FillUnresolvedTextureSourceFiles(serializer.UnresolvedTextureReferences, (texture, relativePath) =>
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
    }

    partial void UninitializePlatform()
    {
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

        // Issue #4416 — reset optional packages' own static state (e.g. ShapeRenderer.Self for
        // Gum.Shapes.MonoGame/KNI) via the same reflection hook RegisterRuntimeTypesThroughReflection
        // uses in the opposite direction.
        UninitializeRuntimeTypesThroughReflection();

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

        Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null;

        // Folded from the end of the pre-split Uninitialize (was a trailing #if XNALIKE _game = null).
        // Nothing between this seam's call site and that point reads _game, so the move is inert.
        _game = null;
    }

    #region Update

    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime, FrameworkElement root) => Update(gameTime, root.Visual);
    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime) => Update(gameTime);
    [Obsolete("Use the version which does not take a Game")]
    public void Update(Game game, GameTime gameTime, GraphicalUiElement root) => Update(gameTime, root);
    [Obsolete("Use the version of this method which does not take a Game")]
    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots) => Update(gameTime, roots);

    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen,
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
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
        var difference = gameTime.ElapsedGameTime.TotalSeconds;

        UpdatePreamble(roots);

        GameTime = gameTime;

        FormsUtilities.Update(_game, gameTime, roots);

        // SystemManagers.Activity (as of Sept 13, 2025) only
        // performs Sprite animation internally. This is not a
        // critical system, but unit tests cannot initialize a SystemManagers
        // because these require a graphics device. Therefore, we can tolerate
        // a null SystemManagers to simplify unit tests.
        _systemManagers?.Activity(gameTime.TotalGameTime.TotalSeconds);

        AnimateRoots(difference, roots);
    }

    #endregion
}
#endif
