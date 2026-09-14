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

    [AvaloniaFact]
    public void EveryChromeIconGeometry_Parses()
    {
        List<string> failed = GumIconGeometries.Keys.Where(key => GumIconGeometries.Get(key) == null).ToList();

        GumIconGeometries.Keys.Count.ShouldBeGreaterThan(50);
        failed.ShouldBeEmpty(string.Join(", ", failed));
    }
}
