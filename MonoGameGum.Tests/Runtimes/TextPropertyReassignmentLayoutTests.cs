using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Re-assigning a text property to the value it already has, or changing and restoring it, must leave the
/// text and its stacking parent laid out as a fresh element would be (#7550 family: state the code changes
/// temporarily and restores only when a layout follows).
/// </summary>
public class TextPropertyReassignmentLayoutTests : BaseTestClass
{
    record struct Rect(float Left, float Top, float Width, float Height);

    static Rect Snapshot(GraphicalUiElement element) =>
        new(element.AbsoluteLeft, element.AbsoluteTop, element.AbsoluteWidth, element.AbsoluteHeight);

    // name -> (apply a value that equals the current one)
    static readonly (string Name, Action<TextRuntime> Reassign)[] Reassignments =
    {
        ("Text", t => t.Text = "Mason"),
        ("TextNoTranslate", t => t.SetTextNoTranslate("Mason")),
        ("SetProperty Text", t => t.SetProperty("Text", "Mason")),
        ("SetProperty TextNoTranslate", t => t.SetProperty("TextNoTranslate", "Mason")),
        ("FontScale", t => t.FontScale = t.FontScale),
        ("LineHeightMultiplier", t => t.LineHeightMultiplier = t.LineHeightMultiplier),
        ("MaxNumberOfLines", t => t.MaxNumberOfLines = t.MaxNumberOfLines),
        ("MaxLettersToShow", t => t.MaxLettersToShow = t.MaxLettersToShow),
        ("FontSize", t => t.FontSize = t.FontSize),
        ("IsBold", t => t.IsBold = t.IsBold),
        ("HorizontalAlignment", t => t.HorizontalAlignment = t.HorizontalAlignment),
        ("VerticalAlignment", t => t.VerticalAlignment = t.VerticalAlignment),
        ("MaxWidth", t => t.MaxWidth = t.MaxWidth),
        ("Width", t => t.Width = t.Width),
    };

    public static IEnumerable<object[]> Cases()
    {
        ChildrenLayout[] parents = { ChildrenLayout.Regular, ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack };
        float[] offsets = { 0, 4 };
        for (int i = 0; i < Reassignments.Length; i++)
        {
            foreach (ChildrenLayout parent in parents)
            {
                foreach (float offset in offsets)
                {
                    yield return new object[] { i, parent, offset };
                }
            }
        }
    }

    static (TextRuntime text, ContainerRuntime parent) Build(ChildrenLayout layout, float offset)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        parent.ChildrenLayout = layout;

        TextRuntime first = new();
        first.Text = "Before";
        parent.AddChild(first);

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = offset;
        text.XOrigin = HorizontalAlignment.Center;
        text.Text = "Mason";
        parent.AddChild(text);

        TextRuntime last = new();
        last.Text = "After";
        parent.AddChild(last);
        return (text, parent);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Reassigning_ShouldKeepLayout_WhenValueIsUnchanged(int index, ChildrenLayout layout, float offset)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, offset);
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        Reassignments[index].Reassign(text);

        Snapshot(text).ShouldBe(textBefore, Reassignments[index].Name);
        Snapshot(parent).ShouldBe(parentBefore, Reassignments[index].Name);
    }

    [Theory]
    [InlineData(ChildrenLayout.Regular)]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Text_ShouldKeepLayout_WhenRefreshedEveryFrame(ChildrenLayout layout)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, 4);
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        for (int frame = 0; frame < 5; frame++)
        {
            text.Text = "Mason";
        }

        Snapshot(text).ShouldBe(textBefore);
        Snapshot(parent).ShouldBe(parentBefore);
    }

    [Theory]
    [InlineData("[Color=Red]Mason[/Color]")]
    [InlineData("[IsBold=true]Mason[/IsBold]")]
    public void Text_ShouldKeepLayout_WhenBbCodeIsSetTwice(string markup)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(ChildrenLayout.TopToBottomStack, 4);
        text.Text = markup;
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        text.Text = markup;

        Snapshot(text).ShouldBe(textBefore);
        Snapshot(parent).ShouldBe(parentBefore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Text_ShouldKeepLayout_WhenSetToSameStringInsideSuspendedLayout(float offset)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(ChildrenLayout.TopToBottomStack, offset);
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        parent.SuspendLayout();
        text.Text = "Mason";
        parent.ResumeLayout();

        Snapshot(text).ShouldBe(textBefore);
        Snapshot(parent).ShouldBe(parentBefore);
    }

    [Fact]
    public void Text_ShouldKeepLayout_WhenSetToSameStringWithTextSuspended()
    {
        (TextRuntime text, ContainerRuntime parent) = Build(ChildrenLayout.TopToBottomStack, 4);
        Rect textBefore = Snapshot(text);

        text.SuspendLayout();
        text.Text = "Mason";
        text.ResumeLayout();

        Snapshot(text).ShouldBe(textBefore);
    }

    [Fact]
    public void Text_ShouldKeepLayout_WhenSetToSameStringWithAllLayoutSuspended()
    {
        (TextRuntime text, ContainerRuntime parent) = Build(ChildrenLayout.TopToBottomStack, 4);
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        GraphicalUiElement.IsAllLayoutSuspended = true;
        try
        {
            text.Text = "Mason";
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
        parent.UpdateLayout();

        Snapshot(text).ShouldBe(textBefore);
        Snapshot(parent).ShouldBe(parentBefore);
    }

    [Fact]
    public void Text_ShouldKeepLayout_WhenHiddenAndShown()
    {
        (TextRuntime text, ContainerRuntime parent) = Build(ChildrenLayout.LeftToRightStack, 4);
        Rect parentBefore = Snapshot(parent);

        text.Visible = false;
        text.Text = "Mason";
        text.Visible = true;
        text.Text = "Mason";

        Snapshot(parent).ShouldBe(parentBefore);
    }
}
