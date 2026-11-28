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

    // The outline padding is part of the measured width, so the texture still stops at 4096, not 4096 plus
    // twice the outline.
    [Fact]
    public void RenderToTexture2D_OutlinedLineWiderThan4096_ClampsTextureWidthTo4096()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        const int outline = 2;
        const int advance = 10;
        string fontData =
$@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline={outline}
common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=65 x=0 y=0 width={advance} height=13 xoffset=0 yoffset=4 xadvance={advance} page=0 chnl=15
";
        using Texture2D fontTexture = new Texture2D(SystemManagers.Default.Renderer.GraphicsDevice, 256, 256);
        BitmapFont font = new BitmapFont(fontTexture, fontData);
        List<string> lines = new List<string> { new string('A', 500) };
        font.GetRequiredWidthAndHeight(lines, out int measuredWidth, out int _);
        measuredWidth.ShouldBe(500 * advance + 2 * outline);

        Texture2D? texture = font.RenderToTexture2D(lines, HorizontalAlignment.Left,
            SystemManagers.Default, toReplace: null, objectRequestingRender: this);

        try
        {
            texture.ShouldNotBeNull();
            texture.Width.ShouldBe(4096);
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
