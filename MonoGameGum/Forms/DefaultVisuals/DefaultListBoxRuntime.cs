using Gum.Converters;
using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultListBoxRuntime : InteractiveGue
{
    public DefaultListBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            var background = new ColoredRectangleRuntime();
            background.Name = "Background";

            var InnerPanel = new ContainerRuntime();
            InnerPanel.Name = "InnerPanelInstance";
            var ClipContainer = new ContainerRuntime();
            ClipContainer.Name = "ClipContainerInstance";
            var VerticalScrollBarInstance = new DefaultScrollBarRuntime();
            VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            //var HorizontalScrollBarInstance = new DefaultScrollBarRuntime();
            //HorizontalScrollBarInstance.Name = "HorizontalScrollBarInstance";

            background.Height = 0f;
            background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.Width = 0f;
            background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            background.X = 0f;
            background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            background.XUnits = GeneralUnitType.PixelsFromMiddle;
            background.Y = 0f;
            background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            background.YUnits = GeneralUnitType.PixelsFromMiddle;
            background.Color = new Microsoft.Xna.Framework.Color(32, 32, 32, 255);
            this.Children.Add(background);

            VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            //VerticalScrollBarInstance.Width = 24;
            VerticalScrollBarInstance.Height = 0;
            VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Children.Add(VerticalScrollBarInstance);


            ClipContainer.ClipsChildren = true;
            ClipContainer.Height = -4f;
            ClipContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            ClipContainer.Width = -27f;
            ClipContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            ClipContainer.X = 2f;
            ClipContainer.Y = 2f;
            ClipContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            ClipContainer.YUnits = GeneralUnitType.PixelsFromSmall;
            this.Children.Add(ClipContainer);


            InnerPanel.Height = 0f;
            InnerPanel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            InnerPanel.Width = 0f;
            InnerPanel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            ClipContainer.Children.Add(InnerPanel);

            var listBoxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            listBoxCategory.Name = "ListBoxCategory";

            // todo - add here:
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBox();
        }
    }

    public ListBox FormsControl => FormsControlAsObject as ListBox;
}
