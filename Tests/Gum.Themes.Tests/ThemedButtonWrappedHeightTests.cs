using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
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

    // Every theme's button is sized by its label: chrome padding (the Height) plus the one text line.
    // The headless tests have no theme font, so the one-line sum is pinned with the real line height
    // of each theme's default font (lineHeight in the theme's FontCache .fnt).
    public static IEnumerable<object[]> LabelSizedCases() => Cases();

    private static readonly Dictionary<string, float> RealLineHeights = new()
    {
        ["Bubblegum"] = 20, // Nunito 14
        ["DarkPro"] = 19, // DM Mono 14
        ["ForestGlade"] = 20, // Nunito 14 Bold
        ["Hazard"] = 24, // Saira Condensed 15
        ["Meadow"] = 24, // Baloo 2 15
        ["Neon"] = 15, // Share Tech Mono 13
        ["Retro95"] = 18, // Nunito 13
        ["Template"] = 19, // DM Mono 14
    };

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void LabelSizedButton_ChromePaddingPlusRealLineHeight_ShouldBeTheOldFixedHeight(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        float realLineHeight = RealLineHeights[theme];
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, ShortLabel);

            visual.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren, $"{theme} {kind}");
            (visual.Height + realLineHeight).ShouldBe(expectedHeight, $"{theme} {kind}");
        });
    }

    [Fact]
    public void TemplateVariantsButton_ChromePaddingPlusRealLineHeight_ShouldBeTheOldFixedHeight()
    {
        WithTheme(Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals, () =>
        {
            Template.Variants.ButtonVisual visual = new();
            visual.TextInstance.Text = ShortLabel;
            visual.UpdateLayout();

            visual.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
            (visual.Height + RealLineHeights["Template"]).ShouldBe(32f);
        });
    }

    // The theme's real default font, parsed from the .fnt the theme templates ship (no texture needed
    // to measure a line), assigned to the label so the button measures like it does in a game.
    private static readonly Dictionary<string, (string Folder, string File)> RealFonts = new()
    {
        ["Bubblegum"] = ("Bubblegum", "Font14Nunito-Regular_ttf_309444ee.fnt"),
        ["DarkPro"] = ("DarkPro", "Font14DMMono-Regular_ttf_1a87468f.fnt"),
        ["ForestGlade"] = ("ForestGlade", "Font14Nunito-Bold_ttf_be304f91_Bold.fnt"),
        ["Hazard"] = ("Hazard", "Font15SairaCondensed-Regular_ttf_6201a4b8.fnt"),
        ["Meadow"] = ("Meadow", "Font15Baloo2-Bold_ttf_f4b22195_Bold.fnt"),
        ["Neon"] = ("Neon", "Font13ShareTechMono-Regular_ttf_64a05457.fnt"),
        ["Retro95"] = ("Retro95", "Font13Nunito-Regular_ttf_309444ee.fnt"),
        ["Template"] = ("DarkPro", "Font14DMMono-Regular_ttf_1a87468f.fnt"),
    };

    private static void UseRealFont(GraphicalUiElement text, string theme)
    {
        (string folder, string file) = RealFonts[theme];
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Tools", "Gum.ProjectServices")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        string path = Path.Combine(dir!, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", folder, "FontCache", file);
        BitmapFont font = new BitmapFont((Texture2D?)null, File.ReadAllText(path));
        for (int i = 0; i < font.Characters.Length; i++)
        {
            font.Characters[i] ??= new BitmapCharacterInfo { XAdvance = 8 };
        }
        ((Text)text.RenderableComponent!).BitmapFont = font;
        text.UpdateLayout();
    }

    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void OneLineLabel_MeasuredWithTheRealFont_ShouldBeTheOldFixedHeight(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, ShortLabel);
            UseRealFont(visual.GetGraphicalUiElementByName("TextInstance")!, theme);
            visual.UpdateLayout();

            visual.AbsoluteHeight.ShouldBe(expectedHeight, $"{theme} {kind}");
        });
    }

    [Fact]
    public void TemplateVariantsButton_OneLineLabel_MeasuredWithTheRealFont_ShouldBe32()
    {
        WithTheme(Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals, () =>
        {
            Template.Variants.ButtonVisual visual = new();
            visual.TextInstance.Text = ShortLabel;
            UseRealFont(visual.TextInstance, "Template");
            visual.UpdateLayout();

            visual.AbsoluteHeight.ShouldBe(32f);
        });
    }

    [Theory]
    [InlineData("Bubblegum", 36f)]
    [InlineData("ForestGlade", 32f)]
    [InlineData("Hazard", 32f)]
    [InlineData("Meadow", 36f)]
    [InlineData("Template", 32f)]
    [InlineData("Retro95", 24f)]
    public void Thumb_ShouldKeepItsFixedHeight(string theme, float expectedHeight)
    {
        (Action configure, Action register) = theme switch
        {
            "Bubblegum" => ((Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals),
            "Hazard" => (Hazard.HazardTheme.ConfigureStyling, Hazard.HazardTheme.RegisterVisuals),
            "ForestGlade" => (ForestGlade.ForestGladeTheme.ConfigureStyling, ForestGlade.ForestGladeTheme.RegisterVisuals),
            "Retro95" => (Retro95.Retro95Theme.ConfigureStyling, Retro95.Retro95Theme.RegisterVisuals),
            "Meadow" => (Meadow.MeadowTheme.ConfigureStyling, Meadow.MeadowTheme.RegisterVisuals),
            _ => (Template.TemplateTheme.ConfigureStyling, Template.TemplateTheme.RegisterVisuals),
        };
        WithTheme(configure, register, () =>
        {
            GraphicalUiElement visual = new Thumb().Visual;
            visual.UpdateLayout();

            visual.HeightUnits.ShouldBe(DimensionUnitType.Absolute, theme);
            visual.AbsoluteHeight.ShouldBe(expectedHeight, theme);
        });
    }
    [Theory]
    [MemberData(nameof(LabelSizedCases))]
    public void EmptyLabel_ShouldCollapseToTheHeightPadding(
        string theme, string kind, Action configureStyling, Action registerVisuals, float expectedHeight)
    {
        WithTheme(configureStyling, registerVisuals, () =>
        {
            GraphicalUiElement visual = CreateVisual(kind, "");

            // With the real font the label collapses to exactly Height; the headless font adds up to 2.
            float oneLine = CreateVisual(kind, ShortLabel).GetAbsoluteHeight();
            visual.GetAbsoluteHeight().ShouldBeLessThanOrEqualTo(visual.Height + 2, $"{theme} {kind}");
            visual.GetAbsoluteHeight().ShouldBeLessThan(oneLine - 10, $"{theme} {kind}");
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
