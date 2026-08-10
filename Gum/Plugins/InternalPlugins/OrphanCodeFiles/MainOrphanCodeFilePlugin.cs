using CodeOutputPlugin.Manager;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using OrphanCodeFilePlugin;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.OrphanCodeFiles;

/// <summary>
/// Surfaces code files left behind on disk when an element was deleted, renamed, or moved without
/// the code files being reconciled (issue #4422). Scans on project load and from the
/// <b>Content</b> ▸ <b>Scan for Orphaned Code Files</b> menu item, and reports through the Errors
/// tab with a per-file Delete action. All logic lives in <see cref="OrphanCodeFileReporter"/> and
/// <see cref="OrphanCodeFileScanService"/> — this plugin is WPF menu/event plumbing only.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainOrphanCodeFilePlugin : WpfPluginBase
{
    public override string FriendlyName => "Orphan Code File Plugin";

    private readonly OrphanCodeFileReporter _reporter;
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
        IMessenger messenger)
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

        _reporter = new OrphanCodeFileReporter(scanService, fileCommands, dialogService);
        _reporter.OrphansChanged += () =>
            _messenger.Send(new RequestErrorRefreshMessage { RequestingPlugin = this });
    }

    public override void StartUp()
    {
        var menuItem = this.AddMenuItem(new[] { "Content", "Scan for Orphaned Code Files…" });
        menuItem.Click += (_, _) => HandleScanRequested();

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
        Refresh(project);
    }

    private void HandleScanRequested()
    {
        Refresh(_projectState.GumProjectSave);

        int count = _reporter.Orphans.Count;
        string message = count == 0
            ? "No orphaned code files were found."
            : $"Found {count} orphaned code file(s). They are listed in the Errors tab, each with a " +
                "Delete File action.\n\nNote that Gum only recognizes files it generated, so extra " +
                "hand-written partial classes are never reported.";
        _dialogService.ShowMessage(message, "Scan for Orphaned Code Files");
    }

    private void Refresh(GumProjectSave? project)
    {
        // Refresh raises OrphansChanged, which is what sends the Errors tab refresh message.
        _reporter.Refresh(project, _projectSettingsManager.CreateOrLoadSettingsForProject());
    }
}
