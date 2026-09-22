using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Services.Dialogs;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The tab around the editing: the View menu entry that shows and hides it, what it shows with
/// nothing (or a behavior, or an instance) selected, and screens as the other kind of element.
/// </summary>
public class TabLifecycleTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void TheViewMenuEntry_HidesAndShowsTheTab_AndItsHeaderFollows()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        editor.Tab.IsVisible.ShouldBeTrue();
        editor.ViewMenuItem.Header.ShouldBe("Hide Animations");

        editor.ViewMenuItem.Invoke();
        editor.Layout();

        editor.Tab.IsVisible.ShouldBeFalse();
        editor.ViewMenuItem.Header.ShouldBe("View Animations");

        editor.ViewMenuItem.Invoke();
        editor.Layout();

        editor.Tab.IsVisible.ShouldBeTrue();
        editor.Tab.IsSelected.ShouldBeTrue();
        editor.ViewMenuItem.Header.ShouldBe("Hide Animations");

        // The tab's own close button hides it the same way.
        editor.Tab.CanClose.ShouldBeTrue();
        editor.Tab.Hide();
        editor.ViewMenuItem.Header.ShouldBe("View Animations");
    }

    [AvaloniaFact]
    public void WithNothingSelected_TheTabSaysSo_AndAddingAnAnimationExplains()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(button);
        editor.AddAnimation("Walk");

        editor.SelectedState.SelectedElement = null;
        editor.Layout();

        editor.ViewModel.AnimationColumnTitle.ShouldBe("No Element Selected");
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.PlayButton.IsEffectivelyVisible.ShouldBeFalse();

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Click(editor.AddAnimationButton);

        editor.Dialogs.Messages.ShouldBe(new[] { "You must first select a Screen or Component" });
        editor.ViewModel.Animations.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void SelectingABehavior_ClearsTheTab_AndSelectingTheElementAgainRestoresIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        editor.Project.Behaviors.Add(behavior);
        editor.Select(button);
        editor.AddAnimation("Walk");

        editor.SelectedState.SelectedBehavior = behavior;
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ViewModel.AnimationColumnTitle.ShouldBe("No Element Selected");
        editor.ViewModel.Animations.ShouldBeEmpty();

        editor.Select(button);

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });
    }

    [AvaloniaFact]
    public void SelectingAnInstance_KeepsTheElementsAnimationsInTheTab()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave icon = editor.AddComponent("Icon", Category, "Dim");
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        InstanceSave instance = editor.AddInstance(button, "IconInstance", icon);
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        ElementAnimationsViewModel tab = editor.ViewModel;

        editor.SelectedState.SelectedInstance = instance;
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ViewModel.ShouldBeSameAs(tab);
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
        editor.ViewModel.AnimationColumnTitle.ShouldBe("Button Animations");
    }

    [AvaloniaFact]
    public void AScreensAnimations_SaveBesideTheScreen_AndReload()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ScreenSave menu = editor.AddScreen("MainMenu", Category, "Shown", "Hidden");
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        editor.Select(menu);

        editor.AddAnimation("FadeIn");
        editor.AddStateKeyframe($"{Category}/Hidden");
        editor.AddStateKeyframe($"{Category}/Shown");

        string path = editor.AnimationFilePath(menu);
        path.ShouldEndWith(Path.Combine("Screens", "MainMenuAnimations.ganx"));
        File.Exists(path).ShouldBeTrue();
        editor.Select(button);
        editor.Select(menu);
        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Hidden", $"{Category}/Shown" });
    }
}
