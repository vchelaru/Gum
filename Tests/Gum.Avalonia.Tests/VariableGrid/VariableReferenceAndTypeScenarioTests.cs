using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>References between instances, the color row, base-type changes, and undo of structural grid edits.</summary>
public class VariableReferenceAndTypeScenarioTests
{
    [AvaloniaFact]
    public void AnInstanceReference_ToASibling_FollowsTheSibling()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(leader);
        grid.TypeAndEnter("X", "25");
        grid.Select(follower);

        grid.TypeLinesAndApply("VariableReferences", "X = Leader.X");

        VariableGridHarness.StoredValue(button, "Follower.X").ShouldBe(25f);
        grid.Member("X").IsReadOnly.ShouldBeTrue();

        grid.Select(leader);
        grid.TypeAndEnter("X", "70");

        VariableGridHarness.StoredValue(button, "Follower.X").ShouldBe(70f);
        grid.Select(follower);
        grid.FieldText("X").ShouldBe("70");
    }

    [AvaloniaFact]
    public void UndoingAVariableReference_RestoresTheValue_AndTheRowIsEditableAgain()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Height", "40");
        grid.TypeAndEnter("Width", "90");
        grid.TypeLinesAndApply("VariableReferences", "Width = Height");
        VariableGridHarness.StoredValue(button, "Width").ShouldBe(40f);

        grid.Undo();

        ElementSave undone = grid.SelectedState.SelectedElement!;
        VariableGridHarness.StoredValue(undone, "Width").ShouldBe(90f);
        grid.Member("Width").IsReadOnly.ShouldBeFalse();
        grid.FieldText("Width").ShouldBe("90");
    }

    [AvaloniaFact]
    public void TypingAHexColor_SetsRedGreenBlue_AndUndoRestoresThem()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        ColorDisplay color = grid.Editor<ColorDisplay>("Color");

        grid.Input.TypeAndEnter(color.HexTextBox, "FF8000");
        grid.Settle();

        // Red already is 255 (Text is white), so the composite leaves that channel inherited.
        VariableGridHarness.StoredValue(button, "Label.Red").ShouldBeNull();
        VariableGridHarness.StoredValue(button, "Label.Green").ShouldBe(128);
        VariableGridHarness.StoredValue(button, "Label.Blue").ShouldBe(0);

        grid.Undo();

        ElementSave undone = grid.SelectedState.SelectedElement!;
        VariableGridHarness.StoredValue(undone, "Label.Red").ShouldBeNull();
        VariableGridHarness.StoredValue(undone, "Label.Green").ShouldBeNull();
        VariableGridHarness.StoredValue(undone, "Label.Blue").ShouldBeNull();
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe("FFFFFF");
    }

    [AvaloniaFact]
    public void ChangingAnInstancesBaseType_ShowsTheNewTypesRows()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave thing = grid.Project.AddInstance(button, "Thing", "Container");
        grid.Select(thing);
        grid.ShownMemberNames().ShouldNotContain("Thing.Text");

        grid.PickComboItem("BaseType", "Text");

        thing.BaseType.ShouldBe("Text");
        grid.ShownMemberNames().ShouldContain("Thing.Text");
    }

    [AvaloniaFact]
    public void SelectingAnotherInstance_OfAnotherType_ShowsItsRows()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(box);
        grid.TypeAndEnter("X", "5");

        grid.Select(label);

        grid.ShownMemberNames().ShouldContain("Label.Text");
        grid.ShownMemberNames().ShouldNotContain(name => name.StartsWith("Box."));
        grid.FieldText("Label.X").ShouldBe("0");
    }

    [AvaloniaFact]
    public void MultiSelect_OfDifferentTypes_EditsASharedRowOnBoth()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");

        grid.Select(box, label);

        grid.TypeAndEnter("Y", "14");

        VariableGridHarness.StoredValue(button, "Box.Y").ShouldBe(14f);
        VariableGridHarness.StoredValue(button, "Label.Y").ShouldBe(14f);
    }

    [AvaloniaFact]
    public void EscapeInTheFilterBox_ClearsIt_AndShowsEveryRow()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        int allRows = grid.ShownMemberNames().Count;
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");
        grid.ShownMemberNames().Count.ShouldBeLessThan(allRows);

        grid.Input.Press(Key.Escape, PhysicalKey.Escape);

        grid.View.FilterTextBox.Text.ShouldBeNullOrEmpty();
        grid.ShownMemberNames().Count.ShouldBe(allRows);
    }

    [AvaloniaFact]
    public void UndoingAnExpose_RemovesTheExposedVariable()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.Dialogs.AnswerNextUserString("LabelText");
        grid.PickRowMenuItem("Text", "Expose Variable");

        grid.Undo();

        grid.SelectedState.SelectedElement!.GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        grid.RowMenu("Text").ShouldContain("Expose Variable");
    }
}
