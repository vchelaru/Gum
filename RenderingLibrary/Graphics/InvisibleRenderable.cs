using Gum;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using BlendState = Gum.BlendState;

namespace RenderingLibrary.Graphics;

public class InvisibleRenderable : RenderableBase, ICloneable, IRenderableIpso
{

    // Is this actually needed publicly?
    // Yes, it is set by CustomSetPropertyOnRenderable
    public virtual int Alpha { get; set; } = 255;

    int IRenderableIpso.Alpha => (int)this.Alpha;

    /// <summary>
    /// Normalizes an alpha value arriving from the string-based property dispatch, which is an
    /// int or a float depending on caller, into the int the Alpha properties expect. Lives here
    /// (shared, FRB-compiled source) rather than on a GueDeriving runtime so both the core and
    /// Skia dispatchers - and the FRB build, which compiles no GueDeriving runtimes - can reach
    /// the same normalization. Unrecognized value types fall back to fully opaque.
    /// </summary>
    public static int NormalizeDispatchedAlpha(object value)
    {
        if (value is int intValue)
        {
            return intValue;
        }
        else if (value is float floatValue)
        {
            return (int)floatValue;
        }
        else
        {
            return 255;
        }
    }

    public override void Render(ISystemManagers managers)
    {

    }

    object ICloneable.Clone()
    {
        return Clone();
    }
    public InvisibleRenderable Clone()
    {
        var newInstance = (InvisibleRenderable)this.MemberwiseClone();
        ((IRenderableIpso)newInstance).SetParentDirect(null);

        newInstance._children = new ();

        return newInstance;
    }

}
