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
    public int Alpha { get; set; } = 255;

    int IRenderableIpso.Alpha => (int)this.Alpha;

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
        ((IRenderableIpso)this).SetParentDirect(null);

        newInstance._children = new ();

        return newInstance;
    }

}
