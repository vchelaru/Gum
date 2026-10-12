using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;

namespace ImportFromGumxPlugin;

/// <summary>
/// Builds the view models behind the Import from .gumx dialog.
/// </summary>
public interface IImportFromGumxLogic
{
    /// <summary>
    /// Whether an import dialog can be shown right now (the project has no unsaved changes).
    /// </summary>
    bool CanImport { get; }

    /// <summary>
    /// Builds a fully-wired view model for the dialog. Call only after checking <see cref="CanImport"/>.
    /// </summary>
    ImportFromGumxViewModel CreateImportViewModel();
}

/// <summary>
/// Business logic behind the "Import from .gumx" menu item, relocated out of the WPF-hosted
/// <c>MainImportFromGumxPlugin</c> (ADR-0005 Phase 3) so it can be unit tested headlessly.
/// </summary>
public class ImportFromGumxLogic : IImportFromGumxLogic
{
    private readonly IProjectState _projectState;
    private readonly IImportLogic _importLogic;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;
    private readonly IDispatcher _dispatcher;
    private readonly IFileWatchIgnoreList _fileWatch;

    public ImportFromGumxLogic(
        IProjectState projectState,
        IImportLogic importLogic,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IDispatcher dispatcher,
        IFileWatchIgnoreList fileWatch)
    {
        _fileWatch = fileWatch;
        _projectState = projectState;
        _importLogic = importLogic;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public bool CanImport => !_projectState.NeedsToSaveProject;

    /// <inheritdoc/>
    public ImportFromGumxViewModel CreateImportViewModel()
    {
        GumxSourceService sourceService = new();
        GumxDependencyResolver dependencyResolver = new();
        ImportVersionGate versionGate = new();
        GumxImportService importService = new(_importLogic, _projectState, _fileCommands, sourceService, versionGate, _fileWatch);

        return new ImportFromGumxViewModel(
            sourceService,
            dependencyResolver,
            importService,
            _projectState,
            _dialogService,
            _dispatcher,
            versionGate);
    }
}
