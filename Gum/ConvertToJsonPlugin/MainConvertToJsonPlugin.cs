using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GumToolUnitTests")]
[assembly: InternalsVisibleTo("Gum.Avalonia.Tests")]

namespace ConvertToJsonPlugin;

/// <summary>
/// Adds the "Convert to JSON" menu item (issue #4175). Converts the whole currently-open project to
/// its JSON representation, leaving the existing XML untouched (ADR: adopting a JSON project format
/// for Native AOT compatibility). All business logic lives in <see cref="ConvertToJsonLogic"/>
/// (headless, unit tested) - this plugin is only menu plumbing.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainConvertToJsonPlugin : PluginBase
{
    public override string FriendlyName => "Convert to JSON Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private readonly ConvertToJsonLogic _convertToJsonLogic;
    private readonly IDialogService _dialogService;

    [ImportingConstructor]
    public MainConvertToJsonPlugin(
        IProjectState projectState,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IFileWatchIgnoreList fileWatchIgnoreList)
    {
        _convertToJsonLogic = new ConvertToJsonLogic(
            projectState, new ConvertProjectToJsonService(fileWatchIgnoreList), fileCommands, dialogService);
        _dialogService = dialogService;
    }

    public override void StartUp()
    {
        AddMenuEntry(async () =>
        {
            try
            {
                await _convertToJsonLogic.ConvertCurrentProjectAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error converting project to JSON:\n{ex.Message}");
            }
        }, "Content", "Convert to JSON…");
    }
}
