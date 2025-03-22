using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary;
public class SystemManagers : ISystemManagers
{
    public bool EnableTouchEvents { get; set; }

    public static SystemManagers Default
    {
        get;
        set;
    }

    Renderer _renderer;
    public Renderer Renderer => _renderer;

    IRenderer ISystemManagers.Renderer => Renderer;


    public SystemManagers()
    {
        _renderer = new Renderer();
    }

    public void InvalidateSurface()
    {

    }
}
