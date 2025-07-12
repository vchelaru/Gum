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
public class WindowVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public Panel InnerPanelInstance { get; private set; }
    public Panel TitleBarInstance { get; private set; }
    public Panel BorderTopLeftInstance { get; private set; }
    public Panel BorderTopRightInstance { get; private set; }
    public Panel BorderBottomLeftInstance { get; private set; }
    public Panel BorderBottomRightInstance { get; private set; }
    public Panel BorderTopInstance { get; private set; }
    public Panel BorderBottomInstance { get; private set; }
    public Panel BorderLeftInstance { get; private set; }
    public Panel BorderRightInstance { get; private set; }


    public float BorderWidth { get; set; } = 10f;
    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 256;
            this.Height = 256;

            var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.X = 0;
            Background.Y = 0;
            Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            Background.XOrigin = HorizontalAlignment.Center;
            Background.YOrigin = VerticalAlignment.Center;
            Background.Width = 0;
            Background.Height = 0;
            Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Color = Styling.Colors.Primary;
            Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Panel);
            this.Children.Add(Background);

            InnerPanelInstance = new Panel();
            InnerPanelInstance.Name = "InnerPanelInstance";
            InnerPanelInstance.Dock(Gum.Wireframe.Dock.Fill);
            this.AddChild(InnerPanelInstance);

            // Do this first so it sits behind the resize panel:
            TitleBarInstance = new Panel();
            TitleBarInstance.Dock(Gum.Wireframe.Dock.Top);
            TitleBarInstance.Height = 24;
            TitleBarInstance.Name = "TitleBarInstance";
            this.AddChild(TitleBarInstance);

            BorderTopLeftInstance = new Panel();
            BorderTopLeftInstance.Name = "BorderTopLeftInstance";
            BorderTopLeftInstance.Anchor(Gum.Wireframe.Anchor.TopLeft);
            BorderTopLeftInstance.Width = BorderWidth;
            BorderTopLeftInstance.Height = BorderWidth;
            BorderTopLeftInstance.CustomCursor = Cursors.SizeNWSE;
            this.AddChild(BorderTopLeftInstance);

            BorderTopRightInstance = new Panel();
            BorderTopRightInstance.Name = "BorderTopRightInstance";
            BorderTopRightInstance.Anchor(Gum.Wireframe.Anchor.TopRight);
            BorderTopRightInstance.Width = BorderWidth;
            BorderTopRightInstance.Height = BorderWidth;
            BorderTopRightInstance.CustomCursor = Cursors.SizeNESW;
            this.AddChild(BorderTopRightInstance);

            BorderBottomLeftInstance = new Panel();
            BorderBottomLeftInstance.Name = "BorderBottomLeftInstance";
            BorderBottomLeftInstance.Anchor(Gum.Wireframe.Anchor.BottomLeft);
            BorderBottomLeftInstance.Width = BorderWidth;
            BorderBottomLeftInstance.Height = BorderWidth;
            BorderBottomLeftInstance.CustomCursor = Cursors.SizeNESW;
            this.AddChild(BorderBottomLeftInstance);

            BorderBottomRightInstance = new Panel();
            BorderBottomRightInstance.Name = "BorderBottomRightInstance";
            BorderBottomRightInstance.Anchor(Gum.Wireframe.Anchor.BottomRight);
            BorderBottomRightInstance.Width = BorderWidth;
            BorderBottomRightInstance.Height = BorderWidth;
            BorderBottomRightInstance.CustomCursor = Cursors.SizeNWSE;
            this.AddChild(BorderBottomRightInstance);

            BorderTopInstance = new Panel();
            BorderTopInstance.Name = "BorderTopInstance";
            BorderTopInstance.Dock(Gum.Wireframe.Dock.Top);
            BorderTopInstance.Height = BorderWidth;
            BorderTopInstance.Width = -BorderWidth * 2;
            BorderTopInstance.CustomCursor = Cursors.SizeNS;
            this.AddChild(BorderTopInstance);

            BorderBottomInstance = new Panel();
            BorderBottomInstance.Name = "BorderBottomInstance";
            BorderBottomInstance.Dock(Gum.Wireframe.Dock.Bottom);
            BorderBottomInstance.Height = BorderWidth;
            BorderBottomInstance.Width = -BorderWidth * 2;
            BorderBottomInstance.CustomCursor = Cursors.SizeNS;
            this.AddChild(BorderBottomInstance);

            BorderLeftInstance = new Panel();
            BorderLeftInstance.Name = "BorderLeftInstance";
            BorderLeftInstance.Dock(Gum.Wireframe.Dock.Left);
            BorderLeftInstance.Width = BorderWidth;
            BorderLeftInstance.Height = -BorderWidth * 2;
            BorderLeftInstance.CustomCursor = Cursors.SizeWE;
            this.AddChild(BorderLeftInstance);

            BorderRightInstance = new Panel();
            BorderRightInstance.Name = "BorderRightInstance";
            BorderRightInstance.Dock(Gum.Wireframe.Dock.Right);
            BorderRightInstance.Width = BorderWidth;
            BorderRightInstance.Height = -BorderWidth * 2;
            BorderRightInstance.CustomCursor = Cursors.SizeWE;
            this.AddChild(BorderRightInstance);
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Window(this);
        }
    }

    public Window FormsControl => FormsControlAsObject as Window;
}
