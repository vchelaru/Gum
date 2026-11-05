using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Bubblegum = Gum.Themes.Bubblegum;
using DarkPro = Gum.Themes.DarkPro;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Neon = Gum.Themes.Neon;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;
using Text = RenderingLibrary.Graphics.Text;

namespace Gum.Themes.Tests;

/// <summary>
/// #7643: a themed Button or ToggleButton is as tall as its label needs, so a label that wraps
/// grows the button instead of running past its bottom edge. A one-line label keeps the height
/// each theme always had.
/// </summary>
public class ThemedButtonWrappedHeightTests
{
    private const string ShortLabel = "OK";
    private const string LongLabel = "This little shack is where the Smith stays and I shouldn't go in if I'm not invited by him";

    public static IEnumerable<object[]> Cases()
    {
        (string, Action, Action, float, float)[] themes =
        {
            ("Bubblegum", Bubblegum.BubblegumTheme.ConfigureStyling, Bubblegum.BubblegumTheme.RegisterVisuals, 36, 32),
            ("DarkPro", DarkPro.DarkProTheme.ConfigureStyling, DarkPro.DarkProTheme.RegisterVisuals, 32, 32),
            ("ForestGlade", ForestGlade.ForestGladeTheme.ConfigureStyling, ForestGlade.ForestGladeTheme.RegisterVisuals, 32, 32),
            ("Hazard", Hazard.HazardTheme.ConfigureStyling, Hazard.HazardTheme.RegisterVisuals, 32, 32),
            ("Meadow", Meadow.MeadowTheme.ConfigureStyling, Meadow.MeadowTheme.RegisterVisuals, 36, 32),
            ("Neon", Neon.NeonTheme.ConfigureStyling, Neon.NeonTheme.RegisterVisuals, 32, 32),
            ("Retro95", Retro95.Retro95Theme.ConfigureStyling, Retro95.Retro95Theme.RegisterVisuals, 24, 24),
            ("Template", Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals, 32, 32),
        };
        foreach ((string name, Action configure, Action register, float button, float toggle) in themes)
        {
            yield return new object[] { name, "Button", configure, register, button };
            yield return new object[] { name, "ToggleButton", configure, register, toggle };
        }
    }

    // Themes whose button is sized by its label, with the chrome padding and the real line height of
    // the theme's font (the headless tests have no theme font, so the one-line sum is pinned here).
    public static IEnumerable<object[]> LabelSizedCases()
    {
        foreach (object[] c in Cases())
        {
            if ((string)c[0] is "ForestGlade" or "Retro95")
            {
                yield return c;
            }
        }
    }

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void LabelSizedButton_ChromePaddingPlusRealLineHeight_ShouldBeTheOldFixedHeight(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        float realLineHeight = theme == "ForestGlade" ? 20 : 18; // Nunito 14 Bold / Nunito 13, measured in GumPreview
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, ShortLabel);

            visual.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren, $"{theme} {kind}");
            (visual.Height + realLineHeight).ShouldBe(expectedHeight, $"{theme} {kind}");
        });
    }

    [Theory]
    [MemberData(nameof(FixedHeightCases))]
    public void OneLineLabel_ShouldKeepTheThemesFixedHeight(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, ShortLabel);

            visual.GetAbsoluteHeight().ShouldBe(expectedHeight, $"{theme} {kind}");
        });
    }

    public static IEnumerable<object[]> FixedHeightCases() =>
        Cases().Where(c => (string)c[0] is not ("ForestGlade" or "Retro95"));

    [Fact]
    public void TemplateVariantsButton_OneLineLabel_ShouldKeepTheFixedHeight()
    {
        WithTheme(Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals, () =>
        {
            Template.Variants.ButtonVisual visual = new();
            visual.TextInstance.Text = ShortLabel;
            visual.UpdateLayout();

            visual.GetAbsoluteHeight().ShouldBe(32f);
        });
    }

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void EmptyLabel_ShouldStillBeOneLineTall(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            float oneLine = CreateVisual(kind, ShortLabel).GetAbsoluteHeight();

            CreateVisual(kind, "").GetAbsoluteHeight().ShouldBe(oneLine, $"{theme} {kind}");
        });
    }

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void ExplicitAbsoluteHeight_ShouldStillBeHonoredWhateverTheLabel(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, LongLabel);
            visual.HeightUnits = DimensionUnitType.Absolute;
            visual.Height = 50;
            visual.UpdateLayout();

            visual.GetAbsoluteHeight().ShouldBe(50f, $"{theme} {kind}");
        });
    }

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void WrappedLabel_ShouldGrowTheButtonByTheExtraLinesAndKeepTextInside(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, ShortLabel);
            float oneLineText = visual.GetGraphicalUiElementByName("TextInstance")!.GetAbsoluteHeight();
            float oneLineButton = visual.GetAbsoluteHeight();

            visual = CreateVisual(kind, LongLabel);

            AssertGrewWithText($"{theme} {kind}", visual, oneLineButton, oneLineText);
        });
    }

    private static void AssertGrewWithText(string label, GraphicalUiElement visual, float oneLineHeight, float oneLineTextHeight)
    {
        GraphicalUiElement textInstance = visual.GetGraphicalUiElementByName("TextInstance")!;
        Text core = (Text)textInstance.RenderableComponent!;
        core.WrappedText.Count.ShouldBeGreaterThanOrEqualTo(2, $"{label}: long label should wrap");

        float extra = textInstance.GetAbsoluteHeight() - oneLineTextHeight;
        extra.ShouldBeGreaterThan(0, label);
        visual.GetAbsoluteHeight().ShouldBe(oneLineHeight + extra, 0.01f, label);

        float top = visual.AbsoluteTop;
        float bottom = top + visual.GetAbsoluteHeight();
        textInstance.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(top - 0.01f, $"{label}: text above the button");
        (textInstance.AbsoluteTop + textInstance.GetAbsoluteHeight()).ShouldBeLessThanOrEqualTo(bottom + 0.01f, $"{label}: text below the button");

        float textCenter = textInstance.AbsoluteTop + textInstance.GetAbsoluteHeight() / 2f;
        textCenter.ShouldBe((top + bottom) / 2f, 1f, $"{label}: text not vertically centered");
    }

    private static GraphicalUiElement CreateVisual(string kind, string text)
    {
        FrameworkElement control = kind == "Button" ? new Button() : new ToggleButton();
        GraphicalUiElement visual = control.Visual;
        ((Gum.GueDeriving.TextRuntime)visual.GetGraphicalUiElementByName("TextInstance")!).Text = text;
        visual.UpdateLayout();
        return visual;
    }

    private static void WithTheme(Action configureStyling, Action registerVisuals, Action body)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();
            body();
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
}
