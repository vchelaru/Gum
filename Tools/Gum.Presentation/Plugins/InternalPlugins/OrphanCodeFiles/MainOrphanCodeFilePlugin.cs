using CodeOutputPlugin.Manager;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using OrphanCodeFilePlugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.OrphanCodeFiles;

/// <summary>
/// Surfaces code files left behind on disk when an element was deleted, renamed, or moved without
/// the code files being reconciled (issue #4422). Scans on project load and from the
/// <b>Content</b> ▸ <b>Scan for Orphaned Code Files</b> menu item, and reports through the Errors
/// tab with a per-file Delete action. <b>Content</b> ▸ <b>Migrate Code Files</b> moves files left at
/// old paths by a code settings change, and <b>Restore Last Code File Migration</b> undoes it (#5846).
/// All logic lives in <see cref="OrphanCodeFileReporter"/>, <see cref="OrphanCodeFileScanService"/>
/// and <see cref="CodeFileMigrator"/> — this plugin is menu/event plumbing only.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainOrphanCodeFilePlugin : PluginBase
{
    public override string FriendlyName => "Orphan Code File Plugin";

    private readonly OrphanCodeFileReporter _reporter;
    private readonly CodeFileMigrator _migrator;
    private readonly CodeOutputProjectSettingsManager _projectSettingsManager;
    private readonly IProjectState _projectState;
    private readonly IMessenger _messenger;

    [ImportingConstructor]
    public MainOrphanCodeFilePlugin(
        INameVerifier nameVerifier,
        LocalizationService localizationService,
        IProjectState projectState,
        IOutputManager outputManager,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IMessenger messenger,
        IDispatcher dispatcher,
        IGuiCommands guiCommands,
        IRetryService retryService)
    {
        _projectState = projectState;
        _messenger = messenger;

        // The code generation pipeline is built per-plugin rather than resolved from DI, matching
        // how MainCodeOutputPlugin and the CLI assemble it.
        IProjectDirectoryProvider projectDirectoryProvider = new ProjectStateDirectoryProvider(projectState);
        var codeGenerationNameVerifier = new CodeGenerationNameVerifier(nameVerifier);
        var elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        ICodeGenLogger logger = new ToolCodeGenLogger(outputManager);
        var codeGenerator = new CodeGenerator(
            codeGenerationNameVerifier, localizationService, elementSettingsManager, projectDirectoryProvider);
        var fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenerationNameVerifier, projectDirectoryProvider);

        _projectSettingsManager = new CodeOutputProjectSettingsManager(logger, projectDirectoryProvider);

        IOrphanCodeFileScanService scanService = new OrphanCodeFileScanService(
            codeGenerator, fileLocationsService, elementSettingsManager, projectDirectoryProvider);

        var customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenerationNameVerifier);
        var codeGenerationService = new CodeGenerationService(guiCommands, codeGenerator, dialogService,
            customCodeGenerator, codeGenerationNameVerifier, projectDirectoryProvider, retryService);
        // Under the tool's user data folder, outside any repo, and honoring --user-data.
        var backupService = new CodeFileBackupService(
            Path.Combine(FileManager.UserApplicationDataForThisApplication, "CodeFileBackups"), () => DateTime.UtcNow);

        _migrator = new CodeFileMigrator(
            new CodeFileMigrationPlanner(codeGenerator, fileLocationsService, elementSettingsManager, new CustomCodeStubDetector()),
            new CodeFileMigrationPlanFormatter(),
            new CodeFileMigrationApplier(backupService, fileCommands,
                new CustomCodeHeaderRewriter(codeGenerator, customCodeGenerator), elementSettingsManager),
            backupService,
            new ElementCodeRegenerator(codeGenerationService, elementSettingsManager),
            dialogService);

        _reporter = new OrphanCodeFileReporter(scanService, fileCommands, dialogService, dispatcher, outputManager);
        _reporter.OrphansChanged += () =>
            _messenger.Send(new RequestErrorRefreshMessage { RequestingPlugin = this });
    }

    public override void StartUp()
    {
        AddMenuEntry(HandleScanRequested, "Content", "Scan for Orphaned Code Files…");
        AddMenuEntry(HandleMigrateRequested, "Content", "Migrate Code Files…");
        AddMenuEntry(HandleRestoreRequested, "Content", "Restore Last Code File Migration…");

        this.ProjectLoad += HandleProjectLoad;
        this.GetAllErrors += HandleGetAllErrors;
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private IEnumerable<ErrorViewModel> HandleGetAllErrors()
    {
        List<ErrorViewModel> errors = _reporter.CreateErrors().ToList();
        foreach (ErrorViewModel error in errors)
        {
            error.OwnerPlugin = this;
        }
        return errors;
    }

    private void HandleProjectLoad(GumProjectSave project)
    {
        // Not awaited: the scan walks the disk off the UI thread and posts its result back (#5140).
        _ = Refresh(project, onApplied: null);
    }

    private void HandleScanRequested()
    {
        _ = Refresh(_projectState.GumProjectSave, ShowScanSummary);
    }

    // Rescans first so the plan reflects the disk now, and again afterwards so the Errors tab
    // drops the migrated files (#5846).
    private void HandleMigrateRequested()
    {
        GumProjectSave? project = _projectState.GumProjectSave;
        if (project?.FullFileName == null)
        {
            return;
        }

        CodeOutputProjectSettings settings = _projectSettingsManager.CreateOrLoadSettingsForProject();
        _ = _reporter.RefreshAsync(project, settings, result =>
        {
            _migrator.Migrate(project, project.FullFileName, settings, result);
            _ = Refresh(project, onApplied: null);
        });
    }

    private void HandleRestoreRequested()
    {
        GumProjectSave? project = _projectState.GumProjectSave;
        if (project?.FullFileName == null)
        {
            return;
        }

        _migrator.RestoreLast(project.FullFileName);
        _ = Refresh(project, onApplied: null);
    }

    private void ShowScanSummary(OrphanCodeFileScanResult result)
    {
        int count = result.Orphans.Count;
        string message = count == 0
            ? "No orphaned code files were found."
            : $"Found {count} orphaned code file(s). They are listed in the Errors tab, each with a " +
                "Delete File action.\n\nNote that Gum only recognizes files it generated, so extra " +
                "hand-written partial classes are never reported.";
        if (result.IsTruncated)
        {
            message += "\n\n" + OrphanCodeFileScanService.GetTruncatedMessage(result.CodeRoot);
        }
        _dialogService.ShowMessage(message, "Scan for Orphaned Code Files");
    }

    private Task Refresh(GumProjectSave? project, Action<OrphanCodeFileScanResult>? onApplied) =>
        // The reporter raises OrphansChanged, which is what sends the Errors tab refresh message.
        _reporter.RefreshAsync(project, _projectSettingsManager.CreateOrLoadSettingsForProject(), onApplied);
}
