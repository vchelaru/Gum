using Gum.Forms.Controls;
using Gum.Wireframe;
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

namespace Gum.Forms;

/// <summary>
/// Base class which can be inherited from when creating new Screen classes
/// </summary>
public class ScreenBase : FrameworkElement
{
    /// <summary>
    /// Creates a new ScreenBase using a ContainerRuntime as the Visual.
    /// </summary>
    public ScreenBase() : base (new ContainerRuntime())
    {
        Visual.Dock(Wireframe.Dock.Fill);
    }

    /// <summary>
    /// Creates a new ScreenBase using the argument as its Visual.
    /// </summary>
    /// <param name="runtime">The visual to use.</param>
    public ScreenBase(InteractiveGue runtime) : base (runtime)
    {
    }
}
