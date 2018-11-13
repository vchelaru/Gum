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
    // MSBuild should not be in the gac:
    // https://stackoverflow.com/questions/45738512/could-not-load-type-microsoft-build-framework-sdkreference-on-project-open-in

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
            this.InstanceAdd += HandleInstanceAdd;
            this.InstanceDelete += HandleInstanceDelete;

            this.ElementAdd += HandleElementAdd;
            this.ElementDelete += HandleElementDelete;
            // todo - what about rename?
        }

        private void HandleInstanceDelete(ElementSave container, InstanceSave instance)
        {
            InstanceRemoveLogic.Self.HandleInstanceDelete(container, instance);
        }

        private void HandleInstanceAdd(ElementSave container, InstanceSave instance)
        {
            InstanceAddLogic.Self.HandleInstanceAdd(container, instance);
        }

        private void HandleVariableSet(ElementSave container, InstanceSave instance, string variableName, object oldValue)
        {
            VariableSetLogic.Self.SetVariable(container, instance, variableName, oldValue);
        }

        private void HandleElementAdd(ElementSave element)
        {
            ElementAddLogic.Self.HandleElementAdd(element);
        }

        private void HandleElementDelete(ElementSave element)
        {
            ElementDeleteLogic.Self.HandleElementDelete(element);
        }

    }
}
