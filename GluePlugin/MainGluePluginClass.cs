using GluePlugin.Logic;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GluePlugin
{
    [Export(typeof(PluginBase))]
    public class MainGluePluginClass : PluginBase
    {
        public override string FriendlyName
        {
            get
            {
                return "Glue Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var menuItem = this.AddMenuItem(new[] { "File", "Load Glue Project" });
            menuItem.Click += (not, used) => GlueProjectLoadingLogic.Self.ShowLoadProjectDialog();

            this.VariableSet += HandleVariableSet;
        }

        private void HandleVariableSet(ElementSave container, InstanceSave instance, string variableName, object oldValue)
        {
            VariableSetLogic.Self.SetVariable(container, instance, variableName, oldValue);
        }
    }
}
