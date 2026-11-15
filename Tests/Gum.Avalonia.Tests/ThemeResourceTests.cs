using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Gum.Avalonia.Themes;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The FRB palette and chrome icons come into the Avalonia head from the WPF tool's own resource
/// files, so these pin that every key is read and resolves in both theme variants.
/// </summary>
public class ThemeResourceTests
{
    [AvaloniaFact]
    public void LightAndDarkVariants_DefineTheSameKeys()
    {
        Dictionary<string, Color> shared = new Dictionary<string, Color>();

        ResourceDictionary light = FrbThemeResources.LoadVariant("Frb.Brushes.Light.xaml", shared);
        ResourceDictionary dark = FrbThemeResources.LoadVariant("Frb.Brushes.Dark.xaml", shared);

        light.Keys.Cast<object>().Select(key => key.ToString()).OrderBy(key => key)
            .ShouldBe(dark.Keys.Cast<object>().Select(key => key.ToString()).OrderBy(key => key));
        light.Count.ShouldBeGreaterThan(30);
    }

    [AvaloniaFact]
    public void Install_ResolvesVariantBrushesAndAccentColors()
    {
        ResourceDictionary resources = new ResourceDictionary();

        FrbThemeResources.Install(resources);

        resources.ContainsKey("Frb.Colors.Primary").ShouldBeTrue();
        resources.ContainsKey("Frb.Brushes.Primary").ShouldBeTrue();
        ResourceDictionary dark = (ResourceDictionary)resources.ThemeDictionaries[global::Avalonia.Styling.ThemeVariant.Dark];
        ((ISolidColorBrush)dark["Frb.Brushes.Background"]!).Color.ShouldBe(Color.Parse("#1c1c1c"));
        ((Color)dark["Frb.Colors.Icon.Blue"]!).ShouldBe(Color.Parse("#51b3ff"));
    }


    [AvaloniaTheory]
    [InlineData("Dark")]
    [InlineData("Light")]
    public void ForegroundMuted_IsOpaqueSolidAndReadableOnTheDialogSurface(string variant)
    {
        ResourceDictionary resources = new ResourceDictionary();
        FrbThemeResources.Install(resources);
        ResourceDictionary theme = (ResourceDictionary)resources.ThemeDictionaries[
            variant == "Dark" ? global::Avalonia.Styling.ThemeVariant.Dark : global::Avalonia.Styling.ThemeVariant.Light];

        Color muted = ((ISolidColorBrush)theme["Frb.Brushes.Foreground.Muted"]!).Color;
        Color surface = ((ISolidColorBrush)theme["Frb.Surface01"]!).Color;

        muted.A.ShouldBe((byte)255);
        double lighter = Math.Max(Luminance(muted), Luminance(surface));
        double darker = Math.Min(Luminance(muted), Luminance(surface));
        ((lighter + 0.05) / (darker + 0.05)).ShouldBeGreaterThanOrEqualTo(4.5);
    }

    private static double Luminance(Color color)
    {
        static double Channel(byte value)
        {
            double c = value / 255d;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    [AvaloniaFact]
    public void EveryChromeIconGeometry_Parses()
    {
        List<string> failed = GumIconGeometries.Keys.Where(key => GumIconGeometries.Get(key) == null).ToList();

        GumIconGeometries.Keys.Count.ShouldBeGreaterThan(50);
        failed.ShouldBeEmpty(string.Join(", ", failed));
    }
}
