using Gum.Wireframe;

using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;
#else
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
#endif
using Gum.Forms.Controls;


namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// A code-only visual for a Window control.
/// </summary>
public class WindowVisual : InteractiveGue
{
    /// <summary>
    /// The Background NineSlice for the Window. This is the visual frame.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The panel which hosts the inner content of the window. Calling AddChild adds children to 
    /// this panel.
    /// </summary>
    public Panel InnerPanelInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the area which the user can push to drag the window.
    /// </summary>
    public Panel TitleBarInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the top-left border for resizing.
    /// </summary>
    public Panel BorderTopLeftInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the top-right border for resizing.
    /// </summary>
    public Panel BorderTopRightInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the bottom-left border for resizing.
    /// </summary>
    public Panel BorderBottomLeftInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the bottom-right border for resizing.
    /// </summary>
    public Panel BorderBottomRightInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the top border for resizing.
    /// </summary>
    public Panel BorderTopInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the bottom border for resizing.
    /// </summary>
    public Panel BorderBottomInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the left border for resizing.
    /// </summary>
    public Panel BorderLeftInstance { get; private set; }

    /// <summary>
    /// The panel which acts as the right border for resizing.
    /// </summary>
    public Panel BorderRightInstance { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The background color which is applied to the window's Background.
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if(!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                // Window doesn't have states...
                //FormsControl?.UpdateState();
                Background.Color = _backgroundColor;
            }
        }
    }

    /// <summary>
    /// Instantiates a new WindowVisual, optionally creating an underlying Forms object.
    /// </summary>
    /// <remarks>
    /// If using using a V3 code-only project, then creating a Window instance automatically creates
    /// a WindowVisual for its Visual, so this is usually not explicitly instantiated.
    /// </remarks>
    /// <param name="fullInstantiation">This parameter is ignored and exists to match other runtime conventions.</param>
    /// <param name="tryCreateFormsObject">Whether to create a new Visual instance. This should be true if
    /// explicitly calling this constructor, but is false when called by the Window class when creating a new Window.
    /// </param>
    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        const float borderSize = 10f;
        Width = 256;
        Height = 256;
        MinHeight = borderSize;
        MinWidth = borderSize;

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
        this.AddChild(BorderRightInstance);

        // Allow the Border drag effect to work outside of this container
        this.RaiseChildrenEventsOutsideOfBounds = true; 

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Window(this);
        }
    }

    /// <summary>
    /// Adjusts the size of the WindowVisual and its InnerPanel to fit around its children,
    /// applying the specified margin on all sides.
    /// </summary>
    /// <param name="innerPanelMargin">The margin to apply on all sides.</param>
    public void MakeSizedToChildren(float innerPanelMargin = 0) => 
        MakeSizedToChildren(innerPanelMargin, innerPanelMargin, innerPanelMargin, innerPanelMargin);

    /// <summary>
    /// Adjusts the size of the WindowVisual and its InnerPanel to fit around its children,
    /// applying the specified margins.
    /// </summary>
    /// <param name="leftMargin">The left margin in pixels</param>
    /// <param name="topMargin">The top margin in pixels</param>
    /// <param name="rightMargin">The right margin in pixels</param>
    /// <param name="bottomMargin">The bottom margin in pixels</param>
    public void MakeSizedToChildren(float leftMargin, float topMargin, 
        float rightMargin, float bottomMargin)
    {
        InnerPanelInstance.Dock(Wireframe.Dock.SizeToChildren);
        InnerPanelInstance.Anchor(Wireframe.Anchor.TopLeft);

        InnerPanelInstance.X = leftMargin;
        InnerPanelInstance.Y = topMargin;

        this.Dock(Wireframe.Dock.SizeToChildren);
        this.Width = rightMargin;
        this.Height = bottomMargin;
    }

    public Window FormsControl => (Window)FormsControlAsObject;
}
