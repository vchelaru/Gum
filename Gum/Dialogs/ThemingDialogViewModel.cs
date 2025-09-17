using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

public class ThemingDialogViewModel : DialogViewModel
{
    private readonly IThemingService _themingService;
    private ThemeConfig? _cachedConfig;
    
    public ThemingDialogViewModel(IThemingService themingService)
    {
        _themingService = themingService;
        
        _cachedConfig = themingService.CurrentTheme;
        
        if (_cachedConfig is { } savedConfig)
        {
            SolidColorBrush? currentAccent = savedConfig.Accent is { } accent ? 
                AccentOptions.FirstOrDefault(x => x.Color == accent) ?? new(accent) : null;
            ThemeMode? currentMode = savedConfig.Mode;
            
            SetWithoutNotifying(currentAccent, nameof(CurrentAccent));
            SetWithoutNotifying(currentMode, nameof(CurrentMode));
        }
    }

    public ThemeMode CurrentMode
    {
        get => Get<ThemeMode>();
        set
        {
            if (Set(value))
            {
                _themingService.SwitchThemes(new ThemeConfig(CurrentMode, CurrentAccent?.Color));
            }
        } 
    }

    public SolidColorBrush? CurrentAccent
    {
        get => Get<SolidColorBrush>();
        set
        {
            if (Set(value))
            {
                _themingService.SwitchThemes(new ThemeConfig(CurrentMode, CurrentAccent?.Color));
            }
        }
    }

    public List<ThemeMode> ThemeModes { get; } = [..Enum.GetValues(typeof(ThemeMode)).OfType<ThemeMode>().Take(2)];
    public List<SolidColorBrush> AccentOptions { get; } = new ()
    {
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

    protected override void OnNegative()
    {
        _themingService.SwitchThemes(_cachedConfig);
        base.OnNegative();
    }
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}

public record ThemeConfig(ThemeMode? Mode = null, Color? Accent = null)
{
    public ThemeConfig() : this(null, null) { }
}

public interface IThemingService
{
    void SwitchThemes(ThemeConfig config);
    
    ThemeConfig? CurrentTheme { get; }
}

public class ThemingService : IThemingService
{
    static readonly Uri LightUri = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Light.xaml");
    static readonly Uri DarkUri  = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Dark.xaml");
    static readonly Uri AccentsUri = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Brushes.Accents.xaml");
    
    private readonly IDispatcher _dispatcher;
    private readonly IMessenger _messenger;
    
    public static ThemeConfig? CurrentTheme { get; private set; }
    
    ThemeConfig? IThemingService.CurrentTheme => CurrentTheme;
    
    private ThemeConfig DefaultThemeConfig { get; } = new(ThemeMode.Light, (Color)ColorConverter.ConvertFromString("#3E9ECE"));

    public ThemingService(IDispatcher dispatcher, IMessenger messenger)
    {
        _dispatcher = dispatcher;
        _messenger = messenger;
    }
    
    public void SwitchThemes(ThemeConfig? config)
    {
        ThemeConfig cached = CurrentTheme ?? DefaultThemeConfig;
        
        config = config switch
        {
            null => DefaultThemeConfig,
            (null, { }) => config with { Mode = (CurrentTheme ?? DefaultThemeConfig).Mode },
            ({ }, null) => config with { Accent = (CurrentTheme ?? DefaultThemeConfig).Accent },
            _ => config
        };

        CurrentTheme = config;

        _dispatcher.Post(() =>
        {
            if (cached.Accent != config.Accent && config.Accent is { } accent)
            {
                if (TryFind(Application.Current.Resources, 
                        d => d.Source?.ToString().EndsWith("Frb.Accents.xaml") is true, 
                        out _, out _, out var resource))
                {
                    resource["Frb.Colors.Primary"] = accent;
                    resource["Frb.Colors.Primary.Dark"] = MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent);
                    resource["Frb.Colors.Primary.Light"] = MaterialDesignColors.ColorManipulation.ColorAssist.Lighten(accent);
                    resource["Frb.Colors.Primary.Contrast"] = MaterialDesignColors.ColorManipulation.ColorAssist.ContrastingForegroundColor(MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent));
                }
            }

            if (TryFind(Application.Current.Resources.MergedDictionaries, 
                    d =>  d.Source?.ToString() is { } s &&
                          (s.EndsWith("Frb.Brushes.Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                           s.EndsWith("Frb.Brushes.Light.xaml", StringComparison.OrdinalIgnoreCase)), 
                    out _, out _, out var themeDict))
            {
                themeDict!.Source = config.Mode == ThemeMode.Dark
                    ? DarkUri
                    : LightUri;
            }
        
            _messenger.Send(new ThemeChangedMessage(config.Mode!.Value));
        });
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
}