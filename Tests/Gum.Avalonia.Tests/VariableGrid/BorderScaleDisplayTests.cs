using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Shouldly;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The NineSlice BorderScale composite (#6983): one uniform field while linked, four per-side fields
/// once unlinked, the same editor shape as the Rectangle corner radius.
/// </summary>
public class BorderScaleDisplayTests
{
    private class Fixture
    {
        public BorderScaleComposite BorderScale { get; set; } = new BorderScaleComposite(1, null, null, null, null);
        public InstanceMember Member() => new InstanceMember(nameof(BorderScale), this);
    }

    private static InstanceSave AddNineSlice(VariableGridHarness grid)
    {
        ComponentSave panel = grid.Project.AddComponent("Panel");
        return grid.Project.AddInstance(panel, "Frame", "NineSlice");
    }

    [AvaloniaFact]
    public void Display_StartsLinkedWithTheUniformValue()
    {
        Fixture fixture = new Fixture();
        BorderScaleDisplay display = new BorderScaleDisplay { InstanceMember = fixture.Member() };

        display.IsLinked.ShouldBeTrue();
        display.UniformTextBox.Text.ShouldBe("1");
    }

    [AvaloniaFact]
    public void Display_UnlinkingSeedsTheSides_AndSideEditsWriteThrough()
    {
        Fixture fixture = new Fixture();
        fixture.BorderScale = new BorderScaleComposite(2, null, null, null, null);
        BorderScaleDisplay display = new BorderScaleDisplay { InstanceMember = fixture.Member() };

        display.ToggleLinked();
        fixture.BorderScale.ShouldBe(new BorderScaleComposite(2, 2, 2, 2, 2));

        display.CornerTextBoxes[0].Text = "0";
        display.CornerTextBoxes[3].Text = "0.5";
        display.Commit();

        fixture.BorderScale.ShouldBe(new BorderScaleComposite(2, 0, 2, 2, 0.5f));
    }

    [AvaloniaFact]
    public void Display_RelinkingClearsEverySideOverride()
    {
        Fixture fixture = new Fixture();
        fixture.BorderScale = new BorderScaleComposite(2, 1, null, 3, null);
        BorderScaleDisplay display = new BorderScaleDisplay { InstanceMember = fixture.Member() };
        display.IsLinked.ShouldBeFalse();

        display.ToggleLinked();

        fixture.BorderScale.ShouldBe(new BorderScaleComposite(2, null, null, null, null));
    }

    [AvaloniaFact]
    public void Display_ShowsAnUnlinkedValue_WithEmptyFieldsForInheritedSides()
    {
        Fixture fixture = new Fixture();
        fixture.BorderScale = new BorderScaleComposite(2, 1, null, 0, null);
        BorderScaleDisplay display = new BorderScaleDisplay { InstanceMember = fixture.Member() };

        display.IsLinked.ShouldBeFalse();
        display.CornerTextBoxes.Select(box => box.Text).ShouldBe(new[] { "1", "", "0", "" });
    }

    [AvaloniaFact]
    public void Display_RejectsAValueThatIsNotABorderScale()
    {
        BorderScaleDisplay display = new BorderScaleDisplay();

        display.TrySetValueOnUi(new CornerRadiusComposite(1, null, null, null, null)).ShouldBe(WpfDataUi.ApplyValueResult.NotSupported);
    }

    [AvaloniaFact]
    public void Row_ForANineSlice_IsOneCompositeRow_AndNotFiveSeparateOnes()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));

        grid.Editor<BorderScaleDisplay>("BorderScale").ShouldNotBeNull();
        Should.Throw<InvalidOperationException>(() => grid.Member("CustomBorderScaleLeft"));
    }

    [AvaloniaFact]
    public void Row_TypedLinked_ThenUnlinkedWithAZeroSide_SavesEachChannel()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave panel = grid.Project.AddComponent("Panel");
        InstanceSave frame = grid.Project.AddInstance(panel, "Frame", "NineSlice");
        grid.Select(frame);

        BorderScaleDisplay display = grid.Editor<BorderScaleDisplay>("BorderScale");
        grid.Input.TypeAndEnter(display.UniformTextBox, "2");
        grid.Settle();
        VariableGridHarness.StoredValue(panel, "Frame.BorderScale").ShouldBe(2f);

        display = grid.Editor<BorderScaleDisplay>("BorderScale");
        grid.Input.Click(display.GetVisualDescendants().OfType<Button>().Single());
        grid.Settle();
        display = grid.Editor<BorderScaleDisplay>("BorderScale");
        display.IsLinked.ShouldBeFalse();
        grid.Input.TypeAndEnter(display.CornerTextBoxes[1], "0");
        grid.Settle();

        ComponentSave saved = grid.ReadSaved(panel);
        VariableGridHarness.StoredValue(saved, "Frame.BorderScale").ShouldBe(2f);
        VariableGridHarness.StoredValue(saved, "Frame.CustomBorderScaleTop").ShouldBe(0f);
    }

    [AvaloniaFact]
    public void Scrub_DraggingTheUniformLabel_CommitsOnceAsAFraction()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));
        Control label = grid.Editor<BorderScaleDisplay>("BorderScale").UniformLabel;
        Point start = grid.Input.CenterOf(label);
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));
        grid.Input.LosePointerCapture();
        grid.Settle();

        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Frame.BorderScale");
        committed.ShouldBeOfType<float>().ShouldBeGreaterThan(1f);
        ((float)committed! * 100f).ShouldBe((float)System.Math.Round((float)committed * 100f), 0.01f);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore + 1);
    }
}
