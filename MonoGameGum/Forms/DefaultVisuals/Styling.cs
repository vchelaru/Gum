using Gum.DataTypes.Variables;
using RenderingLibrary;
using System.ComponentModel;


#if RAYLIB
using Raylib_cs;
namespace Gum.Forms.DefaultVisuals;

#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace MonoGameGum.Forms.DefaultVisuals;
#endif

public class Styling
{
    /// <summary>
    /// This allows someone to get the active style from any instance they create, or from the class self like Styling.ActiveStyle.
    /// </summary>
    public static Styling ActiveStyle { get; set; }
    public Texture2D SpriteSheet { get; set; }

    public Colors Colors { get; set; } = new ();
    public NineSlice NineSlice { get; set; } = new ();

    public Icons Icons { get; set; } = new();

    public Text Text { get; set; } = new ();

    public Styling(Texture2D? spriteSheet)
    {
#if RAYLIB
        this.SpriteSheet = spriteSheet.Value;
#else
        if (spriteSheet == null)
        {
            this.SpriteSheet = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{SystemManagers.AssemblyPrefix}.UISpriteSheet.png");
        }
        else
        {
            this.SpriteSheet = spriteSheet;
        }
#endif

        if (ActiveStyle == null)
        {
            ActiveStyle = this;
        }
    }

}

