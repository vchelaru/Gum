using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using RenderingLibrary.Graphics;
using SvgPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgPlugin
{
    [Export(typeof(PluginBase))]
    public class MainSvgPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "SVG (Skia) Plugin";

        public override Version Version => new Version(1, 0);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();

            AddMenuItems();
        }

        private void AddMenuItems()
        {
            var item = this.AddMenuItem(new List<string>() { "Plugins", "Add SVG Standard" });
            item.Click += (not, used) => SvgStandardAdder.AddSvgStandard();
        }

        private void AssignEvents()
        {
            GetDefaultStateForType += HandleGetDefaultStateForType;
            CreateRenderableForType += HandleCreateRenderbleFor;
        }

        private IRenderableIpso HandleCreateRenderbleFor(string type)
        {
            if (type == "Svg")
            {
                return new RenderableSvg();
            }
            else
            {
                return null;
            }
        }

        private StateSave HandleGetDefaultStateForType(string type)
        {
            if(type == "Svg")
            {
                return DefaultStateManager.GetSvgState();

            }
            else
            {
                return null;
            }
        }

    }
}
