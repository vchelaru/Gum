using Gum.DataTypes.Variables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class Styling
    {
        /// <summary>
        /// This allows someone to get the active style from any instance they create, or from the class self like Styling.ActiveStyle.
        /// </summary>
        public static Styling ActiveStyle { get; set; }
        public Texture2D SpriteSheet { get; set; }

        public Styling(Texture2D spriteSheet)
        {
            if (spriteSheet == null)
            {
                this.SpriteSheet = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{SystemManagers.AssemblyPrefix}.UISpriteSheet.png");
            }
            else
            {
                this.SpriteSheet = spriteSheet;
            }

            if (ActiveStyle == null)
            {
                ActiveStyle = this;
            }
        }

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

        public static class Colors
        {
            public static Color Black { get; set; } = new Color(0, 0, 0);
            public static Color DarkGray { get; set; } = new Color(70, 70, 80);
            public static Color Gray { get; set; } = new Color(130, 130, 130);
            public static Color LightGray { get; set; } = new Color(170, 170, 170);
            public static Color White { get; set; } = new Color(255, 255, 255);
            public static Color PrimaryDark { get; set; } = new Color(4, 120, 137);
            public static Color Primary { get; set; } = new Color(6, 159, 177);
            public static Color PrimaryLight { get; set; } = new Color(74, 180, 193);
            public static Color Success { get; set; } = new Color(62, 167, 48);
            public static Color Warning { get; set; } = new Color(232, 171, 25);
            public static Color Danger { get; set; } = new Color(212, 18, 41);

            public static Color Accent { get; set; } = new Color(140, 48, 138);

        }

        public static class Text
        {
            public static StateSave Normal = CreateFontState("Arial", 18, false, false);
            public static StateSave Strong = CreateFontState("Arial", 18, true, false);
            public static StateSave Emphasis = CreateFontState("Arial", 18, false, true);

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
        }

        public static class NineSlice
        {
            public static StateSave Solid = CreateTextureCoordinateState(0, 48, 24, 24);
            public static StateSave Bordered = CreateTextureCoordinateState(24, 48, 24, 24);
            public static StateSave BracketVertical = CreateTextureCoordinateState(48, 72, 24, 24);
            public static StateSave BracketHorizontal = CreateTextureCoordinateState(72, 72, 24, 24);
            public static StateSave Tab = CreateTextureCoordinateState(48, 48, 24, 24);
            public static StateSave TabBordered = CreateTextureCoordinateState(72, 48, 24, 24);
            public static StateSave Outlined = CreateTextureCoordinateState(0, 72, 24, 24);
            public static StateSave OutlinedHeavy = CreateTextureCoordinateState(24, 72, 24, 24);
            public static StateSave Panel = CreateTextureCoordinateState(96, 48, 24, 24);
            public static StateSave CircleSolid = CreateTextureCoordinateState(0, 96, 24, 24);
            public static StateSave CircleBordered = CreateTextureCoordinateState(24, 96, 24, 24);
            public static StateSave CircleOutlined = CreateTextureCoordinateState(0, 120, 24, 24);
            public static StateSave CircleOutlinedHeavy = CreateTextureCoordinateState(24, 120, 24, 24);

        }

        public class Icons
        {
            public static StateSave Arrow1 => CreateTextureCoordinateState(288, 256, 32, 32);
            public static StateSave Arrow2 => CreateTextureCoordinateState(320, 256, 32, 32);
            public static StateSave Arrow3 => CreateTextureCoordinateState(352, 256, 32, 32);
            public static StateSave Basket => CreateTextureCoordinateState(288, 224, 32, 32);
            public static StateSave Battery => CreateTextureCoordinateState(320, 224, 32, 32);
            public static StateSave Check => CreateTextureCoordinateState(384, 128, 32, 32);
            public static StateSave CheckeredFlag => CreateTextureCoordinateState(384, 288, 32, 32);
            public static StateSave Circle1 => CreateTextureCoordinateState(448, 128, 32, 32);
            public static StateSave Circle2 => CreateTextureCoordinateState(416, 128, 32, 32);
            public static StateSave Close => CreateTextureCoordinateState(416, 192, 32, 32);
            public static StateSave Crosshairs => CreateTextureCoordinateState(352, 288, 32, 32);
            public static StateSave Currency => CreateTextureCoordinateState(352, 224, 32, 32);
            public static StateSave Cursor => CreateTextureCoordinateState(384, 32, 32, 32);
            public static StateSave CursorText => CreateTextureCoordinateState(416, 32, 32, 32);
            public static StateSave Dash => CreateTextureCoordinateState(352, 204, 32, 20);
            public static StateSave Delete => CreateTextureCoordinateState(288, 320, 32, 32);
            public static StateSave Enter => CreateTextureCoordinateState(320, 320, 32, 32);
            public static StateSave Expand => CreateTextureCoordinateState(384, 192, 32, 32);
            public static StateSave Gamepad => CreateTextureCoordinateState(352, 320, 32, 32);
            public static StateSave GamepadNES => CreateTextureCoordinateState(416, 320, 32, 32);
            public static StateSave GamepadSNES => CreateTextureCoordinateState(448, 320, 32, 32);
            public static StateSave GamepadNintendo64 => CreateTextureCoordinateState(384, 352, 32, 32);
            public static StateSave GamepadGamecube => CreateTextureCoordinateState(416, 352, 32, 32);
            public static StateSave GamepadSwitchPro => CreateTextureCoordinateState(352, 320, 32, 32);
            public static StateSave GamepadXbox => CreateTextureCoordinateState(384, 320, 32, 32);
            public static StateSave GamepadPlaystationDualShock => CreateTextureCoordinateState(352, 352, 32, 32);
            public static StateSave GamepadSegaGenesis => CreateTextureCoordinateState(448, 352, 32, 32);
            public static StateSave Gear => CreateTextureCoordinateState(320, 96, 32, 32);
            public static StateSave FastForward => CreateTextureCoordinateState(384, 160, 32, 32);
            public static StateSave FastForwardBar => CreateTextureCoordinateState(416, 160, 32, 32);
            public static StateSave FitToScreen => CreateTextureCoordinateState(288, 192, 32, 32);
            public static StateSave Flame1 => CreateTextureCoordinateState(320, 64, 32, 32);
            public static StateSave Flame2 => CreateTextureCoordinateState(352, 64, 32, 32);
            public static StateSave Heart => CreateTextureCoordinateState(320, 128, 32, 32);
            public static StateSave Info => CreateTextureCoordinateState(416, 256, 32, 32);
            public static StateSave Keyboard => CreateTextureCoordinateState(320, 32, 32, 32);
            public static StateSave Leaf => CreateTextureCoordinateState(288, 64, 32, 32);
            public static StateSave Lightning => CreateTextureCoordinateState(416, 64, 32, 32);
            public static StateSave Minimize => CreateTextureCoordinateState(352, 192, 32, 32);
            public static StateSave Monitor => CreateTextureCoordinateState(448, 192, 32, 32);
            public static StateSave Mouse => CreateTextureCoordinateState(448, 32, 32, 32);
            public static StateSave Music => CreateTextureCoordinateState(384, 224, 32, 32);
            public static StateSave Pause => CreateTextureCoordinateState(320, 160, 32, 32);
            public static StateSave Pencil => CreateTextureCoordinateState(288, 96, 32, 32);
            public static StateSave Play => CreateTextureCoordinateState(288, 160, 32, 32);
            public static StateSave PlayBar => CreateTextureCoordinateState(448, 160, 32, 32);
            public static StateSave Power => CreateTextureCoordinateState(288, 288, 32, 32);
            public static StateSave Radiation => CreateTextureCoordinateState(384, 64, 32, 32);
            public static StateSave Reduce => CreateTextureCoordinateState(320, 192, 32, 32);
            public static StateSave Shield => CreateTextureCoordinateState(416, 288, 32, 32);
            public static StateSave Shot => CreateTextureCoordinateState(320, 288, 32, 32);
            public static StateSave Skull => CreateTextureCoordinateState(448, 288, 32, 32);
            public static StateSave Sliders => CreateTextureCoordinateState(352, 96, 32, 32);
            public static StateSave SoundMaximum => CreateTextureCoordinateState(448, 224, 32, 32);
            public static StateSave SoundMinimum => CreateTextureCoordinateState(416, 224, 32, 32);
            public static StateSave Speech => CreateTextureCoordinateState(448, 96, 32, 32);
            public static StateSave Star => CreateTextureCoordinateState(352, 128, 32, 32);
            public static StateSave Stop => CreateTextureCoordinateState(352, 160, 32, 32);
            public static StateSave Temperature => CreateTextureCoordinateState(448, 64, 32, 32);
            public static StateSave Touch => CreateTextureCoordinateState(352, 32, 32, 32);
            public static StateSave Trash => CreateTextureCoordinateState(416, 96, 32, 32);
            public static StateSave Trophy => CreateTextureCoordinateState(288, 128, 32, 32);
            public static StateSave User => CreateTextureCoordinateState(288, 0, 32, 32);
            public static StateSave UserAdd => CreateTextureCoordinateState(384, 0, 32, 32);
            public static StateSave UserDelete => CreateTextureCoordinateState(416, 0, 32, 32);
            public static StateSave UserGear => CreateTextureCoordinateState(352, 0, 32, 32);
            public static StateSave UserMulti => CreateTextureCoordinateState(320, 0, 32, 32);
            public static StateSave UserRemove => CreateTextureCoordinateState(448, 0, 32, 32);
            public static StateSave Warning => CreateTextureCoordinateState(448, 256, 32, 32);
            public static StateSave Wrench => CreateTextureCoordinateState(384, 96, 32, 32);
        }
    }
}
