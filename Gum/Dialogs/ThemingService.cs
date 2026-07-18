using CommunityToolkit.Mvvm.Messaging;
using Gum.Settings;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Gum.Dialogs;

/// <summary>
/// WPF implementation of <see cref="IThemingService"/>. Drives the running application's merged
/// resource dictionaries and brushes directly (<see cref="Application.Current"/>), so it stays in
/// the Gum tool project rather than the headless Gum.Presentation assembly that hosts the interface
/// (ADR-0005).
/// </summary>
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
