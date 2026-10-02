using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Issue #5434 -- the embedded default font (Font18Arial*) is rendered from Liberation Sans. Its
/// regular atlas also carries baked UI shapes (the radio-button circles) in the rows below the glyphs;
/// those must stay byte-identical at the same coordinates because they are addressed by pixel position.
/// </summary>
public class EmbeddedDefaultFontTests : BaseTestClass
{
    // SHA-256 over the decoded RGBA bytes of atlas rows 160..255 (the whole shape section), taken from the
    // Arial-era atlas before the font was regenerated.
    private const string ShapeSectionSha256 = "03EA45C44709B23EF0AA9B833A029F320E964F5601ED34EF834ABA6330E426A6";
    private const int ShapeSectionTop = 160;

    private static Stream OpenResource(string fileName)
    {
        Assembly assembly = typeof(Text).Assembly;
        string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("." + fileName, StringComparison.Ordinal));
        return assembly.GetManifestResourceStream(name)!;
    }

    [Fact]
    public void RegularAtlas_ShapeSection_IsByteIdenticalToTheOriginal()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Stream stream = OpenResource("Font18Arial_0.png");
        using Texture2D texture = Texture2D.FromStream(game.GraphicsDevice, stream);
        texture.Width.ShouldBe(256);
        texture.Height.ShouldBe(256);

        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        byte[] bytes = pixels.Skip(ShapeSectionTop * texture.Width)
            .SelectMany(c => new[] { c.R, c.G, c.B, c.A })
            .ToArray();

        Convert.ToHexString(SHA256.HashData(bytes)).ShouldBe(ShapeSectionSha256);
    }

    [Fact]
    public void RegularFnt_GlyphsStayAboveShapeSection_AndUseLiberationSansMetrics()
    {
        using Stream stream = OpenResource("Font18Arial.fnt");
        string fnt = new StreamReader(stream).ReadToEnd();

        fnt.ShouldContain("face=\"Liberation Sans\"");
        fnt.ShouldContain("lineHeight=21 base=17");
        var glyphBottoms = Regex.Matches(fnt, @"^char id=\d+\s+x=\d+\s+y=(\d+)\s+width=\d+\s+height=(\d+)", RegexOptions.Multiline)
            .Select(m => int.Parse(m.Groups[1].Value) + int.Parse(m.Groups[2].Value))
            .ToList();
        glyphBottoms.Count.ShouldBe(191);
        glyphBottoms.Max().ShouldBeLessThanOrEqualTo(ShapeSectionTop);
    }

    private sealed class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
