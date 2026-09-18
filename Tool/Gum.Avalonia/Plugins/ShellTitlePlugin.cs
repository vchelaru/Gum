using System;
using System.ComponentModel.Composition;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;

namespace Gum.Avalonia.Plugins;

/// <summary>Keeps the shell on the loaded project's file, which titles the window. The Avalonia twin of <c>MainWindowPlugin</c>.</summary>
[Export(typeof(PluginBase))]
public class ShellTitlePlugin : PluginBase, IPriorityPlugin
{
    private readonly ShellViewModel _shell;

    /// <summary>Creates the plugin over the shell view model the host exports.</summary>
    [ImportingConstructor]
    public ShellTitlePlugin(ShellViewModel shell)
    {
        _shell = shell;
    }

    /// <inheritdoc/>
    public override string FriendlyName => "Shell Title";

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override void StartUp()
    {
        ProjectLoad += UpdateTitle;
        AfterProjectSave += UpdateTitle;
    }

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;

    private void UpdateTitle(GumProjectSave? project) =>
        _shell.ProjectFilePath = string.IsNullOrEmpty(project?.FullFileName) ? null : project.FullFileName;
}
