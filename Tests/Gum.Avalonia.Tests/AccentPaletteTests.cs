using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Gum.Avalonia.Themes;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Avalonia.Services;
using Gum.Dialogs;
using Gum.Settings;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The accent-derived colors the WPF ThemingService computes when the accent switches, and the
/// resources the Avalonia service publishes them under.
/// </summary>
public class AccentPaletteTests
{
    [Fact]
    public void ContrastingForeground_IsBlackOnLightAccents_AndWhiteOnDarkOnes()
    {
        AccentPalette.ContrastingForeground(Color.Parse("#7fd38a")).ShouldBe(Colors.Black);
        AccentPalette.ContrastingForeground(Color.Parse("#1f3a8a")).ShouldBe(Colors.White);
    }

    [Fact]
    public void LightenAndDarken_ShiftTheLightness_TenPercentEachWay()
    {
        Color accent = Color.Parse("#4caf50");

        AccentPalette.Lighten(accent).ToHsl().L.ShouldBe(accent.ToHsl().L + 0.10, 0.001);
        AccentPalette.Darken(accent).ToHsl().L.ShouldBe(accent.ToHsl().L - 0.10, 0.001);
    }

    [AvaloniaFact]
    public void SwitchingTheAccent_PublishesTheDerivedBrushes_UnderTheWpfKeys()
    {
        // Its own messenger and settings: the shared ones would reach recipients other tests left behind.
        AvaloniaThemingService theming = new AvaloniaThemingService(new WeakReferenceMessenger(), new MemoryThemeSettings());
        System.Drawing.Color accent = System.Drawing.Color.FromArgb(255, 0x4c, 0xaf, 0x50);
        Color expectedDark = AccentPalette.Darken(Color.FromRgb(0x4c, 0xaf, 0x50));

        theming.Accent = accent;

        ISolidColorBrush dark = (ISolidColorBrush)Application.Current!.Resources["Frb.Brushes.Primary.Dark"]!;
        ISolidColorBrush contrast = (ISolidColorBrush)Application.Current.Resources["Frb.Brushes.Primary.Contrast"]!;
        dark.Color.ShouldBe(expectedDark);
        contrast.Color.ShouldBe(AccentPalette.ContrastingForeground(expectedDark));
    }

    /// <summary>Theme settings held in memory, never written to disk.</summary>
    private sealed class MemoryThemeSettings : IWritableOptions<ThemeSettings>
    {
        public ThemeSettings CurrentValue { get; } = new ThemeSettings();
        public ThemeSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ThemeSettings, string?> listener) => null;
        public void Update(Action<ThemeSettings> applyChanges) => applyChanges(CurrentValue);
    }
}
