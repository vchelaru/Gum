using CommunityToolkit.Mvvm.Messaging;
using Gum.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Settings;

public class ThemeSettingsResolver : IRecipient<ThemeChangedMessage>
{
    private readonly ThemeDefaultsProvider _defaultsProvider = new();
    private readonly IThemingService _themingService;
    private readonly IWritableOptions<ThemeSettings> _themeSettings;
    private ThemeSettings _current => _themeSettings.CurrentValue;

    public ThemeSettingsResolver(
        IMessenger messenger,
        IThemingService themingService,
        IWritableOptions<ThemeSettings> settings)
    {
        _themingService = themingService;
        _themeSettings = settings;

        messenger.RegisterAll(this);
    }

    public ThemeMode EffectiveMode { get; private set; }

    public ThemeMode Mode
    {
        get => _current.Mode ?? _defaultsProvider.Mode;
        set
        {
            if (_current.Mode != value)
            {
                _themeSettings.Update(s => s.Mode = value);
                _themingService.SwitchMode(value);
            }
        }
    }

    public Color Accent
    {
        get => _current.Accent ?? _defaultsProvider.Accent;
        set
        {
            if (_current.Accent != value)
            {
                _themeSettings.Update(s => s.Accent = value);
                _themingService.SwitchAccent(value);
            }
        }
    }

    public Color CheckerA
    {
        get => _current.CheckerA ?? _defaultsProvider.CheckerA(EffectiveMode);
        set => _themeSettings.Update(s => s.CheckerA = value);
    }

    public Color CheckerB
    {
        get => _current.CheckerB ?? _defaultsProvider.CheckerB(EffectiveMode);
        set => _themeSettings.Update(s => s.CheckerB = value);
    }
    public Color OutlineColor
    {
        get => _current.OutlineColor ?? _defaultsProvider.OutlineColor;
        set => _themeSettings.Update(s => s.OutlineColor = value);
    }
    public Color GuideLine
    {
        get => _current.GuideLine ?? _defaultsProvider.GuideLine;
        set => _themeSettings.Update(s => s.GuideLine = value);
    }
    public Color GuideText
    {
        get => _current.GuideText ?? _defaultsProvider.GuideText;
        set => _themeSettings.Update(s => s.GuideText = value);
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        EffectiveMode = message.Mode;
    }
}