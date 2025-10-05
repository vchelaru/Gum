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

public class DefaultScrollViewerRuntime : InteractiveGue
{

    ColoredRectangleRuntime background = new ColoredRectangleRuntime();
    ContainerRuntime InnerPanel = new ContainerRuntime();
    ContainerRuntime ClipContainer = new ContainerRuntime();
    DefaultScrollBarRuntime VerticalScrollBarInstance = new DefaultScrollBarRuntime();
    ContainerRuntime ScrollAndClipContainer = new ContainerRuntime();
    ContainerRuntime ClipContainerContainer = new ContainerRuntime();
    public void MakeSizedToChildren()
    {
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Height = 4;
        ClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainer.Height = 0;
        InnerPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;


        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Width = 4;
        ClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainer.Width = 0;
        InnerPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

    }

    public DefaultScrollViewerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 150;
            this.Height = 200;

            background.Name = "Background";
            InnerPanel.Name = "InnerPanelInstance";
            ClipContainer.Name = "ClipContainerInstance";
            VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            //var HorizontalScrollBarInstance = new DefaultScrollBarRuntime();
            //HorizontalScrollBarInstance.Name = "HorizontalScrollBarInstance";

            {

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


                ScrollAndClipContainer.Width = 0;
                ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ScrollAndClipContainer.Height = 0;
                ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                this.Children.Add(ScrollAndClipContainer);

                {
                    VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
                    VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
                    VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                    VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                    //VerticalScrollBarInstance.Width = 24;
                    VerticalScrollBarInstance.Height = 0;
                    VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ScrollAndClipContainer.Children.Add(VerticalScrollBarInstance);

                    // clip container container uses a ratio to fill available space,
                    // and the clip container is inside of that and adds its own margins
                    ClipContainerContainer = new ContainerRuntime();
                    ClipContainerContainer.Height = 0f;
                    ClipContainerContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerContainer.Width = 1;
                    ClipContainerContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
                    ScrollAndClipContainer.Children.Add(ClipContainerContainer);

                    {
                        ClipContainer.ClipsChildren = true;
                        ClipContainer.Height = -4f;
                        ClipContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                        ClipContainer.Width = -4;
                        ClipContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                        ClipContainer.X = 2f;
                        ClipContainer.Y = 2f;
                        ClipContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                        ClipContainer.YUnits = GeneralUnitType.PixelsFromSmall;
                        ClipContainerContainer.Children.Add(ClipContainer);

                        {

                            InnerPanel.Height = 0f;
                            InnerPanel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                            InnerPanel.Width = 0f;
                            InnerPanel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                            InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
                            ClipContainer.Children.Add(InnerPanel);
                        }
                    }
                }
            }




        }

        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollViewer(this);
        }
        //HorizontalScrollBarInstance.Height = 0f;
        //HorizontalScrollBarInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Relative
    }

    public ScrollViewer FormsControl => this.FormsControlAsObject as ScrollViewer;
}
