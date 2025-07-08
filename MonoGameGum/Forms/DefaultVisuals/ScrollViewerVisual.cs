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

    public NineSliceRuntime Background { get; set; }
    public ScrollBarVisual VerticalScrollBarInstance { get; set; }
    public ContainerRuntime InnerPanel { get; set; }
    public ContainerRuntime ClipContainer { get; set; }
    public ContainerRuntime ScrollAndClipContainer { get; set; }
    public ContainerRuntime ClipContainerContainer { get; set; }
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

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 150;
            this.Height = 200;

            var uiSpriteSheetTexture = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{RenderingLibrary.SystemManagers.AssemblyPrefix}.UISpriteSheet.png");

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
                Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
                Background.Texture = uiSpriteSheetTexture;
                Background.ApplyState(NineSliceStyles.Solid);
                this.Children.Add(Background);

                ScrollAndClipContainer = new ContainerRuntime();
                ScrollAndClipContainer.Width = 0;
                ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ScrollAndClipContainer.Height = 0;
                ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                this.Children.Add(ScrollAndClipContainer);

                {
                    VerticalScrollBarInstance = new ScrollBarVisual();
                    VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
                    VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
                    VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
                    VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                    VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                    VerticalScrollBarInstance.Height = 0;
                    VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ScrollAndClipContainer.Children.Add(VerticalScrollBarInstance);

                    // ClipContainerContainer uses a ratio to fill available space,
                    // and the clip container is inside of that and adds its own margins
                    ClipContainerContainer = new ContainerRuntime();
                    ClipContainerContainer.Height = 0f;
                    ClipContainerContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerContainer.Width = 1;
                    ClipContainerContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
                    ScrollAndClipContainer.Children.Add(ClipContainerContainer);

                    {
                        ClipContainer = new ContainerRuntime();
                        ClipContainer.Name = "ClipContainerInstance";
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
                            InnerPanel = new ContainerRuntime();
                            InnerPanel.Name = "InnerPanelInstance";
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
    }

    public ScrollViewer FormsControl => this.FormsControlAsObject as ScrollViewer;
}
