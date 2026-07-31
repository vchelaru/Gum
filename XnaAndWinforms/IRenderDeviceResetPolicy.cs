using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// Decides whether the shared graphics device needs to be reset for a given surface size, without
/// touching any GPU resources. Kept separate from <see cref="ISharedRenderDeviceHost"/> so the
/// decision can be exercised without a real <see cref="GraphicsDevice"/>.
/// </summary>
public interface IRenderDeviceResetPolicy
{
    /// <summary>
    /// Evaluates the device state and the surface size the caller wants to draw at.
    /// </summary>
    /// <param name="deviceStatus">The current status of the shared graphics device.</param>
    /// <param name="backBufferWidth">The shared device's current back buffer width.</param>
    /// <param name="backBufferHeight">The shared device's current back buffer height.</param>
    /// <param name="surfaceWidth">The width in pixels the caller wants to draw at.</param>
    /// <param name="surfaceHeight">The height in pixels the caller wants to draw at.</param>
    RenderDeviceResetDecision Evaluate(
        GraphicsDeviceStatus deviceStatus,
        int backBufferWidth,
        int backBufferHeight,
        int surfaceWidth,
        int surfaceHeight);
}
