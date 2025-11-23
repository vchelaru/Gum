using Gum.Wireframe;

using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Gum.GueDeriving;
#else
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
using Styling = Gum.Forms.DefaultVisuals.Styling;
using MonoGameGum;
using Microsoft.Xna.Framework;

namespace Gum.Forms.DefaultVisuals.V3;

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

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if(value != _backgroundColor)
            {
                _backgroundColor = value;
                // Window doesn't have states...
                //FormsControl?.UpdateState();
                Background.Color = _backgroundColor;
            }
        }
    }

    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        const float borderSize = 10f;
        Width = 256;
        Height = 256;
        MinHeight = borderSize;
        MinWidth = borderSize;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;

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
        Background.Color = _backgroundColor;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Panel);
        this.AddChild(Background);

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
        BorderTopLeftInstance.X = -borderSize;
        BorderTopLeftInstance.Y = -borderSize;
        BorderTopLeftInstance.Width = borderSize;
        BorderTopLeftInstance.Height = borderSize;
        this.AddChild(BorderTopLeftInstance);

        BorderTopRightInstance = new Panel();
        BorderTopRightInstance.Name = "BorderTopRightInstance";
        BorderTopRightInstance.Anchor(Gum.Wireframe.Anchor.TopRight);
        BorderTopRightInstance.X = borderSize;
        BorderTopRightInstance.Y = -borderSize;
        BorderTopRightInstance.Width = borderSize;
        BorderTopRightInstance.Height = borderSize;
        this.AddChild(BorderTopRightInstance);

        BorderBottomLeftInstance = new Panel();
        BorderBottomLeftInstance.Name = "BorderBottomLeftInstance";
        BorderBottomLeftInstance.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        BorderBottomLeftInstance.X = -borderSize;
        BorderBottomLeftInstance.Y = borderSize;
        BorderBottomLeftInstance.Width = borderSize;
        BorderBottomLeftInstance.Height = borderSize;
        this.AddChild(BorderBottomLeftInstance);

        BorderBottomRightInstance = new Panel();
        BorderBottomRightInstance.Name = "BorderBottomRightInstance";
        BorderBottomRightInstance.Anchor(Gum.Wireframe.Anchor.BottomRight);
        BorderBottomRightInstance.X = borderSize;
        BorderBottomRightInstance.Y = borderSize;
        BorderBottomRightInstance.Width = borderSize;
        BorderBottomRightInstance.Height = borderSize;
        this.AddChild(BorderBottomRightInstance);

        BorderTopInstance = new Panel();
        BorderTopInstance.Name = "BorderTopInstance";
        BorderTopInstance.Dock(Gum.Wireframe.Dock.Top);
        BorderTopInstance.Y = -borderSize;
        BorderTopInstance.Height = borderSize;
        BorderTopInstance.Width = 0;
        this.AddChild(BorderTopInstance);

        BorderBottomInstance = new Panel();
        BorderBottomInstance.Name = "BorderBottomInstance";
        BorderBottomInstance.Dock(Gum.Wireframe.Dock.Bottom);
        BorderBottomInstance.Y = borderSize;
        BorderBottomInstance.Height = borderSize;
        BorderBottomInstance.Width = 0;
        this.AddChild(BorderBottomInstance);

        BorderLeftInstance = new Panel();
        BorderLeftInstance.Name = "BorderLeftInstance";
        BorderLeftInstance.Dock(Gum.Wireframe.Dock.Left);
        BorderLeftInstance.X = -borderSize;
        BorderLeftInstance.Width = borderSize;
        BorderLeftInstance.Height = 0;
        this.AddChild(BorderLeftInstance);

        BorderRightInstance = new Panel();
        BorderRightInstance.Name = "BorderRightInstance";
        BorderRightInstance.Dock(Gum.Wireframe.Dock.Right);
        BorderRightInstance.X = borderSize;
        BorderRightInstance.Width = borderSize;
        BorderRightInstance.Height = 0;

        // Allow the Border drag effect to work outside of this container
        this.RaiseChildrenEventsOutsideOfBounds = true; 

        //SetCustomCursorForResizing();

        this.AddChild(BorderRightInstance);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Window(this);
        }
    }

    public Window FormsControl => FormsControlAsObject as Window;
}
