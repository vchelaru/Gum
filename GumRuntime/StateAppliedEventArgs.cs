using Gum.DataTypes.Variables;
using System;

namespace Gum.Wireframe
{
    public class StateAppliedEventArgs : EventArgs
    {
        public StateSave State { get; }
        public StateAppliedEventArgs(StateSave state)
        {
            State = state;
        }
    }
}
