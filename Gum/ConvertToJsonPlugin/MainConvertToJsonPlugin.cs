using Gum.Commands;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GumToolUnitTests")]

namespace ConvertToJsonPlugin;

/// <summary>
/// Adds the "Convert to JSON" menu item (issue #4175). Converts the whole currently-open project to
/// its JSON representation, leaving the existing XML untouched (ADR: adopting a JSON project format
/// for Native AOT compatibility). All business logic lives in <see cref="ConvertToJsonLogic"/>
/// (headless, unit tested) - this plugin is only WPF menu-item plumbing.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainConvertToJsonPlugin : WpfPluginBase
{
    public override string FriendlyName => "Convert to JSON Plugin";
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private readonly ConvertToJsonLogic _convertToJsonLogic;

    [ImportingConstructor]
    public MainConvertToJsonPlugin(
        IProjectState projectState,
        IFileCommands fileCommands,
        IDialogService dialogService)
    {
        _convertToJsonLogic = new ConvertToJsonLogic(
            projectState, new ConvertProjectToJsonService(), fileCommands, dialogService);
    }

    public override void StartUp()
    {
        var menuItem = this.AddMenuItem(new[] { "Content", "Convert to JSON…" });
        menuItem.Click += (_, _) => _convertToJsonLogic.ConvertCurrentProject();
    }
}
