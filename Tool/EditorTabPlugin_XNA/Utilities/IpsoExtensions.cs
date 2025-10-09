using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.Utilities;

public static class IpsoExtensions
{
    public static (float left, float top, float right, float bottom) GetBounds(this IRenderableIpso ipso)
    {
        var left = ipso.GetAbsoluteX();
        var top = ipso.GetAbsoluteY();
        var right = ipso.GetAbsoluteX() + ipso.Width;
        var bottom = ipso.GetAbsoluteY() + ipso.Height;

        if (ipso.IsRenderTarget && ipso is GraphicalUiElement gue)
        {
            var extraWidth = (ipso.RenderTargetScaleX - 1) * gue.Width;
            var extraHeight = (ipso.RenderTargetScaleY - 1) * gue.Height;

            switch (gue.XOrigin)
            {
                case HorizontalAlignment.Center:
                    left -= extraWidth / 2.0f;
                    right += extraWidth / 2.0f;
                    break;
                case HorizontalAlignment.Right:
                    left -= extraWidth;
                    break;
            }
            switch (gue.YOrigin)
            {
                case VerticalAlignment.Center:
                    top -= extraHeight / 2.0f;
                    bottom += extraHeight / 2.0f;
                    break;
                case VerticalAlignment.Bottom:
                    top -= extraHeight;
                    break;
            }
        }

        return (left, top, right, bottom);
    }
}
