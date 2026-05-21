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
