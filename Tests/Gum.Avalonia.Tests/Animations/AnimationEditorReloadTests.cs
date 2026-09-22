using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The tab against the sidecar file and the rest of the tool: reloading on reselect and on an
/// external edit, showing itself for an element that has animations, the delete confirmation's
/// file option, states deleted under it, and the column ratio setting.
/// </summary>
public class AnimationEditorReloadTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void ReselectingTheElement_ReloadsEveryKindOfKeyframe_FromTheSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(component);
        editor.AddAnimation("Blink");
        AnimatedKeyframeViewModel bouncy = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.DetailCombos[1].SelectedItem = InterpolationType.Bounce;
        editor.DetailCombos[2].SelectedItem = Easing.InOut;
        editor.Layout();
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Dialogs.AnswerNextUserString("Footstep");
        editor.PickAddKeyframe("Named Event");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        ElementAnimationsViewModel before = editor.ViewModel;

        editor.Select(other);
        editor.ViewModel.ShouldNotBeSameAs(before);
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.Select(component);

        editor.ViewModel.ShouldNotBeSameAs(before);
        editor.ViewModel.Animations.Select(animation => (animation.Name, animation.Loops)).ShouldBe(new[] { ("Blink", false), ("Walk", true) });
        AnimationViewModel blink = editor.ViewModel.Animations[0];
        blink.Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time)).ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 1f) });
        blink.Keyframes[1].InterpolationType.ShouldBe(InterpolationType.Bounce);
        blink.Keyframes[1].Easing.ShouldBe(Easing.InOut);
        AnimationViewModel walk = editor.ViewModel.Animations[1];
        walk.Keyframes.Select(keyframe => keyframe.DisplayString).ShouldBe(new[] { $"{Category}/Pressed (0.00)", "Footstep (1.00)", "Blink (2.00)" });
        walk.Keyframes[2].SubAnimationViewModel.ShouldNotBeNull().Keyframes.Count.ShouldBe(2);
        walk.HasBrokenKeyframe.ShouldBeFalse();
        editor.AnimationList.Items.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void EditingTheSidecarOnDisk_ReloadsTheTab_AndKeepsTheSelection()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.RowFor(editor.KeyframeList, editor.ViewModel.SelectedAnimation!.Keyframes[1]));
        string path = editor.AnimationFilePath(component);
        ElementAnimationsSave onDisk = ElementAnimationsSave.Load(path);
        onDisk.Animations[0].States[1].Time = 3;
        onDisk.Save(path);

        editor.Plugin.CallReactToFileChanged(new FilePath(path));
        editor.Layout();

        AnimationViewModel walk = editor.ViewModel.Animations.Single();
        walk.Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 3f });
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
        walk.SelectedKeyframe.ShouldBeSameAs(walk.Keyframes[1]);
        editor.KeyframeList.SelectedItem.ShouldBeSameAs(walk.Keyframes[1]);
        editor.Scrubber.Maximum.ShouldBe(3);
    }

    [AvaloniaFact]
    public void SelectingAnElementWithASidecar_ShowsTheHiddenTab()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave animated = editor.AddComponent("Animated", Category, "Pressed");
        ComponentSave plain = editor.AddComponent("Plain", Category, "Pressed");
        editor.Select(animated);
        editor.AddAnimation("Walk");
        editor.Tab.Hide();

        editor.Select(plain);
        editor.Tab.IsVisible.ShouldBeFalse();

        editor.Select(animated);
        editor.Tab.IsVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void DeletingTheElement_OffersToDeleteItsSidecar_AndDoesWhenLeftChecked()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(component);
        editor.AddAnimation("Walk");
        File.Exists(editor.AnimationFilePath(component)).ShouldBeTrue();
        DeleteOptionsDialogViewModel dialog = new DeleteOptionsDialogViewModel();
        object[] deleting = { component };

        editor.PluginManager.ShowDeleteOptions(dialog, deleting);

        dialog.CheckBoxes.ShouldContain(option => option.Label == "Delete Animation file (.ganx)" && option.IsChecked);

        editor.PluginManager.ConfirmDeleteOptions(dialog, deleting);

        File.Exists(editor.AnimationFilePath(component)).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DeletingAReferencedState_MarksTheKeyframeBroken_AndUndoRepairsIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Released");
        StateSave released = component.Categories[0].States.Single(state => state.Name == "Released");

        // The tree's Delete, once confirmed: the state is removed under an undo lock.
        using (editor.UndoManager.RequestLock())
        {
            TestAppBuilder.Services.GetRequiredService<IDeleteLogic>().Remove(released);
        }
        editor.Layout();

        keyframe.IsMissingReference.ShouldBeTrue();
        walk.HasBrokenKeyframe.ShouldBeTrue();
        editor.RowFor(editor.KeyframeList, keyframe).GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "⚠");
        editor.RowFor(editor.AnimationList, walk).GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "⚠").IsVisible.ShouldBeTrue();

        editor.UndoManager.PerformUndo();
        editor.Layout();

        component.Categories[0].States.ShouldContain(state => state.Name == "Released");
        editor.ViewModel.Animations.Single().Keyframes.Single().IsMissingReference.ShouldBeFalse();
        editor.ViewModel.Animations.Single().HasBrokenKeyframe.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DraggingTheColumnSplitter_SavesTheRatio_UnderTheUserDataFolder()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(component);
        Point from = editor.CenterOf(editor.ColumnSplitter);

        editor.Drag(from, new Point(from.X + 120, from.Y));

        string settings = Path.Combine(editor.ProjectFolder, "UserData", "AnimationPlugin", "GlobalAnimationSettings.json");
        File.Exists(settings).ShouldBeTrue("the ratio must be saved under the run's user-data folder, not the user's AppData");
        File.ReadAllText(settings).ShouldContain("FirstToSecondColumnRatio");
        File.ReadAllText(settings).ShouldNotContain("\"FirstToSecondColumnRatio\":1.0");
    }

    [AvaloniaFact]
    public void TheColumnRatio_ComesBack_WhenTheTabIsOpenedAgain_WithTheSameUserData()
    {
        string userData = Path.Combine(Path.GetTempPath(), "GumAnimationEditor", "UserData" + Guid.NewGuid().ToString("N"));
        try
        {
            double ratio;
            using (AnimationEditorHarness first = new AnimationEditorHarness(userDataFolder: userData))
            {
                ComponentSave component = first.AddComponent("Button", Category, "Pressed");
                first.Select(component);
                first.AnimationColumnRatio.ShouldBe(1, 0.01);
                Point from = first.CenterOf(first.ColumnSplitter);

                first.Drag(from, new Point(from.X + 120, from.Y));

                ratio = first.AnimationColumnRatio;
                ratio.ShouldBeGreaterThan(1.3);
            }

            using AnimationEditorHarness second = new AnimationEditorHarness(userDataFolder: userData);
            ComponentSave again = second.AddComponent("Button", Category, "Pressed");
            second.Select(again);

            second.AnimationColumnRatio.ShouldBe(ratio, 0.05);
        }
        finally
        {
            if (Directory.Exists(userData))
            {
                Directory.Delete(userData, recursive: true);
            }
        }
    }
}
