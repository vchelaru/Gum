using Gum.DataTypes.Variables;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.StateAnimation.Runtime;
public class AnimationRuntime
{
    public List<KeyframeRuntime> Keyframes { get; private set; } = new List<KeyframeRuntime>();

    public void RefreshCumulativeStates(ElementSave element, bool useDefaultAsStarting = true)
    {
    }

}
