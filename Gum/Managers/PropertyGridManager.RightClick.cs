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



        void OnUnExposeVariableClick(object sender, EventArgs e)
        {
            // Find this variable in the source instance and make it not exposed
            VariableSave variableSave = SelectedState.Self.SelectedVariableSave;

            if (variableSave != null)
            {
                variableSave.ExposedAsName = null;
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

            }

            RefreshUI();
        }

        void OnResetToDefaultClick(object sender, EventArgs e)
        {
            PropertyGridManager.Self.ResetSelectedValueToDefault();

        }

        void OnExposeVariableClick(object sender, EventArgs e)
        {
            // This code is going to go away.  It's copied into STateReferencingInstanceMember
            // where it will be maintained going forward. I copied it because this depends on
            // the SelectedVariableSave and a few other properties that don't exist in the 
            // STateReferencingInstanceMember

            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter variable name:";

            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
            VariableSave variableSave = SelectedState.Self.SelectedVariableSave;
            StateSave currentStateSave = SelectedState.Self.SelectedStateSave;




            if (variableSave == null)
            {
                // This variable hasn't been assigned yet.  Let's make a new variable with a null value

                string variableName = instanceSave.Name + "." + this.SelectedLabel;
                string rawVariableName = this.SelectedLabel;

                ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
                string variableType = elementForInstance.DefaultState.GetVariableSave(rawVariableName).Type;

                currentStateSave.SetValue(variableName, null, instanceSave, variableType);

                // Now the variable should be created so we can access it
                variableSave = SelectedState.Self.SelectedVariableSave;
            }

            //tiw.Result = instanceSave.Name + variableSave.Name;
            // We want to use the name without the dots.
            // So something like TextInstance.Text would be
            // TextInstanceText
            tiw.Result = variableSave.Name.Replace(".", "");

            DialogResult result = tiw.ShowDialog();
            if (result == DialogResult.OK)
            {
                SelectedState.Self.SelectedVariableSave.ExposedAsName = tiw.Result;

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
            else
            {
                currentStateSave.Variables.Remove(variableSave);
            }
        }

        private void InitializeRightClickMenu()
        {
            mExposeVariable = new ToolStripMenuItem("Expose Variable");
            mExposeVariable.Click += new EventHandler(OnExposeVariableClick);

            mResetToDefault = new ToolStripMenuItem("Reset to default");
            mResetToDefault.Click += new EventHandler(OnResetToDefaultClick);

            mUnExposeVariable = new ToolStripMenuItem("Un-Expose Variable");
            mUnExposeVariable.Click += new EventHandler(OnUnExposeVariableClick);
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

            if ((variableSave != null && variableSave.GetRootName() == "SourceFile") ||
                (variableSave == null && this.SelectedLabel == "SourceFile")                
                )
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem("File from project");

                List<string> otherFiles = ObjectFinder.Self.GetAllFilesInProject();

                foreach (string file in otherFiles)
                {
                    menuItem.DropDownItems.Add(file, null, HandleFileFromProjectSelect);
                }

                contextMenuStrip.Items.Add(menuItem);

            }

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
