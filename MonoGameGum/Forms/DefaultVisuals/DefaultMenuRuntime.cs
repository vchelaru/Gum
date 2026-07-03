#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
using Gum.Wireframe;
using Gum.Forms.Controls;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace MonoGameGum.Forms.DefaultVisuals;
#else
namespace Gum.Forms.DefaultVisuals;
#endif

[Obsolete("Legacy V1 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V1 default visuals are slated for removal in a future release.")]
internal class DefaultMenuRuntime : InteractiveGue
{
    public DefaultMenuRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        if(fullInstantiation)
        {
            this.Width = 0;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.Height = 0;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            var background = new ColoredRectangleRuntime();
            background.Name = "Background";

            var InnerPanel = new ContainerRuntime();
            InnerPanel.Name = "InnerPanelInstance";

            background.Height = 0f;
            background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.Width = 0f;
            background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.X = 0f;
            background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            background.XUnits = GeneralUnitType.PixelsFromMiddle;
            background.Y = 0f;
            background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            background.YUnits = GeneralUnitType.PixelsFromMiddle;
            background.Color = new Microsoft.Xna.Framework.Color(32, 32, 32, 255);
            this.Children.Add(background);

            InnerPanel.Height = 0f;
            InnerPanel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            InnerPanel.Width = 0f;
            InnerPanel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            InnerPanel.WrapsChildren = true;
            InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            InnerPanel.HasEvents = false;
            this.Children.Add(InnerPanel);

            var menuCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            menuCategory.Name = Menu.MenuCategoryState;
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Menu(this);
        }
    }

    public Menu FormsControl => FormsControlAsObject as Menu;
}
