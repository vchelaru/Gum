using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using Shouldly;
using Bubblegum = Gum.Themes.Bubblegum;
using DarkPro = Gum.Themes.DarkPro;
using Editor = Gum.Themes.Editor;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Neon = Gum.Themes.Neon;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;

namespace Gum.Themes.Tests;

/// <summary>
/// The selected text of a TextBox or PasswordBox is drawn over the selection highlight without being
/// recolored, so an opaque highlight has to stay at least 3:1 away from the text color (#7330).
/// A translucent highlight takes its look from the box background beneath it, so it is left to that
/// background's own contrast with the text.
/// </summary>
public class TextSelectionContrastTests
{
    public static IEnumerable<object[]> Cases()
    {
        (string, Action, Action)[] themes =
        {
            ("Bubblegum", Bubblegum.BubblegumTheme.ConfigureStyling, Bubblegum.BubblegumTheme.RegisterVisuals),
            ("DarkPro", DarkPro.DarkProTheme.ConfigureStyling, DarkPro.DarkProTheme.RegisterVisuals),
            ("Editor", Editor.EditorTheme.ConfigureStyling, Editor.EditorTheme.RegisterVisuals),
            ("ForestGlade", ForestGlade.ForestGladeTheme.ConfigureStyling, ForestGlade.ForestGladeTheme.RegisterVisuals),
            ("Hazard", Hazard.HazardTheme.ConfigureStyling, Hazard.HazardTheme.RegisterVisuals),
            ("Meadow", Meadow.MeadowTheme.ConfigureStyling, Meadow.MeadowTheme.RegisterVisuals),
            ("Neon", Neon.NeonTheme.ConfigureStyling, Neon.NeonTheme.RegisterVisuals),
            ("Retro95", Retro95.Retro95Theme.ConfigureStyling, Retro95.Retro95Theme.RegisterVisuals),
            ("Template", Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals),
        };
        foreach ((string name, Action configure, Action register) in themes)
        {
            yield return new object[] { name, "TextBox", configure, register };
            yield return new object[] { name, "PasswordBox", configure, register };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SelectionHighlight_CodeTheme_HasContrastWithText(string theme, string control, Action configureStyling, Action registerVisuals)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            FrameworkElement element = control == "TextBox" ? new TextBox() : new PasswordBox();
            TextBoxBaseVisual visual = (TextBoxBaseVisual)element.Visual;
            visual.States.Focused.Apply!();

            Color selection = visual.SelectionInstance.Color;
            Color text = visual.TextInstance.Color;
            double ratio = ContrastRatio(selection, text);

            if (selection.A < 255)
            {
                return;
            }

            ratio.ShouldBeGreaterThanOrEqualTo(3.0, $"{theme} {control}: text {text} over highlight {selection}");
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
        }
    }

    private static double ContrastRatio(Color a, Color b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color color)
    {
        static double Linear(double v)
        {
            v /= 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }
}
