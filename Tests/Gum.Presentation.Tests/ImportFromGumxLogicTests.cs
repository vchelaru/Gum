using Gum.Commands;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainImportFromGumxPlugin into the headless Gum.Presentation
/// assembly (ADR-0005 Phase 3) so this business logic is unit testable.
/// </summary>
public class ImportFromGumxLogicTests
{
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IImportLogic> _importLogic = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly ImportFromGumxLogic _logic;

    public ImportFromGumxLogicTests()
    {
        _logic = new ImportFromGumxLogic(
            _projectState.Object, _importLogic.Object, _fileCommands.Object, _dialogService.Object, _dispatcher.Object);
    }

    [Fact]
    public void CanImport_ProjectNeedsToSave_ReturnsFalse()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);

        _logic.CanImport.ShouldBeFalse();
    }

    [Fact]
    public void CanImport_ProjectSaved_ReturnsTrue()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);

        _logic.CanImport.ShouldBeTrue();
    }

    [Fact]
    public void CreateImportViewModel_ReturnsNonNullViewModel()
    {
        var viewModel = _logic.CreateImportViewModel();

        viewModel.ShouldNotBeNull();
    }
}
