using Gum;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
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

        public override string FriendlyName => "Skia Plugin";

        public override Version Version => new Version(1, 1);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();

            AddMenuItems();

            RegisterEnumTypes();
        }

        private void RegisterEnumTypes()
        {
            TypeManager.Self.AddType(typeof(GradientType));
        }

        private void AddMenuItems()
        {
            var item = this.AddMenuItem(new List<string>() { "Plugins", "Add Skia Standard Elements" });
            item.Click += (not, used) =>
            {
                StandardAdder.AddAllStandards();
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            };
        }

        private void AssignEvents()
        {
            GetDefaultStateForType += HandleGetDefaultStateForType;
            CreateRenderableForType += HandleCreateRenderbleFor;
            VariableExcluded += DefaultStateManager.GetIfVariableIsExcluded;
            VariableSet += DefaultStateManager.HandleVariableSet;
        }

        private IRenderableIpso HandleCreateRenderbleFor(string type)
        {
            switch (type)
            {
                case "Svg": return new RenderableSvg();
                case "ColoredCircle": return new RenderableCircle();
                case "RoundedRectangle": return new RenderableRoundedRectangle();
                case "Arc": return new RenderableArc();
            }

            return null;
        }

        private StateSave HandleGetDefaultStateForType(string type)
        {
            switch(type)
            {
                case "Svg": return DefaultStateManager.GetSvgState();
                case "ColoredCircle": return DefaultStateManager.GetColoredCircleState();
                case "RoundedRectangle": return DefaultStateManager.GetRoundedRectangleState();
                case "Arc": return DefaultStateManager.GetArcState();
            }
            return null;
        }

    }
}