public class Colors
{
    private static StateSave CreateTextureCoordinateState(int left, int top, int width, int height)
    {
        return new()
        {
            Variables = new()
            {
                new() { Name = "TextureLeft", Type = "int", Value = left },
                new() { Name = "TextureTop", Type = "int", Value = top },
                new() { Name = "TextureWidth", Type = "int", Value = width },
                new() { Name = "TextureHeight", Type = "int", Value = height },
                new() { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
            }
        };
    }

    public Color Black { get; set; } = new Color(0, 0, 0);
    public Color DarkGray { get; set; } = new Color(70, 70, 80);
    public Color Gray { get; set; } = new Color(130, 130, 130);
    public Color LightGray { get; set; } = new Color(170, 170, 170);
    public Color White { get; set; } = new Color(255, 255, 255);
    public Color PrimaryDark { get; set; } = new Color(4, 120, 137);
    public Color Primary { get; set; } = new Color(6, 159, 177);
    public Color PrimaryLight { get; set; } = new Color(74, 180, 193);
    public Color Success { get; set; } = new Color(62, 167, 48);
    public Color Warning { get; set; } = new Color(232, 171, 25);
    public Color Danger { get; set; } = new Color(212, 18, 41);

    public Color Accent { get; set; } = new Color(140, 48, 138);

}


public class NineSlice
{
    private static StateSave CreateTextureCoordinateState(int left, int top, int width, int height)
    {
        return new()
        {
            Variables = new()
            {
                new() { Name = "TextureLeft", Type = "int", Value = left },
                new() { Name = "TextureTop", Type = "int", Value = top },
                new() { Name = "TextureWidth", Type = "int", Value = width },
                new() { Name = "TextureHeight", Type = "int", Value = height },
                new() { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
            }
        };
    }

    public StateSave Solid = CreateTextureCoordinateState(0, 48, 24, 24);
    public StateSave Bordered = CreateTextureCoordinateState(24, 48, 24, 24);
    public StateSave BracketVertical = CreateTextureCoordinateState(48, 72, 24, 24);
    public StateSave BracketHorizontal = CreateTextureCoordinateState(72, 72, 24, 24);
    public StateSave Tab = CreateTextureCoordinateState(48, 48, 24, 24);
    public StateSave TabBordered = CreateTextureCoordinateState(72, 48, 24, 24);
    public StateSave Outlined = CreateTextureCoordinateState(0, 72, 24, 24);
    public StateSave OutlinedHeavy = CreateTextureCoordinateState(24, 72, 24, 24);
    public StateSave Panel = CreateTextureCoordinateState(96, 48, 24, 24);
    public StateSave CircleSolid = CreateTextureCoordinateState(0, 96, 24, 24);
    public StateSave CircleBordered = CreateTextureCoordinateState(24, 96, 24, 24);
    public StateSave CircleOutlined = CreateTextureCoordinateState(0, 120, 24, 24);
    public StateSave CircleOutlinedHeavy = CreateTextureCoordinateState(24, 120, 24, 24);

}

public class Icons
{
    private static StateSave CreateTextureCoordinateState(int left, int top, int width, int height)
    {
        return new()
        {
            Variables = new()
            {
                new() { Name = "TextureLeft", Type = "int", Value = left },
                new() { Name = "TextureTop", Type = "int", Value = top },
                new() { Name = "TextureWidth", Type = "int", Value = width },
                new() { Name = "TextureHeight", Type = "int", Value = height },
                new() { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
            }
        };
    }

    public StateSave Arrow1 => CreateTextureCoordinateState(288, 256, 32, 32);
    public StateSave Arrow2 => CreateTextureCoordinateState(320, 256, 32, 32);
    public StateSave Arrow3 => CreateTextureCoordinateState(352, 256, 32, 32);
    public StateSave Basket => CreateTextureCoordinateState(288, 224, 32, 32);
    public StateSave Battery => CreateTextureCoordinateState(320, 224, 32, 32);
    public StateSave Check => CreateTextureCoordinateState(384, 128, 32, 32);
    public StateSave CheckeredFlag => CreateTextureCoordinateState(384, 288, 32, 32);
    public StateSave Circle1 => CreateTextureCoordinateState(448, 128, 32, 32);
    public StateSave Circle2 => CreateTextureCoordinateState(416, 128, 32, 32);
    public StateSave Close => CreateTextureCoordinateState(416, 192, 32, 32);
    public StateSave Crosshairs => CreateTextureCoordinateState(352, 288, 32, 32);
    public StateSave Currency => CreateTextureCoordinateState(352, 224, 32, 32);
    public StateSave Cursor => CreateTextureCoordinateState(384, 32, 32, 32);
    public StateSave CursorText => CreateTextureCoordinateState(416, 32, 32, 32);
    public StateSave Dash => CreateTextureCoordinateState(352, 204, 32, 20);
    public StateSave Delete => CreateTextureCoordinateState(288, 320, 32, 32);
    public StateSave Enter => CreateTextureCoordinateState(320, 320, 32, 32);
    public StateSave Expand => CreateTextureCoordinateState(384, 192, 32, 32);
    public StateSave Gamepad => CreateTextureCoordinateState(352, 320, 32, 32);
    public StateSave GamepadNES => CreateTextureCoordinateState(416, 320, 32, 32);
    public StateSave GamepadSNES => CreateTextureCoordinateState(448, 320, 32, 32);
    public StateSave GamepadNintendo64 => CreateTextureCoordinateState(384, 352, 32, 32);
    public StateSave GamepadGamecube => CreateTextureCoordinateState(416, 352, 32, 32);
    public StateSave GamepadSwitchPro => CreateTextureCoordinateState(352, 320, 32, 32);
    public StateSave GamepadXbox => CreateTextureCoordinateState(384, 320, 32, 32);
    public StateSave GamepadPlaystationDualShock => CreateTextureCoordinateState(352, 352, 32, 32);
    public StateSave GamepadSegaGenesis => CreateTextureCoordinateState(448, 352, 32, 32);
    public StateSave Gear => CreateTextureCoordinateState(320, 96, 32, 32);
    public StateSave FastForward => CreateTextureCoordinateState(384, 160, 32, 32);
    public StateSave FastForwardBar => CreateTextureCoordinateState(416, 160, 32, 32);
    public StateSave FitToScreen => CreateTextureCoordinateState(288, 192, 32, 32);
    public StateSave Flame1 => CreateTextureCoordinateState(320, 64, 32, 32);
    public StateSave Flame2 => CreateTextureCoordinateState(352, 64, 32, 32);
    public StateSave Heart => CreateTextureCoordinateState(320, 128, 32, 32);
    public StateSave Info => CreateTextureCoordinateState(416, 256, 32, 32);
    public StateSave Keyboard => CreateTextureCoordinateState(320, 32, 32, 32);
    public StateSave Leaf => CreateTextureCoordinateState(288, 64, 32, 32);
    public StateSave Lightning => CreateTextureCoordinateState(416, 64, 32, 32);
    public StateSave Minimize => CreateTextureCoordinateState(352, 192, 32, 32);
    public StateSave Monitor => CreateTextureCoordinateState(448, 192, 32, 32);
    public StateSave Mouse => CreateTextureCoordinateState(448, 32, 32, 32);
    public StateSave Music => CreateTextureCoordinateState(384, 224, 32, 32);
    public StateSave Pause => CreateTextureCoordinateState(320, 160, 32, 32);
    public StateSave Pencil => CreateTextureCoordinateState(288, 96, 32, 32);
    public StateSave Play => CreateTextureCoordinateState(288, 160, 32, 32);
    public StateSave PlayBar => CreateTextureCoordinateState(448, 160, 32, 32);
    public StateSave Power => CreateTextureCoordinateState(288, 288, 32, 32);
    public StateSave Radiation => CreateTextureCoordinateState(384, 64, 32, 32);
    public StateSave Reduce => CreateTextureCoordinateState(320, 192, 32, 32);
    public StateSave Shield => CreateTextureCoordinateState(416, 288, 32, 32);
    public StateSave Shot => CreateTextureCoordinateState(320, 288, 32, 32);
    public StateSave Skull => CreateTextureCoordinateState(448, 288, 32, 32);
    public StateSave Sliders => CreateTextureCoordinateState(352, 96, 32, 32);
    public StateSave SoundMaximum => CreateTextureCoordinateState(448, 224, 32, 32);
    public StateSave SoundMinimum => CreateTextureCoordinateState(416, 224, 32, 32);
    public StateSave Speech => CreateTextureCoordinateState(448, 96, 32, 32);
    public StateSave Star => CreateTextureCoordinateState(352, 128, 32, 32);
    public StateSave Stop => CreateTextureCoordinateState(352, 160, 32, 32);
    public StateSave Temperature => CreateTextureCoordinateState(448, 64, 32, 32);
    public StateSave Touch => CreateTextureCoordinateState(352, 32, 32, 32);
    public StateSave Trash => CreateTextureCoordinateState(416, 96, 32, 32);
    public StateSave Trophy => CreateTextureCoordinateState(288, 128, 32, 32);
    public StateSave User => CreateTextureCoordinateState(288, 0, 32, 32);
    public StateSave UserAdd => CreateTextureCoordinateState(384, 0, 32, 32);
    public StateSave UserDelete => CreateTextureCoordinateState(416, 0, 32, 32);
    public StateSave UserGear => CreateTextureCoordinateState(352, 0, 32, 32);
    public StateSave UserMulti => CreateTextureCoordinateState(320, 0, 32, 32);
    public StateSave UserRemove => CreateTextureCoordinateState(448, 0, 32, 32);
    public StateSave Warning => CreateTextureCoordinateState(448, 256, 32, 32);
    public StateSave Wrench => CreateTextureCoordinateState(384, 96, 32, 32);
}

public class Text
{
#if RAYLIB
    public StateSave Normal = new();
    public StateSave Strong = new();
    public StateSave Emphasis = new();
#else
    public StateSave Normal = CreateFontState("Arial", 18, false, false);
    public StateSave Strong = CreateFontState("Arial", 18, true, false);
    public StateSave Emphasis = CreateFontState("Arial", 18, false, true);
    private static StateSave CreateFontState(string fontName, int fontSize, bool isBold, bool isItalic)
    {
        return new()
        {
            Variables = new()
                {
                    new () { Name = "Font", Type = "string", Value = fontName },
                    new () { Name = "FontSize", Type = "int", Value = fontSize },
                    new () { Name = "IsBold", Type = "bool", Value = isBold },
                    new () { Name = "IsItalic", Type = "bool", Value = isItalic }
                }
        };
    }
#endif

}

