using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.ToolStates;

namespace Gum.Commands
{
    public class FileCommands
    {
        public void TryAutoSaveCurrentElement()
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave && SelectedState.Self.SelectedElement != null)
            {
                ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);


            }

        }
    }
}
