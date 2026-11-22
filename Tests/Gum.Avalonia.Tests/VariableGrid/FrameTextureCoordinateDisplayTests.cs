using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The NineSlice source cut composite (#8354): Custom Frame Texture Coordinate Width plus a cut per side,
/// the same editor as Border Scale except that a blank shared value means a third of the texture.
/// </summary>
public class FrameTextureCoordinateDisplayTests
{
    private class Fixture
    {
        public NineSliceSidesComposite Cut { get; set; } = new NineSliceSidesComposite(null, null, null, null, null);
        public InstanceMember Member() => new InstanceMember(nameof(Cut), this);
    }

    [AvaloniaFact]
    public void Display_ShowsABlankSharedValue_AndABlankCommitStaysNull()
    {
        Fixture fixture = new Fixture { Cut = new NineSliceSidesComposite(4, null, null, null, null) };
        FrameTextureCoordinateDisplay display = new FrameTextureCoordinateDisplay { InstanceMember = fixture.Member() };
        display.UniformTextBox.Text.ShouldBe("4");

        display.UniformTextBox.Text = "";
        display.Commit();

        fixture.Cut.ShouldBe(new NineSliceSidesComposite(null, null, null, null, null));
        display.UniformTextBox.Text.ShouldBe("");
    }

    [AvaloniaFact]
    public void Unlinking_WithABlankSharedValue_StaysUnlinked_UntilASideIsTyped()
    {
        Fixture fixture = new Fixture();
        FrameTextureCoordinateDisplay display = new FrameTextureCoordinateDisplay { InstanceMember = fixture.Member() };

        display.ToggleLinked();

        display.IsLinked.ShouldBeFalse();
        display.CornerTextBoxes.Select(box => box.Text).ShouldBe(new[] { "", "", "", "" });

        display.CornerTextBoxes[1].Text = "6";
        display.Commit();

        fixture.Cut.ShouldBe(new NineSliceSidesComposite(null, null, 6, null, null));
        display.IsLinked.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void Unlinking_WithASharedValue_SeedsEverySide()
    {
        Fixture fixture = new Fixture { Cut = new NineSliceSidesComposite(4, null, null, null, null) };
        FrameTextureCoordinateDisplay display = new FrameTextureCoordinateDisplay { InstanceMember = fixture.Member() };

        display.ToggleLinked();

        fixture.Cut.ShouldBe(new NineSliceSidesComposite(4, 4, 4, 4, 4));
    }

    [AvaloniaFact]
    public void Row_ForANineSlice_IsOneCompositeRow_AndATypedSideSaves()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave panel = grid.Project.AddComponent("Panel");
        InstanceSave frame = grid.Project.AddInstance(panel, "Frame", "NineSlice");
        grid.Select(frame);
        Should.Throw<InvalidOperationException>(() => grid.Member("CustomFrameTextureCoordinateLeft"));

        FrameTextureCoordinateDisplay display = grid.Editor<FrameTextureCoordinateDisplay>("CustomFrameTextureCoordinateWidth");
        grid.Input.Click(display.GetVisualDescendants().OfType<Button>().Single());
        grid.Settle();
        display = grid.Editor<FrameTextureCoordinateDisplay>("CustomFrameTextureCoordinateWidth");
        grid.Input.TypeAndEnter(display.CornerTextBoxes[0], "5");
        grid.Settle();

        ComponentSave saved = grid.ReadSaved(panel);
        VariableGridHarness.StoredValue(saved, "Frame.CustomFrameTextureCoordinateLeft").ShouldBe(5f);
        VariableGridHarness.StoredValue(saved, "Frame.CustomFrameTextureCoordinateWidth").ShouldBeNull();
    }
}
