using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.ToolStates;
using Gum.Managers;
using Gum.Wireframe;

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

        internal void NewProject()
        {
            ProjectManager.Self.CreateNewProject();

            ElementTreeViewManager.Self.RefreshUI();
            StateTreeViewManager.Self.RefreshUI(null);
            PropertyGridManager.Self.RefreshUI();
            WireframeObjectManager.Self.RefreshAll(true);

        }

        internal void SaveProject()
        {
            ProjectManager.Self.SaveProject();
        }
    }
}
