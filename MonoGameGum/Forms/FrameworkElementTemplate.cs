using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Gum.Forms.Controls;
namespace Gum.Forms;

#else
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms;
#endif

public class FrameworkElementTemplate
{
    Func<FrameworkElement> creationFunc;

    public FrameworkElementTemplate(Type type)
    {
#if DEBUG


        if (typeof(FrameworkElement).IsAssignableFrom(type) == false)
        {
            throw new ArgumentException($"The type {type} must be derived from FrameworkElement");
        }
#endif
        var constructor = type.GetConstructor(Type.EmptyTypes);

#if DEBUG
        if (constructor == null)
        {
            throw new ArgumentException($"The type {type} must have a constructor with no arguments");
        }
#endif

        Initialize(() => constructor.Invoke(null) as FrameworkElement);
    }

    public FrameworkElementTemplate(Func<FrameworkElement> creationFunc)
    {
        Initialize(creationFunc);
    }

    private void Initialize(Func<FrameworkElement> creationFunc)
    {
        this.creationFunc = creationFunc;
    }

    public FrameworkElement CreateContent()
    {
        return creationFunc();
    }
}
