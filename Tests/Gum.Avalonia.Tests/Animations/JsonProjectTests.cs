using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// A .gumj project keeps its animations in .ganj sidecars (issue #4182): every path the tab
/// writes, moves, copies or offers to delete must follow the project's own format.
/// </summary>
public class JsonProjectTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void InAJsonProject_TheTabWritesAJsonSidecar_AndReadsItBack()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness(jsonProject: true);
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(button);

        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");

        string path = editor.AnimationFilePath(button);
        path.ShouldEndWith("ButtonAnimations.ganj");
        File.Exists(path).ShouldBeTrue();
        File.ReadAllText(path).TrimStart().ShouldStartWith("{");
        Directory.GetFiles(Path.GetDirectoryName(path)!, "*.ganx").ShouldBeEmpty();
        editor.Select(other);
        editor.Select(button);
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(2);
        editor.ViewModel.Animations.Single().Loops.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void InAJsonProject_RenameAndDuplicate_MoveAndCopyTheJsonSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness(jsonProject: true);
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        string oldPath = editor.AnimationFilePath(button);

        button.Name = "PushButton";
        editor.PluginManager.ElementRename(button, "Button");
        editor.Layout();

        File.Exists(oldPath).ShouldBeFalse();
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Walk");

        ComponentSave copy = editor.AddComponent("PushButtonCopy", Category, "Pressed");
        editor.PluginManager.ElementDuplicate(button, copy);

        editor.AnimationFilePath(copy).ShouldEndWith(".ganj");
        editor.ReadSavedAnimations(copy).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Walk");
    }

    [AvaloniaFact]
    public void InAJsonProject_TheDeleteConfirmation_NamesTheJsonSidecar_AndDeletesIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness(jsonProject: true);
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(button);
        editor.AddAnimation("Walk");
        DeleteOptionsDialogViewModel dialog = new DeleteOptionsDialogViewModel();
        object[] deleting = { button };

        editor.PluginManager.ShowDeleteOptions(dialog, deleting);

        // Other plugins add options of their own (generated code, for one).
        DeleteOptionCheckboxViewModel option = dialog.CheckBoxes.Single(candidate => candidate.Label.StartsWith("Delete Animation file"));
        option.Label.ShouldBe("Delete Animation file (.ganj)");
        option.IsChecked.ShouldBeTrue();

        editor.PluginManager.ConfirmDeleteOptions(dialog, deleting);

        File.Exists(editor.AnimationFilePath(button)).ShouldBeFalse();
    }
}
