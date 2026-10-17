using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// A toggle-strip row's label column gives way before the strip wraps: 100 wide when there is room,
/// shrinking toward the floor only as far as the strip needs, and the strip wraps only below that.
/// </summary>
public class ToggleButtonOptionLayoutTests
{
    private sealed class Rig
    {
        public Window Window { get; }
        public TextBlock Label { get; }
        public Border Strip { get; }

        public Rig(double windowWidth)
        {
            EditorFixture fixture = new EditorFixture { Choice = FixtureChoice.First };
            ToggleButtonOption[] options = new[]
            {
                new ToggleButtonOption("A", FixtureChoice.First),
                new ToggleButtonOption("B", FixtureChoice.Second),
                new ToggleButtonOption("C", FixtureChoice.Third),
                new ToggleButtonOption("D", FixtureChoice.First),
                new ToggleButtonOption("E", FixtureChoice.Second),
            };
            ToggleButtonOptionDisplay display = new ToggleButtonOptionDisplay(options)
            {
                InstanceMember = fixture.Member(nameof(EditorFixture.Choice)),
            };
            Window = new Window { Content = display, Width = windowWidth, Height = 300 };
            Window.Show();
            Window.UpdateLayout();

            Label = display.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == nameof(EditorFixture.Choice));
            Strip = display.GetVisualDescendants().OfType<Border>().First(border => border.Classes.Contains(ToggleButtonOptionDisplay.OptionGroupClass));
        }
    }

    private static double NaturalStripWidth()
    {
        Rig wide = new Rig(1000);
        double width = wide.Strip.Bounds.Width;
        wide.Window.Close();
        return width;
    }

    [AvaloniaFact]
    public void LabelColumn_WithRoomToSpare_StaysAtItsFullWidth()
    {
        Rig rig = new Rig(NaturalStripWidth() + 400);

        rig.Label.Bounds.Width.ShouldBe(100);
        rig.Window.Close();
    }

    [AvaloniaFact]
    public void LabelColumn_WhenTight_ShrinksBySomeAmountTheStripNeeds_AndTheStripStaysOnOneLine()
    {
        double stripWidth = NaturalStripWidth();
        Rig oneLine = new Rig(1000);
        double oneLineHeight = oneLine.Strip.Bounds.Height;
        oneLine.Window.Close();

        Rig rig = new Rig(stripWidth + 60);

        rig.Label.Bounds.Width.ShouldBe(60, 1.0);
        rig.Strip.Bounds.Height.ShouldBe(oneLineHeight);
        rig.Window.Close();
    }

    [AvaloniaFact]
    public void LabelColumn_WhenStillTooTight_StopsAtTheFloor_AndTheStripWraps()
    {
        double stripWidth = NaturalStripWidth();
        Rig oneLine = new Rig(1000);
        double oneLineHeight = oneLine.Strip.Bounds.Height;
        oneLine.Window.Close();

        Rig rig = new Rig(DataUiRowLayout.MinLabelWidth + stripWidth - 40);

        rig.Label.Bounds.Width.ShouldBe(DataUiRowLayout.MinLabelWidth);
        rig.Strip.Bounds.Height.ShouldBeGreaterThan(oneLineHeight);
        rig.Window.Close();
    }
}
