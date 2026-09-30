using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A prompt raised while a dialog is open is modal over that dialog (#5538). Avalonia's ShowDialog
/// disables only the owner, so a prompt owned by the main window left the dialog behind it clickable.
/// These run the head's own dialog service (<see cref="HeadDialogScript"/>), not scripted answers.
/// </summary>
[Trait("Category", "EndToEnd")]
public class NestedDialogScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "DLG-006")]
    public void RenameComponent_TheRenameFilePromptIsOwnedByTheRenameDialog()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        using HeadDialogScript dialogs = new HeadDialogScript();
        Window? renameDialog = null;
        Window? promptOwner = null;
        string? promptTitle = null;
        dialogs.When(1, rename =>
        {
            renameDialog = rename.Window;
            rename.TypeInto(rename.Find<TextBox>(), "PrimaryButton");
            rename.Click(rename.AffirmativeButton);
        });
        dialogs.When(2, prompt =>
        {
            promptOwner = prompt.Window.Owner as Window;
            promptTitle = prompt.Title;
            prompt.ClickButton("Yes");
        });

        tree.Press(Key.F2, PhysicalKey.F2);
        dialogs.AssertFinished();

        promptTitle.ShouldBe("Rename Object and File?");
        renameDialog.ShouldNotBeNull();
        promptOwner.ShouldBeSameAs(renameDialog);
        button.Name.ShouldBe("PrimaryButton");
    }

    [AvaloniaFact]
    [Trait("Feature", "DLG-021")]
    public void AddForms_TheOverwritePromptIsOwnedByTheAddFormsDialog()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        // A styled, used standard is what the theme would overwrite, so Add Forms asks first.
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Label", "Text");
        tree.Project.Standard("Text").DefaultState.ShouldNotBeNull().SetValue("FontSize", 40, "int");
        tree.SaveAll();
        using HeadDialogScript dialogs = new HeadDialogScript();
        Window? addFormsDialog = null;
        Window? promptOwner = null;
        string? promptTitle = null;
        dialogs.When(1, addForms =>
        {
            addFormsDialog = addForms.Window;
            addForms.Click(addForms.AffirmativeButton);
        });
        dialogs.When(2, prompt =>
        {
            promptOwner = prompt.Window.Owner as Window;
            promptTitle = prompt.Title;
            prompt.ClickButton("No");
        });

        tree.PickMainMenu("Content", "Add Forms Components");
        dialogs.AssertFinished();

        promptTitle.ShouldBe("Overwrite files?");
        addFormsDialog.ShouldNotBeNull();
        promptOwner.ShouldBeSameAs(addFormsDialog);
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Card" }, "declining the overwrite imports nothing");
    }
}
