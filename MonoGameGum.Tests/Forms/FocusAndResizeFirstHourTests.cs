using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Keyboard focus navigation through a StackPanel of buttons, and docked/anchored layout after the
/// window (canvas) is resized, as shown in the keyboard and resizing tutorials.
/// </summary>
public class FocusAndResizeFirstHourTests : BeginnerDriverTestBase
{
    public FocusAndResizeFirstHourTests()
    {
        Gum.GumService.Default.UseKeyboardDefaults();
    }

    private Button[] AddButtons(int count)
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button[] buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            buttons[i] = new Button { Name = $"B{i}", Text = $"Button {i}" };
            panel.AddChild(buttons[i]);
        }
        Driver.Step();
        return buttons;
    }

    [Fact]
    public void Tab_ShouldMoveFocusToNextButton()
    {
        Button[] buttons = AddButtons(3);
        buttons[0].IsFocused = true;

        Driver.PressKey(GumKeys.Tab);

        buttons[1].IsFocused.ShouldBeTrue();
        buttons[0].IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Tab_ShouldSkipDisabledButton()
    {
        Button[] buttons = AddButtons(3);
        buttons[1].IsEnabled = false;
        buttons[0].IsFocused = true;

        Driver.PressKey(GumKeys.Tab);

        buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Tab_ShouldSkipHiddenButton()
    {
        Button[] buttons = AddButtons(3);
        buttons[1].IsVisible = false;
        buttons[0].IsFocused = true;

        Driver.PressKey(GumKeys.Tab);

        buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Tab_ShouldNotThrow_WhenNothingIsFocused()
    {
        AddButtons(2);

        Should.NotThrow(() => Driver.PressKey(GumKeys.Tab));
    }

    [Fact]
    public void Enter_ShouldClickFocusedButton()
    {
        Button[] buttons = AddButtons(2);
        int clicks = 0;
        buttons[1].Click += (_, _) => clicks++;
        buttons[1].IsFocused = true;

        Driver.PressKey(GumKeys.Enter);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Enter_ShouldNotClick_WhenFocusedButtonIsDisabledAfterFocus()
    {
        Button[] buttons = AddButtons(2);
        int clicks = 0;
        buttons[0].Click += (_, _) => clicks++;
        buttons[0].IsFocused = true;
        buttons[0].IsEnabled = false;

        Driver.PressKey(GumKeys.Enter);

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Enter_ShouldClickNewScreensButton_WhenFocusWasGivenToItAfterScreenSwitch()
    {
        Button[] first = AddButtons(1);
        first[0].IsFocused = true;
        first[0].Visual.Parent!.Parent = null;
        Driver.Step();
        Button second = AddToRoot(new Button { Text = "Second" }, "Second");
        int clicks = 0;
        second.Click += (_, _) => clicks++;
        second.IsFocused = true;

        Driver.PressKey(GumKeys.Enter);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void AbsoluteRight_ShouldFollowCanvasWidth_WhenTopRightAnchoredButtonCanvasGrows()
    {
        Button button = new() { Name = "Corner", Width = 100 };
        button.AddToRoot();
        button.Visual.Anchor(Anchor.TopRight);
        Driver.Step();
        button.Visual.AbsoluteRight.ShouldBe(800, 0.5f);

        GraphicalUiElement.CanvasWidth = 1000;
        GraphicalUiElement.CanvasHeight = 700;
        Driver.Step(2);

        button.Visual.AbsoluteRight.ShouldBe(1000, 0.5f);
    }

    [Fact]
    public void Width_ShouldFollowCanvas_WhenPanelDockedFillAndCanvasShrinks()
    {
        StackPanel panel = new() { Name = "Fill" };
        panel.AddToRoot();
        panel.Visual.Dock(Dock.Fill);
        Driver.Step();
        panel.Visual.AbsoluteWidth.ShouldBe(800, 0.5f);

        GraphicalUiElement.CanvasWidth = 640;
        GraphicalUiElement.CanvasHeight = 480;
        Driver.Step(2);

        panel.Visual.AbsoluteWidth.ShouldBe(640, 0.5f);
        panel.Visual.AbsoluteHeight.ShouldBe(480, 0.5f);
    }

    [Fact]
    public void Click_ShouldStillHit_WhenCenteredButtonIsLaidOutAfterCanvasResize()
    {
        Button button = new() { Name = "Center", Width = 100 };
        button.AddToRoot();
        button.Visual.Anchor(Anchor.Center);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        GraphicalUiElement.CanvasWidth = 1280;
        GraphicalUiElement.CanvasHeight = 720;
        Driver.Step(2);

        Driver.Click("Center");

        clicks.ShouldBe(1);
    }
}
