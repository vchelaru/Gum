using Gum.Commands;
using Gum.Gui.Controls;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Behaviors;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.PropertiesWindowPlugin
{
    [Export(typeof(PluginBase))]
    class MainPlugin : InternalPlugin
    {
        ProjectPropertiesControl control;

        ProjectPropertiesViewModel viewModel;

        public override void StartUp()
        {
            this.AddMenuItem(new List<string> { "Edit", "Properties" }).Click += HandlePropertiesClicked;

            viewModel = new PropertiesWindowPlugin.ProjectPropertiesViewModel();

            // todo - handle loading new Gum project when this window is shown - re-call BindTo
        }

        private void HandlePropertiesClicked(object sender, EventArgs e)
        {
            if(control == null)
            {
                control = new ProjectPropertiesControl();
                control.PropertyChanged += HandlePropertyChanged;

                control.CloseClicked += HandleCloseClicked;
            }
            viewModel.BindTo(ProjectManager.Self.GeneralSettingsFile, ProjectState.Self.GumProjectSave);

            GumCommands.Self.GuiCommands.AddControl(control, "Project Properties");
            GumCommands.Self.GuiCommands.ShowControl(control);
            control.ViewModel = viewModel;
        }

        private void HandlePropertyChanged(object sender, EventArgs e)
        {
            viewModel.ApplyToBoundObjects();

            GumCommands.Self.WireframeCommands.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveProject();


            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }

        private void HandleCloseClicked(object sender, EventArgs e)
        {
            GumCommands.Self.GuiCommands.RemoveControl(control);
        }
    }
}
