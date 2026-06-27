using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterizes how the animation view models detect missing-state errors (<c>RefreshErrors</c> →
/// <c>GetErrors</c>) and how a state rename propagates to animation keyframes. Companion to
/// <see cref="RenameManagerTests"/>: those pin the keyframe-mutating overloads in isolation, these
/// pin the resulting error state — the same error the tree "!" indicator and the Errors tab surface
/// (issue #3293).
///
/// <para>
/// <see cref="HandleRename_OfCategorizedState_RewritesKeyframeAndClearsErrorOnRefresh"/> is the
/// #3383 regression guard for the fixed plugin ordering: the keyframe is rewritten <em>before</em>
/// errors are recomputed, so no stale missing-state error survives the rename. (The WPF
/// ComboBox-binding half of #3383 — the selection nulling that dropped the reference — is verified
/// manually, since it needs a WPF host.)
/// </para>
/// </summary>
public class AnimationStateRenameErrorTests : BaseTestClass
{
    private readonly IBitmapLoader _bitmapLoader = Mock.Of<IBitmapLoader>();
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void HandleRename_OfCategorizedState_RewritesKeyframeAndClearsErrorOnRefresh()
    {
        RunOnSta(() =>
        {
            ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
            StateSave idle = element.Categories[0].States[0];
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

            // Mirror RenameLogic.RenameState: the model is renamed first, then plugins are notified.
            idle.Name = "Walk";

            // Fixed plugin ordering (#3383): rewrite the keyframe BEFORE recomputing errors. The
            // pre-fix plugin recomputed errors first (against the still-old keyframe name) and never
            // re-ran them, leaving a stale missing-state error; this is the regression guard.
            RenameManagerFor(element).HandleRename(idle, "Idle", viewModel);
            viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("Cat/Walk");

            viewModel.RefreshErrors(element);
            viewModel.GetErrors().ShouldBeEmpty();
        });
    }

    [Fact]
    public void RefreshErrors_ReportsKeyframeReferencingMissingCategorizedState()
    {
        RunOnSta(() =>
        {
            ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Missing")));

            viewModel.RefreshErrors(element);

            viewModel.GetErrors().Count().ShouldBe(1);
            viewModel.GetErrors().Single().Message.ShouldContain("Cat/Missing");
        });
    }

    [Fact]
    public void RefreshErrors_ReportsNoError_ForKeyframeReferencingExistingCategorizedState()
    {
        RunOnSta(() =>
        {
            ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "Cat/Idle")));

            viewModel.RefreshErrors(element);

            viewModel.GetErrors().ShouldBeEmpty();
        });
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
            Mock.Of<IAnimationCollectionViewModelManager>());
    }

    private AnimatedKeyframeViewModel Keyframe(string stateName)
    {
        return new AnimatedKeyframeViewModel(_bitmapLoader) { StateName = stateName };
    }

    private AnimationViewModel CreateAnimationWithKeyframes(params AnimatedKeyframeViewModel[] keyframes)
    {
        AnimationViewModel animation = new AnimationViewModel(_bitmapLoader, _selectedState, _wireframeObjectManager) { Name = "Anim" };
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
            _bitmapLoader,
            _selectedState,
            _wireframeObjectManager,
            Mock.Of<IOutputManager>());
        foreach (AnimationViewModel animation in animations)
        {
            viewModel.Animations.Add(animation);
        }
        return viewModel;
    }

    /// <summary>
    /// WPF controls (the right-click MenuItems built in ElementAnimationsViewModel's constructor)
    /// require an STA thread; xUnit's default runner is MTA.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        Thread thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught != null)
        {
            throw new Exception("Test body threw on the STA thread.", caught);
        }
    }
}
