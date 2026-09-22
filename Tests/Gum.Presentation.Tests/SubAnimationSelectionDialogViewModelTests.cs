using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Which of an element's own animations the sub-animation picker offers for the animation being
/// edited: never that animation, and never one that already plays it, since either would loop.
/// </summary>
public class SubAnimationSelectionDialogViewModelTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "GumSubAnimationDialog", Guid.NewGuid().ToString("N"));

    [Fact]
    public void PickingForAnAnimation_LeavesOutTheAnimationsThatAlreadyPlayIt_EvenIndirectly()
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        ElementAnimationsSave all = new ElementAnimationsSave();
        AnimationSave walk = new AnimationSave { Name = "Walk" };
        AnimationSave run = new AnimationSave { Name = "Run" };
        run.Animations.Add(new AnimationReferenceSave { Name = "Walk", Time = 0 });
        AnimationSave sprint = new AnimationSave { Name = "Sprint" };
        sprint.Animations.Add(new AnimationReferenceSave { Name = "Run", Time = 0 });
        AnimationSave blink = new AnimationSave { Name = "Blink" };
        blink.States.Add(new AnimatedStateSave { StateName = "Looks/Dim", Time = 0 });
        all.Animations.Add(walk);
        all.Animations.Add(run);
        all.Animations.Add(sprint);
        all.Animations.Add(blink);
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "ButtonAnimations.ganx");
        all.Save(path);
        IAnimationSaveRepository repository = Mock.Of<IAnimationSaveRepository>(r => r.GetElementAnimationsSave(element) == all);
        IAnimationFilePathService paths = Mock.Of<IAnimationFilePathService>(p => p.GetAbsoluteAnimationFileNameFor(element) == new FilePath(path));
        SubAnimationSelectionDialogViewModel dialog = new SubAnimationSelectionDialogViewModel(
            repository, Mock.Of<IOutputManager>(), Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>(), paths)
        {
            AnimationToExclude = new AnimationViewModel(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" },
            AnimationContainers = new List<AnimationContainerViewModel> { new AnimationContainerViewModel(element, null) },
        };

        dialog.SelectedContainer = dialog.AnimationContainers.Single();

        dialog.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink" });
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
