using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Commands
{
    public class EditCommands
    {
        public void AddState()
        {
            StateTreeViewManager.Self.AddStateClick();

        }

        public void AddCategory()
        {
            StateTreeViewManager.Self.AddStateCategoryClick();
        }
    }
}
