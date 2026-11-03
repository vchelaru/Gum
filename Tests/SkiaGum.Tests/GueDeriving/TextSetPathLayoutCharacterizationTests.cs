using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Skia's Text and SetTextNoTranslate paths (plain, BBCode, wrapped, RelativeToChildren) lay out the same as a
/// fresh element after repeated, changed and same-size assignments.
/// </summary>
public class TextSetPathLayoutCharacterizationTests : BaseTestClass
{
    public TextSetPathLayoutCharacterizationTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    record struct Rect(float Left, float Top, float Width, float Height);

    static Rect Snapshot(GraphicalUiElement e) => new(e.AbsoluteLeft, e.AbsoluteTop, e.AbsoluteWidth, e.AbsoluteHeight);

    public static IEnumerable<object[]> Cases()
    {
        string[] texts =
        {
            "Mason", "[Color=Red]Mason[/Color]", "[IsBold=true]Mason[/IsBold] and more",
            "Mason went to the market to buy some bread and cheese", "Line1\nLine2"
        };
        foreach (string t in texts)
            foreach (float offset in new[] { 0f, 4f, -4f })
                foreach (HorizontalAlignment x in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
                    foreach (bool noTranslate in new[] { false, true })
                        yield return new object[] { t, offset, x, noTranslate };
    }

    static (TextRuntime text, ContainerRuntime parent) Build(float offset, HorizontalAlignment x)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = offset;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.XOrigin = x;
        text.X = 20;
        text.MaxWidth = 120;
        parent.AddChild(text);
        return (text, parent);
    }

    static void Assign(TextRuntime t, string s, bool noTranslate)
    {
        if (noTranslate) t.SetTextNoTranslate(s); else t.Text = s;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_ShouldMatchFreshLayout_AfterRepeatedAssignment(string value, float offset, HorizontalAlignment x, bool noTranslate)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(offset, x);
        Assign(text, "Hi", noTranslate);
        Assign(text, value, noTranslate);
        Assign(text, value, noTranslate);
        (TextRuntime fresh, ContainerRuntime freshParent) = Build(offset, x);
        Assign(fresh, value, noTranslate);

        Snapshot(text).ShouldBe(Snapshot(fresh));
        Snapshot(parent).ShouldBe(Snapshot(freshParent));
    }
}
