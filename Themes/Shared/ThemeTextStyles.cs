using Gum.DataTypes.Variables;
using V3Text = Gum.Forms.DefaultVisuals.V3.Text;

namespace Gum.Themes;

/// <summary>
/// Fills V3 <c>Styling.Text</c> with a theme's text ladder (Tiny through Title), the same rungs and sizes
/// the theme's Forms template carries in its Styles component (#8509). Body rungs use the body font;
/// Strong and the headings are bold.
/// </summary>
internal static class ThemeTextStyles
{
    public static void ApplyLadder(V3Text text, string bodyFont, int bodySize, string headingFont,
        string titleFont, bool titleIsItalic = false, int? emphasisSize = null)
    {
        Set(text.Tiny, bodyFont, 10, isBold: false, isItalic: false);
        Set(text.Small, bodyFont, 12, isBold: false, isItalic: false);
        Set(text.Normal, bodyFont, bodySize, isBold: false, isItalic: false);
        Set(text.Emphasis, bodyFont, emphasisSize ?? bodySize, isBold: false, isItalic: true);
        Set(text.Strong, bodyFont, bodySize, isBold: true, isItalic: false);
        Set(text.H3, headingFont, 16, isBold: true, isItalic: false);
        Set(text.H2, headingFont, 18, isBold: true, isItalic: false);
        Set(text.H1, headingFont, 22, isBold: true, isItalic: false);
        Set(text.Title, titleFont, 28, isBold: true, isItalic: titleIsItalic);
    }

    private static void Set(StateSave state, string font, int fontSize, bool isBold, bool isItalic)
    {
        state.Clear();
        state.Variables.Add(new VariableSave { Name = "Font", Type = "string", Value = font });
        state.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = fontSize });
        state.Variables.Add(new VariableSave { Name = "IsBold", Type = "bool", Value = isBold });
        state.Variables.Add(new VariableSave { Name = "IsItalic", Type = "bool", Value = isItalic });
    }
}
