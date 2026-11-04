using System;
using System.Drawing;
using System.Linq;
using Gum.Dialogs;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Settings;
using Gum.SettingsWindow;
using Microsoft.Extensions.Options;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>
/// The rows of the Application > Appearance page: each reads and writes the same theming service
/// property the page always did, and each color has its own reset.
/// </summary>
public class AppearanceRowsTests
{
    private static (FakeThemingService Service, ThemingDialogViewModel ViewModel, AppearanceRows Rows) Create()
    {
        FakeThemingService service = new();
        ThemingDialogViewModel viewModel = new(service, new FakeOptionsMonitor());
        return (service, viewModel, new AppearanceRows(viewModel));
    }

    private static InstanceMember Row(AppearanceRows rows, string name) =>
        rows.BuildCategories().SelectMany(section => section.Members).Single(member => member.Name == name);

    [Fact]
    public void EveryRow_HasALabel_AHint_AndTheSharedRowShape()
    {
        (_, _, AppearanceRows rows) = Create();

        InstanceMember[] members = rows.BuildCategories().SelectMany(section => section.Members).ToArray();

        members.Length.ShouldBe(7);
        foreach (InstanceMember member in members)
        {
            member.DisplayName.ShouldNotBeNullOrWhiteSpace();
            member.DetailText.ShouldNotBeNullOrWhiteSpace();
            member.IsSpaciousRow.ShouldBeTrue();
            member.FirstGridLength.ShouldBe(SettingsRows.LabelColumnWidth);
        }
    }

    [Fact]
    public void SearchTerms_IncludeEveryLabelAndHint()
    {
        (_, _, AppearanceRows rows) = Create();

        string[] terms = rows.SearchTerms().ToArray();

        terms.ShouldContain("Accent color");
        terms.ShouldContain("Guide text color");
        terms.Length.ShouldBe(14);
    }

    [Fact]
    public void ThemeMode_ReadsAndWritesTheService()
    {
        (FakeThemingService service, _, AppearanceRows rows) = Create();
        InstanceMember mode = Row(rows, nameof(ThemingDialogViewModel.Mode));

        mode.Value.ShouldBe(ThemeMode.System);
        mode.Value = ThemeMode.Dark;

        service.Mode.ShouldBe(ThemeMode.Dark);
        mode.Value.ShouldBe(ThemeMode.Dark);
    }

    [Theory]
    [InlineData(nameof(ThemingDialogViewModel.AccentColor))]
    [InlineData(nameof(ThemingDialogViewModel.CheckerAColor))]
    [InlineData(nameof(ThemingDialogViewModel.CheckerBColor))]
    [InlineData(nameof(ThemingDialogViewModel.OutlineColor))]
    [InlineData(nameof(ThemingDialogViewModel.GuideLineColor))]
    [InlineData(nameof(ThemingDialogViewModel.GuideTextColor))]
    public void ColorRow_IsAColorRow_WritesTheService_AndCanResetOnceCustom(string name)
    {
        (_, ThemingDialogViewModel viewModel, AppearanceRows rows) = Create();
        Color picked = Color.FromArgb(255, 10, 20, 30);
        InstanceMember row = Row(rows, name);
        row.PreferredDisplayer.ShouldBe(typeof(GumDisplayers.Color));
        row.ResetAction.ShouldNotBeNull();
        row.CanReset.ShouldBeFalse();

        row.Value = picked;

        row.Value.ShouldBe(picked);
        row.CanReset.ShouldBeTrue();

        row.ResetAction!();

        row.CanReset.ShouldBeFalse();
        typeof(ThemingDialogViewModel).GetProperty("HasExplicit" + name)!.GetValue(viewModel).ShouldBe(false);
    }

    [Fact]
    public void ResetAll_IsOnlyAvailableWhileAnyColorIsCustom_AndResetsEveryChangedColor()
    {
        (FakeThemingService service, ThemingDialogViewModel viewModel, AppearanceRows rows) = Create();
        viewModel.HasAnyExplicitColor.ShouldBeFalse();

        viewModel.AccentColor = Color.FromArgb(255, 1, 2, 3);
        viewModel.GuideLineColor = Color.FromArgb(255, 4, 5, 6);
        viewModel.HasAnyExplicitColor.ShouldBeTrue();

        viewModel.ResetCommand.Execute(null);

        service.Accent.ShouldBeNull();
        service.GuideLine.ShouldBeNull();
        viewModel.HasAnyExplicitColor.ShouldBeFalse();
        rows.BuildCategories().SelectMany(section => section.Members).Where(member => member.ResetAction != null)
            .ShouldAllBe(member => !member.CanReset);
    }

    [Fact]
    public void ResetOfOneColor_LeavesTheOthersAlone()
    {
        (FakeThemingService service, ThemingDialogViewModel viewModel, AppearanceRows rows) = Create();
        viewModel.AccentColor = Color.FromArgb(255, 1, 2, 3);
        viewModel.GuideLineColor = Color.FromArgb(255, 4, 5, 6);

        Row(rows, nameof(ThemingDialogViewModel.AccentColor)).ResetAction!();

        service.Accent.ShouldBeNull();
        service.GuideLine.ShouldNotBeNull();
    }

    [Fact]
    public void ChangingTheMode_RefreshesTheColorRowsItChanges()
    {
        (_, ThemingDialogViewModel viewModel, AppearanceRows rows) = Create();
        InstanceMember checker = Row(rows, nameof(ThemingDialogViewModel.CheckerAColor));
        System.Collections.Generic.List<string?> changed = new();
        checker.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        // The view listens while it is on screen.
        Action handler = () => { };
        rows.Reloaded += handler;

        viewModel.Mode = ThemeMode.Dark;

        changed.ShouldContain("Value");
        rows.Reloaded -= handler;
    }

    private sealed class FakeThemingService : IThemingService, IEffectiveThemeSettings
    {
        public ThemeMode? Mode { get; set; }
        public Color? Accent { get; set; }
        public Color? CheckerA { get; set; }
        public Color? CheckerB { get; set; }
        public Color? OutlineColor { get; set; }
        public Color? GuideLine { get; set; }
        public Color? GuideText { get; set; }
        public bool IsSystemInDarkMode { get; set; }
        public IEffectiveThemeSettings EffectiveSettings => this;
        public void ApplyInitialTheme() { }
        ThemeMode IEffectiveThemeSettings.Mode => Mode ?? ThemeMode.System;
        Color IEffectiveThemeSettings.Accent => Accent ?? Color.Green;
        Color IEffectiveThemeSettings.CheckerA => CheckerA ?? Color.Gray;
        Color IEffectiveThemeSettings.CheckerB => CheckerB ?? Color.DarkGray;
        Color IEffectiveThemeSettings.OutlineColor => OutlineColor ?? Color.White;
        Color IEffectiveThemeSettings.GuideLine => GuideLine ?? Color.White;
        Color IEffectiveThemeSettings.GuideText => GuideText ?? Color.White;
    }

    private sealed class FakeOptionsMonitor : IOptionsMonitor<ThemeSettings>
    {
        public ThemeSettings CurrentValue { get; } = new ThemeSettings();
        public ThemeSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ThemeSettings, string?> listener) => null;
    }
}
