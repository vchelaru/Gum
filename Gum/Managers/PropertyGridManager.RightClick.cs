using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.ToolCommands;
using CommonFormsAndControls;
using Gum.DataTypes;

namespace Gum.Managers
{
    public partial class PropertyGridManager
	{


        void OnResetToDefaultClick(object sender, EventArgs e)
        {
            PropertyGridManager.Self.ResetSelectedValueToDefault();

        }

        private void InitializeRightClickMenu()
        {
            mResetToDefault = new ToolStripMenuItem("Reset to default");
            mResetToDefault.Click += new EventHandler(OnResetToDefaultClick);

        }

        public void OnPropertyGridRightClick(ContextMenuStrip contextMenuStrip)
        {
            contextMenuStrip.Items.Clear();

            contextMenuStrip.Items.Add(mResetToDefault);

            VariableSave variableSave = SelectedState.Self.SelectedVariableSave;

            if (SelectedState.Self.SelectedInstance != null &&
                SelectedState.Self.SelectedComponent != null)
            {
                if (variableSave == null || string.IsNullOrEmpty(variableSave.ExposedAsName))
                {

                    // We're on a variable that is on an instance, and in a
                    // component, so we can expose it
                    contextMenuStrip.Items.Add(mExposeVariable);
                }
            }

            if (variableSave != null && !string.IsNullOrEmpty(variableSave.ExposedAsName))
            {
                contextMenuStrip.Items.Add(mUnExposeVariable);
            }

            // We don't use a PropertyGrid anymore
            //if ((variableSave != null && variableSave.GetRootName() == "SourceFile") ||
            //    (variableSave == null && this.SelectedLabel == "SourceFile")                
            //    )
            //{
            //    ToolStripMenuItem menuItem = new ToolStripMenuItem("File from project");

            //    List<string> otherFiles = ObjectFinder.Self.GetAllFilesInProject();

            //    foreach (string file in otherFiles)
            //    {
            //        menuItem.DropDownItems.Add(file, null, HandleFileFromProjectSelect);
            //    }

            //    contextMenuStrip.Items.Add(menuItem);

            //}

        }

        void HandleFileFromProjectSelect(object sender, EventArgs args)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;

            string text = menuItem.Text;

            // We don't want to use a recursive variable finder because if the Instance
            // doesn't have this variable then the RVF will return the variable from the
            // base.  Instead, we want to set the value right on the instance if an instance
            // is selected.
            //VariableSave variable = SelectedState.Self.SelectedRecursiveVariableFinder.GetVariable("SourceFile");
            if (SelectedState.Self.SelectedInstance != null)
            {
                SelectedState.Self.SelectedStateSave.SetValue(
                    SelectedState.Self.SelectedInstance.Name + ".SourceFile", text);

            }
            else
            {
                SelectedState.Self.SelectedStateSave.SetValue(
                    "SourceFile", text);

            }

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            RefreshUI();

            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }
    }
}
