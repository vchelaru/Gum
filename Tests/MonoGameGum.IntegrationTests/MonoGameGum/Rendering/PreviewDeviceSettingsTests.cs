using GumPreview;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// An IsRenderTarget container switches the render target mid-frame; with the default
/// DiscardContents the back buffer is cleared on the switch back, wiping everything drawn before
/// Gum (the Preview's background clear included).
/// </summary>
public class PreviewDeviceSettingsTests
{
    [Fact]
    public void PreserveBackBufferContents_SetsPreserveContentsOnThePresentationParameters()
    {
        GraphicsDeviceInformation information = new GraphicsDeviceInformation
        {
            PresentationParameters = new PresentationParameters { RenderTargetUsage = RenderTargetUsage.DiscardContents },
        };

        PreviewDeviceSettings.PreserveBackBufferContents(null, new PreparingDeviceSettingsEventArgs(information));

        information.PresentationParameters.RenderTargetUsage.ShouldBe(RenderTargetUsage.PreserveContents);
    }
}
