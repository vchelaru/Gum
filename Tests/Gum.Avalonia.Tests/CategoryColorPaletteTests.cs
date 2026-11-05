using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Gum.Plugins.InternalPlugins.StatePlugin;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The state category palette (#7591) in each theme: every name resolves to a brush, each color keeps at
/// least 3:1 against the surfaces it is drawn on, black banner text reads on it, and no two colors (nor
/// a status color) are too close to tell apart.
/// </summary>
public class CategoryColorPaletteTests
{
    // The surfaces the dot, the row bar and the banner sit on, read from the theme and the Variables tab's own
    // header colors: a States row is the theme's Surface01; a Variables category header is a translucent tint
    // (CategorySortAndColorLogic) over it.
    private static (Color Header, Color Row, Color DimensionsHeader) SurfacesOf(string theme)
    {
        Application.Current!.TryFindResource("Frb.Colors.Surface01", Variant(theme), out object? resource).ShouldBeTrue("Frb.Colors.Surface01");
        Color row = (Color)resource!;
        List<VariableCategoryDescriptor> categories = new CategorySortAndColorLogic().SortAndColorCategories(
            new List<VariableCategoryDescriptor> { new VariableCategoryDescriptor("Position"), new VariableCategoryDescriptor("Dimensions") });
        Color HeaderOver(string name)
        {
            System.Drawing.Color tint = categories.Single(category => category.Name == name).GetHeaderColor()!.Value;
            double alpha = tint.A / 255.0;
            byte Mix(byte under, byte over) => (byte)Math.Round(under * (1 - alpha) + over * alpha);
            return Color.FromRgb(Mix(row.R, tint.R), Mix(row.G, tint.G), Mix(row.B, tint.B));
        }
        return (HeaderOver("Position"), row, HeaderOver("Dimensions"));
    }

    private static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Color ColorOf(string name, string theme)
    {
        Application.Current!.TryFindResource(CategoryColors.ResourceKey(name), Variant(theme), out object? resource).ShouldBeTrue($"{name} in {theme}");
        return ((ISolidColorBrush)resource!).Color;
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void EveryPaletteName_HasABrushInTheTheme(string theme)
    {
        foreach (string name in CategoryColors.Names)
        {
            ColorOf(name, theme);
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void EveryColor_HasAtLeast3To1Contrast_AgainstTheSurfacesItSitsOn(string theme)
    {
        (Color header, Color row, Color dimensionsHeader) = SurfacesOf(theme);
        foreach (string name in CategoryColors.Names)
        {
            Color color = ColorOf(name, theme);
            Contrast(color, header).ShouldBeGreaterThanOrEqualTo(3.0, $"{name} on the {theme} Variables header");
            Contrast(color, row).ShouldBeGreaterThanOrEqualTo(3.0, $"{name} on the {theme} States row");
            Contrast(color, dimensionsHeader).ShouldBeGreaterThanOrEqualTo(3.0, $"{name} on the {theme} Dimensions header");
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void EveryColor_CarriesBlackBannerTextAtLeast4_5To1(string theme)
    {
        foreach (string name in CategoryColors.Names)
        {
            Contrast(ColorOf(name, theme), Colors.Black).ShouldBeGreaterThanOrEqualTo(4.5, $"black text on {name} in {theme}");
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void NoTwoColors_AreCloserThanDeltaE19(string theme)
    {
        foreach (string a in CategoryColors.Names)
        {
            foreach (string b in CategoryColors.Names.Where(other => other != a))
            {
                DeltaE(ColorOf(a, theme), ColorOf(b, theme)).ShouldBeGreaterThanOrEqualTo(19.0, $"{a} vs {b} in {theme}");
            }
        }
    }

    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void NoColor_IsCloserThanDeltaE12_ToTheThemesStatusColors(string theme)
    {
        foreach (string status in new[] { "Frb.Colors.Error", "Frb.Colors.Success", "Frb.Colors.Warning", "Frb.Colors.Link" })
        {
            Application.Current!.TryFindResource(status, Variant(theme), out object? resource).ShouldBeTrue(status);
            Color statusColor = (Color)resource!;
            foreach (string name in CategoryColors.Names)
            {
                DeltaE(ColorOf(name, theme), statusColor).ShouldBeGreaterThanOrEqualTo(12.0, $"{name} vs {status} in {theme}");
            }
        }
    }

    [Fact]
    public void ResourceKey_IsThePaletteNameUnderTheCategoryColorPrefix()
    {
        CategoryColors.ResourceKey("Indigo").ShouldBe("Frb.CategoryColor.Indigo");
    }

    private static double Luminance(Color c)
    {
        static double Lin(double v) { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
    }

    private static double Contrast(Color a, Color b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static (double L, double A, double B) Lab(Color c)
    {
        static double Lin(double v) { v /= 255; return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        double r = Lin(c.R), g = Lin(c.G), b = Lin(c.B);
        double x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047, y = 0.2126 * r + 0.7152 * g + 0.0722 * b, z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883;
        static double F(double t) => t > 0.008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116;
        return (116 * F(y) - 16, 500 * (F(x) - F(y)), 200 * (F(y) - F(z)));
    }

    // CIE76.
    private static double DeltaE(Color a, Color b)
    {
        (double L, double A, double B) la = Lab(a), lb = Lab(b);
        return Math.Sqrt(Math.Pow(la.L - lb.L, 2) + Math.Pow(la.A - lb.A, 2) + Math.Pow(la.B - lb.B, 2));
    }
}
