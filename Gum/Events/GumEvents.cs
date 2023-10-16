using Gum.Managers;
using System;

namespace Gum.Events
{
    public class GumEvents : Singleton<GumEvents>
    {
        public Action InstanceSelected;

        internal void CallInstanceSelected()
        {
            if (InstanceSelected != null)
            {
                InstanceSelected();
            }
        }
    }
}
