using System;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// Picks the ShadowDusk compile target for the graphics device the game is running on, so
/// <c>UseShadowDusk()</c> takes no argument and one Game class can be shared by source link across
/// the platforms.
/// </summary>
internal static class ShadowDuskTargetDetector
{
    public static PlatformTarget Detect(GraphicsDevice graphicsDevice)
    {
#if FNA
        // One D3D9 .fxb serves every FNA backend.
        return PlatformTarget.Fna;
#elif KNI
        // GraphicsAdapter.Backend exists since KNI 3.11.9002.
        return FromBackendName((graphicsDevice.Adapter ?? GraphicsAdapter.DefaultAdapter).Backend.ToString());
#else
        return FromBackendName(MonoGame.Framework.Utilities.PlatformInfo.GraphicsBackend.ToString());
#endif
    }

    /// <summary>
    /// Maps the name of MonoGame's <c>PlatformInfo.GraphicsBackend</c> or KNI's <c>GraphicsAdapter.Backend</c>
    /// to a compile target. A backend ShadowDusk cannot compile for throws, naming it, so a shader never
    /// compiles for the wrong one.
    /// </summary>
    internal static PlatformTarget FromBackendName(string backendName)
    {
        switch (backendName)
        {
            case "OpenGL":
            case "GLES":
            case "WebGL":
                return PlatformTarget.OpenGL;
            case "DirectX":
            case "DirectX11":
                return PlatformTarget.DirectX;
            case "DirectX12":
                return PlatformTarget.DirectX12;
            case "Vulkan":
                return PlatformTarget.Vulkan;
            default:
                throw new NotSupportedException(
                    $"Gum.ShadowDusk cannot compile shaders for the graphics backend '{backendName}'. It supports OpenGL, DirectX, DirectX12 and Vulkan.");
        }
    }
}
