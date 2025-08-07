using Gum.DataTypes.Variables;
using RenderingLibrary;
using System.ComponentModel;


#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endif
namespace Gum.Forms.DefaultVisuals;

public class Styling
{
    /// <summary>
    /// This allows someone to get the active style from any instance they create, or from the class self like Styling.ActiveStyle.
    /// </summary>
    public static Styling ActiveStyle { get; set; }

    private Texture2D _spriteSheet;
    public Texture2D SpriteSheet 
    { 
        get => _spriteSheet;
        set
        {
            _spriteSheet = value;
            if (UseDefaults)
            {
                NineSlice.UpdateTextures(_spriteSheet);
                Icons.UpdateTextures(_spriteSheet);
            }
        }
    }

    public Colors Colors { get; set; } = new ();
    public NineSlice NineSlice { get; set; }

    public Icons Icons { get; set; }

    public Text Text { get; set; } = new ();

    public bool UseDefaults { get; set; }

    public Styling(Texture2D? spriteSheet, bool useDefaults = true)
    {
#if RAYLIB
        this.SpriteSheet = spriteSheet.Value;
#else
        if (spriteSheet == null)
        {
            this.SpriteSheet = (Texture2D)global::RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{SystemManagers.AssemblyPrefix}.UISpriteSheet.png");
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

        UseDefaults = useDefaults;
        NineSlice = new();
        Icons = new();
        if (useDefaults)
        {
            NineSlice.UseDefaults(this.SpriteSheet);
            Icons.UseDefaults(this.SpriteSheet);
        }
    }

    /// <summary>
    /// Creates a StateSave object that represents the rectangle position on a specific Texture2D
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static StateSave CreateTextureCoordinateState(int left, int top, int width, int height, Texture2D? texture)
    {
        var variables = new List<VariableSave>
        {
            new() { Name = "TextureLeft", Type = "int", Value = left },
            new() { Name = "TextureTop", Type = "int", Value = top },
            new() { Name = "TextureWidth", Type = "int", Value = width },
            new() { Name = "TextureHeight", Type = "int", Value = height },
            new() { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
        };

        if (texture != null)
        {
            variables.Add(new VariableSave { Name = "Texture", Type = "Texture2D", Value = texture });
        }

        return new StateSave
        {
            Variables = variables
        };
    }
}

public class Colors
{
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
    public StateSave Solid;
    public StateSave Bordered;
    public StateSave BracketVertical;
    public StateSave BracketHorizontal;
    public StateSave Tab;
    public StateSave TabBordered;
    public StateSave Outlined;
    public StateSave OutlinedHeavy;
    public StateSave Panel;
    public StateSave CircleSolid;
    public StateSave CircleBordered;
    public StateSave CircleOutlined;
    public StateSave CircleOutlinedHeavy;

    public void UseDefaults(Texture2D texture)
    {
        Solid = Styling.CreateTextureCoordinateState(0, 48, 24, 24, texture);
        Bordered = Styling.CreateTextureCoordinateState(24, 48, 24, 24, texture);
        BracketVertical = Styling.CreateTextureCoordinateState(48, 72, 24, 24, texture);
        BracketHorizontal = Styling.CreateTextureCoordinateState(72, 72, 24, 24, texture);
        Tab = Styling.CreateTextureCoordinateState(48, 48, 24, 24, texture);
        TabBordered = Styling.CreateTextureCoordinateState(72, 48, 24, 24, texture);
        Outlined = Styling.CreateTextureCoordinateState(0, 72, 24, 24, texture);
        OutlinedHeavy = Styling.CreateTextureCoordinateState(24, 72, 24, 24, texture);
        Panel = Styling.CreateTextureCoordinateState(96, 48, 24, 24, texture);
        CircleSolid = Styling.CreateTextureCoordinateState(0, 96, 24, 24, texture);
        CircleBordered = Styling.CreateTextureCoordinateState(24, 96, 24, 24, texture);
        CircleOutlined = Styling.CreateTextureCoordinateState(0, 120, 24, 24, texture);
        CircleOutlinedHeavy = Styling.CreateTextureCoordinateState(24, 120, 24, 24, texture);
    }

    public void UpdateTextures(Texture2D texture)
    {
        UpdateTextureInState(Solid, texture);
        UpdateTextureInState(Bordered, texture);
        UpdateTextureInState(BracketVertical, texture);
        UpdateTextureInState(BracketHorizontal, texture);
        UpdateTextureInState(Tab, texture);
        UpdateTextureInState(TabBordered, texture);
        UpdateTextureInState(Outlined, texture);
        UpdateTextureInState(OutlinedHeavy, texture);
        UpdateTextureInState(Panel, texture);
        UpdateTextureInState(CircleSolid, texture);
        UpdateTextureInState(CircleBordered, texture);
        UpdateTextureInState(CircleOutlined, texture);
        UpdateTextureInState(CircleOutlinedHeavy, texture);
    }

    private void UpdateTextureInState(StateSave state, Texture2D texture)
    {
        var valueToChange = state?.Variables?.FirstOrDefault(v => v.Name == "Texture");
        if (valueToChange != null)
        {
            valueToChange.Value = texture;
        }
    }
}

public class Icons
{
    public StateSave Arrow1;
    public StateSave Arrow2;
    public StateSave Arrow3;
    public StateSave Basket;
    public StateSave Battery;
    public StateSave Check;
    public StateSave CheckeredFlag;
    public StateSave Circle1;
    public StateSave Circle2;
    public StateSave Close;
    public StateSave Crosshairs;
    public StateSave Currency;
    public StateSave Cursor;
    public StateSave CursorText;
    public StateSave Dash;
    public StateSave Delete;
    public StateSave Enter;
    public StateSave Expand;
    public StateSave Gamepad;
    public StateSave GamepadNES;
    public StateSave GamepadSNES;
    public StateSave GamepadNintendo64;
    public StateSave GamepadGamecube;
    public StateSave GamepadSwitchPro;
    public StateSave GamepadXbox;
    public StateSave GamepadPlaystationDualShock;
    public StateSave GamepadSegaGenesis;
    public StateSave Gear;
    public StateSave FastForward;
    public StateSave FastForwardBar;
    public StateSave FitToScreen;
    public StateSave Flame1;
    public StateSave Flame2;
    public StateSave Heart;
    public StateSave Info;
    public StateSave Keyboard;
    public StateSave Leaf;
    public StateSave Lightning;
    public StateSave Minimize;
    public StateSave Monitor;
    public StateSave Mouse;
    public StateSave Music;
    public StateSave Pause;
    public StateSave Pencil;
    public StateSave Play;
    public StateSave PlayBar;
    public StateSave Power;
    public StateSave Radiation;
    public StateSave Reduce;
    public StateSave Shield;
    public StateSave Shot;
    public StateSave Skull;
    public StateSave Sliders;
    public StateSave SoundMaximum;
    public StateSave SoundMinimum;
    public StateSave Speech;
    public StateSave Star;
    public StateSave Stop;
    public StateSave Temperature;
    public StateSave Touch;
    public StateSave Trash;
    public StateSave Trophy;
    public StateSave User;
    public StateSave UserAdd;
    public StateSave UserDelete;
    public StateSave UserGear;
    public StateSave UserMulti;
    public StateSave UserRemove;
    public StateSave Warning;
    public StateSave Wrench;

    public void UseDefaults(Texture2D texture)
    {
        Arrow1 = Styling.CreateTextureCoordinateState(288, 256, 32, 32, texture);
        Arrow2 = Styling.CreateTextureCoordinateState(320, 256, 32, 32, texture);
        Arrow3 = Styling.CreateTextureCoordinateState(352, 256, 32, 32, texture);
        Basket = Styling.CreateTextureCoordinateState(288, 224, 32, 32, texture);
        Battery = Styling.CreateTextureCoordinateState(320, 224, 32, 32, texture);
        Check = Styling.CreateTextureCoordinateState(384, 128, 32, 32, texture);
        CheckeredFlag = Styling.CreateTextureCoordinateState(384, 288, 32, 32, texture);
        Circle1 = Styling.CreateTextureCoordinateState(448, 128, 32, 32, texture);
        Circle2 = Styling.CreateTextureCoordinateState(416, 128, 32, 32, texture);
        Close = Styling.CreateTextureCoordinateState(416, 192, 32, 32, texture);
        Crosshairs = Styling.CreateTextureCoordinateState(352, 288, 32, 32, texture);
        Currency = Styling.CreateTextureCoordinateState(352, 224, 32, 32, texture);
        Cursor = Styling.CreateTextureCoordinateState(384, 32, 32, 32, texture);
        CursorText = Styling.CreateTextureCoordinateState(416, 32, 32, 32, texture);
        Dash = Styling.CreateTextureCoordinateState(352, 204, 32, 20, texture);
        Delete = Styling.CreateTextureCoordinateState(288, 320, 32, 32, texture);
        Enter = Styling.CreateTextureCoordinateState(320, 320, 32, 32, texture);
        Expand = Styling.CreateTextureCoordinateState(384, 192, 32, 32, texture);
        Gamepad = Styling.CreateTextureCoordinateState(352, 320, 32, 32, texture);
        GamepadNES = Styling.CreateTextureCoordinateState(416, 320, 32, 32, texture);
        GamepadSNES = Styling.CreateTextureCoordinateState(448, 320, 32, 32, texture);
        GamepadNintendo64 = Styling.CreateTextureCoordinateState(384, 352, 32, 32, texture);
        GamepadGamecube = Styling.CreateTextureCoordinateState(416, 352, 32, 32, texture);
        GamepadSwitchPro = Styling.CreateTextureCoordinateState(352, 320, 32, 32, texture);
        GamepadXbox = Styling.CreateTextureCoordinateState(384, 320, 32, 32, texture);
        GamepadPlaystationDualShock = Styling.CreateTextureCoordinateState(352, 352, 32, 32, texture);
        GamepadSegaGenesis = Styling.CreateTextureCoordinateState(448, 352, 32, 32, texture);
        Gear = Styling.CreateTextureCoordinateState(320, 96, 32, 32, texture);
        FastForward = Styling.CreateTextureCoordinateState(384, 160, 32, 32, texture);
        FastForwardBar = Styling.CreateTextureCoordinateState(416, 160, 32, 32, texture);
        FitToScreen = Styling.CreateTextureCoordinateState(288, 192, 32, 32, texture);
        Flame1 = Styling.CreateTextureCoordinateState(320, 64, 32, 32, texture);
        Flame2 = Styling.CreateTextureCoordinateState(352, 64, 32, 32, texture);
        Heart = Styling.CreateTextureCoordinateState(320, 128, 32, 32, texture);
        Info = Styling.CreateTextureCoordinateState(416, 256, 32, 32, texture);
        Keyboard = Styling.CreateTextureCoordinateState(320, 32, 32, 32, texture);
        Leaf = Styling.CreateTextureCoordinateState(288, 64, 32, 32, texture);
        Lightning = Styling.CreateTextureCoordinateState(416, 64, 32, 32, texture);
        Minimize = Styling.CreateTextureCoordinateState(352, 192, 32, 32, texture);
        Monitor = Styling.CreateTextureCoordinateState(448, 192, 32, 32, texture);
        Mouse = Styling.CreateTextureCoordinateState(448, 32, 32, 32, texture);
        Music = Styling.CreateTextureCoordinateState(384, 224, 32, 32, texture);
        Pause = Styling.CreateTextureCoordinateState(320, 160, 32, 32, texture);
        Pencil = Styling.CreateTextureCoordinateState(288, 96, 32, 32, texture);
        Play = Styling.CreateTextureCoordinateState(288, 160, 32, 32, texture);
        PlayBar = Styling.CreateTextureCoordinateState(448, 160, 32, 32, texture);
        Power = Styling.CreateTextureCoordinateState(288, 288, 32, 32, texture);
        Radiation = Styling.CreateTextureCoordinateState(384, 64, 32, 32, texture);
        Reduce = Styling.CreateTextureCoordinateState(320, 192, 32, 32, texture);
        Shield = Styling.CreateTextureCoordinateState(416, 288, 32, 32, texture);
        Shot = Styling.CreateTextureCoordinateState(320, 288, 32, 32, texture);
        Skull = Styling.CreateTextureCoordinateState(448, 288, 32, 32, texture);
        Sliders = Styling.CreateTextureCoordinateState(352, 96, 32, 32, texture);
        SoundMaximum = Styling.CreateTextureCoordinateState(448, 224, 32, 32, texture);
        SoundMinimum = Styling.CreateTextureCoordinateState(416, 224, 32, 32, texture);
        Speech = Styling.CreateTextureCoordinateState(448, 96, 32, 32, texture);
        Star = Styling.CreateTextureCoordinateState(352, 128, 32, 32, texture);
        Stop = Styling.CreateTextureCoordinateState(352, 160, 32, 32, texture);
        Temperature = Styling.CreateTextureCoordinateState(448, 64, 32, 32, texture);
        Touch = Styling.CreateTextureCoordinateState(352, 32, 32, 32, texture);
        Trash = Styling.CreateTextureCoordinateState(416, 96, 32, 32, texture);
        Trophy = Styling.CreateTextureCoordinateState(288, 128, 32, 32, texture);
        User = Styling.CreateTextureCoordinateState(288, 0, 32, 32, texture);
        UserAdd = Styling.CreateTextureCoordinateState(384, 0, 32, 32, texture);
        UserDelete = Styling.CreateTextureCoordinateState(416, 0, 32, 32, texture);
        UserGear = Styling.CreateTextureCoordinateState(352, 0, 32, 32, texture);
        UserMulti = Styling.CreateTextureCoordinateState(320, 0, 32, 32, texture);
        UserRemove = Styling.CreateTextureCoordinateState(448, 0, 32, 32, texture);
        Warning = Styling.CreateTextureCoordinateState(448, 256, 32, 32, texture);
        Wrench = Styling.CreateTextureCoordinateState(384, 96, 32, 32, texture);
    }

    public void UpdateTextures(Texture2D texture)
    {
        UpdateTextureInState(Arrow1, texture);
        UpdateTextureInState(Arrow2, texture);
        UpdateTextureInState(Arrow3, texture);
        UpdateTextureInState(Basket, texture);
        UpdateTextureInState(Battery, texture);
        UpdateTextureInState(Check, texture);
        UpdateTextureInState(CheckeredFlag, texture);
        UpdateTextureInState(Circle1, texture);
        UpdateTextureInState(Circle2, texture);
        UpdateTextureInState(Close, texture);
        UpdateTextureInState(Crosshairs, texture);
        UpdateTextureInState(Currency, texture);
        UpdateTextureInState(Cursor, texture);
        UpdateTextureInState(CursorText, texture);
        UpdateTextureInState(Dash, texture);
        UpdateTextureInState(Delete, texture);
        UpdateTextureInState(Enter, texture);
        UpdateTextureInState(Expand, texture);
        UpdateTextureInState(Gamepad, texture);
        UpdateTextureInState(GamepadNES, texture);
        UpdateTextureInState(GamepadSNES, texture);
        UpdateTextureInState(GamepadNintendo64, texture);
        UpdateTextureInState(GamepadGamecube, texture);
        UpdateTextureInState(GamepadSwitchPro, texture);
        UpdateTextureInState(GamepadXbox, texture);
        UpdateTextureInState(GamepadPlaystationDualShock, texture);
        UpdateTextureInState(GamepadSegaGenesis, texture);
        UpdateTextureInState(Gear, texture);
        UpdateTextureInState(FastForward, texture);
        UpdateTextureInState(FastForwardBar, texture);
        UpdateTextureInState(FitToScreen, texture);
        UpdateTextureInState(Flame1, texture);
        UpdateTextureInState(Flame2, texture);
        UpdateTextureInState(Heart, texture);
        UpdateTextureInState(Info, texture);
        UpdateTextureInState(Keyboard, texture);
        UpdateTextureInState(Leaf, texture);
        UpdateTextureInState(Lightning, texture);
        UpdateTextureInState(Minimize, texture);
        UpdateTextureInState(Monitor, texture);
        UpdateTextureInState(Mouse, texture);
        UpdateTextureInState(Music, texture);
        UpdateTextureInState(Pause, texture);
        UpdateTextureInState(Pencil, texture);
        UpdateTextureInState(Play, texture);
        UpdateTextureInState(PlayBar, texture);
        UpdateTextureInState(Power, texture);
        UpdateTextureInState(Radiation, texture);
        UpdateTextureInState(Reduce, texture);
        UpdateTextureInState(Shield, texture);
        UpdateTextureInState(Shot, texture);
        UpdateTextureInState(Skull, texture);
        UpdateTextureInState(Sliders, texture);
        UpdateTextureInState(SoundMaximum, texture);
        UpdateTextureInState(SoundMinimum, texture);
        UpdateTextureInState(Speech, texture);
        UpdateTextureInState(Star, texture);
        UpdateTextureInState(Stop, texture);
        UpdateTextureInState(Temperature, texture);
        UpdateTextureInState(Touch, texture);
        UpdateTextureInState(Trash, texture);
        UpdateTextureInState(Trophy, texture);
        UpdateTextureInState(User, texture);
        UpdateTextureInState(UserAdd, texture);
        UpdateTextureInState(UserDelete, texture);
        UpdateTextureInState(UserGear, texture);
        UpdateTextureInState(UserMulti, texture);
        UpdateTextureInState(UserRemove, texture);
        UpdateTextureInState(Warning, texture);
        UpdateTextureInState(Wrench, texture);
    }

    private void UpdateTextureInState(StateSave state, Texture2D texture)
    {
        var valueToChange = state?.Variables?.FirstOrDefault(v => v.Name == "Texture");
        if (valueToChange != null)
        {
            valueToChange.Value = texture;
        }
    }
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

