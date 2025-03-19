using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.RenderingLibrary;
public class SystemManagers : ISystemManagers
{
    public bool EnableTouchEvents { get; set; }

    public static SystemManagers Default
    {
        get;
        set;
    }

    Renderer _renderer;
    public IRenderer Renderer => _renderer;

    public SystemManagers()
    {
        _renderer = new Renderer();
    }

    public void InvalidateSurface()
    {

    }
}
