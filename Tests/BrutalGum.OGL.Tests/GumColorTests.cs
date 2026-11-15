using Gum;
using Gum.GueDeriving;
using Shouldly;
using SkiaSharp;
using System.Reflection;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace SkiaHostTests;

/// <summary>
/// Gum.Color matches MonoGame's Color and converts to and from SKColor, so MonoGame docs snippets compile
/// unchanged on this host. Linked into every test project of a host that compiles Gum.Color.
/// </summary>
public class GumColorTests
{
    public static IEnumerable<object[]> XnaNamedColors() =>
        typeof(XnaColor).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(XnaColor))
            .Select(property => new object[] { property.Name });

    [Theory]
    [MemberData(nameof(XnaNamedColors))]
    public void NamedColor_MatchesMonoGame(string name)
    {
        PropertyInfo? gumProperty = typeof(Color).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        gumProperty.ShouldNotBeNull($"Gum.Color has no {name}");

        Color gumColor = (Color)gumProperty.GetValue(null)!;
        XnaColor xnaColor = (XnaColor)typeof(XnaColor).GetProperty(name)!.GetValue(null)!;

        ShouldMatch(gumColor, xnaColor);
    }

    [Fact]
    public void NamedColors_HaveNoNamesMonoGameLacks()
    {
        HashSet<string> xnaNames = XnaNamedColors().Select(row => (string)row[0]).ToHashSet();

        typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Color))
            .Select(property => property.Name)
            .Where(name => !xnaNames.Contains(name))
            .ShouldBeEmpty();
    }

    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(-5, 128, 300)]
    public void Constructor_IntRgb_MatchesMonoGame(int r, int g, int b) =>
        ShouldMatch(new Color(r, g, b), new XnaColor(r, g, b));

    [Theory]
    [InlineData(10, 20, 30, 40)]
    [InlineData(-5, 128, 300, 999)]
    public void Constructor_IntRgba_MatchesMonoGame(int r, int g, int b, int a) =>
        ShouldMatch(new Color(r, g, b, a), new XnaColor(r, g, b, a));

    [Fact]
    public void Constructor_ByteRgba_MatchesMonoGame() =>
        ShouldMatch(new Color((byte)1, (byte)2, (byte)3, (byte)4), new XnaColor((byte)1, (byte)2, (byte)3, (byte)4));

    [Theory]
    [InlineData(0.5f, 0.25f, 1f)]
    [InlineData(-0.1f, 0.999f, 1.5f)]
    public void Constructor_FloatRgb_MatchesMonoGame(float r, float g, float b) =>
        ShouldMatch(new Color(r, g, b), new XnaColor(r, g, b));

    [Theory]
    [InlineData(0.5f, 0.25f, 1f, 0.75f)]
    [InlineData(-0.1f, 0.999f, 1.5f, 2f)]
    public void Constructor_FloatRgba_MatchesMonoGame(float r, float g, float b, float a) =>
        ShouldMatch(new Color(r, g, b, a), new XnaColor(r, g, b, a));

    [Theory]
    [InlineData(0.5f)]
    [InlineData(2f)]
    public void MultiplyByFloat_MatchesMonoGame(float scale) =>
        ShouldMatch(new Color(200, 100, 50, 255) * scale, new XnaColor(200, 100, 50, 255) * scale);

    [Fact]
    public void FromNonPremultiplied_MatchesMonoGame() =>
        ShouldMatch(Color.FromNonPremultiplied(200, 100, 50, 128), XnaColor.FromNonPremultiplied(200, 100, 50, 128));

    [Fact]
    public void ToString_MatchesMonoGame() =>
        new Color(1, 2, 3, 4).ToString().ShouldBe(new XnaColor(1, 2, 3, 4).ToString());

    [Fact]
    public void Equality_ComparesChannels()
    {
        (new Color(1, 2, 3, 4) == new Color(1, 2, 3, 4)).ShouldBeTrue();
        (new Color(1, 2, 3, 4) != new Color(1, 2, 3, 5)).ShouldBeTrue();
        new Color(1, 2, 3, 4).Equals((object)new Color(1, 2, 3, 4)).ShouldBeTrue();
        new Color(1, 2, 3, 4).GetHashCode().ShouldBe(new Color(1, 2, 3, 4).GetHashCode());
    }

    [Fact]
    public void Equality_WithSKColor_ComparesChannelsEitherWay()
    {
        (Color.Red == SKColors.Red).ShouldBeTrue();
        (SKColors.Red == Color.Red).ShouldBeTrue();
        (Color.Red != SKColors.Blue).ShouldBeTrue();
        (SKColors.Blue != Color.Red).ShouldBeTrue();
    }

    [Fact]
    public void SKColorConversion_RoundTrips()
    {
        SKColor skColor = new Color(10, 20, 30, 40);
        skColor.ShouldBe(new SKColor(10, 20, 30, 40));

        Color back = skColor;
        back.ShouldBe(new Color(10, 20, 30, 40));
    }

    [Fact]
    public void VisualColor_AcceptsNamedAndConstructedColors()
    {
        ColoredRectangleRuntime rectangle = new();
        rectangle.Color = Color.Red;
        rectangle.Color.ShouldBe(SKColors.Red);
        (rectangle.Color == Color.Red).ShouldBeTrue();

        TextRuntime text = new();
        text.Color = new Color(0, 0, 255);
        text.Color.ShouldBe(SKColors.Blue);

        Color read = text.Color;
        read.ShouldBe(Color.Blue);
    }

    private static void ShouldMatch(Color gumColor, XnaColor xnaColor)
    {
        gumColor.R.ShouldBe(xnaColor.R);
        gumColor.G.ShouldBe(xnaColor.G);
        gumColor.B.ShouldBe(xnaColor.B);
        gumColor.A.ShouldBe(xnaColor.A);
    }
}
