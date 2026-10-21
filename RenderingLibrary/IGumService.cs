using Gum;
using Gum.Forms.Controls;
using Gum.Threading;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    /// <summary>
    /// Platform-agnostic abstraction for the runtime Gum service. Allows engine and
    /// game code to depend on Gum without taking a hard reference on a specific
    /// runtime (MonoGameGum, RaylibGum, etc.). The concrete <c>GumService</c> in
    /// each runtime implements this interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>GumService</c> and <c>IGumService</c> are both supported. <c>GumService</c> is strongly typed
    /// (for example its <c>Cursor</c> and <c>Keyboard</c> are the concrete types) and is the more convenient
    /// choice for most games. <c>IGumService</c> is for code that should be truly cross-platform, such as
    /// code that expects to migrate or to support several runtimes, and it exposes interfaces
    /// (<see cref="ICursor"/>, <see cref="IInputReceiverKeyboard"/>). Neither replaces the other, and each is
    /// meant to be as fully featured as it can be.
    /// </para>
    /// <para>
    /// The no-arg <see cref="Initialize()"/> works on runtimes that do not require
    /// a host object (e.g. Raylib). On runtimes that do — currently MonoGame,
    /// KNI, and FNA, which all need a <c>Game</c> instance — the call throws
    /// <see cref="System.NotSupportedException"/>. Engine code targeting those
    /// runtimes should call the concrete <c>GumService.Initialize(Game ...)</c>
    /// overload first, then consume <see cref="Default"/> through this
    /// interface from platform-agnostic code.
    /// </para>
    /// </remarks>
    public interface IGumService
    {
        /// <summary>
        /// Initializes the service on runtimes that do not require platform-specific
        /// arguments. Throws <see cref="System.NotSupportedException"/> on runtimes
        /// that need additional context (e.g. MonoGame's <c>Game</c> instance) — call
        /// the concrete <c>GumService.Initialize(...)</c> overload on those platforms.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Initializes the service and loads the Gum project at the given path. Runtime
        /// support matches <see cref="Initialize()"/>: no-host runtimes (e.g. Raylib)
        /// load the project; runtimes that need additional context (e.g. MonoGame)
        /// throw <see cref="System.NotSupportedException"/>.
        /// </summary>
        /// <param name="gumProjectFile">Path to the .gumx project file to load.</param>
        void Initialize(string gumProjectFile);

        /// <summary>
        /// Gets whether the underlying GumService has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the renderer used by the service. The renderer exposes the active
        /// <see cref="Camera"/> and layer collection.
        /// </summary>
        IRenderer Renderer { get; }

        /// <summary>
        /// Gets the cursor Forms reads each frame: the runtime's default cursor (mouse or touch, depending on
        /// the platform's input capabilities), or the one installed with <c>FormsUtilities.SetCursor</c>.
        /// Null before <c>Initialize</c> and on runtimes without an input pump.
        /// </summary>
        /// <remarks>
        /// Unlike the strongly typed <c>GumService.Cursor</c>, which is null while a custom cursor is installed,
        /// this returns whichever <see cref="ICursor"/> is installed.
        /// </remarks>
        ICursor Cursor { get; }

        /// <summary>
        /// Gets the keyboard Forms reads each frame: the runtime's default keyboard, or the one installed with
        /// <c>FormsUtilities.SetKeyboard</c>. Null before <c>Initialize</c> and on runtimes without an input pump.
        /// </summary>
        /// <remarks>
        /// Unlike the strongly typed <c>GumService.Keyboard</c>, which is null while a custom keyboard is
        /// installed, this returns whichever <see cref="IInputReceiverKeyboard"/> is installed. The default
        /// implementation returns null, so an existing implementation of this interface keeps compiling.
        /// </remarks>
        IInputReceiverKeyboard? Keyboard => null;

        /// <summary>
        /// Gets or sets the width of the canvas, which acts as the root-most
        /// coordinate space.
        /// </summary>
        float CanvasWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the canvas, which acts as the root-most
        /// coordinate space.
        /// </summary>
        float CanvasHeight { get; set; }

        /// <summary>
        /// Draws the current frame using the active <see cref="ISystemManagers"/>.
        /// </summary>
        void Draw();

        /// <summary>
        /// The root container that owns top-level elements. <c>GraphicalUiElement.AddToRoot</c>
        /// and the Forms <c>AddToRoot</c> extension dispatch through this so callers in
        /// <c>GumCommon</c> can add elements without taking a runtime-specific reference.
        /// </summary>
        InteractiveGue Root { get; }

        /// <summary>
        /// Queue used to defer actions onto the runtime's main loop (typically the
        /// next <c>Update</c>). Callers in <c>GumCommon</c> consume this when they
        /// need to marshal work back to the game thread without depending on a
        /// specific runtime — for example, applying the result of a completed
        /// async operation that may have finished on a worker thread.
        /// </summary>
        DeferredActionQueue DeferredQueue { get; }

        /// <summary>
        /// Seconds since the start of the runtime, sampled at the most recent Update call,
        /// or <c>null</c> if no Update has run yet. Callers in <c>GumCommon</c> use this
        /// when they need a platform-agnostic frame-clock value — for example, debouncing
        /// dialog-dismissal input against the elapsed game time. The underlying source
        /// differs per runtime (MonoGame's <c>GameTime.TotalGameTime.TotalSeconds</c>,
        /// Raylib's accumulated frame seconds, etc.) and is normalized to <c>float</c>
        /// here.
        /// </summary>
        float? GameTime { get; }

        /// <summary>
        /// The native (OS-provided) modal text-input dialog implementation for the
        /// active runtime, or <c>null</c> if the runtime does not have one. Forms
        /// controls in <c>GumCommon</c> — primarily <c>TextBoxBase</c> — consult
        /// this when a control wants to bring up the platform's on-screen
        /// keyboard. Null on runtimes without native text input (Raylib, FNA,
        /// Sokol, browser, etc.); callers should treat null as a no-op.
        /// </summary>
        INativeTextInput? NativeTextInput { get; }

        /// <summary>
        /// The OS clipboard implementation for the active runtime, or <c>null</c> if the
        /// runtime does not have one (iOS, headless tests, etc.). Forms controls in
        /// <c>GumCommon</c> — primarily <c>TextBox</c> and <c>PasswordBox</c> — consult
        /// this when handling copy / cut / paste. Callers should treat null as a no-op.
        /// </summary>
        IGumClipboard? Clipboard { get; }

        /// <summary>
        /// Creates an empty (no-texture) sprite renderable for the active runtime. Used by
        /// Forms controls in <c>GumCommon</c> — primarily <c>Image</c> — that need to seed
        /// a visual with a sprite renderable without taking a reference to a runtime-specific
        /// Sprite type. Each runtime returns its own <c>Sprite</c> implementation.
        /// </summary>
        IRenderable CreateSpriteRenderable();

        /// <summary>
        /// Creates the cursor (mouse or touch, depending on the platform's input capabilities) for the
        /// active runtime. Called by <c>FormsUtilities.InitializeDefaults</c> so cursor creation no
        /// longer references a concrete per-platform input type. Render-only hosts (e.g. the Skia
        /// standalone service) inherit this default and return <c>null</c> — they have no input pump.
        /// </summary>
        ICursor? CreateCursor() => null;

        /// <summary>
        /// Creates the keyboard for the active runtime. Called by <c>FormsUtilities.InitializeDefaults</c>
        /// so keyboard creation no longer references a concrete per-platform input type. Render-only hosts
        /// inherit this default and return <c>null</c> — they have no input pump.
        /// </summary>
        IInputReceiverKeyboard? CreateKeyboard() => null;

        /// <summary>
        /// Applies the current OS gamepad state at <paramref name="index"/> to <paramref name="gamepad"/>
        /// for the active runtime. This is a per-frame driver (not a create-once factory), called each
        /// frame by <c>FormsUtilities.UpdateGamepads</c>. Render-only hosts inherit this default no-op —
        /// they have no gamepad input source.
        /// </summary>
        /// <param name="gamepad">The platform-neutral gamepad holder to update.</param>
        /// <param name="index">The zero-based gamepad index to sample.</param>
        /// <param name="time">The total elapsed game time, in seconds.</param>
        void ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time) { }

        /// <summary>
        /// Gets the container for non-modal popups, drawn above <see cref="Root"/>. This is the Forms popup
        /// root of the service, so a control opened as a popup lands here. The default returns the current
        /// Forms popup root.
        /// </summary>
        InteractiveGue PopupRoot => null!;

        /// <summary>
        /// Gets the topmost container, which blocks input to everything below it while it has children.
        /// The default returns the current Forms modal root.
        /// </summary>
        InteractiveGue ModalRoot => null!;

        /// <summary>
        /// Gets the gamepads Gum samples each frame. Entries are filled in on runtimes that read gamepads
        /// and stay unplugged otherwise. The default is an empty array, which a custom implementation that
        /// has no gamepad input keeps.
        /// </summary>
        Gum.Input.GamePad[] Gamepads => null!;

        /// <summary>
        /// Per-frame tick. Call once per frame, before <see cref="Draw"/>, with the total number of seconds
        /// since the application started (not the time since the previous frame). Pumps the cursor,
        /// keyboard and gamepad, raises control events, runs deferred actions and advances animations, and
        /// records <paramref name="totalSeconds"/> as <see cref="GameTime"/>. The default does nothing, so a
        /// custom implementation that has no per-frame work keeps compiling.
        /// </summary>
        /// <remarks>
        /// Each runtime also has its own overload that takes its engine's time type, such as the
        /// <c>GameTime</c> of MonoGame, KNI and FNA. Both advance the same frame; use the one that matches
        /// how your code is written.
        /// </remarks>
        /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
        void Update(double totalSeconds) { }

        /// <summary>
        /// Registers the default keyboard so Forms controls respond to it. The default adds
        /// <see cref="Keyboard"/> to <see cref="FrameworkElement.KeyboardsForUiControl"/>.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">There is no keyboard, because Gum has not been initialized.</exception>
        void UseKeyboardDefaults() { }

        /// <summary>
        /// Registers <see cref="Gamepads"/> so Forms controls respond to them. The default adds every
        /// gamepad to <see cref="FrameworkElement.GamePadsForUiControl"/>.
        /// </summary>
        void UseGamepadDefaults() { }

        /// <summary>
        /// Re-applies all styles on <see cref="Root"/>, <see cref="PopupRoot"/> and <see cref="ModalRoot"/>.
        /// Call after changing the variable references or default states the visuals read.
        /// </summary>
        void RefreshStyles() { }

        /// <summary>
        /// Re-applies all styles on <paramref name="target"/> and its children.
        /// </summary>
        /// <param name="target">The root of the subtree to refresh.</param>
        void RefreshStyles(GraphicalUiElement target) { }

        /// <summary>
        /// Re-translates all live text on <see cref="Root"/>, <see cref="PopupRoot"/> and
        /// <see cref="ModalRoot"/> using the current language of <see cref="LocalizationService"/>. Text
        /// assigned through the no-translate path is skipped.
        /// </summary>
        void RefreshLocalization() { }

        /// <summary>
        /// Gets the service that provides localized strings, or null when none is assigned. The default
        /// returns the process-wide active service.
        /// </summary>
        Gum.Localization.ILocalizationService? LocalizationService => null;

        /// <summary>
        /// Gets the result of the most recent project load made by <c>Initialize</c>, including its
        /// non-fatal warnings. Null when no project file was loaded, and on runtimes that do not record it.
        /// </summary>
        Gum.DataTypes.GumLoadResult? LastLoadResult => null;

        /// <summary>
        /// Exports the live UI tree under <see cref="Root"/> to a Gum project at
        /// <paramref name="filePath"/>, so it can be opened and inspected in the Gum tool.
        /// </summary>
        /// <param name="filePath">Destination .gumx path. Its folder receives the Screens and Standards folders.</param>
        /// <param name="shake">When true, values equal to the standard-element default are left out.</param>
        void ExportSnapshot(string filePath, bool shake = true) { }

        /// <summary>
        /// Gets the single-threaded synchronization context installed by <see cref="UseSingleThreadedAsync"/>,
        /// or null if it has not been called. The default is null.
        /// </summary>
        Gum.Async.SingleThreadSynchronizationContext? SynchronizationContext => null;

        /// <summary>
        /// Installs a synchronization context on the calling thread so that <c>await</c> continuations
        /// resume on the game's main thread, inside <see cref="Update(double)"/>. Call once, after
        /// <c>Initialize</c>. Skip it when you have installed your own context. The default does nothing.
        /// </summary>
        void UseSingleThreadedAsync() { }

        /// <summary>
        /// Tears the service down so <c>Initialize</c> can be called again: clears the roots, Forms state,
        /// the loaded project and cached content. A service that is not initialized ignores the call. The
        /// default does nothing.
        /// </summary>
        void Uninitialize() { }

        // The per-service state this service owns while it is initialized, or null. Internal: the runtimes
        // reach it through InternalsVisibleTo; it is not part of the public API.
        internal GumContext? Context => null;

        /// <summary>
        /// The current default <see cref="IGumService"/>, and the one "current service" pointer: every
        /// per-service static (<c>ObjectFinder.Self</c>, <c>LoaderManager.Self</c>, the Forms statics,
        /// <c>SystemManagers.Default</c> and the rest) resolves the current service through it. Assigning it
        /// switches all of them to that service. <c>Update</c> and <c>Draw</c> make their service the default for
        /// the length of the call and put the previous default back. <c>Initialize</c> runs as the service being
        /// initialized and makes it the default only when no default exists: <b>with two services, assign
        /// <c>IGumService.Default</c> (or <c>GumService.Default</c>) explicitly to switch; a second
        /// <c>Initialize</c> never changes it.</b> <c>Uninitialize</c> clears it when it is the default. May be
        /// null before initialization.
        /// </summary>
        public static IGumService? Default
        {
            get => _default;
            set
            {
                _default = value;
                DefaultChanged?.Invoke(value);
            }
        }

        private static IGumService? _default;

        /// <summary>
        /// Raised after <see cref="Default"/> is assigned. The runtimes use it to keep their typed
        /// <c>GumService.Default</c> in step without casting on every read.
        /// </summary>
        internal static event System.Action<IGumService?>? DefaultChanged;
    }
}
