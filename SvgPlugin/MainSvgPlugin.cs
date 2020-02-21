using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using RenderingLibrary.Graphics;
using SkiaPlugin.Managers;
using SkiaPlugin.Renderables;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin
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
            var item = this.AddMenuItem(new List<string>() { "Plugins", "Add Skia Standard Elements" });
            item.Click += (not, used) => StandardAdder.AddAllStandards();
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
            else if(type == "ColoredCircle")
            {
                return new RenderableCircle();
            }
            else
            {
                return null;
            }
        }

        private StateSave HandleGetDefaultStateForType(string type)
        {
            switch(type)
            {
                case "Svg": return DefaultStateManager.GetSvgState();
                case "ColoredCircle": return DefaultStateManager.GetColoredCircleState();
            }
            return null;
        }

    }
}
