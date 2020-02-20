using CodeOutputPlugin.Manager;
using Gum;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Code Output Plugin";

        public override Version Version => new Version(1, 0);

        Views.CodeWindow control;
        ViewModels.CodeWindowViewModel viewModel;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();

            var item = this.AddMenuItem("Plugins", "View Code");
            item.Click += HandleViewCodeClicked;


        }

        private void AssignEvents()
        {
            this.InstanceSelected += HandleInstanceSelected;
            this.VariableSet += HandleVariableSet;
        }

        private void HandleInstanceSelected(ElementSave arg1, InstanceSave instance)
        {
            if(instance != null && control != null)
            {
                RefreshCodeDisplay();
            }
        }

        private void HandleVariableSet(ElementSave arg1, InstanceSave instance, string arg3, object arg4)
        {
            if (instance != null && control != null)
            {
                RefreshCodeDisplay();
            }
        }

        private void HandleViewCodeClicked(object sender, EventArgs e)
        {

            if (control == null)
            {
                CreateControl();
            }

            GumCommands.Self.GuiCommands.ShowControl(control);

            RefreshCodeDisplay();
        }

        private void RefreshCodeDisplay()
        {
            var instance = SelectedState.Self.SelectedInstance;

            if (instance == null)
            {
                return; // todo - spit out some output
            }
            string gumCode = CodeGenerator.GetCodeForInstance(instance, VisualApi.Gum);
            string xamarinFormsCode = CodeGenerator.GetCodeForInstance(instance, VisualApi.XamarinForms);
            viewModel.Code = $"//Gum Code:\n{gumCode}\n\n//Xamarin Forms Code:\n{xamarinFormsCode}";
        }

        private void CreateControl()
        {
            control = new Views.CodeWindow();
            viewModel = new ViewModels.CodeWindowViewModel();

            control.DataContext = viewModel;

            GumCommands.Self.GuiCommands.AddControl(control, "Code", TabLocation.Right);

        }


    }
}
