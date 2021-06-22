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
    }
}
