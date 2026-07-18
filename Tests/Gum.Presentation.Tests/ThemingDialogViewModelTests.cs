using Gum.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.Options;
using Shouldly;
using System;
using System.Drawing;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ThemingDialogViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754). Its six color properties were
/// converted from WPF's System.Windows.Media.Color to System.Drawing.Color (ADR-0004) as part of
/// the move, dropping the old WPF-Color-to-Drawing-Color conversion helpers; these tests pin the
/// get/set roundtrip through that neutral type plus the reset/cancel behavior. Its View
/// (ThemingDialogView) stays in the Gum tool assembly, paired via
/// [Dialog(typeof(ThemingDialogViewModel))] - see DialogViewResolverTests (GumToolUnitTests) for
/// the cross-assembly resolution pin.
/// </summary>
public class ThemingDialogViewModelTests
{
    [Fact]
    public void AccentColor_Get_ReturnsEffectiveSettingsAccent_AndSet_UpdatesThemingServiceAndHasExplicitFlag()
    {
        FakeThemingService themingService = new();
        ThemingDialogViewModel viewModel = new(themingService, new FakeOptionsMonitor(new ThemeSettings()));

        viewModel.AccentColor.ShouldBe(themingService.DefaultAccent);
        viewModel.HasExplicitAccentColor.ShouldBeFalse();

        Color newAccent = Color.FromArgb(255, 10, 20, 30);
        viewModel.AccentColor = newAccent;

        themingService.Accent.ShouldBe(newAccent);
        viewModel.AccentColor.ShouldBe(newAccent);
        viewModel.HasExplicitAccentColor.ShouldBeTrue();
    }

    [Fact]
    public void Reset_WithPropertyName_ClearsOnlyThatColor()
    {
        FakeThemingService themingService = new()
        {
            Accent = Color.FromArgb(255, 1, 2, 3),
            CheckerA = Color.FromArgb(255, 4, 5, 6),
        };
        ThemingDialogViewModel viewModel = new(themingService, new FakeOptionsMonitor(new ThemeSettings()));

        viewModel.ResetCommand.Execute(nameof(ThemingDialogViewModel.AccentColor));

        themingService.Accent.ShouldBeNull();
        themingService.CheckerA.ShouldNotBeNull();
    }

    [Fact]
    public void Reset_WithNullPropertyName_ClearsAllColors()
    {
        FakeThemingService themingService = new()
        {
            Accent = Color.FromArgb(255, 1, 2, 3),
            CheckerA = Color.FromArgb(255, 4, 5, 6),
            CheckerB = Color.FromArgb(255, 7, 8, 9),
            OutlineColor = Color.FromArgb(255, 10, 11, 12),
            GuideLine = Color.FromArgb(255, 13, 14, 15),
            GuideText = Color.FromArgb(255, 16, 17, 18),
        };
        ThemingDialogViewModel viewModel = new(themingService, new FakeOptionsMonitor(new ThemeSettings()));

        viewModel.ResetCommand.Execute(null);

        themingService.Accent.ShouldBeNull();
        themingService.CheckerA.ShouldBeNull();
        themingService.CheckerB.ShouldBeNull();
        themingService.OutlineColor.ShouldBeNull();
        themingService.GuideLine.ShouldBeNull();
        themingService.GuideText.ShouldBeNull();
    }

    [Fact]
    public void OnNegative_RestoresColorsCachedAtConstruction()
    {
        ThemeSettings initialSettings = new()
        {
            Mode = ThemeMode.Dark,
            Accent = Color.FromArgb(255, 1, 2, 3),
        };
        FakeThemingService themingService = new() { Mode = initialSettings.Mode, Accent = initialSettings.Accent };
        ThemingDialogViewModel viewModel = new(themingService, new FakeOptionsMonitor(initialSettings));

        viewModel.AccentColor = Color.FromArgb(255, 9, 9, 9);
        viewModel.Mode = ThemeMode.Light;

        viewModel.NegativeCommand.Execute(null);

        themingService.Accent.ShouldBe(initialSettings.Accent);
        themingService.Mode.ShouldBe(initialSettings.Mode);
    }

    private sealed class FakeThemingService : IThemingService, IEffectiveThemeSettings
    {
        public Color DefaultAccent { get; }

        public FakeThemingService()
        {
            DefaultAccent = Color.FromArgb(255, 85, 161, 121);
        }

        public ThemeMode? Mode { get; set; }
        public Color? Accent { get; set; }
        public Color? CheckerA { get; set; }
        public Color? CheckerB { get; set; }
        public Color? OutlineColor { get; set; }
        public Color? GuideLine { get; set; }
        public Color? GuideText { get; set; }
        public bool IsSystemInDarkMode { get; set; }

        public IEffectiveThemeSettings EffectiveSettings => this;

        public void ApplyInitialTheme()
        {
        }

        ThemeMode IEffectiveThemeSettings.Mode => Mode ?? ThemeMode.Light;
        Color IEffectiveThemeSettings.Accent => Accent ?? DefaultAccent;
        Color IEffectiveThemeSettings.CheckerA => CheckerA ?? Color.Gray;
        Color IEffectiveThemeSettings.CheckerB => CheckerB ?? Color.DarkGray;
        Color IEffectiveThemeSettings.OutlineColor => OutlineColor ?? Color.White;
        Color IEffectiveThemeSettings.GuideLine => GuideLine ?? Color.White;
        Color IEffectiveThemeSettings.GuideText => GuideText ?? Color.White;
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<ThemeSettings>
    {
        public FakeOptionsMonitor(ThemeSettings value) => CurrentValue = value;

        public ThemeSettings CurrentValue { get; }

        public ThemeSettings Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<ThemeSettings, string?> listener) => null;
    }
}
