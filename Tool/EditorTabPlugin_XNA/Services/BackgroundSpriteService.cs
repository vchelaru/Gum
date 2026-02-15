using Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Services;
using Gum.Settings;

namespace EditorTabPlugin_XNA.Services;
internal class BackgroundSpriteService : IRecipient<ThemeChangedMessage>, IDisposable
{
    private const int BackgroundSolidSize = 8192 * 4;
    private const int BackgroundSpriteSize = 8192 * 2;
    private const int CheckerboardRepeatCount = 256 * 2;

    private readonly WireframeCommands _wireframeCommands;
    private readonly IMessenger _messenger;
    private readonly IThemingService _themingService;

    private Sprite _backgroundSprite;
    private SolidRectangle _backgroundSolidColor;

    public BackgroundSpriteService(
        WireframeCommands wireframeCommands,
        IMessenger messenger,
        IThemingService themingService)
    {
        _wireframeCommands = wireframeCommands;
        _messenger = messenger;
        _themingService = themingService;
        _messenger.RegisterAll(this);
    }

    public void Initialize(SystemManagers systemManagers)
    {
        _backgroundSolidColor = new SolidRectangle();
        _backgroundSolidColor.Name = "Background Solid Color";
        _backgroundSolidColor.Width = BackgroundSolidSize;
        _backgroundSolidColor.Height = BackgroundSolidSize;
        _backgroundSolidColor.X = -_backgroundSolidColor.Width / 2.0f;
        _backgroundSolidColor.Y = -_backgroundSolidColor.Height / 2.0f;
        systemManagers.ShapeManager.Add(_backgroundSolidColor);

        Texture2D texture = CreateCheckerboardTexture(systemManagers);

        _backgroundSprite = new Sprite(texture);
        _backgroundSprite.Name = "Background checkerboard Sprite";
        _backgroundSprite.Wrap = true;
        _backgroundSprite.Width = BackgroundSpriteSize;
        _backgroundSprite.Height = BackgroundSpriteSize;
        _backgroundSprite.X = -_backgroundSprite.Width / 2;
        _backgroundSprite.Y = -_backgroundSprite.Height / 2;
        _backgroundSprite.SourceRectangle =
            new System.Drawing.Rectangle(0, 0, CheckerboardRepeatCount * texture.Width, CheckerboardRepeatCount * texture.Height);

        systemManagers.SpriteManager.Add(_backgroundSprite);

        var themeSettings = _themingService.EffectiveSettings;
        ApplyThemingSettings(themeSettings);
    }

    private Texture2D CreateCheckerboardTexture(SystemManagers systemManagers)
    {
        ImageData imageData = new ImageData(2, 2, systemManagers);

        Microsoft.Xna.Framework.Color opaqueColor = Microsoft.Xna.Framework.Color.White;
        Microsoft.Xna.Framework.Color transparent = new Microsoft.Xna.Framework.Color(0, 0, 0, 0);

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                bool isDark = ((x + y) % 2 == 0);
                if (isDark)
                {
                    imageData.SetPixel(x, y, transparent);
                }
                else
                {
                    imageData.SetPixel(x, y, opaqueColor);
                }
            }
        }

        Texture2D texture = imageData.ToTexture2D(generateMipmaps: false);
        texture.Name = "Background Checkerboard";

        return texture;
    }

    public void Activity()
    {
        Debug.Assert(_backgroundSprite != null, "BackgroundSpriteService.Initialize must be called before Activity");
        Debug.Assert(_backgroundSolidColor != null, "BackgroundSpriteService.Initialize must be called before Activity");

        _backgroundSprite.Visible = _wireframeCommands.IsBackgroundGridVisible;
    }

    private void ApplyThemingSettings(IEffectiveThemeSettings settings)
    {
        _backgroundSolidColor.Color = settings.CheckerA;
        _backgroundSprite.Color = settings.CheckerB;
    }
    
    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        ApplyThemingSettings(message.settings);
    }

    public void Dispose()
    {
        _messenger.UnregisterAll(this);
    }
}