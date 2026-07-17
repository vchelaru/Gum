using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterizes how the animation view models detect missing-state errors (<c>RefreshErrors</c> →
/// <c>GetErrors</c>) and how a state rename propagates to animation keyframes. Companion to
/// <see cref="RenameManagerTests"/>: those pin the keyframe-mutating overloads in isolation, these
/// pin the resulting error state — the same error the tree "!" indicator and the Errors tab surface
/// (issue #3293), and that a rename must rewrite the keyframe before recomputing so it leaves no
/// stale error (issue #3383).
///
/// These were the behaviors repeatedly mis-read while reasoning about #3293/#3383; they are pinned
/// here so future changes start from executable ground truth rather than speculation.
/// </summary>
public class AnimationStateRenameErrorTests : BaseTestClass
{
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void HandleRename_OfCategorizedState_RewritesKeyframeAndClearsErrorOnRefresh()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        StateSave idle = element.Categories[0].States[0];
        ElementAnimationsViewModel viewModel = CreateViewModelWith(
            CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

        // Mirror RenameLogic.RenameState: the model is renamed first, then plugins are notified.
        idle.Name = "Walk";
        RenameManagerFor(element).HandleRename(idle, "Idle", viewModel);

        viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("Cat/Walk");

        viewModel.RefreshErrors(element);
        viewModel.GetErrors().ShouldBeEmpty();
    }

    [Fact]
    public void GetAvailableStates_PreservesKeyframeStateName_WhenItsCategoryWasDeleted()
    {
        // Deleting a category recomputes the available-state list bound to the editable state
        // ComboBox. If the still-referenced "Cat/Idle" were dropped from that list, the ComboBox
        // would coerce its Text (bound to StateName) to empty, collapsing the broken state keyframe
        // into an event (flag icon) instead of showing the red broken-reference icon (issue #3392).
        // GetAvailableStates must keep the referenced-but-missing state so the ComboBox holds its
        // selection; brokenness is still reported separately via RefreshErrors/HasValidState.
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        ElementAnimationsViewModel viewModel = CreateViewModelWith(
            CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

        element.Categories.Clear();  // delete the whole category, as DeleteLogic does

        List<string> availableStates = MainStateAnimationPlugin.GetAvailableStates(element, viewModel);

        availableStates.ShouldContain("Cat/Idle");
    }

    [Fact]
    public void RefreshErrors_ReportsKeyframeReferencingMissingCategorizedState()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        ElementAnimationsViewModel viewModel = CreateViewModelWith(
            CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Missing")));

        viewModel.RefreshErrors(element);

        viewModel.GetErrors().Count().ShouldBe(1);
        viewModel.GetErrors().Single().Message.ShouldContain("Cat/Missing");
    }

    [Fact]
    public void RefreshErrors_ReportsNoError_ForKeyframeReferencingExistingCategorizedState()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        ElementAnimationsViewModel viewModel = CreateViewModelWith(
            CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

        viewModel.RefreshErrors(element);

        viewModel.GetErrors().ShouldBeEmpty();
    }

    [Fact]
    public void RefreshAfterStateRename_RewritesKeyframeThenRecomputes_LeavingNoStaleError()
    {
        // Models MainStateAnimationPlugin.HandleStateRename. The keyframe reference must be rewritten
        // BEFORE errors are recomputed. The old order (recompute, THEN rewrite, never recompute again)
        // left a stale missing-state error: the keyframe showed the new name but kept the broken-
        // reference icon (#3383, made visible by the #3386 icon). RefreshAfterStateRename owns the
        // correct order; if its two steps are swapped this assertion fails.
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        StateSave idle = element.Categories[0].States[0];
        ElementAnimationsViewModel viewModel = CreateViewModelWith(
            CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

        idle.Name = "Walk";  // state already renamed when the plugin event fires
        MainStateAnimationPlugin.RefreshAfterStateRename(
            RenameManagerFor(element), viewModel, element, idle, "Idle");

        viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("Cat/Walk");
        viewModel.GetErrors().ShouldBeEmpty();
    }

    private static ComponentSave ElementWithCategorizedState(string categoryName, string stateName)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        category.States.Add(new StateSave { Name = stateName });
        ComponentSave element = new ComponentSave { Name = "Foo" };
        element.Categories.Add(category);
        return element;
    }

    private RenameManager RenameManagerFor(ElementSave selectedElement)
    {
        return new RenameManager(
            Mock.Of<ISelectedState>(s => s.SelectedElement == selectedElement),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IProjectManager>());
    }

    private AnimatedKeyframeViewModel Keyframe(string stateName)
    {
        return new AnimatedKeyframeViewModel() { StateName = stateName };
    }

    private AnimationViewModel CreateAnimationWithKeyframes(params AnimatedKeyframeViewModel[] keyframes)
    {
        AnimationViewModel animation = new AnimationViewModel(_selectedState, _wireframeObjectManager) { Name = "Anim" };
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            animation.Keyframes.Add(keyframe);
        }
        return animation;
    }

    private ElementAnimationsViewModel CreateViewModelWith(params AnimationViewModel[] animations)
    {
        ElementAnimationsViewModel viewModel = new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _selectedState,
            _wireframeObjectManager,
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IUiTimer>());
        foreach (AnimationViewModel animation in animations)
        {
            viewModel.Animations.Add(animation);
        }
        return viewModel;
    }
}
