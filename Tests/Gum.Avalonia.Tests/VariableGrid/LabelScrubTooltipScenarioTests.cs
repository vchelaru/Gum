using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>While a label is being dragged to change its value, no tooltip shows and the label is highlighted.</summary>
public class LabelScrubTooltipScenarioTests
{
    private const string Tip = "The X position";

    [AvaloniaFact]
    public void LabelScrub_WhileDragging_HidesTheRowToolTip_AndRestoresItOnRelease()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave sprite = grid.Project.AddInstance(button, "Sprite1", "Sprite");
        grid.Select(sprite);
        grid.Settle();
        Control row = grid.Row("X");
        ToolTip.SetTip(row, Tip);
        Point start = grid.Input.CenterOf(grid.Editor<TextBoxDisplay>("X").LabelHost);

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));

        ToolTip.GetTip(row).ShouldBeNull();

        grid.Input.Window.MouseUp(start + new Point(10, 0), MouseButton.Left, RawInputModifiers.None);
        grid.Settle();

        ToolTip.GetTip(row).ShouldBe(Tip);
    }

    [AvaloniaFact]
    public void LabelScrub_WhileDragging_HighlightsTheLabel_AndClearsItOnRelease()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave sprite = grid.Project.AddInstance(button, "Sprite1", "Sprite");
        grid.Select(sprite);
        grid.Settle();
        Border labelHost = (Border)grid.Editor<TextBoxDisplay>("X").LabelHost;
        Point start = grid.Input.CenterOf(labelHost);
        IBrush? before = labelHost.Background;

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));

        labelHost.Background.ShouldNotBe(before);
        ((ISolidColorBrush)labelHost.Background!).Color.A.ShouldBeGreaterThan((byte)0);

        grid.Input.Window.MouseUp(start + new Point(10, 0), MouseButton.Left, RawInputModifiers.None);
        grid.Settle();

        labelHost.Background.ShouldBe(before);
    }
}
