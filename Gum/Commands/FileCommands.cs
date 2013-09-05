using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.ToolStates;
using Gum.Managers;
using Gum.Wireframe;
using Gum.DataTypes;
using System.Windows.Forms;

namespace Gum.Commands
{
    public class FileCommands
    {
        public void TryAutoSaveCurrentElement()
        {
            TryAutoSaveElement(SelectedState.Self.SelectedElement);
        }


        public void TryAutoSaveElement(ElementSave elementSave)
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave && elementSave != null)
            {
                ProjectManager.Self.SaveElement(elementSave);
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

        public void TryAutoSaveProject()
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave && !ProjectManager.Self.HaveErrorsOccurred)
            {
                ForceSaveProject();
            }
        }

        internal void ForceSaveProject()
        {
            if (!ProjectManager.Self.HaveErrorsOccurred)
            {
                ProjectManager.Self.SaveProject();
            }
            else
            {
                MessageBox.Show("Cannot save project because of earlier errors");
            }
        }
    }
}
