using Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Services;

namespace EditorTabPlugin_XNA.Services;
internal class BackgroundSpriteService : IRecipient<ThemeChangedMessage>
{
    private readonly WireframeCommands _wireframeCommands;
    private readonly IMessenger _messenger;
    
    Sprite BackgroundSprite;
    SolidRectangle BackgroundSolidColor;

    public BackgroundSpriteService()
    {
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
        _messenger = Locator.GetRequiredService<IMessenger>();
        _messenger.RegisterAll(this);
    }

    public void Initialize(SystemManagers systemManagers)
    {
        BackgroundSolidColor = new SolidRectangle();
        BackgroundSolidColor.Name = "Background Solid Color";
        BackgroundSolidColor.Width = 8192*4;
        BackgroundSolidColor.Height = 8192*4;
        BackgroundSolidColor.X = -BackgroundSolidColor.Width / 2.0f;
        BackgroundSolidColor.Y = -BackgroundSolidColor.Height / 2.0f;
        systemManagers.ShapeManager.Add(BackgroundSolidColor);

        // Create the Texture2D here
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

        Texture2D texture = imageData.ToTexture2D(false);
        texture.Name = "Background Checkerboard";

        BackgroundSprite = new Sprite(texture);
        BackgroundSprite.Name = "Background checkerboard Sprite";
        BackgroundSprite.Wrap = true;
        BackgroundSprite.Width = 8192*2;
        BackgroundSprite.Height = 8192*2;
        BackgroundSprite.X = -BackgroundSprite.Width/2;
        BackgroundSprite.Y = -BackgroundSprite.Height/2;
        BackgroundSprite.Color = System.Drawing.Color.FromArgb(255, 150, 150, 150);

        BackgroundSprite.Wrap = true;
        int timesToRepeat = 256*2;
        BackgroundSprite.SourceRectangle =
            new System.Drawing.Rectangle(0, 0, timesToRepeat * texture.Width, timesToRepeat * texture.Height);

        systemManagers.SpriteManager.Add(BackgroundSprite);
    }

    public void Activity()
    {
        BackgroundSprite.Visible =
                _wireframeCommands.IsBackgroundGridVisible;

        // if (ProjectManager.Self.GeneralSettingsFile != null)
        // {
        //     BackgroundSolidColor.Color = System.Drawing.Color.FromArgb(255,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor1R,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor1G,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor1B
        //     );
        //
        //
        //     BackgroundSprite.Color = System.Drawing.Color.FromArgb(255,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor2R,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor2G,
        //         ProjectManager.Self.GeneralSettingsFile.CheckerColor2B
        //     );
        //
        // }
        (var checkerColor1, var checkerColor2) = GetCheckerColors();
        BackgroundSolidColor.Color = checkerColor1;
        BackgroundSprite.Color = checkerColor2;
    }

    public (System.Drawing.Color, System.Drawing.Color) GetCheckerColors()
    {
        return GetThemeDefaultCheckerColors();
    }

    private (System.Drawing.Color, System.Drawing.Color) GetThemeDefaultCheckerColors()
    {
        return _currentThemeMode switch
        {
            ThemeMode.Dark => (System.Drawing.Color.FromArgb(255, 45, 45, 45),
                System.Drawing.Color.FromArgb(255, 62, 62, 62)),
            _ => (System.Drawing.Color.FromArgb(255, 150, 150, 150), System.Drawing.Color.FromArgb(255, 170, 170, 170)),
        };
    }

    private ThemeMode _currentThemeMode;
    
    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        _currentThemeMode = message.Mode;
    }
}