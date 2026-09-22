using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// What the tool's error checker says about an element's animations after edits in the tab, and how
/// the tab flags keyframes whose animation was deleted under them.
/// </summary>
public class AnimationErrorTests
{
    private const string Category = "AnimationCategory";

    private static IErrorChecker ErrorChecker => TestAppBuilder.Services.GetRequiredService<IErrorChecker>();

    [AvaloniaFact]
    public void AKeyframeOnAMissingState_IsReportedByTheErrorChecker_UntilRepaired()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        ErrorChecker.GetErrorsFor(button, editor.Project).ShouldBeEmpty();

        editor.TypeAndEnter(editor.StateComboTextBox, $"{Category}/Gone");

        ErrorViewModel error = ErrorChecker.GetErrorsFor(button, editor.Project).ShouldHaveSingleItem();
        error.ElementName.ShouldBe("Button");
        error.Message.ShouldContain("Walk");
        error.Message.ShouldContain($"{Category}/Gone");

        editor.DetailCombos[0].SelectedItem = $"{Category}/Released";
        editor.Layout();

        ErrorChecker.GetErrorsFor(button, editor.Project).ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void DeletingAnAnimation_OthersReferTo_FlagsTheirKeyframes_AndTheErrorChecker()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        AnimatedKeyframeViewModel reference = walk.SelectedKeyframe.ShouldNotBeNull();
        editor.Click(editor.RowFor(editor.AnimationList, blink));

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);

        editor.ViewModel.Animations.ShouldBe(new[] { walk });
        reference.IsMissingReference.ShouldBeTrue("Blink is gone, so the keyframe that plays it is broken");
        walk.HasBrokenKeyframe.ShouldBeTrue();
        ErrorViewModel error = ErrorChecker.GetErrorsFor(button, editor.Project).ShouldHaveSingleItem();
        error.Message.ShouldContain("Blink");
    }
}
