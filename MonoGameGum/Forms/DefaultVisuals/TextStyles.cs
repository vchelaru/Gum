using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class TextStyles
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
}
