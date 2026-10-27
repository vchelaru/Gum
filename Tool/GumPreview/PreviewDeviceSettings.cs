using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GumPreview;

/// <summary>Graphics device settings the Preview window needs.</summary>
internal static class PreviewDeviceSettings
{
    /// <summary>
    /// Keeps the back buffer's contents across the render-target switches an IsRenderTarget
    /// container makes; with the default DiscardContents everything drawn before Gum in the frame
    /// is cleared.
    /// </summary>
    public static void PreserveBackBufferContents(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
    }
}
