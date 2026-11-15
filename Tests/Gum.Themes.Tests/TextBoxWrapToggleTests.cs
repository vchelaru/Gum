using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
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
using Text = RenderingLibrary.Graphics.Text;

namespace Gum.Themes.Tests;

/// <summary>
/// Toggling TextWrapping and AcceptsReturn at runtime, in any order and repeatedly, leaves a
/// code-only themed TextBox laid out for its current configuration: no wrap keeps one visual
/// line per paragraph and scrolls with the caret, wrap fits the text to the box (#7434).
/// </summary>
public class TextBoxWrapToggleTests
{
    private const string LongLine =
        "This little shack is where the Smith stays and I shouldn't go in if I'm not invited by him";
    private const string Paragraphs = "first line of text here\nsecond line of text here\nthird line of text here";

    private static readonly (TextWrapping Wrap, bool Accepts)[] Configurations =
    {
        (TextWrapping.Wrap, false),
        (TextWrapping.Wrap, true),
        (TextWrapping.NoWrap, false),
        (TextWrapping.NoWrap, true),
    };

    public static IEnumerable<object[]> Themes()
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
            yield return new object[] { name, configure, register };
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void CodeThemeTextBox_ToggledAtRuntime_EveryConfigurationIsLaidOutForItself(string theme, Action configureStyling, Action registerVisuals)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();
            TextBox box = new();
            box.Width = 160;
            box.Height = 120;
            List<string> problems = new();

            foreach ((TextWrapping Wrap, bool Accepts) first in Configurations)
            {
                foreach ((TextWrapping Wrap, bool Accepts) second in Configurations)
                {
                    foreach (var step in new[] { first, second, first })
                    {
                        box.TextWrapping = step.Wrap;
                        box.AcceptsReturn = step.Accepts;
                        string? violation = Violation(box);
                        if (violation != null)
                        {
                            problems.Add($"{theme} {first} -> {second}: {violation}");
                        }
                    }
                }
            }

            problems.Distinct().ShouldBeEmpty(string.Join("\n", problems.Distinct().Take(12)));
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

    private static string? Violation(TextBox box)
    {
        (TextWrapping wrap, bool accepts) = (box.TextWrapping, box.AcceptsReturn);
        GraphicalUiElement visual = box.Visual;
        GraphicalUiElement textInstance = visual.GetGraphicalUiElementByName("TextInstance")!;
        Text core = (Text)textInstance.RenderableComponent!;
        bool singleLineMode = wrap == TextWrapping.NoWrap && !accepts;

        foreach (string text in accepts ? new[] { LongLine, Paragraphs } : new[] { LongLine })
        {
            box.Text = text;
            box.IsFocused = true;
            visual.UpdateLayout();
            box.CaretIndex = text.Length;
            visual.UpdateLayout();
            int paragraphs = text.Split('\n').Length;
            int lines = core.WrappedText.Count;

            DimensionUnitType expectedUnits = wrap == TextWrapping.Wrap
                ? DimensionUnitType.RelativeToParent
                : DimensionUnitType.RelativeToChildren;
            if (textInstance.WidthUnits != expectedUnits)
            {
                return $"[{wrap},{accepts}] text WidthUnits {textInstance.WidthUnits}, expected {expectedUnits}";
            }
            VerticalAlignment expectedAlignment = singleLineMode ? VerticalAlignment.Center : VerticalAlignment.Top;
            if (core.VerticalAlignment != expectedAlignment)
            {
                return $"[{wrap},{accepts}] text VerticalAlignment {core.VerticalAlignment}, expected {expectedAlignment}";
            }
            if (wrap == TextWrapping.Wrap && text == LongLine && lines < 2)
            {
                return $"[{wrap},{accepts}] long line did not wrap ({lines} lines)";
            }
            if (wrap == TextWrapping.NoWrap)
            {
                if (lines != paragraphs)
                {
                    return $"[{wrap},{accepts}] expected {paragraphs} lines, got {lines}";
                }
                if (text == LongLine && textInstance.GetAbsoluteWidth() <= visual.GetAbsoluteWidth())
                {
                    return $"[{wrap},{accepts}] long no-wrap text not wider than the box";
                }
            }

            GraphicalUiElement caret = visual.GetGraphicalUiElementByName("CaretInstance")!;
            float left = visual.AbsoluteLeft;
            float right = left + visual.GetAbsoluteWidth();
            if (caret.AbsoluteLeft < left - 1 || caret.AbsoluteLeft > right + 1)
            {
                return $"[{wrap},{accepts}] caret x {caret.AbsoluteLeft} outside the box {left}..{right}";
            }
        }
        return null;
    }
}
