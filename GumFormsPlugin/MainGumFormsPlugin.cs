using Gum;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.ToolStates;
using GumFormsPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsPlugin;


[Export(typeof(PluginBase))]
internal class MainGumFormsPlugin : PluginBase
{
    public override string FriendlyName => "Gum Forms Plugin";
    public override Version Version => new Version(1, 0);
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    public override void StartUp()
    {
        this.AddMenuItemTo("Add Forms Components", HandleAddFormsComponents, "Content");
    }

    private void HandleAddFormsComponents(object sender, EventArgs e)
    {
        #region Early Out

        if (GumState.Self.ProjectState.NeedsToSaveProject)
        {
            GumCommands.Self.GuiCommands.ShowMessage("You must first save the project before importing forms");
            return;
        }
        #endregion

        var view = new AddFormsWindow();
        view.ShowDialog();

    }

}


