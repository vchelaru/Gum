using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>The tab while the project changes around it: deletes, locks, standard elements, bad references.</summary>
public class VariableLifecycleScenarioTests
{
    [AvaloniaFact]
    public void DeletingTheSelectedInstance_LeavesTheElementsRows()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        IDeleteLogic deleteLogic = TestAppBuilder.Services.GetRequiredService<IDeleteLogic>();
        using (grid.UndoManager.RequestLock())
        {
            deleteLogic.RemoveInstance(label, button);
        }
        grid.Settle();

        grid.ShownMemberNames().ShouldNotContain(name => name.StartsWith("Label."));
        grid.ShownMemberNames().ShouldContain("Width");
    }

    [AvaloniaFact]
    public void ALockedInstance_ShowsReadOnlyRows()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        grid.Editor<AvaloniaDataUi.Controls.CheckBoxDisplay>("Locked").CheckBox.IsChecked = true;
        grid.Settle();

        label.Locked.ShouldBeTrue();
        grid.Member("X").IsReadOnly.ShouldBeTrue();
        grid.TextField("X").IsEffectivelyEnabled.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void EditingAStandardElement_ChangesItsDefault_AndUndoRestoresIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        StandardElementSave text = grid.Project.Standard("Text");
        object? fontSizeBefore = VariableGridHarness.StoredValue(text, "FontSize");
        grid.Select(text);

        grid.TypeAndEnter("FontSize", "31");

        VariableGridHarness.StoredValue(text, "FontSize").ShouldBe(31);

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "FontSize").ShouldBe(fontSizeBefore);
        grid.FieldText("FontSize").ShouldBe(fontSizeBefore?.ToString());
    }

    [AvaloniaFact]
    public void AReferenceToAMissingVariable_IsReported_AndCommentedOut()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Width", "90");

        grid.Dialogs.AnswerNextMessage(Gum.Services.Dialogs.MessageDialogResult.Affirmative);
        grid.TypeLinesAndApply("VariableReferences", "Width = NoSuchVariable");

        grid.Dialogs.Messages.Single().ShouldContain("Width = NoSuchVariable");
        VariableGridHarness.StoredValue(button, "Width").ShouldBe(90f);
        button.GetDefaultStateOrThrow().GetVariableListSave("VariableReferences")!.ValueAsIList!.Cast<string>().Single().ShouldStartWith("//");
        grid.TypeAndEnter("Height", "12");
        VariableGridHarness.StoredValue(button, "Height").ShouldBe(12f);
    }

    [AvaloniaFact]
    public void RenamingAStateWhileItIsShown_KeepsEditingThatState()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        grid.Select(button);
        grid.Select(pressed);

        grid.Dialogs.AnswerNextUserString("Down");
        TestAppBuilder.Services.GetRequiredService<Gum.Commands.IEditCommands>().AskToRenameState(pressed, button);
        grid.Settle();
        grid.TypeAndEnter("Width", "215");

        pressed.Name.ShouldBe("Down");
        VariableGridHarness.StoredValue(button, "Width", pressed).ShouldBe(215f);
        (grid.ViewModel.StateInformation ?? "").ShouldContain("Down");
    }
}
