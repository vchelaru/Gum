using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The sidecar and the keyframes following the element around: element rename and duplicate,
/// category rename and delete, and a new state showing up in the keyframe's state box.
/// </summary>
public class ElementLifecycleTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void RenamingTheSelectedElement_MovesItsSidecar_AndTheTabKeepsShowingIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        string oldPath = editor.AnimationFilePath(button);
        File.Exists(oldPath).ShouldBeTrue();

        button.Name = "PushButton";
        editor.PluginManager.ElementRename(button, "Button");
        editor.Layout();

        File.Exists(oldPath).ShouldBeFalse("the old sidecar must be gone");
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Walk");
        editor.ViewModel.Animations.Single().Name.ShouldBe("Walk");
        editor.ViewModel.AnimationColumnTitle.ShouldBe("PushButton Animations");
    }

    [AvaloniaFact]
    public void RenamingAnUnselectedElement_MovesItsSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave icon = editor.AddComponent("Icon", Category, "Dim", "Bright");
        editor.Select(icon);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Dim");
        string oldPath = editor.AnimationFilePath(icon);
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(button);

        icon.Name = "Glyph";
        editor.PluginManager.ElementRename(icon, "Icon");
        editor.Layout();

        File.Exists(oldPath).ShouldBeFalse();
        editor.ReadSavedAnimations(icon).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Blink");
    }

    [AvaloniaFact]
    public void DuplicatingAnElement_CopiesItsSidecar_SoTheCopyOpensWithTheSameAnimations()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        ComponentSave copy = editor.AddComponent("ButtonCopy", Category, "Pressed", "Released");

        editor.PluginManager.ElementDuplicate(button, copy);
        editor.Select(copy);

        editor.ViewModel.Animations.Select(animation => (animation.Name, animation.Loops)).ShouldBe(new[] { ("Walk", true) });
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(2);
        editor.ReadSavedAnimations(copy).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(2);
        // The original is untouched.
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void RenamingACategory_FollowsIntoTheKeyframes_TheTimelineRow_AndTheSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");
        StateSaveCategory category = button.Categories[0];

        category.Name = "Looks";
        editor.PluginManager.CategoryRename(category, Category);
        editor.Layout();

        keyframe.StateName.ShouldBe("Looks/Pressed");
        keyframe.IsMissingReference.ShouldBeFalse();
        walk.HasBrokenKeyframe.ShouldBeFalse();
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { "Looks" });
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Single().StateName.ShouldBe("Looks/Pressed");
    }

    [AvaloniaFact]
    public void DeletingACategory_MarksItsKeyframesBroken_KeepsTheirNames_AndUndoRepairsThem()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");
        StateSaveCategory category = button.Categories[0];

        using (editor.UndoManager.RequestLock())
        {
            TestAppBuilder.Services.GetRequiredService<IDeleteLogic>().RemoveStateCategory(category, button);
        }
        editor.Layout();

        button.Categories.ShouldBeEmpty();
        keyframe.StateName.ShouldBe($"{Category}/Pressed", "a broken keyframe keeps its name so the user can fix the state (issue #3392)");
        keyframe.IsMissingReference.ShouldBeTrue();
        walk.HasBrokenKeyframe.ShouldBeTrue();
        keyframe.AvailableStates.ShouldContain($"{Category}/Pressed");

        editor.UndoManager.PerformUndo();
        editor.Layout();

        button.Categories.Select(item => item.Name).ShouldBe(new[] { Category });
        editor.ViewModel.Animations.Single().Keyframes.Single().IsMissingReference.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void AddingAState_OffersItInTheKeyframesStateBox()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");
        keyframe.AvailableStates.ShouldNotContain($"{Category}/Hover");

        StateSave hover = new StateSave { Name = "Hover", ParentContainer = button };
        button.Categories[0].States.Add(hover);
        editor.PluginManager.StateAdd(hover);
        editor.Layout();

        keyframe.AvailableStates.ShouldContain($"{Category}/Hover");
        editor.DetailCombos[0].Items.Cast<string>().ShouldContain($"{Category}/Hover");
    }
}
