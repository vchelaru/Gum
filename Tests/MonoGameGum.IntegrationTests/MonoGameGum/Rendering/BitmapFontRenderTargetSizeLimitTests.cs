using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Text measurement has no size limit (#8506), but the render target RenderTarget-mode text draws into
/// is still limited to 4096 pixels on each side, the GPU size limit it exists for.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class BitmapFontRenderTargetSizeLimitTests : BaseTestClass
{
    [Fact]
    public void RenderToTexture2D_TextTallerThan4096_ClampsTextureHeightTo4096()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        BitmapFont font = Text.DefaultBitmapFont!;
        List<string> lines = Enumerable.Repeat("Line", 400).ToList();
        font.GetRequiredWidthAndHeight(lines, out int _, out int measuredHeight);
        measuredHeight.ShouldBeGreaterThan(4096);

        Texture2D? texture = font.RenderToTexture2D(lines, HorizontalAlignment.Left,
            SystemManagers.Default, toReplace: null, objectRequestingRender: this);

        try
        {
            texture.ShouldNotBeNull();
            texture.Height.ShouldBe(4096);
        }
        finally
        {
            texture?.Dispose();
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => global::Gum.GumService.Default.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.Newest);

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
