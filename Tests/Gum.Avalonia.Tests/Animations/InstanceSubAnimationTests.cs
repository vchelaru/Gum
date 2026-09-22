using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// Sub-animations that play an instance's own animation ("Instance.Animation"): picking one in the
/// dialog, how it is saved and reloaded, and what happens when the instance is renamed or its
/// animation goes away.
/// </summary>
public class InstanceSubAnimationTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void PickingAnInstancesAnimation_AddsAKeyframeNamedInstanceDotAnimation_WithTheInstanceAnimationsLength()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);

        AnimatedKeyframeViewModel sub = AddIconBlink(editor, icon);

        sub.AnimationName.ShouldBe("IconInstance.Blink");
        sub.Length.ShouldBe(1f);
        sub.SubAnimationViewModel.ShouldNotBeNull().Keyframes.Count.ShouldBe(2);
        sub.IsMissingReference.ShouldBeFalse();
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { Category, "IconInstance.Blink" });
        AnimationReferenceSave saved = editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Animations.Single();
        saved.Name.ShouldBe("IconInstance.Blink");
        saved.SourceObject.ShouldBe("IconInstance");
        saved.RootName.ShouldBe("Blink");
        editor.SaveFrame("instance-sub-animation");
    }

    [AvaloniaFact]
    public void ReselectingTheElement_ResolvesTheInstancesAnimation_FromTheInstancesOwnSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);
        AddIconBlink(editor, icon);
        ComponentSave other = editor.AddComponent("Other", Category, "A");

        editor.Select(other);
        editor.Select(button);

        AnimatedKeyframeViewModel sub = editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => keyframe.AnimationName == "IconInstance.Blink");
        sub.SubAnimationViewModel.ShouldNotBeNull().Keyframes.Count.ShouldBe(2);
        sub.Length.ShouldBe(1f);
        sub.IsMissingReference.ShouldBeFalse();
        editor.ViewModel.Animations.Single().Length.ShouldBe(2f);
    }

    [AvaloniaFact]
    public void RenamingTheInstance_FollowsIntoTheSubAnimationKeyframe_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);
        AnimatedKeyframeViewModel sub = AddIconBlink(editor, icon);

        icon.Name = "Glyph";
        editor.PluginManager.InstanceRename(button, icon, "IconInstance");
        editor.Layout();

        sub.AnimationName.ShouldBe("Glyph.Blink");
        editor.Timeline.Rows.Select(row => row.Name).ShouldContain("Glyph.Blink");
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Animations.Single().Name.ShouldBe("Glyph.Blink");
    }

    [AvaloniaFact]
    public void WhenTheInstancesAnimationIsGone_TheKeyframeIsFlagged_OnReload()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);
        AddIconBlink(editor, icon);
        ComponentSave iconType = editor.Project.Components.Single(component => component.Name == "Icon");
        string iconAnimations = editor.AnimationFilePath(iconType);
        ElementAnimationsSave onDisk = ElementAnimationsSave.Load(iconAnimations);
        onDisk.Animations.Clear();
        onDisk.Save(iconAnimations);
        ComponentSave other = editor.AddComponent("Other", Category, "A");

        editor.Select(other);
        editor.Select(button);

        AnimationViewModel walk = editor.ViewModel.Animations.Single();
        AnimatedKeyframeViewModel sub = walk.Keyframes.Single(keyframe => keyframe.AnimationName == "IconInstance.Blink");
        sub.IsMissingReference.ShouldBeTrue();
        walk.HasBrokenKeyframe.ShouldBeTrue();
        editor.Click(editor.RowFor(editor.AnimationList, walk));
        editor.RowFor(editor.KeyframeList, sub).GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "⚠");
    }

    [AvaloniaFact]
    public void ScrubbingIntoTheSubAnimation_AppliesTheInstancesState()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);
        AddIconBlink(editor, icon);

        editor.TypeAndEnter(editor.TimelineTimeBox, "1.5");

        editor.ViewModel.DisplayedAnimationTime.ShouldBe(1.5);
        editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
    }

    [AvaloniaFact]
    public void AScreen_CanPlayAnInstancesAnimation_AndSavesItBesideTheScreen()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave iconType = editor.AddComponent("Icon", Category, "Dim", "Bright");
        editor.Select(iconType);
        editor.AddAnimation("Blink", loops: true);
        editor.AddStateKeyframe($"{Category}/Dim");
        editor.AddStateKeyframe($"{Category}/Bright");
        ScreenSave menu = editor.AddScreen("MainMenu", Category, "Shown", "Hidden");
        InstanceSave icon = editor.AddInstance(menu, "IconInstance", iconType);
        editor.Select(menu);
        AnimationViewModel intro = editor.AddAnimation("Intro");
        editor.AddStateKeyframe($"{Category}/Shown");

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.AnimationContainers!.Select(container => container.Name).ShouldBe(new[] { "MainMenu (container)", "IconInstance (Icon)" });
            dialog.SelectedContainer = dialog.AnimationContainers.Single(container => container.InstanceSave == icon);
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");

        AnimatedKeyframeViewModel sub = intro.SelectedKeyframe.ShouldNotBeNull();
        sub.AnimationName.ShouldBe("IconInstance.Blink");
        sub.Length.ShouldBe(1f);
        intro.Length.ShouldBe(2f);
        AnimationReferenceSave saved = editor.ReadSavedAnimations(menu).ShouldNotBeNull().Animations.Single().Animations.Single();
        saved.SourceObject.ShouldBe("IconInstance");
        saved.RootName.ShouldBe("Blink");
        editor.Select(iconType);
        editor.Select(menu);
        editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => keyframe.AnimationName == "IconInstance.Blink").IsMissingReference.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void RenamingTheInstancesAnimation_InItsOwnElement_FollowsIntoTheReferencingElement()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        (ComponentSave button, InstanceSave icon) = ButtonWithAnimatedIcon(editor);
        AddIconBlink(editor, icon);
        ComponentSave iconType = editor.Project.Components.Single(component => component.Name == "Icon");
        editor.Select(iconType);
        AnimationViewModel blink = editor.ViewModel.Animations.Single();
        editor.Click(editor.RowFor(editor.AnimationList, blink));

        editor.Dialogs.AnswerNextUserString("Wink");
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Rename Animation").Action!();
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Animations.Single().Name.ShouldBe("IconInstance.Wink");
        editor.Select(button);
        AnimatedKeyframeViewModel sub = editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => !string.IsNullOrEmpty(keyframe.AnimationName));
        sub.AnimationName.ShouldBe("IconInstance.Wink");
        sub.IsMissingReference.ShouldBeFalse();
    }

    /// <summary>
    /// An Icon component whose Blink animation runs one second, and a Button holding an IconInstance
    /// of it; leaves Button selected with an empty Walk animation plus a Pressed keyframe at 0.
    /// </summary>
    private static (ComponentSave Button, InstanceSave Icon) ButtonWithAnimatedIcon(AnimationEditorHarness editor)
    {
        ComponentSave iconType = editor.AddComponent("Icon", Category, "Dim", "Bright");
        editor.Select(iconType);
        editor.AddAnimation("Blink", loops: true);
        editor.AddStateKeyframe($"{Category}/Dim");
        editor.AddStateKeyframe($"{Category}/Bright");

        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        InstanceSave icon = editor.AddInstance(button, "IconInstance", iconType);
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        return (button, icon);
    }

    private static AnimatedKeyframeViewModel AddIconBlink(AnimationEditorHarness editor, InstanceSave icon)
    {
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.AnimationContainers!.Select(container => container.Name).ShouldBe(new[] { "Button (container)", "IconInstance (Icon)" });
            dialog.SelectedContainer = dialog.AnimationContainers.Single(container => container.InstanceSave == icon);
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        return editor.ViewModel.SelectedAnimation!.SelectedKeyframe.ShouldNotBeNull();
    }
}
