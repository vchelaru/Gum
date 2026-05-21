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
    /// Initialization is intentionally not part of this interface because each
    /// runtime requires platform-specific arguments (e.g. MonoGame's <c>Game</c>
    /// instance). Call the runtime-specific <c>Initialize</c> on the concrete
    /// <c>GumService</c>, then consume <see cref="Default"/> through this
    /// interface from platform-agnostic code.
    /// </remarks>
    public interface IGumService
    {
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
