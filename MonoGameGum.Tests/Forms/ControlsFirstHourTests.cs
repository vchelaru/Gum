using System.Collections.Generic;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Button, Label, CheckBox and Slider as a beginner uses them: created in code, added to a
/// StackPanel, clicked and dragged by the cursor, disabled and re-enabled.
/// </summary>
public class ControlsFirstHourTests : BeginnerDriverTestBase
{
    [Fact]
    public void Click_ShouldRaiseOnce_WhenButtonIsInsideStackPanel()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button button = new() { Name = "Btn", Text = "Click Me", Width = 350 };
        panel.AddChild(button);
        Driver.Step();
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Driver.Click("Btn");

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Click_ShouldNotRaise_WhenButtonIsDisabled()
    {
        Button button = AddToRoot(new Button { Text = "Disabled" }, "Btn");
        button.IsEnabled = false;
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        Driver.Click("Btn");

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Click_ShouldRaiseAgain_WhenButtonIsDisabledThenReenabled()
    {
        Button button = AddToRoot(new Button { Text = "Toggle" }, "Btn");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        button.IsEnabled = false;
        Driver.Click("Btn");

        button.IsEnabled = true;
        Driver.Click("Btn");

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Click_ShouldNotRaise_WhenButtonIsDisabledBetweenPushAndRelease()
    {
        Button button = AddToRoot(new Button { Text = "Hold" }, "Btn");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Driver.MoveTo("Btn");
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);

        button.IsEnabled = false;
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Text_ShouldNotThrow_WhenButtonAndLabelAreGivenNull()
    {
        Button button = new() { Text = "x" };
        Label label = new() { Text = "x" };

        Should.NotThrow(() =>
        {
            button.Text = null!;
            label.Text = null!;
        });
    }

    [Fact]
    public void Click_ShouldRaiseCheckedThenUnchecked_WhenCheckBoxClickedTwice()
    {
        CheckBox checkBox = AddToRoot(new CheckBox { Text = "Sound" }, "Cb");
        List<string> events = new();
        checkBox.Checked += (_, _) => events.Add("checked");
        checkBox.Unchecked += (_, _) => events.Add("unchecked");

        Driver.Click("Cb");
        Driver.Click("Cb");

        events.ShouldBe(new[] { "checked", "unchecked" });
        checkBox.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void Click_ShouldNotToggle_WhenCheckBoxIsDisabled()
    {
        CheckBox checkBox = AddToRoot(new CheckBox(), "Cb");
        checkBox.IsEnabled = false;

        Driver.Click("Cb");

        checkBox.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void Click_ShouldCycleThroughIndeterminate_WhenCheckBoxIsThreeState()
    {
        CheckBox checkBox = AddToRoot(new CheckBox { IsThreeState = true }, "Cb");
        List<bool?> seen = new();

        for (int i = 0; i < 4; i++)
        {
            Driver.Click("Cb");
            seen.Add(checkBox.IsChecked);
        }

        seen.ShouldBe(new bool?[] { true, null, false, true });
    }

    [Fact]
    public void IsChecked_ShouldBeSettableToNull_WhenCheckBoxIsNotThreeState()
    {
        CheckBox checkBox = new();
        bool indeterminate = false;
        checkBox.Indeterminate += (_, _) => indeterminate = true;

        checkBox.IsChecked = null;

        indeterminate.ShouldBeTrue();
        checkBox.IsChecked.ShouldBeNull();
    }

    [Fact]
    public void Value_ShouldNotRaiseValueChanged_WhenSetToTheCurrentValue()
    {
        Slider slider = new() { Minimum = 0, Maximum = 100 };
        slider.Value = 40;
        int changes = 0;
        slider.ValueChanged += (_, _) => changes++;

        slider.Value = 40;

        changes.ShouldBe(0);
    }

    [Fact]
    public void Value_ShouldRaiseValueChangedOnce_WhenValueChanges()
    {
        Slider slider = new() { Minimum = 0, Maximum = 100 };
        int changes = 0;
        slider.ValueChanged += (_, _) => changes++;

        slider.Value = 25;

        changes.ShouldBe(1);
    }

    [Fact]
    public void Value_ShouldClampToNewMaximum_WhenMaximumLoweredBelowValue()
    {
        Slider slider = new() { Minimum = 0, Maximum = 100, Value = 80 };

        slider.Maximum = 10;

        slider.Value.ShouldBe(10);
    }

    [Fact]
    public void Value_ShouldReachMaximum_WhenThumbIsDraggedPastTheRightEnd()
    {
        Slider slider = AddToRoot(new Slider { Width = 200, Minimum = 0, Maximum = 100 }, "Sl");
        Driver.Step();
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x + 400, y);

        slider.Value.ShouldBe(100, 0.001);
    }

    [Fact]
    public void Value_ShouldReachMinimum_WhenThumbIsDraggedPastTheLeftEnd()
    {
        Slider slider = AddToRoot(new Slider { Width = 200, Minimum = 0, Maximum = 100 }, "Sl");
        slider.Value = 100;
        Driver.Step();
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x - 400, y);

        slider.Value.ShouldBe(0, 0.001);
    }

    [Fact]
    public void Value_ShouldLandOnTicks_WhenSnapToTickIsEnabledAndThumbIsDragged()
    {
        Slider slider = AddToRoot(new Slider
        {
            Width = 200, Minimum = 0, Maximum = 100, IsSnapToTickEnabled = true, TicksFrequency = 10
        }, "Sl");
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x + 33, y);

        (slider.Value % 10).ShouldBe(0, 0.001);
    }

    [Fact]
    public void Value_ShouldChangeBySmallChange_WhenArrowKeyIsPressedOnFocusedSlider()
    {
        Gum.GumService.Default.UseKeyboardDefaults();
        Slider slider = AddToRoot(new Slider { Minimum = 0, Maximum = 100, SmallChange = 5, Value = 50 }, "Sl");
        slider.IsFocused = true;

        Driver.PressKey(GumKeys.Right);

        slider.Value.ShouldBe(55, 0.001);
    }

    [Fact]
    public void Value_ShouldNotChange_WhenSliderIsDisabledAndDragged()
    {
        Slider slider = AddToRoot(new Slider { Width = 200, Minimum = 0, Maximum = 100 }, "Sl");
        slider.IsEnabled = false;
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x + 100, y);

        slider.Value.ShouldBe(0);
    }
}
