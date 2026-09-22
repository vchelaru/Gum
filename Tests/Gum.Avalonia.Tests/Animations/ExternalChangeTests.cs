using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The tab under changes it did not make: sidecars edited or deleted on disk, undo after moving
/// between elements, and fast switching between animated elements.
/// </summary>
public class ExternalChangeTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void DeletingTheSidecarOnDisk_EmptiesTheTab_WhichKeepsWorking()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        string path = editor.AnimationFilePath(button);
        File.Delete(path);

        editor.Plugin.CallReactToFileChanged(new FilePath(path));
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.ViewModel.AnimationColumnTitle.ShouldBe("Button Animations");

        editor.AddAnimation("Run");

        File.Exists(path).ShouldBeTrue();
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Run");
    }

    [AvaloniaFact]
    public void AnEditToAnotherElementsSidecar_LeavesTheTabAlone()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave icon = editor.AddComponent("Icon", Category, "Dim");
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(icon);
        editor.AddAnimation("Blink");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        ElementAnimationsViewModel tab = editor.ViewModel;
        string iconPath = editor.AnimationFilePath(icon);
        ElementAnimationsSave iconAnimations = ElementAnimationsSave.Load(iconPath);
        iconAnimations.Animations[0].Name = "Wink";
        iconAnimations.Save(iconPath);

        editor.Plugin.CallReactToFileChanged(new FilePath(iconPath));
        editor.Layout();

        editor.ViewModel.ShouldBeSameAs(tab);
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
    }

    [AvaloniaFact]
    public void AnExternalEdit_ThatRemovesTheSelectedKeyframe_ReloadsWithoutCrashing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.RowFor(editor.KeyframeList, second));
        string path = editor.AnimationFilePath(button);
        ElementAnimationsSave onDisk = ElementAnimationsSave.Load(path);
        onDisk.Animations[0].States.RemoveAt(1);
        onDisk.Save(path);

        editor.Plugin.CallReactToFileChanged(new FilePath(path));
        editor.Layout();

        editor.ThrowIfPluginFailed();
        AnimationViewModel walk = editor.ViewModel.Animations.Single();
        walk.Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Pressed" });
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
        editor.KeyframeList.Items.Count.ShouldBe(1);
        editor.Timeline.Rows[0].Items.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void ASidecarCorruptedOnDisk_EmptiesTheTab_WhichKeepsWorking_AndWritesAReadableFileAgain()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        string path = editor.AnimationFilePath(button);
        File.WriteAllText(path, "<ElementAnimationsSave><Animations><AnimationSave><Name>Walk");

        editor.Plugin.CallReactToFileChanged(new FilePath(path));
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ViewModel.Element.ShouldBeSameAs(button);
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.ViewModel.AnimationColumnTitle.ShouldBe("Button Animations");

        editor.Select(other);
        editor.Select(button);
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.AddAnimation("Run");
        editor.AddStateKeyframe($"{Category}/Released");

        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Run");
    }

    [AvaloniaFact]
    public void Undo_AfterVisitingAnotherElement_RevertsTheFirstElementsAnimationEdit()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave icon = editor.AddComponent("Icon", Category, "Dim");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.Select(icon);
        editor.Select(button);
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(2);

        editor.UndoManager.PerformUndo();
        editor.Layout();

        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Pressed" });
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void SwitchingBetweenAnimatedElementsRepeatedly_AlwaysShowsTheRightOne()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        ComponentSave icon = editor.AddComponent("Icon", Category, "Dim");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Select(icon);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Dim");

        for (int i = 0; i < 10; i++)
        {
            editor.Select(button);
            editor.ViewModel.Animations.Single().Name.ShouldBe("Walk");
            editor.AnimationList.Items.Count.ShouldBe(1);
            editor.Select(icon);
            AnimationViewModel blink = editor.ViewModel.Animations.Single();
            blink.Name.ShouldBe("Blink");
            // A reloaded element starts with no animation selected, so the timeline is empty.
            editor.Timeline.Rows.ShouldBeEmpty();
            editor.Click(editor.RowFor(editor.AnimationList, blink));
            editor.Timeline.Rows.Single().Items.Single().StateName.ShouldBe($"{Category}/Dim");
        }
    }
}
