using CodeOutputPlugin.Manager;
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
        public override string FriendlyName => "Code Output Plugin";

        public override Version Version => new Version(1, 0);

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

        private void HandleViewCodeClicked(object sender, EventArgs e)
        {
            var instance = SelectedState.Self.SelectedInstance;

            if (instance == null)
            {
                return; // todo - spit out some output
            }

            string code = CodeGenerator.GetCodeForInstance(instance);

            var saveLocation = "TempCodeOutput.txt";
            System.IO.File.WriteAllText(saveLocation, code);
            System.Diagnostics.Process.Start(saveLocation);
        }



        private void AssignEvents()
        {
            //this.InstanceSelected += HandleInstanceSelected;
        }

    }
}
