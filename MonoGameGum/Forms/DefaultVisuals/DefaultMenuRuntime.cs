using Gum.Converters;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

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
