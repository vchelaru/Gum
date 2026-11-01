using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Vertical scroll bar built from each packaged theme's files: the arrow buttons receive clicks and the thumb
/// reaches the full range. Standard and Editor rotate their buttons about an edge, and AbsoluteLeft/AbsoluteTop
/// report the unrotated frame, so buttons are clicked where they are drawn: between the bar edge and the track.
/// </summary>
public class FileThemeScrollBarButtonTests : FileThemeInputTestBase
{
    // The scroll bar template fills its parent (RelativeToContainer height), so Height = 200 alone makes it 800 tall.
    private ScrollBar PlaceVerticalScrollBar(string theme)
    {
        ScrollBar bar = Place<ScrollBar>(LoadTheme(theme), "ScrollBar");
        bar.Orientation = Orientation.Horizontal;
        bar.Orientation = Orientation.Vertical;
        bar.Visual.HeightUnits = DimensionUnitType.Absolute;
        bar.Visual.Height = 200;
        bar.Minimum = 0;
        bar.Maximum = 100;
        bar.ViewportSize = 20;
        bar.LargeChange = 20;
        bar.SmallChange = 5;
        bar.Value = 0;
        Driver.Step(3);
        return bar;
    }

    private void DragThumb(ScrollBar bar, int toY)
    {
        GraphicalUiElement thumb = bar.Visual.GetGraphicalUiElementByName("ThumbInstance")!;
        int x = CenterX(thumb);
        DragFrom(x, CenterY(thumb), x, toY);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DragThumb_VerticalScrollBar_ReachesMaximumAndMinimum(string theme)
    {
        ScrollBar bar = PlaceVerticalScrollBar(theme);
        GraphicalUiElement track = bar.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        DragThumb(bar, (int)(track.AbsoluteTop + track.AbsoluteHeight + 300));
        bar.Value.ShouldBe(bar.Maximum, 1.0, theme + " drag to bottom");

        DragThumb(bar, (int)(track.AbsoluteTop - 300));
        bar.Value.ShouldBe(bar.Minimum, 1.0, theme + " drag to top");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickUpButton_VerticalScrollBar_ScrollsUp(string theme)
    {
        ScrollBar bar = PlaceVerticalScrollBar(theme);
        GraphicalUiElement track = bar.Visual.GetGraphicalUiElementByName("TrackInstance")!;
        if (bar.Visual.GetGraphicalUiElementByName("UpButtonInstance") == null)
        {
            return;
        }
        bar.Value = 50;
        Driver.Step(2);

        Driver.Click(CenterX(track), (int)((bar.Visual.AbsoluteTop + track.AbsoluteTop) / 2));

        bar.Value.ShouldBeLessThan(50, $"{theme} up button did not receive the click; hit {Describe(Driver.Cursor.VisualOver)}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickDownButton_VerticalScrollBar_ScrollsDown(string theme)
    {
        ScrollBar bar = PlaceVerticalScrollBar(theme);
        GraphicalUiElement track = bar.Visual.GetGraphicalUiElementByName("TrackInstance")!;
        if (bar.Visual.GetGraphicalUiElementByName("DownButtonInstance") == null)
        {
            return;
        }
        float trackBottom = track.AbsoluteTop + track.AbsoluteHeight;
        float barBottom = bar.Visual.AbsoluteTop + bar.Visual.AbsoluteHeight;

        Driver.Click(CenterX(track), (int)((trackBottom + barBottom) / 2));

        bar.Value.ShouldBeGreaterThan(0, $"{theme} down button did not receive the click; hit {Describe(Driver.Cursor.VisualOver)}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickDownButton_ListBox_ScrollsTheList(string theme)
    {
        ListBox listBox = Place<ListBox>(LoadTheme(theme), "ListBox");
        listBox.Height = 200;
        listBox.Width = 250;
        for (int i = 0; i < 40; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        Driver.Step(3);
        GraphicalUiElement bar = listBox.Visual.GetGraphicalUiElementByName(ScrollViewer.VerticalScrollBarInstanceName)!;
        GraphicalUiElement? down = bar.GetGraphicalUiElementByName("DownButtonInstance");
        if (down == null)
        {
            return;
        }
        GraphicalUiElement track = bar.GetGraphicalUiElementByName("TrackInstance")!;
        float trackBottom = track.AbsoluteTop + track.AbsoluteHeight;
        float barBottom = bar.AbsoluteTop + bar.AbsoluteHeight;

        Driver.Click(CenterX(track), (int)((trackBottom + barBottom) / 2));

        listBox.VerticalScrollBarValue.ShouldBeGreaterThan(0, $"{theme} list down button did not receive the click; hit {Describe(Driver.Cursor.VisualOver)}");
    }
}
