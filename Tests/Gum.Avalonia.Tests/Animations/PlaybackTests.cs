using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// Playback in the tab: the timer that drives the current time, what it leaves on the canvas,
/// the speed buttons, and that playback stops when the animation or the element goes away.
/// </summary>
public class PlaybackTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void SwitchingElements_WhilePlaying_StopsTheOldTabsPlayback()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(button);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        ElementAnimationsViewModel buttonTab = editor.ViewModel;
        editor.Click(editor.PlayButton);
        buttonTab.IsPlaying.ShouldBeTrue();

        editor.Select(other);

        buttonTab.IsPlaying.ShouldBeFalse("the replaced tab must not keep a timer running");
        double timeWhenReplaced = buttonTab.DisplayedAnimationTime;
        editor.Wait(TimeSpan.FromMilliseconds(300));
        buttonTab.DisplayedAnimationTime.ShouldBe(timeWhenReplaced);
        editor.ViewModel.IsPlaying.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DeletingTheAnimation_WhilePlaying_StopsPlayback()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.PlayButton);
        editor.ViewModel.IsPlaying.ShouldBeTrue();
        editor.Click(editor.RowFor(editor.AnimationList, walk));

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);

        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.ViewModel.IsPlaying.ShouldBeFalse("nothing is playing once the animation is gone, and the Play button is hidden so the user could not stop it");
        double timeWhenDeleted = editor.ViewModel.DisplayedAnimationTime;
        editor.Wait(TimeSpan.FromMilliseconds(300));
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(timeWhenDeleted);
    }

    [AvaloniaFact]
    public void PlayingToTheEnd_LeavesTheLastKeyframesState_OnTheCanvas()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.TypeAndEnter(editor.DetailTimeBox, "0.2");
        released.Time.ShouldBe(0.2f);

        editor.Click(editor.PlayButton);
        editor.Wait(TimeSpan.FromMilliseconds(600));

        editor.ViewModel.IsPlaying.ShouldBeFalse();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.2, tolerance: 0.001);
        StateSave shown = editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
        ((float)shown.GetValue("X")).ShouldBe(100f, tolerance: 0.01f);
    }

    [AvaloniaFact]
    public void AtFourTimesSpeed_AOneSecondAnimationFinishesInUnderHalfASecond()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        Button faster = editor.Window.GetVisualDescendants().OfType<Button>().Single(button => button.Content is "+" && ToolTip.GetTip(button) is string tip && tip.StartsWith("Speed Up"));
        editor.Click(faster);
        editor.Click(faster);
        editor.ViewModel.CurrentGameSpeed.ShouldBe("500%");

        editor.Click(editor.PlayButton);
        editor.Wait(TimeSpan.FromMilliseconds(450));

        editor.ViewModel.IsPlaying.ShouldBeFalse();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(1, tolerance: 0.001);
    }

    [AvaloniaFact]
    public void PressingPlayAgain_RestartsFromZero()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.PlayButton);
        editor.Wait(TimeSpan.FromMilliseconds(250));
        editor.Click(editor.PlayButton);
        editor.ViewModel.IsPlaying.ShouldBeFalse();
        editor.ViewModel.DisplayedAnimationTime.ShouldBeGreaterThan(0);

        editor.Click(editor.PlayButton);

        editor.ViewModel.IsPlaying.ShouldBeTrue();
        // At most the first timer tick has run.
        editor.ViewModel.DisplayedAnimationTime.ShouldBeLessThan(0.05);
        editor.Click(editor.PlayButton);
    }
}
