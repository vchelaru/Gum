using Gum.DataTypes;
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

namespace Gum.Presentation.Tests;

public class AnimationRenameManagerTests
{
    [Fact]
    public void HandleRename_ElementNotShownInTheAnimationsTab_InAnUnsavedProject_DoesNotThrow()
    {
        // A project that was never saved has no folder, so there is no animation file to move.
        Mock<IProjectManager> projectManager = new();
        projectManager.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave());
        RenameManager sut = new(
            Mock.Of<ISelectedState>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            projectManager.Object);
        ElementAnimationsViewModel viewModel = new(
            Mock.Of<INameVerifier>(), Mock.Of<IDialogService>(), Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(), Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IOutputManager>(), Mock.Of<IAnimationFilePathService>(), Mock.Of<IUiTimer>());
        ComponentSave renamed = new() { Name = "NewName" };

        Should.NotThrow(() => sut.HandleRename(renamed, "OldName", viewModel));
    }
}
