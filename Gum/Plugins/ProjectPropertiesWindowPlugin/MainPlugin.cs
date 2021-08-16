using Gum.Commands;
using Gum.Gui.Controls;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Behaviors;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using WpfDataUi.EventArguments;

namespace Gum.Plugins.PropertiesWindowPlugin
{
    /// <summary>
    /// Plugin for displaying project properties
    /// </summary>
    [Export(typeof(PluginBase))]
    class MainPlugin : InternalPlugin
    {
        ProjectPropertiesControl control;

        ProjectPropertiesViewModel viewModel;

        public override void StartUp()
        {
            this.AddMenuItem(new List<string> { "Edit", "Properties" }).Click += HandlePropertiesClicked;

            viewModel = new PropertiesWindowPlugin.ProjectPropertiesViewModel();
            viewModel.PropertyChanged += HandlePropertyChanged;

            // todo - handle loading new Gum project when this window is shown - re-call BindTo
        }

        private void HandlePropertiesClicked(object sender, EventArgs e)
        {
            try
            {
                if(control == null)
                {
                    control = new ProjectPropertiesControl();

                    control.CloseClicked += HandleCloseClicked;
                }
                viewModel.BindTo(ProjectManager.Self.GeneralSettingsFile, ProjectState.Self.GumProjectSave);

                GumCommands.Self.GuiCommands.AddControl(control, "Project Properties");
                GumCommands.Self.GuiCommands.ShowControl(control);
                control.ViewModel = viewModel;
            }
            catch(Exception ex)
            {
                GumCommands.Self.GuiCommands.PrintOutput($"Error showing project properties:\n{ex.ToString()}");
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(!viewModel.IsUpdatingFromModel)
            {
                viewModel.ApplyToModelObjects();

                var shouldSaveAndRefresh = true;

                switch(e.PropertyName)
                {
                    case nameof(viewModel.LocalizationFile):


                        if (!string.IsNullOrEmpty(viewModel.LocalizationFile) && FileManager.IsRelative(viewModel.LocalizationFile) == false)
                        {
                            viewModel.LocalizationFile = FileManager.MakeRelative(viewModel.LocalizationFile, 
                                GumState.Self.ProjectState.ProjectDirectory);
                            shouldSaveAndRefresh = false;
                        }
                        else
                        {
                            GumCommands.Self.FileCommands.LoadLocalizationFile();

                            WireframeObjectManager.Self.RefreshAll(forceLayout: true, forceReloadTextures: false);
                        }
                        break;
                    case nameof(viewModel.LanguageIndex):
                        LocalizationManager.CurrentLanguage = viewModel.LanguageIndex;
                        break;
                }

                if(shouldSaveAndRefresh)
                {
                    GumCommands.Self.WireframeCommands.Refresh();

                    GumCommands.Self.FileCommands.TryAutoSaveProject();
                }
            }
        }

        private void HandleCloseClicked(object sender, EventArgs e)
        {
            GumCommands.Self.GuiCommands.RemoveControl(control);
        }
    }
}
