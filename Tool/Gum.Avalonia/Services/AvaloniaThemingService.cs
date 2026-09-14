using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Dialogs;
using Gum.Settings;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Services;

/// <summary>
/// Avalonia implementation of <see cref="IThemingService"/>: the same persisted settings and
/// defaults as the WPF head, applied through the Fluent theme's variant and accent resources, with
/// OS dark mode read from Avalonia's platform settings instead of the Windows registry.
/// </summary>
public class AvaloniaThemingService : IThemingService, IEffectiveThemeSettings
{
    private readonly IMessenger _messenger;
    private readonly IWritableOptions<ThemeSettings> _themeSettings;
    private readonly ThemeDefaultsProvider _defaultsProvider;

    /// <summary>Creates the service over the persisted theme settings.</summary>
    public AvaloniaThemingService(IMessenger messenger, IWritableOptions<ThemeSettings> themeSettings)
    {
        _messenger = messenger;
        _themeSettings = themeSettings;
        _defaultsProvider = new ThemeDefaultsProvider();
    }

    /// <inheritdoc/>
    public IEffectiveThemeSettings EffectiveSettings => this;

    /// <inheritdoc/>
    public void ApplyInitialTheme()
    {
        SwitchMode(Mode ?? _defaultsProvider.Mode);
        SwitchAccent(Accent ?? _defaultsProvider.Accent);
        if (Application.Current?.PlatformSettings is { } platformSettings)
        {
            platformSettings.ColorValuesChanged += (_, _) =>
            {
                if ((Mode ?? _defaultsProvider.Mode) == ThemeMode.System)
                {
                    SwitchMode(ThemeMode.System);
                    _messenger.Send(new ThemeChangedMessage(this));
                }
            };
        }
        _messenger.Send(new ThemeChangedMessage(this));
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public System.Drawing.Color? CheckerA
    {
        get => _themeSettings.CurrentValue.CheckerA;
        set { _themeSettings.Update(s => s.CheckerA = value); _messenger.Send(new ThemeChangedMessage(this)); }
    }
    System.Drawing.Color IEffectiveThemeSettings.CheckerA => _themeSettings.CurrentValue.CheckerA ?? _defaultsProvider.CheckerA(EffectiveSettings.Mode);

    /// <inheritdoc/>
    public System.Drawing.Color? CheckerB
    {
        get => _themeSettings.CurrentValue.CheckerB;
        set { _themeSettings.Update(s => s.CheckerB = value); _messenger.Send(new ThemeChangedMessage(this)); }
    }
    System.Drawing.Color IEffectiveThemeSettings.CheckerB => _themeSettings.CurrentValue.CheckerB ?? _defaultsProvider.CheckerB(EffectiveSettings.Mode);

    /// <inheritdoc/>
    public System.Drawing.Color? OutlineColor
    {
        get => _themeSettings.CurrentValue.OutlineColor;
        set { _themeSettings.Update(s => s.OutlineColor = value); _messenger.Send(new ThemeChangedMessage(this)); }
    }
    System.Drawing.Color IEffectiveThemeSettings.OutlineColor => _themeSettings.CurrentValue.OutlineColor ?? _defaultsProvider.OutlineColor;

    /// <inheritdoc/>
    public System.Drawing.Color? GuideLine
    {
        get => _themeSettings.CurrentValue.GuideLine;
        set { _themeSettings.Update(s => s.GuideLine = value); _messenger.Send(new ThemeChangedMessage(this)); }
    }
    System.Drawing.Color IEffectiveThemeSettings.GuideLine => _themeSettings.CurrentValue.GuideLine ?? _defaultsProvider.GuideLine;

    /// <inheritdoc/>
    public System.Drawing.Color? GuideText
    {
        get => _themeSettings.CurrentValue.GuideText;
        set { _themeSettings.Update(s => s.GuideText = value); _messenger.Send(new ThemeChangedMessage(this)); }
    }
    System.Drawing.Color IEffectiveThemeSettings.GuideText => _themeSettings.CurrentValue.GuideText ?? _defaultsProvider.GuideText;

    /// <inheritdoc/>
    public bool IsSystemInDarkMode =>
        Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;

    private void SwitchMode(ThemeMode mode)
    {
        if (Application.Current == null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }

    private static void SwitchAccent(System.Drawing.Color color)
    {
        if (Application.Current == null)
        {
            return;
        }

        Color accent = Color.FromArgb(color.A, color.R, color.G, color.B);
        // Fluent reads the accent through this resource; overriding it at the application level
        // recolors every accent-driven control, and the Frb.* keys keep the WPF-era names alive
        // for views that are ported over later.
        Application.Current.Resources["SystemAccentColor"] = accent;
        Application.Current.Resources["Frb.Colors.Primary"] = accent;
        Application.Current.Resources["Frb.Brushes.Primary"] = new SolidColorBrush(accent);
        Application.Current.Resources["Frb.Brushes.Primary.Transparent"] = new SolidColorBrush(accent) { Opacity = 0.15 };
        // The tints the WPF ThemingService derives: hover and pressed fills, and the text that reads
        // on the darker one (the default button's foreground).
        Color light = AccentPalette.Lighten(accent);
        Color dark = AccentPalette.Darken(accent);
        Color contrast = AccentPalette.ContrastingForeground(dark);
        Application.Current.Resources["Frb.Colors.Primary.Light"] = light;
        Application.Current.Resources["Frb.Colors.Primary.Dark"] = dark;
        Application.Current.Resources["Frb.Colors.Primary.Contrast"] = contrast;
        Application.Current.Resources["Frb.Brushes.Primary.Light"] = new SolidColorBrush(light);
        Application.Current.Resources["Frb.Brushes.Primary.Dark"] = new SolidColorBrush(dark);
        Application.Current.Resources["Frb.Brushes.Primary.Contrast"] = new SolidColorBrush(contrast);
        // The Fluent control resources that draw from the Primary brushes hold the old ones until re-pointed.
        Themes.FrbThemeResources.ApplyControlAliases(Application.Current.Resources);
    }
}
