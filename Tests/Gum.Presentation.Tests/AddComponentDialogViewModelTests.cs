using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolStates;
using Moq;

namespace Gum.Presentation.Tests;

public class AddComponentDialogViewModelTests : BaseTestClass
{
    private readonly AddComponentDialogViewModel _sut;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IProjectState> _projectState;

    public AddComponentDialogViewModelTests()
    {
        _nameVerifier = new Mock<INameVerifier>();
        _selectedState = new Mock<ISelectedState>();
        _projectState = new Mock<IProjectState>();

        // Make the name validation pass so OnAffirmative is not short-circuited by an Error.
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { FullFileName = "C:/project/Test.gumx" };
        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsElementNameValid(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ElementSave?>(), out whyNotValid))
            .Returns(true);

        // With no component-folder tree node selected, OnAffirmative falls back to
        // IProjectState.ComponentFilePath; the mock returns a null FilePath, so the method
        // short-circuits before touching ICopyPasteProjectCommands/IFileLocations. Those
        // collaborators are therefore never invoked on this path - both are supplied as null
        // deliberately.
        _sut = new AddComponentDialogViewModel(
            _nameVerifier.Object,
            _selectedState.Object,
            projectCommands: null!,
            fileLocations: null!,
            _projectState.Object);
    }

    [Fact]
    public void OnAffirmative_WhenSelectedNodeIsNotInComponentsFolder_ReadsComponentFilePathFromProjectState()
    {
        _sut.Value = "NewComponent";

        _sut.OnAffirmative();

        _projectState.Verify(x => x.ComponentFilePath, Times.Once);
    }
}
