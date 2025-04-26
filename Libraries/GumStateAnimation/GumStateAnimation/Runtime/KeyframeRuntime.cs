using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class KeyframeRuntime
{
    public StateSave CachedCumulativeState
    {
        get;
        set;
    }

    public string StateName
    {
        get;
        set;
    }
}
