using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for RenameElementDialogViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM whose two
/// injected interfaces (ISetVariableLogic, INameVerifier) are both already headless.
/// </summary>
public class RenameElementDialogViewModelTests : BaseTestClass
{
    private readonly Mock<ISetVariableLogic> _setVariableLogic;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly RenameElementDialogViewModel _viewModel;

    public RenameElementDialogViewModelTests()
    {
        _setVariableLogic = new Mock<ISetVariableLogic>();
        _nameVerifier = new Mock<INameVerifier>();

        _viewModel = new RenameElementDialogViewModel(_setVariableLogic.Object, _nameVerifier.Object);
    }

    [Fact]
    public void OnAffirmative_ShouldNotChangeName_WhenNewNameMatchesOldName()
    {
        _viewModel.ElementSave = new ComponentSave
        {
            Name = "Folder/SameName"
        };
        _viewModel.Value = "SameName";

        _viewModel.OnAffirmative();

        _setVariableLogic.Verify(x => x.PropertyValueChanged(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<InstanceSave>(),
            It.IsAny<StateSave>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Validate_ShouldCheckForDuplicateNames()
    {
        _viewModel.ElementSave = new ComponentSave
        {
            Name = "Folder/OldName"
        };

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(new ComponentSave
        {
            Name = "Folder/OldName"
        });

        string? whyNotValid = "Cannot be duplicate.";

        _nameVerifier
            .Setup(x => x.IsElementNameValid(
                "OldName",
                "Folder/",
                _viewModel.ElementSave,
                out whyNotValid))
            .Returns(false);

        _viewModel.Validate();

        _viewModel.Error.ShouldBe("Cannot be duplicate.");
    }
}
