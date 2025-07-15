using Gum.Converters;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class ScrollViewerVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public ScrollBarVisual VerticalScrollBarInstance { get; private set; }
    public ContainerRuntime InnerPanelInstance { get; private set; }
    public ContainerRuntime ClipContainerInstance { get; private set; }
    public ContainerRuntime ScrollAndClipContainer { get; private set; }
    public ContainerRuntime ClipContainerContainer { get; private set; }

    public void MakeSizedToChildren()
    {
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Height = 4;
        ClipContainerInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerInstance.Height = 0;
        InnerPanelInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Width = 4;
        ClipContainerInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerInstance.Width = 0;
        InnerPanelInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    }

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 150;
        Height = 200;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.Height = 0f;
            Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Width = 0f;
            Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.X = 0f;
            Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Y = 0f;
            Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Color = Styling.Colors.DarkGray;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Bordered);
            this.AddChild(Background);

            ScrollAndClipContainer = new ContainerRuntime();
            ScrollAndClipContainer.Name = "ScrollAndClipContainer";
            ScrollAndClipContainer.Width = 0;
            ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ScrollAndClipContainer.Height = 0;
            ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.AddChild(ScrollAndClipContainer);

            {
                VerticalScrollBarInstance = new ScrollBarVisual();
                VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
                VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
                VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
                VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                VerticalScrollBarInstance.Height = 0;
                VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ScrollAndClipContainer.AddChild(VerticalScrollBarInstance);

                // ClipContainerContainer uses a ratio to fill available space,
                // and the clip container is inside of that and adds its own margins
                ClipContainerContainer = new ContainerRuntime();
                ClipContainerContainer.Name = "ClipContainerContainer";
                ClipContainerContainer.Height = 0f;
                ClipContainerContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ClipContainerContainer.Width = 1;
                ClipContainerContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
                ScrollAndClipContainer.AddChild(ClipContainerContainer);

                {
                    ClipContainerInstance = new ContainerRuntime();
                    ClipContainerInstance.Name = "ClipContainerInstance";
                    ClipContainerInstance.ClipsChildren = true;
                    ClipContainerInstance.Height = -4f;
                    ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerInstance.Width = -4;
                    ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerInstance.X = 2f;
                    ClipContainerInstance.Y = 2f;
                    ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                    ClipContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;
                    ClipContainerContainer.AddChild(ClipContainerInstance);

                    {
                        InnerPanelInstance = new ContainerRuntime();
                        InnerPanelInstance.Name = "InnerPanelInstance";
                        InnerPanelInstance.Height = 0f;
                        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                        InnerPanelInstance.Width = 0f;
                        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
                        ClipContainerInstance.AddChild(InnerPanelInstance);
                    }
                }
            }
        }

        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollViewer(this);
        }
    }

    public ScrollViewer FormsControl => this.FormsControlAsObject as ScrollViewer;
}
