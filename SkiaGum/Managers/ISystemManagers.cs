using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Managers
{
    public interface ISystemManagers
    {
        void InvalidateSurface();
        bool EnableTouchEvents { get; set; }
        RenderingLibrary.Graphics.Renderer Renderer { get; }
    }
}
