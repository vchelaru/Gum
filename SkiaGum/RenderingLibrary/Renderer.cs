using Gum.Wireframe;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class Renderer
    {
        public static bool UseBasicEffectRendering { get; set; } = true;

        public Camera Camera { get; private set; }

        public void Initialize(SystemManagers managers)
        {
            Camera = new Camera(managers);
        }

        //public void Draw(SystemManagers systemManagers)
        //{
        //    var canvas = systemManagers.Canvas;
        //}

        // This syntax is a little different than standard Gum, but we're moving in that direction incrementally:
        public void Draw(IList<BindableGraphicalUiElement> whatToRender, SystemManagers managers)
        {
            managers.Canvas.Clear();

            if (Camera.Zoom != 1)
            {
                managers.Canvas.Scale(Camera.Zoom);
            }

            foreach (var element in whatToRender)
            {
                if (element.Visible)
                {
                    element.UpdateLayout();
                    ((IRenderable)element).Render(managers.Canvas);
                }
            }

            managers.Canvas.Restore();
        }
    }
}
