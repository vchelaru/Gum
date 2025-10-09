using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Win32;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Gum.Mvvm;
using Microsoft.Extensions.Options;

namespace Gum.Dialogs;

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
        get => _themingService.Mode;
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
        get => _themingService.EffectiveSettings.Accent.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.Accent)
            {
                _themingService.Accent = value?.ToDrawingColor();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitAccentColor));
            }
        }
    }

    public Color? CheckerAColor
    {
        get => _themingService.EffectiveSettings.CheckerA.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.CheckerA)
            {
                _themingService.CheckerA = value?.ToDrawingColor();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitCheckerAColor));
            }
        }
    }

    public Color? CheckerBColor
    {
        get => _themingService.EffectiveSettings.CheckerB.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.CheckerB)
            {
                _themingService.CheckerB = value?.ToDrawingColor();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitCheckerBColor));
            }
        }
    }

    public Color? OutlineColor
    {
        get => _themingService.EffectiveSettings.OutlineColor.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.OutlineColor)
            {
                _themingService.OutlineColor = value?.ToDrawingColor();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitOutlineColor));
            }
        }
    }

    public Color? GuideLineColor
    {
        get => _themingService.EffectiveSettings.GuideLine.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.GuideLine)
            {
                _themingService.GuideLine = value?.ToDrawingColor();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HasExplicitGuideLineColor));
            }
        }
    }

    public Color? GuideTextColor
    {
        get => _themingService.EffectiveSettings.GuideText.ToColor();
        set
        {
            if (value?.ToDrawingColor() != _themingService.GuideText)
            {
                _themingService.GuideText = value?.ToDrawingColor();
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

file static class Helpers
{
    public static Color ToColor(this System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static System.Drawing.Color ToDrawingColor(this Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}

public enum ThemeMode
{
    System,
    Light,
    Dark,
}


public interface IThemingService
{
    IEffectiveThemeSettings EffectiveSettings { get; }
    ThemeMode? Mode { get; set; }
    System.Drawing.Color? Accent { get; set; }
    System.Drawing.Color? CheckerA { get; set; }
    System.Drawing.Color? CheckerB { get; set; }
    System.Drawing.Color? OutlineColor { get; set; }
    System.Drawing.Color? GuideLine { get; set; }
    System.Drawing.Color? GuideText { get; set; }
    bool IsSystemInDarkMode { get; }
    void ApplyInitialTheme();
}

public interface IEffectiveThemeSettings
{
    ThemeMode Mode { get; }
    System.Drawing.Color Accent { get; }
    System.Drawing.Color CheckerA { get; }
    System.Drawing.Color CheckerB { get; }
    System.Drawing.Color OutlineColor { get; }
    System.Drawing.Color GuideLine { get; }
    System.Drawing.Color GuideText { get; }
    bool IsSystemInDarkMode { get; }
}


public class ThemingService : IThemingService, IEffectiveThemeSettings
{
    static readonly Uri LightUri = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Light.xaml");
    static readonly Uri DarkUri  = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Dark.xaml");
    
    private readonly IMessenger _messenger;
    private readonly IWritableOptions<ThemeSettings> _themeSettings;
    private readonly ThemeDefaultsProvider _defaultsProvider = new();

    public IEffectiveThemeSettings EffectiveSettings => this;

    public ThemingService(IMessenger messenger, IWritableOptions<ThemeSettings> themeSettings)
    {
        _messenger = messenger;
        _themeSettings = themeSettings;
    }

    public void ApplyInitialTheme()
    {
        SwitchMode(Mode ?? _defaultsProvider.Mode);
        SwitchAccent(Accent ?? _defaultsProvider.Accent);
        _messenger.Send(new ThemeChangedMessage(this));
    }

    public ThemeMode? Mode
    {
        get => _themeSettings.CurrentValue.Mode;
        set
        {
            SwitchMode(value ?? _defaultsProvider.Mode);
            _themeSettings.Update(s => s.Mode = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }

    ThemeMode IEffectiveThemeSettings.Mode => (Mode ?? _defaultsProvider.Mode) switch
    {
        { } val when val is ThemeMode.Dark or ThemeMode.Light => val,
        _ => IsSystemInDarkMode ? ThemeMode.Dark : ThemeMode.Light,
    };

    public System.Drawing.Color? Accent
    {
        get => _themeSettings.CurrentValue.Accent;
        set
        {
            SwitchAccent(value ?? _defaultsProvider.Accent);
            _themeSettings.Update(s => s.Accent = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    
    System.Drawing.Color IEffectiveThemeSettings.Accent => _themeSettings.CurrentValue.Accent ?? _defaultsProvider.Accent;

    public System.Drawing.Color? CheckerA
    {
        get => _themeSettings.CurrentValue.CheckerA;
        set
        {
            _themeSettings.Update(s => s.CheckerA = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    System.Drawing.Color IEffectiveThemeSettings.CheckerA => _themeSettings.CurrentValue.CheckerA ?? _defaultsProvider.CheckerA(EffectiveSettings.Mode);

    public System.Drawing.Color? CheckerB
    {
        get => _themeSettings.CurrentValue.CheckerB;
        set
        {
            _themeSettings.Update(s => s.CheckerB = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    System.Drawing.Color IEffectiveThemeSettings.CheckerB => _themeSettings.CurrentValue.CheckerB ?? _defaultsProvider.CheckerB(EffectiveSettings.Mode);

    public System.Drawing.Color? OutlineColor
    {
        get => _themeSettings.CurrentValue.OutlineColor;
        set
        {
            _themeSettings.Update(s => s.OutlineColor = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    System.Drawing.Color IEffectiveThemeSettings.OutlineColor => _themeSettings.CurrentValue.OutlineColor ?? _defaultsProvider.OutlineColor;

    public System.Drawing.Color? GuideLine
    {
        get => _themeSettings.CurrentValue.GuideLine;
        set
        {
            _themeSettings.Update(s => s.GuideLine = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    System.Drawing.Color IEffectiveThemeSettings.GuideLine => _themeSettings.CurrentValue.GuideLine ?? _defaultsProvider.GuideLine;

    public System.Drawing.Color? GuideText
    {
        get => _themeSettings.CurrentValue.GuideText;
        set
        {
            _themeSettings.Update(s => s.GuideText = value);
            _messenger.Send(new ThemeChangedMessage(this));
        }
    }
    System.Drawing.Color IEffectiveThemeSettings.GuideText => _themeSettings.CurrentValue.GuideText ?? _defaultsProvider.GuideText;

    private void SwitchMode(ThemeMode mode)
    {
        ThemeMode effectiveMode = mode switch
        {
            ThemeMode.Dark or ThemeMode.Light => mode,
            _ => IsSystemInDarkMode ? ThemeMode.Dark : ThemeMode.Light,
        };

        ResourceDictionary next = effectiveMode switch
        {
            ThemeMode.Dark => new() { Source = DarkUri },
            ThemeMode.Light => new() { Source = LightUri },
            _ => throw new InvalidOperationException("We shouldn't get here.")
        };
        ResourceDictionary current =
            Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == LightUri || d.Source == DarkUri);

        Application.Current.Resources.MergedDictionaries.Remove(current);
        Application.Current.Resources.MergedDictionaries.Add(next);
    }

    private void SwitchAccent(System.Drawing.Color color)
    {
        if (color is { } c && Color.FromArgb(c.A, c.R, c.G, c.B) is { } accent)
        {
            // Update colors first
            Application.Current.Resources["Frb.Colors.Primary"] = accent;
            Application.Current.Resources["Frb.Colors.Primary.Dark"] = MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent);
            Application.Current.Resources["Frb.Colors.Primary.Light"] = MaterialDesignColors.ColorManipulation.ColorAssist.Lighten(accent);
            Application.Current.Resources["Frb.Colors.Primary.Contrast"] = MaterialDesignColors.ColorManipulation.ColorAssist.ContrastingForegroundColor(MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent));

            // Force brush recreation by directly setting new brush instances
            Application.Current.Resources["Frb.Brushes.Primary"] = new SolidColorBrush(accent);
            Application.Current.Resources["Frb.Brushes.Primary.Transparent"] = new SolidColorBrush(accent) { Opacity = 0.15 };
            Application.Current.Resources["Frb.Brushes.Primary.Light"] = new SolidColorBrush(MaterialDesignColors.ColorManipulation.ColorAssist.Lighten(accent));
            Application.Current.Resources["Frb.Brushes.Primary.Dark"] = new SolidColorBrush(MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent));
            Application.Current.Resources["Frb.Brushes.Primary.Contrast"] = new SolidColorBrush(MaterialDesignColors.ColorManipulation.ColorAssist.ContrastingForegroundColor(MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent)));
        }
    }

    public bool IsSystemInDarkMode
    {
        get
        {
            const string key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(key);
            if (personalizeKey == null)
                return false; // default to light if not found 

            var appsUseLightTheme = personalizeKey.GetValue("AppsUseLightTheme");
            if (appsUseLightTheme is int value)
            {
                return value == 0; // 0 = dark, 1 = light 
            }

            return false;
        }
    }
}