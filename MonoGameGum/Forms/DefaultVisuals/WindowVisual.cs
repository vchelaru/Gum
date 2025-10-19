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

namespace Gum.Forms.DefaultVisuals;

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

    public float BorderSize { get; set; } = 10f;

    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 256;
        Height = 256;
        MinHeight = BorderSize;
        MinWidth = BorderSize;

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
        Background.Color = Styling.ActiveStyle.Colors.Primary;
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
        BorderTopLeftInstance.X = -BorderSize;
        BorderTopLeftInstance.Y = -BorderSize;
        BorderTopLeftInstance.Width = BorderSize;
        BorderTopLeftInstance.Height = BorderSize;
        this.AddChild(BorderTopLeftInstance);

        BorderTopRightInstance = new Panel();
        BorderTopRightInstance.Name = "BorderTopRightInstance";
        BorderTopRightInstance.Anchor(Gum.Wireframe.Anchor.TopRight);
        BorderTopRightInstance.X = BorderSize;
        BorderTopRightInstance.Y = -BorderSize;
        BorderTopRightInstance.Width = BorderSize;
        BorderTopRightInstance.Height = BorderSize;
        this.AddChild(BorderTopRightInstance);

        BorderBottomLeftInstance = new Panel();
        BorderBottomLeftInstance.Name = "BorderBottomLeftInstance";
        BorderBottomLeftInstance.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        BorderBottomLeftInstance.X = -BorderSize;
        BorderBottomLeftInstance.Y = BorderSize;
        BorderBottomLeftInstance.Width = BorderSize;
        BorderBottomLeftInstance.Height = BorderSize;
        this.AddChild(BorderBottomLeftInstance);

        BorderBottomRightInstance = new Panel();
        BorderBottomRightInstance.Name = "BorderBottomRightInstance";
        BorderBottomRightInstance.Anchor(Gum.Wireframe.Anchor.BottomRight);
        BorderBottomRightInstance.X = BorderSize;
        BorderBottomRightInstance.Y = BorderSize;
        BorderBottomRightInstance.Width = BorderSize;
        BorderBottomRightInstance.Height = BorderSize;
        this.AddChild(BorderBottomRightInstance);

        BorderTopInstance = new Panel();
        BorderTopInstance.Name = "BorderTopInstance";
        BorderTopInstance.Dock(Gum.Wireframe.Dock.Top);
        BorderTopInstance.Y = -BorderSize;
        BorderTopInstance.Height = BorderSize;
        BorderTopInstance.Width = 0;
        this.AddChild(BorderTopInstance);

        BorderBottomInstance = new Panel();
        BorderBottomInstance.Name = "BorderBottomInstance";
        BorderBottomInstance.Dock(Gum.Wireframe.Dock.Bottom);
        BorderBottomInstance.Y = BorderSize;
        BorderBottomInstance.Height = BorderSize;
        BorderBottomInstance.Width = 0;
        this.AddChild(BorderBottomInstance);

        BorderLeftInstance = new Panel();
        BorderLeftInstance.Name = "BorderLeftInstance";
        BorderLeftInstance.Dock(Gum.Wireframe.Dock.Left);
        BorderLeftInstance.X = -BorderSize;
        BorderLeftInstance.Width = BorderSize;
        BorderLeftInstance.Height = 0;
        this.AddChild(BorderLeftInstance);

        BorderRightInstance = new Panel();
        BorderRightInstance.Name = "BorderRightInstance";
        BorderRightInstance.Dock(Gum.Wireframe.Dock.Right);
        BorderRightInstance.X = BorderSize;
        BorderRightInstance.Width = BorderSize;
        BorderRightInstance.Height = 0;

        // Testing the border position for the drag feature
        //var col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.White;
        //BorderTopLeftInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.White;
        //BorderTopRightInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.White;
        //BorderBottomLeftInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.White;
        //BorderBottomRightInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.Red;
        //BorderTopInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.Red;
        //BorderBottomInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.Blue;
        //BorderLeftInstance.AddChild(col);
        //col = new ColoredRectangleRuntime();
        //col.WidthUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.HeightUnits = DataTypes.DimensionUnitType.RelativeToParent;
        //col.Width = 0;
        //col.Height = 0;
        //col.Color = Color.Blue;
        //BorderRightInstance.AddChild(col);

        // Allow the Border drag effect to work outside of this container
        this.RaiseChildrenEventsOutsideOfBounds = true; 

        SetCustomCursorForResizing();

        this.AddChild(BorderRightInstance);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Window(this);
        }
    }


    public override object FormsControlAsObject 
    { 
        get => base.FormsControlAsObject;
        set
        {
            var oldWindow = base.FormsControlAsObject as Window;
            if(oldWindow != null)
            {
                oldWindow.ResizeModeChanged -= HandleResizeModeChanged;
            }
            base.FormsControlAsObject = value;
            if(value is Window window)
            {
                window.ResizeModeChanged += HandleResizeModeChanged;
            }
        }
    }

    private void HandleResizeModeChanged(object? sender, EventArgs e)
    {
        if(FormsControl?.ResizeMode == ResizeMode.NoResize)
        {
            BorderTopLeftInstance.CustomCursor = null;
            BorderTopRightInstance.CustomCursor = null;
            BorderBottomLeftInstance.CustomCursor = null;
            BorderBottomRightInstance.CustomCursor = null;
            BorderTopInstance.CustomCursor = null;
            BorderBottomInstance.CustomCursor = null;
            BorderLeftInstance.CustomCursor = null;
            BorderRightInstance.CustomCursor = null;
        }
        else
        {
            SetCustomCursorForResizing();
        }
    }
    private void SetCustomCursorForResizing()
    {
        BorderTopLeftInstance.CustomCursor = Cursors.SizeNWSE;
        BorderTopRightInstance.CustomCursor = Cursors.SizeNESW;
        BorderBottomLeftInstance.CustomCursor = Cursors.SizeNESW;
        BorderBottomRightInstance.CustomCursor = Cursors.SizeNWSE;
        BorderTopInstance.CustomCursor = Cursors.SizeNS;
        BorderBottomInstance.CustomCursor = Cursors.SizeNS;
        BorderLeftInstance.CustomCursor = Cursors.SizeWE;
        BorderRightInstance.CustomCursor = Cursors.SizeWE;
    }

    public Window FormsControl => FormsControlAsObject as Window;
}
