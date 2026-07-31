using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <inheritdoc/>
public class RenderDeviceResetPolicy : IRenderDeviceResetPolicy
{
    /// <inheritdoc/>
    public RenderDeviceResetDecision Evaluate(
        GraphicsDeviceStatus deviceStatus,
        int backBufferWidth,
        int backBufferHeight,
        int surfaceWidth,
        int surfaceHeight)
    {
        bool isDeviceLost = false;
        bool needsReset = false;
        string? message = null;

        switch (deviceStatus)
        {
            case GraphicsDeviceStatus.Lost:
                isDeviceLost = true;
                message = "Graphics device lost";
                break;

            case GraphicsDeviceStatus.NotReset:
                needsReset = true;
                message = "Graphics device needs reset";
                break;

            default:
                // The shared device demand-grows to the largest of its clients, so it only needs a
                // reset when the requested surface no longer fits inside the current back buffer.
                if (surfaceWidth > backBufferWidth || surfaceHeight > backBufferHeight)
                {
                    needsReset = true;
                    message = "Resolution has changed, needs reset";
                }
                break;
        }

        return new RenderDeviceResetDecision(
            isDeviceLost,
            needsReset,
            message,
            Math.Max(1, surfaceWidth),
            Math.Max(1, surfaceHeight));
    }
}
