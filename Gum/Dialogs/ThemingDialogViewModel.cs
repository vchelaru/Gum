using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Gum.Dialogs;

public class ThemingDialogViewModel : DialogViewModel
{
    private readonly IThemingService _themingService;
    private readonly ThemeSettingsResolver _colorSettings;
    private ThemeMode CachedMode { get; }
    private System.Drawing.Color CachedAccent { get; }
    
    public ThemingDialogViewModel(IThemingService themingService, ThemeSettingsResolver colorSettings)
    {
        _themingService = themingService;
        _colorSettings = colorSettings;

        CachedMode = colorSettings.Mode;
        CachedAccent = colorSettings.Accent;

        var c = Color.FromArgb(CachedAccent.A, CachedAccent.R, CachedAccent.G, CachedAccent.B);
        CurrentAccent = AccentOptions.FirstOrDefault(b => b.Color == c) ?? new(c);
        CurrentMode = CachedMode;
    }

    public ThemeMode CurrentMode
    {
        get => Get<ThemeMode>();
        set
        {
            if (Set(value))
            {
                _themingService.SwitchMode(value);
            }
        } 
    }

    public SolidColorBrush CurrentAccent
    {
        get => Get<SolidColorBrush>();
        set
        {
            if (Set(value))
            {
                var c = value.Color;
                _themingService.SwitchAccent(System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B));
            }
        }
    }

    public List<ThemeMode> ThemeModes { get; } = [..Enum.GetValues(typeof(ThemeMode)).OfType<ThemeMode>()];
    public static List<SolidColorBrush> AccentOptions { get; } = new ()
    {
        new ((Color)ColorConverter.ConvertFromString("#6cc395")), // Gum Green
        new ((Color)ColorConverter.ConvertFromString("#3E9ECE")), // Default FRB Blue
        new ((Color)ColorConverter.ConvertFromString("#F44336")), // Red
        new ((Color)ColorConverter.ConvertFromString("#E91E63")), // Pink
        new ((Color)ColorConverter.ConvertFromString("#9C27B0")), // Purple
        new ((Color)ColorConverter.ConvertFromString("#673AB7")), // Deep Purple
        new ((Color)ColorConverter.ConvertFromString("#3F51B5")), // Indigo
        new ((Color)ColorConverter.ConvertFromString("#2196F3")), // Blue
        new ((Color)ColorConverter.ConvertFromString("#03A9F4")), // Light Blue
        new ((Color)ColorConverter.ConvertFromString("#00BCD4")), // Cyan
        new ((Color)ColorConverter.ConvertFromString("#009688")), // Teal
        new ((Color)ColorConverter.ConvertFromString("#4CAF50")), // Green
        new ((Color)ColorConverter.ConvertFromString("#8BC34A")), // Light Green
        new ((Color)ColorConverter.ConvertFromString("#CDDC39")), // Lime
        new ((Color)ColorConverter.ConvertFromString("#FFEB3B")), // Yellow
        new ((Color)ColorConverter.ConvertFromString("#FFC107")), // Amber
        new ((Color)ColorConverter.ConvertFromString("#FF9800")), // Orange
        new ((Color)ColorConverter.ConvertFromString("#FF5722")), // Deep Orange
        new ((Color)ColorConverter.ConvertFromString("#795548")), // Brown
        new ((Color)ColorConverter.ConvertFromString("#9E9E9E")), // Grey
        new ((Color)ColorConverter.ConvertFromString("#607D8B"))  // Blue Grey
    };

    protected override void OnAffirmative()
    {
        if (_colorSettings.Mode != CurrentMode)
        {
            _colorSettings.Mode = CurrentMode;
        }
        var c = CurrentAccent.Color;
        if (_colorSettings.Accent != System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B))
        {
            _colorSettings.Accent = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }
        base.OnAffirmative();
    }

    protected override void OnNegative()
    {
        _themingService.SwitchMode(CachedMode);
        _themingService.SwitchAccent(CachedAccent);
        base.OnNegative();
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
    void SwitchMode(ThemeMode mode);
    void SwitchAccent(System.Drawing.Color color);
    bool IsSystemInDarkMode { get; }
}

public class ThemingService : IThemingService
{
    static readonly Uri LightUri = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Light.xaml");
    static readonly Uri DarkUri  = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Dark.xaml");
    static readonly Uri AccentsUri = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Accents.xaml");
    
    private readonly IDispatcher _dispatcher;
    private readonly IMessenger _messenger;

    public ThemingService(IDispatcher dispatcher, IMessenger messenger)
    {
        _dispatcher = dispatcher;
        _messenger = messenger;
    }

    public void SwitchMode(ThemeMode mode)
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

        _messenger.Send(new ThemeChangedMessage(effectiveMode));
    }

    public void SwitchAccent(System.Drawing.Color color)
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

        ThemeMode current = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source == LightUri) != null
            ? ThemeMode.Light
            : ThemeMode.Dark;
        _messenger.Send(new ThemeChangedMessage(current));
    }
    
    static bool TryFind(
        ICollection<ResourceDictionary> roots,
        Predicate<ResourceDictionary> match,
        out ResourceDictionary? parent, out int index, out ResourceDictionary? found)
    {
        parent = null; index = -1; found = null;
        int i = 0;
        foreach (var r in roots)
        {
            if (match(r)) { parent = null; index = i; found = r; return true; }
            if (TryFind(r, match, out parent, out index, out found)) return true;
            i++;
        }
        return false;
    }

    static bool TryFind(
        ResourceDictionary root,
        Predicate<ResourceDictionary> match,
        out ResourceDictionary? parent, out int index, out ResourceDictionary? found)
    {
        parent = null; index = -1; found = null;
        for (int i = 0; i < root.MergedDictionaries.Count; i++)
        {
            var child = root.MergedDictionaries[i];
            if (match(child)) { parent = root; index = i; found = child; return true; }
            if (TryFind(child, match, out parent, out index, out found)) return true;
        }
        return false;
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