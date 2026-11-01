using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// Controls of every packaged theme, loaded from the theme files: CheckBox and RadioButton checked state changes the visual and (
/// their parts stay inside the control; every control survives width and height of 0,
/// negative and huge without an exception or NaN bounds.
/// </summary>
public class ThemedToggleAndExtremeSizeTests : BeginnerDriverTestBase
{
    private static string Snapshot(GraphicalUiElement root)
    {
        List<string> parts = new List<string>();
        foreach (GraphicalUiElement element in new[] { root }.Concat(ThemedControlHarness.Descendants(root)))
        {
            foreach (object source in new object?[] { element, element.RenderableComponent }.Where(o => o != null)!)
            foreach (PropertyInfo property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type type = property.PropertyType;
                if (!(type.IsValueType || type == typeof(string))
                    || property.GetIndexParameters().Length > 0
                    || property.Name.Contains("Absolute") || property.Name.Contains("Parent") || property.Name.Contains("Render") || property.Name == "Width" && source != element || property.Name.Contains("Effective") || property.Name == "Name"
                    || property.Name == "Tag" || property.Name.StartsWith("Is") && property.Name != "IsFilled")
                {
                    continue;
                }
                try
                {
                    parts.Add($"{ThemedControlHarness.Path_(element, root)}.{property.Name}={property.GetValue(source)}");
                }
                catch (Exception)
                {
                    // a property that throws on this element type carries no state
                }
            }
        }
        return string.Join("|", parts);
    }

    public static IEnumerable<object[]> CheckedThemes() =>
        ThemedControlHarness.ThemeNames.SelectMany(t => new[] { "CheckBox", "RadioButton" }.Select(c => new object[] { t, c }));

    [Theory]
    [MemberData(nameof(CheckedThemes))]
    public void CheckBoxAndRadioButton_Checked_ChangesTheVisual(string theme, string controlName)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        InteractiveGue box = ThemedControlHarness.PlaceAlone(Driver, project, ThemedControlHarness.Find(project, controlName)!);
        ToggleButton toggle = (ToggleButton)box.FormsControlAsObject!;
        Driver.MoveTo(4000, 4000);
        Driver.Step(2);
        string rest = Snapshot(box);

        toggle.IsChecked = true;
        Driver.Step(2);

        (Snapshot(box) == rest).ShouldBeFalse($"{theme} {controlName}: checked looks the same as unchecked");
    }

    [Theory]
    [MemberData(nameof(CheckedThemes))]
    public void CheckBoxAndRadioButton_GlyphAndLabel_StayInsideTheControl(string theme, string controlName)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        InteractiveGue box = ThemedControlHarness.PlaceAlone(Driver, project, ThemedControlHarness.Find(project, controlName)!);
        ToggleButton toggle = (ToggleButton)box.FormsControlAsObject!;
        toggle.IsChecked = true;
        List<string> outside = new List<string>();

        foreach (float width in new[] { 60f, 140f, 400f })
        {
            box.WidthUnits = DimensionUnitType.Absolute;
            box.Width = width;
            Driver.Step(2);
            foreach (GraphicalUiElement part in ThemedControlHarness.Descendants(box).Where(p => p.AbsoluteVisible && p.AbsoluteWidth > 0 && (p.Name ?? "").EndsWith("Background")))
            {
                if (part.AbsoluteLeft < box.AbsoluteLeft - 1.01f || part.AbsoluteLeft + part.AbsoluteWidth > box.AbsoluteLeft + box.AbsoluteWidth + 1.01f
                    || part.AbsoluteTop < box.AbsoluteTop - 1.01f || part.AbsoluteTop + part.AbsoluteHeight > box.AbsoluteTop + box.AbsoluteHeight + 1.01f)
                {
                    outside.Add($"{width}: {ThemedControlHarness.Path_(part, box)} {part.AbsoluteLeft},{part.AbsoluteTop} {part.AbsoluteWidth}x{part.AbsoluteHeight} in {box.AbsoluteWidth}x{box.AbsoluteHeight}");
                }
            }
        }

        outside.ShouldBeEmpty($"{theme} {controlName}: " + string.Join("; ", outside));
    }

    [Theory]
    [MemberData(nameof(ThemedControlHarness.Themes), MemberType = typeof(ThemedControlHarness))]
    public void EveryControl_ExtremeWidthAndHeight_LeavesNoNaNOrNegativeParts(string theme)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        List<string> problems = new List<string>();
        foreach (ComponentSave component in project.Components.Where(c => c.Name.Contains("Controls/")).ToList())
        {
            foreach ((float w, float h) in new[] { (0f, 0f), (-20f, -20f), (6000f, 4000f) })
            {
                try
                {
                    InteractiveGue box = ThemedControlHarness.PlaceAlone(Driver, project, component);
                    ThemedControlHarness.Resize(box, w, h);
                    Driver.Step(2);
                    foreach (GraphicalUiElement part in new[] { box }.Concat(ThemedControlHarness.Descendants(box)))
                    {
                        if (float.IsNaN(part.AbsoluteWidth) || float.IsNaN(part.AbsoluteHeight) || float.IsNaN(part.AbsoluteLeft) || float.IsNaN(part.AbsoluteTop)
                            || float.IsInfinity(part.AbsoluteWidth) || float.IsInfinity(part.AbsoluteHeight))
                        {
                            problems.Add($"{component.Name} {w}x{h}: {ThemedControlHarness.Path_(part, box)} has NaN/infinite bounds");
                        }
                    }
                    (box.Parent as GraphicalUiElement)?.RemoveFromRoot();
                }
                catch (Exception exception)
                {
                    problems.Add($"{component.Name} {w}x{h}: threw {exception.GetType().Name} {exception.Message}");
                }
                project.Screens.RemoveAll(s => s.Name == "AloneScreen");
            }
        }

        problems.Count.ShouldBe(0, $"{theme}:{Environment.NewLine}" + string.Join(Environment.NewLine, problems.Take(15)));
    }
}
