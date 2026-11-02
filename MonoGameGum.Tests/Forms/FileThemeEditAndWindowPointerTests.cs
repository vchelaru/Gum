using System.Collections.Generic;
using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pointer behavior of TextBox, PasswordBox, Window and Splitter built from each packaged theme's files:
/// click to focus and caret, drag select, title drag and resizing from every edge and corner, splitter drag.
/// </summary>
public class FileThemeEditAndWindowPointerTests : FileThemeInputTestBase
{
    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Click_TextBox_FocusesIt(string theme)
    {
        TextBox box = Place<TextBox>(LoadTheme(theme), "TextBox");
        ClickCenter(box.Visual);
        box.IsFocused.ShouldBeTrue($"{theme} hit {Describe(Driver.Cursor.VisualOver)}");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_PasswordBox_FocusesIt(string theme)
    {
        PasswordBox box = Place<PasswordBox>(LoadTheme(theme), "PasswordBox");
        ClickCenter(box.Visual);
        box.IsFocused.ShouldBeTrue($"{theme} hit {Describe(Driver.Cursor.VisualOver)}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Click_TextBoxNearLeftAndRight_PutsCaretAtStartAndEnd(string theme)
    {
        TextBox box = Place<TextBox>(LoadTheme(theme), "TextBox");
        box.Text = "Hello world";
        Driver.Step(2);
        int y = CenterY(box.Visual);

        Driver.Click((int)(box.Visual.AbsoluteLeft + box.Visual.AbsoluteWidth - 3), y);
        box.CaretIndex.ShouldBe(box.Text.Length, $"{theme} click at the right edge");

        Driver.Click((int)(box.Visual.AbsoluteLeft + 3), y);
        box.CaretIndex.ShouldBe(0, $"{theme} click at the left edge");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void DragAcrossText_TextBox_SelectsText(string theme)
    {
        TextBox box = Place<TextBox>(LoadTheme(theme), "TextBox");
        box.Text = "Hello world";
        Driver.Step(2);
        int y = CenterY(box.Visual);
        int left = (int)(box.Visual.AbsoluteLeft + 3);
        int right = (int)(box.Visual.AbsoluteLeft + box.Visual.AbsoluteWidth - 3);

        Driver.MoveTo(left, y);
        Driver.SetButton(MouseButton.Primary, held: true);
        Driver.MoveTo((left + right) / 2, y);
        Driver.MoveTo(right, y);
        Driver.SetButton(MouseButton.Primary, held: false);

        box.SelectionLength.ShouldBeGreaterThan(0, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_DisabledTextBox_DoesNotFocus(string theme)
    {
        TextBox box = Place<TextBox>(LoadTheme(theme), "TextBox");
        box.IsEnabled = false;
        Driver.Step();
        ClickCenter(box.Visual);
        box.IsFocused.ShouldBeFalse(theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void ClickAway_FocusedTextBox_LosesFocus(string theme)
    {
        TextBox box = Place<TextBox>(LoadTheme(theme), "TextBox");
        ClickCenter(box.Visual);
        Driver.Click(OffX, OffY);
        box.IsFocused.ShouldBeFalse(theme);
    }

    private Window PlaceWindow(string theme)
    {
        Window window = Place<Window>(LoadTheme(theme), "Window");
        window.X = 250;
        window.Y = 200;
        window.Width = 300;
        window.Height = 200;
        Driver.Step(3);
        return window;
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void DragTitleBarWithRightButton_Window_DoesNotMove(string theme)
    {
        Window window = PlaceWindow(theme);
        GraphicalUiElement title = window.Visual.GetGraphicalUiElementByName("TitleBarInstance")!;
        float startX = window.Visual.AbsoluteLeft;
        int x = CenterX(title);
        int y = CenterY(title);

        Driver.MoveTo(x, y);
        Driver.SetButton(MouseButton.Secondary, held: true);
        Driver.MoveTo(x + 40, y);
        Driver.MoveTo(x + 40, y);
        Driver.SetButton(MouseButton.Secondary, held: false);

        window.Visual.AbsoluteLeft.ShouldBe(startX, 1f, theme);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void DragRightBorderOutsideBounds_Window_Resizes(string theme)
    {
        // The border pieces sit outside the window bounds, so the Window component must opt in to
        // RaiseChildrenEventsOutsideOfBounds for them to receive the cursor (#7412).
        Window window = PlaceWindow(theme);
        window.Visual.RaiseChildrenEventsOutsideOfBounds.ShouldBeTrue(theme);
        InteractiveGue border = window.Visual.Find<InteractiveGue>("BorderRightInstance")!;
        float startWidth = window.Visual.AbsoluteWidth;
        int x = (int)(window.Visual.AbsoluteRight + 3);
        int y = CenterY(window.Visual);

        Driver.MoveTo(x, y);
        Driver.Step();
        Driver.Cursor.VisualOver.ShouldBeSameAs(border, $"{theme} hit {Describe(Driver.Cursor.VisualOver)}");

        Driver.SetButton(MouseButton.Primary, held: true);
        Driver.MoveTo(x + 40, y);
        Driver.MoveTo(x + 40, y);
        Driver.SetButton(MouseButton.Primary, held: false);

        window.Visual.AbsoluteWidth.ShouldBe(startWidth + 40, 1f, theme);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Click_ButtonInsideWindow_RaisesClick(string theme)
    {
        Window window = PlaceWindow(theme);
        Button inside = new Button();
        inside.Visual.Y = window.TitleHeight + 20;
        window.AddChild(inside);
        Driver.Step(3);
        int clicks = 0;
        inside.Click += (_, _) => clicks++;

        ClickCenter(inside.Visual);

        clicks.ShouldBe(1, $"{theme} hit {Describe(Driver.Cursor.VisualOver)} at {CenterX(inside.Visual)},{CenterY(inside.Visual)} size {inside.Visual.AbsoluteWidth}x{inside.Visual.AbsoluteHeight} inner {window.InnerPanel.AbsoluteTop}+{window.InnerPanel.AbsoluteHeight} title {window.Visual.GetGraphicalUiElementByName("TitleBarInstance")!.AbsoluteTop}+{window.Visual.GetGraphicalUiElementByName("TitleBarInstance")!.AbsoluteHeight} window {window.Visual.AbsoluteLeft},{window.Visual.AbsoluteTop},{window.Visual.AbsoluteWidth}x{window.Visual.AbsoluteHeight}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void DragSplitter_BetweenColumns_ResizesThePreviousSibling(string theme)
    {
        StackPanel host = Place<StackPanel>(LoadTheme(theme), "StackPanel");
        host.Orientation = Orientation.Horizontal;
        Panel first = new Panel();
        first.Width = 100;
        first.Height = 100;
        Splitter splitter = new Splitter();
        splitter.Width = 12;
        splitter.Height = 100;
        Panel second = new Panel();
        second.Width = 100;
        second.Height = 100;
        host.AddChild(first);
        host.AddChild(splitter);
        host.AddChild(second);
        Driver.Step(3);
        float before = first.Visual.AbsoluteWidth;

        DragFrom(CenterX(splitter.Visual), CenterY(splitter.Visual), CenterX(splitter.Visual) + 30, CenterY(splitter.Visual));

        first.Visual.AbsoluteWidth.ShouldBe(before + 30, 3f, theme);
    }
}
