using CommunityToolkit.Mvvm.Input;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Gum.Dialogs;

/// <summary>
/// Backs the "Theming" dialog. Colors are exposed as <see cref="Color"/> (headless
/// <c>System.Drawing.Color</c>, per ADR-0004) rather than WPF's <c>System.Windows.Media.Color</c>;
/// the WPF View converts at the binding boundary so this view model can live in the headless
/// Gum.Presentation assembly (ADR-0005). See <see cref="IThemingService"/> for the split-out
/// interface and <c>Gum.Dialogs.ThemingService</c> (Gum tool project) for the WPF implementation.
/// </summary>
public partial class ThemingDialogViewModel : DialogViewModel
{
    private readonly IThemingService _themingService;
    private ThemeSettings _cachedSettings;

    public ThemingDialogViewModel(IThemingService themingService, IOptionsMonitor<ThemeSettings> settings)
    {
        _themingService = themingService;

        // Create a deep copy of the current settings for cancellation
        var currentSettings = settings.CurrentValue;
        _cachedSettings = new ThemeSettings
        {
            Mode = currentSettings.Mode,
            Accent = currentSettings.Accent,
            CheckerA = currentSettings.CheckerA,
            CheckerB = currentSettings.CheckerB,
            OutlineColor = currentSettings.OutlineColor,
            GuideLine = currentSettings.GuideLine,
            GuideText = currentSettings.GuideText
        };
    }

    public ThemeMode? Mode
    {
        get => _themingService.Mode ?? ThemeMode.System;
        set
        {
            if (value != _themingService.Mode)
            {
                _themingService.Mode = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CheckerAColor));
                NotifyPropertyChanged(nameof(CheckerBColor));
            }
        }
    }

    public Color? AccentColor
    {
        get => _themingService.EffectiveSettings.Accent;
        set
        {
            if (value != _themingService.Accent)
            {
                _themingService.Accent = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitAccentColor));
            }
        }
    }

    public Color? CheckerAColor
    {
        get => _themingService.EffectiveSettings.CheckerA;
        set
        {
            if (value != _themingService.CheckerA)
            {
                _themingService.CheckerA = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitCheckerAColor));
            }
        }
    }

    public Color? CheckerBColor
    {
        get => _themingService.EffectiveSettings.CheckerB;
        set
        {
            if (value != _themingService.CheckerB)
            {
                _themingService.CheckerB = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitCheckerBColor));
            }
        }
    }

    public Color? OutlineColor
    {
        get => _themingService.EffectiveSettings.OutlineColor;
        set
        {
            if (value != _themingService.OutlineColor)
            {
                _themingService.OutlineColor = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitOutlineColor));
            }
        }
    }

    public Color? GuideLineColor
    {
        get => _themingService.EffectiveSettings.GuideLine;
        set
        {
            if (value != _themingService.GuideLine)
            {
                _themingService.GuideLine = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitGuideLineColor));
            }
        }
    }

    public Color? GuideTextColor
    {
        get => _themingService.EffectiveSettings.GuideText;
        set
        {
            if (value != _themingService.GuideText)
            {
                _themingService.GuideText = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitGuideTextColor));
            }
        }
    }

    // Properties to check if settings are explicitly set (not null)
    public bool HasExplicitAccentColor => _themingService.Accent != null;
    public bool HasExplicitCheckerAColor => _themingService.CheckerA != null;
    public bool HasExplicitCheckerBColor => _themingService.CheckerB != null;
    public bool HasExplicitOutlineColor => _themingService.OutlineColor != null;
    public bool HasExplicitGuideLineColor => _themingService.GuideLine != null;
    public bool HasExplicitGuideTextColor => _themingService.GuideText != null;

    [RelayCommand]
    private void Reset(string? propertyName)
    {
        if (propertyName is not null)
        {
            switch (propertyName)
            {
                case nameof(AccentColor):
                    _themingService.Accent = null;
                    NotifyPropertyChanged(nameof(AccentColor));
                    NotifyPropertyChanged(nameof(HasExplicitAccentColor));
                    break;
                case nameof(CheckerAColor):
                    _themingService.CheckerA = null;
                    NotifyPropertyChanged(nameof(CheckerAColor));
                    NotifyPropertyChanged(nameof(HasExplicitCheckerAColor));
                    break;
                case nameof(CheckerBColor):
                    _themingService.CheckerB = null;
                    NotifyPropertyChanged(nameof(CheckerBColor));
                    NotifyPropertyChanged(nameof(HasExplicitCheckerBColor));
                    break;
                case nameof(OutlineColor):
                    _themingService.OutlineColor = null;
                    NotifyPropertyChanged(nameof(OutlineColor));
                    NotifyPropertyChanged(nameof(HasExplicitOutlineColor));
                    break;
                case nameof(GuideLineColor):
                    _themingService.GuideLine = null;
                    NotifyPropertyChanged(nameof(GuideLineColor));
                    NotifyPropertyChanged(nameof(HasExplicitGuideLineColor));
                    break;
                case nameof(GuideTextColor):
                    _themingService.GuideText = null;
                    NotifyPropertyChanged(nameof(GuideTextColor));
                    NotifyPropertyChanged(nameof(HasExplicitGuideTextColor));
                    break;
            }
        }
        else
        {
            // Reset all colors
            _themingService.Accent = null;
            _themingService.CheckerA = null;
            _themingService.CheckerB = null;
            _themingService.OutlineColor = null;
            _themingService.GuideLine = null;
            _themingService.GuideText = null;

            // Notify all property changes
            NotifyPropertyChanged(nameof(AccentColor));
            NotifyPropertyChanged(nameof(CheckerAColor));
            NotifyPropertyChanged(nameof(CheckerBColor));
            NotifyPropertyChanged(nameof(OutlineColor));
            NotifyPropertyChanged(nameof(GuideLineColor));
            NotifyPropertyChanged(nameof(GuideTextColor));
            NotifyPropertyChanged(nameof(HasExplicitAccentColor));
            NotifyPropertyChanged(nameof(HasExplicitCheckerAColor));
            NotifyPropertyChanged(nameof(HasExplicitCheckerBColor));
            NotifyPropertyChanged(nameof(HasExplicitOutlineColor));
            NotifyPropertyChanged(nameof(HasExplicitGuideLineColor));
            NotifyPropertyChanged(nameof(HasExplicitGuideTextColor));
        }
    }

    public List<ThemeMode> ThemeModes { get; } = [..Enum.GetValues(typeof(ThemeMode)).OfType<ThemeMode>()];

    protected override void OnNegative()
    {
        // Restore all cached settings when user cancels
        _themingService.Mode = _cachedSettings.Mode;
        _themingService.Accent = _cachedSettings.Accent;
        _themingService.CheckerA = _cachedSettings.CheckerA;
        _themingService.CheckerB = _cachedSettings.CheckerB;
        _themingService.OutlineColor = _cachedSettings.OutlineColor;
        _themingService.GuideLine = _cachedSettings.GuideLine;
        _themingService.GuideText = _cachedSettings.GuideText;
        base.OnNegative();
    }
}
