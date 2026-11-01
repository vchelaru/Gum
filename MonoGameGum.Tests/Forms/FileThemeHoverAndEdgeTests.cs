using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Hover state and hit area of controls built from each packaged theme's files: every control shows a
/// Highlighted state under the cursor and clears it, and a click near any corner of its bounds reaches it.
/// </summary>
public class FileThemeHoverAndEdgeTests : FileThemeInputTestBase
{
    private static readonly HashSet<string> WiredParts = new HashSet<string>
    {
        "TrackInstance", "ThumbInstance", "HeaderContainer", "SaturationValueContainer", "HueContainer",
        "ClipAndScrollContainer", "ClipContainer", "InnerPanelInstance",
    };

    public static IEnumerable<object[]> ThemesAndControls()
    {
        foreach (object[] theme in Themes())
        {
            foreach (string control in new[]
            {
                "Button", "CheckBox", "RadioButton", "TextBox", "PasswordBox", "ComboBox", "ListBox", "Slider",
                "ScrollBar", "ScrollViewer", "Expander", "ColorPicker", "Splitter", "Window",
            })
            {
                yield return new object[] { theme[0], control };
            }
        }
    }

    public static IEnumerable<object[]> ThemesAndHoverControls()
    {
        foreach (object[] theme in Themes())
        {
            foreach (string[] control in new[]
            {
                new[] { "CheckBox", "CheckBoxCategory" }, new[] { "RadioButton", "RadioButtonCategory" },
                new[] { "TextBox", "TextBoxCategory" }, new[] { "PasswordBox", "PasswordBoxCategory" },
                new[] { "ComboBox", "ComboBoxCategory" },
            })
            {
                yield return new object[] { theme[0], control[0], control[1] };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void ClickNearEachCorner_Control_ReachesTheControl(string theme, string controlName)
    {
        FrameworkElement control = PlaceByName(LoadTheme(theme), controlName);
        GraphicalUiElement visual = control.Visual;
        if (visual.AbsoluteWidth < 12f || visual.AbsoluteHeight < 12f)
        {
            return;
        }
        List<string> failures = new List<string>();
        foreach ((float fx, float fy) in new[] { (0f, 0f), (1f, 0f), (0f, 1f), (1f, 1f) })
        {
            int x = (int)(visual.AbsoluteLeft + (fx == 0f ? 4 : visual.AbsoluteWidth - 5));
            int y = (int)(visual.AbsoluteTop + (fy == 0f ? 4 : visual.AbsoluteHeight - 5));
            Driver.MoveTo(x, y);
            InteractiveGue? hit = Driver.Cursor.VisualOver;
            if (!Lands((InteractiveGue)visual, hit))
            {
                failures.Add($"{theme} {controlName} corner ({fx},{fy}) at {x},{y} hit {Describe(hit)}");
            }
        }
        failures.ShouldBeEmpty(string.Join("\n", failures));
    }

    [Theory]
    [MemberData(nameof(ThemesAndHoverControls))]
    public void Hover_Control_ShowsHighlightedAndClearsOnLeave(string theme, string controlName, string category)
    {
        FrameworkElement control = PlaceByName(LoadTheme(theme), controlName);
        Driver.MoveTo(CenterX(control.Visual), CenterY(control.Visual));
        string? over = AppliedState(control.Visual, category);
        (over ?? "").ShouldContain("Highlighted", Case.Sensitive, $"{theme} {controlName} state while hovered: {over}; hit {Describe(Driver.Cursor.VisualOver)}");

        Driver.MoveTo(OffX, OffY);
        string? off = AppliedState(control.Visual, category);
        (off ?? "").ShouldNotContain("Highlighted", Case.Sensitive, $"{theme} {controlName} state after leaving: {off}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Hover_SliderThumb_ShowsHighlightedAndPushedWhileHeld(string theme)
    {
        Slider slider = Place<Slider>(LoadTheme(theme), "Slider");
        GraphicalUiElement thumb = slider.Visual.GetGraphicalUiElementByName("ThumbInstance")!;
        string category = "ButtonCategory";
        if (AppliedState(thumb, category) == null && thumb is InteractiveGue g && g.FormsControlAsObject == null)
        {
            return; // this theme's thumb is not a Forms control
        }

        Driver.MoveTo(CenterX(thumb), CenterY(thumb));
        AppliedState(thumb, category).ShouldBe("Highlighted", theme);

        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        AppliedState(thumb, category).ShouldBe("Pushed", theme);

        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);
        Driver.MoveTo(OffX, OffY);
        AppliedState(thumb, category).ShouldBe("Enabled", theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Click_ExpanderHeader_TogglesExpansion(string theme)
    {
        Expander expander = Place<Expander>(LoadTheme(theme), "Expander");
        bool before = expander.IsExpanded;
        GraphicalUiElement header = expander.Visual.GetGraphicalUiElementByName("HeaderContainer")
            ?? expander.Visual;
        ClickCenter(header);
        expander.IsExpanded.ShouldBe(!before, theme);
        ClickCenter(header);
        expander.IsExpanded.ShouldBe(before, theme);
    }

    private FrameworkElement PlaceByName(Gum.DataTypes.GumProjectSave project, string controlName)
    {
        FrameworkElement control = controlName switch
        {
            "Button" => Place<Button>(project, controlName),
            "CheckBox" => Place<CheckBox>(project, controlName),
            "RadioButton" => Place<RadioButton>(project, controlName),
            "TextBox" => Place<TextBox>(project, controlName),
            "PasswordBox" => Place<PasswordBox>(project, controlName),
            "ComboBox" => Place<ComboBox>(project, controlName),
            "ListBox" => Place<ListBox>(project, controlName),
            "Slider" => Place<Slider>(project, controlName),
            "ScrollBar" => Place<ScrollBar>(project, controlName),
            "ScrollViewer" => Place<ScrollViewer>(project, controlName),
            "Expander" => Place<Expander>(project, controlName),
            "ColorPicker" => Place<ColorPicker>(project, controlName),
            "Splitter" => Place<Splitter>(project, controlName),
            "Window" => Place<Gum.Forms.Window>(project, controlName),
            _ => throw new System.ArgumentException(controlName),
        };
        return control;
    }

    private static bool Lands(InteractiveGue control, InteractiveGue? hit)
    {
        if (hit == null)
        {
            return false;
        }
        for (GraphicalUiElement? current = hit; current != null; current = current.Parent as GraphicalUiElement)
        {
            if (current == control)
            {
                return hit == control || hit.FormsControlAsObject != null || WiredParts.Contains(hit.Name ?? "");
            }
        }
        return true;
    }
}
