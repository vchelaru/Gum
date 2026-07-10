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
    /// The no-arg <see cref="Initialize"/> works on runtimes that do not require
    /// a host object (e.g. Raylib). On runtimes that do — currently MonoGame,
    /// KNI, and FNA, which all need a <c>Game</c> instance — the call throws
    /// <see cref="System.NotSupportedException"/>. Engine code targeting those
    /// runtimes should call the concrete <c>GumService.Initialize(Game ...)</c>
    /// overload first, then consume <see cref="Default"/> through this
    /// interface from platform-agnostic code.
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
        /// Gets the default cursor for the active runtime — either mouse or touch
        /// depending on the platform's input capabilities.
        /// </summary>
        ICursor Cursor { get; }

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

#if NET6_0_OR_GREATER
        /// <summary>
        /// The current default <see cref="IGumService"/>. Assigned by the
        /// concrete runtime's <c>GumService.Initialize</c> and cleared by
        /// <c>Uninitialize</c>. May be null before initialization.
        /// </summary>
        public static IGumService? Default { get; set; }
#endif
    }
}
