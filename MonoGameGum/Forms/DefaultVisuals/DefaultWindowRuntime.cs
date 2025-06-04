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
public class DefaultWindowRuntime : InteractiveGue
{

    public DefaultWindowRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 256;
            this.Height = 256;

            var border = new ColoredRectangleRuntime();
            border.Name = "WindowBorder";
            border.Dock(Gum.Wireframe.Dock.Fill);
            border.Color = Styling.Colors.DarkGray;
            this.AddChild(border);

            var background = new ColoredRectangleRuntime();
            background.Name = "WindowBackground";
            background.Dock(Gum.Wireframe.Dock.Fill);
            // This is too thick, looks bad:
            //background.Width = -2 * borderWidth;
            //background.Height = -2 * borderWidth;
            background.Width = -4;
            background.Height = -4;

            background.Color = Styling.Colors.Gray;
            this.AddChild(background);

            float borderWidth = 10;

            var innerPanel = new ContainerRuntime();
            innerPanel.Name = "InnerPanelInstance";
            innerPanel.Dock(Gum.Wireframe.Dock.Fill);
            this.AddChild(innerPanel);

            // Do this first so it sits behind the resize panel:
            var titlePanel = new Panel();
            titlePanel.Dock(Gum.Wireframe.Dock.Top);
            titlePanel.Height = 32;
            titlePanel.Name = "TitleBarInstance";
            this.AddChild(titlePanel);

            var borderTopLeft = new Panel();
            borderTopLeft.Name = "BorderTopLeftInstance";
            borderTopLeft.Anchor(Gum.Wireframe.Anchor.TopLeft);
            borderTopLeft.Width = borderWidth;
            borderTopLeft.Height = borderWidth;
            borderTopLeft.CustomCursor = Cursors.SizeNWSE;
            this.AddChild(borderTopLeft);

            var borderTopRight = new Panel();
            borderTopRight.Name = "BorderTopRightInstance";
            borderTopRight.Anchor(Gum.Wireframe.Anchor.TopRight);
            borderTopRight.Width = borderWidth;
            borderTopRight.Height = borderWidth;
            borderTopRight.CustomCursor = Cursors.SizeNESW;
            this.AddChild(borderTopRight);

            var borderBottomLeft = new Panel();
            borderBottomLeft.Name = "BorderBottomLeftInstance";
            borderBottomLeft.Anchor(Gum.Wireframe.Anchor.BottomLeft);
            borderBottomLeft.Width = borderWidth;
            borderBottomLeft.Height = borderWidth;
            borderBottomLeft.CustomCursor = Cursors.SizeNESW;
            this.AddChild(borderBottomLeft);

            var borderBottomRight = new Panel();
            borderBottomRight.Name = "BorderBottomRightInstance";
            borderBottomRight.Anchor(Gum.Wireframe.Anchor.BottomRight);
            borderBottomRight.Width = borderWidth;
            borderBottomRight.Height = borderWidth;
            borderBottomRight.CustomCursor = Cursors.SizeNWSE;
            this.AddChild(borderBottomRight);

            var borderTop = new Panel();
            borderTop.Name = "BorderTopInstance";
            borderTop.Dock(Gum.Wireframe.Dock.Top);
            borderTop.Height = borderWidth;
            borderTop.Width = -borderWidth*2;
            borderTop.CustomCursor = Cursors.SizeNS;
            this.AddChild(borderTop);

            var borderBottom = new Panel();
            borderBottom.Name = "BorderBottomInstance";
            borderBottom.Dock(Gum.Wireframe.Dock.Bottom);
            borderBottom.Height = borderWidth;
            borderBottom.Width = -borderWidth*2;
            borderBottom.CustomCursor = Cursors.SizeNS;
            this.AddChild(borderBottom);

            var borderLeft = new Panel();
            borderLeft.Name = "BorderLeftInstance";
            borderLeft.Dock(Gum.Wireframe.Dock.Left);
            borderLeft.Width = borderWidth;
            borderLeft.Height = -borderWidth*2;
            borderLeft.CustomCursor = Cursors.SizeWE;
            this.AddChild(borderLeft);

            var borderRight = new Panel();
            borderRight.Name = "BorderRightInstance";
            borderRight.Dock(Gum.Wireframe.Dock.Right);
            borderRight.Width = borderWidth;
            borderRight.Height = -borderWidth*2;
            borderRight.CustomCursor = Cursors.SizeWE;
            this.AddChild(borderRight);


            // no states needed:
        }


        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Window(this);
        }
    }

    public Window FormsControl => FormsControlAsObject as Window;
}
