using Gum.DataTypes.Variables;
using Microsoft.Xna.Framework;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public static class Styling
    {
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

            private static StateSave CreateTextureCoordinateState(int left, int top, int width, int height)
            {
                return new()
                {
                    Variables = new()
            {
                new () { Name = "TextureLeft", Type = "int", Value = left },
                new () { Name = "TextureTop", Type = "int", Value = top },
                new () { Name = "TextureWidth", Type = "int", Value = width },
                new () { Name = "TextureHeight", Type = "int", Value = height },
                new () { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
            }
                };
            }
        }
    }
}
