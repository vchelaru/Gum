using Brutal.GlfwApi;
using Gum;
using Gum.Forms.Controls;
using Gum.Input;
using Shouldly;

namespace BrutalGum.Tests.Forms;

/// <summary>
/// A gamepad on the fake GLFW window, driven through <see cref="GumService.Update(double)"/> the way a
/// game loop does, reaching <see cref="GumService.Gamepads"/> and the focused Forms control. Pins parity
/// with the MonoGame family, Silk and raylib hosts.
/// </summary>
public class GamepadFormsTests : BaseTestClass
{
    private static FakeBrutalInput Window => TestAssemblyInitialize.Input;

    private double _time = 100;

    public GamepadFormsTests()
    {
        Window.Gamepads.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
    }

    public override void Dispose()
    {
        Window.Gamepads.Clear();
        Frame();
        FrameworkElement.GamePadsForUiControl.Clear();
        base.Dispose();
    }

    private FakeGamepadReading Connect(int index = 0)
    {
        FakeGamepadReading reading = new();
        Window.Gamepads[index] = reading;
        return reading;
    }

    private void Frame() => GumService.Default.Update(_time += 1);

    private static Button AddButtonFocused()
    {
        Button button = new();
        button.AddToRoot();
        button.IsFocused = true;
        return button;
    }

    private static (Button First, Button Second) AddTwoButtons()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button first = new();
        Button second = new();
        panel.AddChild(first);
        panel.AddChild(second);
        first.IsFocused = true;
        return (first, second);
    }

    [Fact]
    public void Update_WithGamepadConnected_ReportsConnectedOnGumServiceGamepads()
    {
        Connect();

        Frame();

        GumService.Default.Gamepads[0].IsConnected.ShouldBeTrue();
        GumService.Default.Gamepads[1].IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Update_ReadsEachGamepadFromItsOwnIndex()
    {
        Connect(2).Hold(GlfwGamepadButton.Y);

        Frame();

        GumService.Default.Gamepads[2].ButtonDown(GamepadButton.Y).ShouldBeTrue();
        GumService.Default.Gamepads[0].ButtonDown(GamepadButton.Y).ShouldBeFalse();
        GumService.Default.Gamepads[2].IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void Update_LeftStickPushedUp_ReadsAsDPadUp()
    {
        // GLFW's positive Y is down, Gum's is up.
        Connect().SetLeftStick(0f, -1f);

        Frame();

        GumService.Default.Gamepads[0].LeftStick.AsDPadDown(DPadDirection.Up).ShouldBeTrue();
        GumService.Default.Gamepads[0].LeftStick.AsDPadDown(DPadDirection.Down).ShouldBeFalse();
    }

    [Fact]
    public void Update_StickInsideDefaultDeadzone_ReadsZero()
    {
        Connect().SetLeftStick(0.05f, -0.05f);

        Frame();

        GumService.Default.Gamepads[0].LeftStick.X.ShouldBe(0f);
        GumService.Default.Gamepads[0].LeftStick.Y.ShouldBe(0f);
    }

    [Fact]
    public void Update_StickPastDeadzone_ReadsNonZero()
    {
        Connect().SetLeftStick(0.9f, 0f);

        Frame();

        GumService.Default.Gamepads[0].LeftStick.X.ShouldBeGreaterThan(0.5f);
    }

    [Fact]
    public void UseGamepadDefaults_AButtonPushed_ClicksFocusedButton()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = AddButtonFocused();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.A);
        Frame();

        clicks.ShouldBe(1);
    }

    [Fact]
    public void UseGamepadDefaults_AButtonHeld_ClicksOnlyOnce()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = AddButtonFocused();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.A);
        Frame();
        Frame();
        Frame();

        clicks.ShouldBe(1);
    }

    [Fact]
    public void WithoutGamepadDefaults_AButtonPushed_DoesNotClick()
    {
        Button button = AddButtonFocused();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.A);
        Frame();

        clicks.ShouldBe(0);
    }

    [Fact]
    public void UseGamepadDefaults_DPadDown_MovesFocusToNextControl()
    {
        GumService.Default.UseGamepadDefaults();
        (Button first, Button second) = AddTwoButtons();
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.DpadDown);
        Frame();

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }
    private static (Button Top, Button Middle, Button Bottom) AddThreeButtonsFocusMiddle()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button top = new();
        Button middle = new();
        Button bottom = new();
        panel.AddChild(top);
        panel.AddChild(middle);
        panel.AddChild(bottom);
        middle.IsFocused = true;
        return (top, middle, bottom);
    }

    [Fact]
    public void UseGamepadDefaults_LeftStickDown_MovesFocusToControlBelow()
    {
        GumService.Default.UseGamepadDefaults();
        (Button top, Button middle, Button bottom) = AddThreeButtonsFocusMiddle();
        FakeGamepadReading pad = Connect();
        Frame();

        // GLFW's positive Y is down.
        pad.SetLeftStick(0f, 1f);
        Frame();

        bottom.IsFocused.ShouldBeTrue();
        top.IsFocused.ShouldBeFalse();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void UseGamepadDefaults_LeftStickUp_MovesFocusToControlAbove()
    {
        GumService.Default.UseGamepadDefaults();
        (Button top, Button middle, Button bottom) = AddThreeButtonsFocusMiddle();
        FakeGamepadReading pad = Connect();
        Frame();

        pad.SetLeftStick(0f, -1f);
        Frame();

        top.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
        bottom.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void UseGamepadDefaults_DPadRight_MovesFocusedSliderUp()
    {
        GumService.Default.UseGamepadDefaults();
        Slider slider = new() { Minimum = 0, Maximum = 100, SmallChange = 5, Value = 50 };
        slider.AddToRoot();
        slider.IsFocused = true;
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.DpadRight);
        Frame();

        slider.Value.ShouldBe(55);
    }

    [Fact]
    public void UseGamepadDefaults_DPadDown_MovesListBoxSelection()
    {
        GumService.Default.UseGamepadDefaults();
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 5; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }
        listBox.SelectedIndex = 0;
        listBox.DoListItemsHaveFocus = true;
        listBox.IsFocused = true;
        FakeGamepadReading pad = Connect();
        Frame();

        pad.Hold(GlfwGamepadButton.DpadDown);
        Frame();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Update_GamepadUnplugged_DisconnectsWithoutThrowing()
    {
        Connect();
        Frame();
        GumService.Default.Gamepads[0].IsConnected.ShouldBeTrue();

        Window.Gamepads.Clear();

        Should.NotThrow(Frame);
        GumService.Default.Gamepads[0].IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Update_GamepadReplugged_ReconnectsAndWorksAgain()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = AddButtonFocused();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Connect();
        Frame();
        Window.Gamepads.Clear();
        Frame();

        Connect().Hold(GlfwGamepadButton.A);
        Frame();

        GumService.Default.Gamepads[0].IsConnected.ShouldBeTrue();
        clicks.ShouldBe(1);
    }

    [Fact]
    public void Update_NoGamepadEverConnected_DoesNotThrowAndIsNoOpForFocusedControls()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = AddButtonFocused();
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Should.NotThrow(() =>
        {
            Frame();
            Frame();
        });

        clicks.ShouldBe(0);
        button.IsFocused.ShouldBeTrue();
    }
}
