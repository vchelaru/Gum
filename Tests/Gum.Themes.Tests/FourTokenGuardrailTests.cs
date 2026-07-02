using System.Reflection;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Microsoft.Xna.Framework;
using Shouldly;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;

namespace Gum.Themes.Tests;

/// <summary>
/// Every shipped theme's <c>XyzColors</c> must expose TextPrimary/TextMuted/Primary/Accent,
/// because every theme's Apply()/ConfigureStyling() pushes exactly these 4 tokens into
/// <see cref="V3Styling.ActiveStyle"/>'s Colors for the stock, un-subclassed V3 visuals
/// (e.g. Label) a theme leaves in place. This is the one shared contract across themes —
/// see the "4-token guardrail" section of the theme styling design.
/// </summary>
public class FourTokenGuardrailTests
{
    public static IEnumerable<object[]> ThemeColorsTypes()
    {
        yield return new object[] { typeof(DarkProColors), "DarkProColors" };
        yield return new object[] { typeof(BubblegumColors), "BubblegumColors" };
        yield return new object[] { typeof(EditorColors), "EditorColors" };
    }

    [Theory]
    [MemberData(nameof(ThemeColorsTypes))]
    public void ColorsType_ExposesFourGuardrailTokensAsColorProperties(Type colorsType, string themeName)
    {
        string[] requiredTokenNames = { "TextPrimary", "TextMuted", "Primary", "Accent" };

        foreach (string tokenName in requiredTokenNames)
        {
            PropertyInfo? property = colorsType.GetProperty(tokenName, BindingFlags.Public | BindingFlags.Instance);

            property.ShouldNotBeNull($"{themeName} is missing the guardrail token '{tokenName}'.");
            property!.PropertyType.ShouldBe(typeof(Color),
                $"{themeName}.{tokenName} should be a Color-typed property.");
        }
    }

    [Fact]
    public void DarkProTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(11, 22, 33);
        Color textMuted = new Color(44, 55, 66);
        Color primary = new Color(77, 88, 99);
        Color accent = new Color(100, 110, 120);

        DarkProStyling.ActiveStyle.Colors.Text = textPrimary;
        DarkProStyling.ActiveStyle.Colors.Muted = textMuted;
        DarkProStyling.ActiveStyle.Colors.Surface1 = primary;
        DarkProStyling.ActiveStyle.Colors.Accent = accent;

        DarkProTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void BubblegumTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(12, 23, 34);
        Color textMuted = new Color(45, 56, 67);
        Color primary = new Color(78, 89, 100);
        Color accent = new Color(101, 111, 121);

        BubblegumStyling.ActiveStyle.Colors.Text = textPrimary;
        BubblegumStyling.ActiveStyle.Colors.Muted = textMuted;
        BubblegumStyling.ActiveStyle.Colors.Surface1 = primary;
        BubblegumStyling.ActiveStyle.Colors.Accent = accent;

        BubblegumTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }

    [Fact]
    public void EditorTheme_ConfigureStyling_SyncsFourGuardrailTokensIntoV3Styling()
    {
        Color textPrimary = new Color(13, 24, 35);
        Color textMuted = new Color(46, 57, 68);
        Color primary = new Color(79, 90, 101);
        Color accent = new Color(102, 112, 122);

        EditorStyling.ActiveStyle.Colors.TextPrimary = textPrimary;
        EditorStyling.ActiveStyle.Colors.TextMuted = textMuted;
        EditorStyling.ActiveStyle.Colors.Primary = primary;
        EditorStyling.ActiveStyle.Colors.Accent = accent;

        EditorTheme.ConfigureStyling();

        V3Styling.ActiveStyle.Colors.TextPrimary.ShouldBe(textPrimary);
        V3Styling.ActiveStyle.Colors.TextMuted.ShouldBe(textMuted);
        V3Styling.ActiveStyle.Colors.Primary.ShouldBe(primary);
        V3Styling.ActiveStyle.Colors.Accent.ShouldBe(accent);
    }
}
