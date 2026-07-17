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
using System.Threading;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning the keyframe-mutating <see cref="RenameManager"/> overloads.
/// These were deferred when ACVMM/RenameManager were drained (#3281) because the animation view
/// models could not be constructed in isolation. They can now: the animation view models take all
/// of their dependencies via the constructor (stubbed here with Moq), and
/// <see cref="ElementAnimationsViewModel"/>'s WPF right-click <c>MenuItem</c> creation is handled
/// by building it on an STA thread.
/// </summary>
public class RenameManagerTests : BaseTestClass
{
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void HandleRename_Animation_RenamesMatchingKeyframeAnimationNames()
    {
        RunOnSta(() =>
        {
            // GetElementsReferencing walks the project; an empty project keeps the test on the
            // pure view-model mutation path (no file IO).
            ObjectFinder.Self.GumProjectSave = new GumProjectSave();

            AnimationViewModel animation = CreateAnimationWithKeyframes(
                Keyframe(animationName: "OldAnim"));
            AnimationViewModel renamed = new AnimationViewModel(_selectedState, _wireframeObjectManager) { Name = "NewAnim" };

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                renamed, "OldAnim", new[] { animation }, new ComponentSave { Name = "Foo" });

            animation.Keyframes[0].AnimationName.ShouldBe("NewAnim");
        });
    }

    [Fact]
    public void HandleRename_Element_NotCurrentlySelected_ReadsProjectManagerGumProjectSave()
    {
        RunOnSta(() =>
        {
            ComponentSave elementSave = new ComponentSave { Name = "RenamedElement" };
            ElementAnimationsViewModel viewModel = CreateViewModelWith();

            GumProjectSave gumProjectSave = new GumProjectSave
            {
                FullFileName = "C:/NonExistentGumRenameManagerTest/Project.gumx"
            };
            Mock<IProjectManager> projectManagerMock = new Mock<IProjectManager>();
            projectManagerMock.Setup(x => x.GumProjectSave).Returns(gumProjectSave);

            RenameManager renameManager = CreateRenameManager(projectManagerMock.Object);

            renameManager.HandleRename(elementSave, "OldElementName", viewModel);

            projectManagerMock.VerifyGet(x => x.GumProjectSave, Times.AtLeastOnce);
        });
    }

    [Fact]
    public void HandleRename_Instance_RenamesQualifiedAnimationNamePrefix()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(animationName: "OldInstance.Walk")));

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new InstanceSave { Name = "NewInstance" }, "OldInstance", viewModel);

            viewModel.Animations[0].Keyframes[0].AnimationName.ShouldBe("NewInstance.Walk");
        });
    }

    [Fact]
    public void HandleRename_State_RenamesMatchingKeyframeStateName()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "OldState")));

            // No selected element -> uncategorized rename (no category prefix).
            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new StateSave { Name = "NewState" }, "OldState", viewModel);

            viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("NewState");
        });
    }

    [Fact]
    public void HandleRename_StateCategory_RenamesCategoryPrefixOnKeyframeStateNames()
    {
        RunOnSta(() =>
        {
            ElementAnimationsViewModel viewModel = CreateViewModelWith(
                CreateAnimationWithKeyframes(Keyframe(stateName: "OldCategory/Idle")));

            RenameManager renameManager = CreateRenameManager();

            renameManager.HandleRename(
                new StateSaveCategory { Name = "NewCategory" }, "OldCategory", viewModel);

            viewModel.Animations[0].Keyframes[0].StateName.ShouldBe("NewCategory/Idle");
        });
    }

    private AnimatedKeyframeViewModel Keyframe(string? stateName = null, string? animationName = null)
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel();
        if (stateName != null)
        {
            keyframe.StateName = stateName;
        }
        if (animationName != null)
        {
            keyframe.AnimationName = animationName;
        }
        return keyframe;
    }

    private AnimationViewModel CreateAnimationWithKeyframes(params AnimatedKeyframeViewModel[] keyframes)
    {
        AnimationViewModel animation = new AnimationViewModel(_selectedState, _wireframeObjectManager);
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
            Mock.Of<IOutputManager>());
        foreach (AnimationViewModel animation in animations)
        {
            viewModel.Animations.Add(animation);
        }
        return viewModel;
    }

    private static RenameManager CreateRenameManager(IProjectManager? projectManager = null)
    {
        return new RenameManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            projectManager ?? Mock.Of<IProjectManager>());
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
