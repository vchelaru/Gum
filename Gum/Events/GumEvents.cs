using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
