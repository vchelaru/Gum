using System;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Undo;
using Gum.Gui.Forms;

namespace Gum.Managers
{
    public class MenuStripManager
    {
        #region Fields

        private MenuStrip menuStrip1;

        private ToolStripMenuItem fileToolStripMenuItem;

        private ToolStripMenuItem editToolStripMenuItem;

        private ToolStripMenuItem viewToolStripMenuItem;

        private ToolStripMenuItem contentToolStripMenuItem;

        private ToolStripMenuItem helpToolStripMenuItem;
        
        private ToolStripMenuItem RemoveStateMenuItem;
        private ToolStripMenuItem RemoveElementMenuItem;
        private ToolStripMenuItem RemoveVariableMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem saveAllToolStripMenuItem;
        private ToolStripMenuItem newProjectToolStripMenuItem;
        private ToolStripMenuItem findFileReferencesToolStripMenuItem;
        private ToolStripMenuItem pluginsToolStripMenuItem;
        private ToolStripMenuItem managePluginsToolStripMenuItem;

        static MenuStripManager mSelf;

        #endregion

        public void Initialize(Form mainWindow)
        {
            // Load Recent handled in MainRecentFilesPlugin

            #region Local Functions
            ToolStripMenuItem Add(ToolStripMenuItem parent, string text, Action clickEvent)
            {
                var tsmi = new ToolStripMenuItem();
                tsmi.Text = text;
                if(clickEvent != null)
                {
                    tsmi.Click += (not, used) => clickEvent();
                }
                parent.DropDownItems.Add(tsmi);
                return tsmi;
            }

            void AddSeparator(ToolStripMenuItem parent)
            {
                var separator = new System.Windows.Forms.ToolStripSeparator();
                parent.DropDownItems.Add(separator);
            }
            #endregion

            this.RemoveElementMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveStateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemoveVariableMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findFileReferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managePluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.editToolStripMenuItem = new ToolStripMenuItem();
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";

            var undoMenuItem = Add(editToolStripMenuItem, "Undo", UndoManager.Self.PerformUndo);
            undoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));

            var redoMenuItem = Add(editToolStripMenuItem, "Redo", UndoManager.Self.PerformRedo);
            redoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));

            AddSeparator(editToolStripMenuItem);

            var addToolStripMenuItem = Add(editToolStripMenuItem, "Add", null);
            var removeToolStripMenuItem = Add(editToolStripMenuItem, "Remove", null);


            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveElementMenuItem,
            this.RemoveStateMenuItem,
            this.RemoveVariableMenuItem});




            Add(addToolStripMenuItem, "Screen", () => ElementTreeViewManager.Self.AddScreenClick(this, null));
            Add(addToolStripMenuItem, "Component", () => ElementTreeViewManager.Self.AddComponentClick(this, null));
            Add(addToolStripMenuItem, "Instance", () => ElementTreeViewManager.Self.AddInstanceClick(this, null));
            Add(addToolStripMenuItem, "State", () => GumCommands.Self.GuiCommands.ShowAddStateWindow());

            // 
            // RemoveElementMenuItem
            // 
            this.RemoveElementMenuItem.Name = "RemoveElementMenuItem";
            this.RemoveElementMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveElementMenuItem.Text = "Element";
            this.RemoveElementMenuItem.Click += RemoveElementClicked;

            // 
            // RemoveStateMenuItem
            // 
            this.RemoveStateMenuItem.Name = "RemoveStateMenuItem";
            this.RemoveStateMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveStateMenuItem.Text = "State";
            this.RemoveStateMenuItem.Click += RemoveStateOrCategoryClicked;

            // 
            // RemoveVariableMenuItem
            // 
            this.RemoveVariableMenuItem.Name = "RemoveVariableMenuItem";
            this.RemoveVariableMenuItem.Size = new System.Drawing.Size(117, 22);
            this.RemoveVariableMenuItem.Text = "Variable";
            this.RemoveVariableMenuItem.Click += HanldeRemoveBehaviorVariableClicked;


            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += (not, used) => GumCommands.Self.FileCommands.NewProject();

            // 
            // pluginsToolStripMenuItem
            // 
            this.pluginsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.managePluginsToolStripMenuItem});
            this.pluginsToolStripMenuItem.Name = "pluginsToolStripMenuItem";
            this.pluginsToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.pluginsToolStripMenuItem.Text = "Plugins";
            // 
            // managePluginsToolStripMenuItem
            // 
            this.managePluginsToolStripMenuItem.Name = "managePluginsToolStripMenuItem";
            this.managePluginsToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.managePluginsToolStripMenuItem.Text = "Manage Plugins";
            this.managePluginsToolStripMenuItem.Click += (not, used) =>
            {
                PluginsWindow pluginsWindow = new PluginsWindow();
                pluginsWindow.Show();
            };
            

            // 
            // findFileReferencesToolStripMenuItem
            // 
            this.findFileReferencesToolStripMenuItem.Name = "findFileReferencesToolStripMenuItem";
            this.findFileReferencesToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.findFileReferencesToolStripMenuItem.Text = "Find file references...";
            this.findFileReferencesToolStripMenuItem.Click += (not, used) =>
            {
                CommonFormsAndControls.TextInputWindow tiw = new CommonFormsAndControls.TextInputWindow();
                tiw.Message = "Enter entire or partial file name:";
                var dialogResult = tiw.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    var elements = ObjectFinder.Self.GetElementsReferencing(tiw.Result);

                    string message = "File referenced by:";

                    if (elements.Count == 0)
                    {
                        message += "\nNothing references this file";
                    }
                    else
                    {
                        foreach (var element in elements)
                        {
                            message += "\n" + element.ToString();
                        }
                    }
                    MessageBox.Show(message);
                }

            };
            

            // 
            // contentToolStripMenuItem
            // 
            this.contentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.findFileReferencesToolStripMenuItem});
            this.contentToolStripMenuItem.Name = "contentToolStripMenuItem";
            this.contentToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.contentToolStripMenuItem.Text = "Content";

            // 
            // saveAllToolStripMenuItem
            // 
            this.saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            this.saveAllToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.saveAllToolStripMenuItem.Text = "Save All";
            this.saveAllToolStripMenuItem.Click += (not, used) =>
            {
                if (ObjectFinder.Self.GumProjectSave == null)
                {
                    MessageBox.Show("There is no project loaded.  Either load a project or create a new project before saving");
                }
                else
                {
                    // Don't do an auto save, force it!
                    GumCommands.Self.FileCommands.ForceSaveProject(true);
                }
            };

            

            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += (not, used) => MessageBox.Show("Gum version " + Application.ProductVersion); 


            this.viewToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem.Text = "View";

            //Add(viewToolStripMenuItem, "Hide Tools", () =>
            //{
            //    GumCommands.Self.GuiCommands.HideTools();
            //});

            //Add(viewToolStripMenuItem, "Show Tools", () =>
            //{
            //    GumCommands.Self.GuiCommands.ShowTools();
            //});




            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";


            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            Add(fileToolStripMenuItem, "Load Project...", () => ProjectManager.Self.LoadProject());

            Add(fileToolStripMenuItem, "Save Project", () =>
            {
                if (ObjectFinder.Self.GumProjectSave == null)
                {
                    MessageBox.Show("There is no project loaded.  Either load a project or create a new project before saving");
                }
                else
                {
                    // Don't do an auto save, force it!
                    GumCommands.Self.FileCommands.ForceSaveProject();
                }
            });

            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            
            this.saveAllToolStripMenuItem,
            this.newProjectToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";


            this.menuStrip1 = new System.Windows.Forms.MenuStrip();

            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.editToolStripMenuItem,
                this.viewToolStripMenuItem,
                this.contentToolStripMenuItem,
                this.pluginsToolStripMenuItem,
                this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1076, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";

            mainWindow.Controls.Add(this.menuStrip1);
            mainWindow.MainMenuStrip = this.menuStrip1;




            RefreshUI();

        }

        private void HanldeRemoveBehaviorVariableClicked(object sender, EventArgs e)
        {
            GumCommands.Self.Edit.RemoveBehaviorVariable(
                SelectedState.Self.SelectedBehavior,
                SelectedState.Self.SelectedBehaviorVariable);
        }

        public void RefreshUI()
        {
            if (SelectedState.Self.SelectedStateSave != null && SelectedState.Self.SelectedStateSave.Name != "Default")
            {
                RemoveStateMenuItem.Text = "State " + SelectedState.Self.SelectedStateSave.Name;
                RemoveStateMenuItem.Enabled = true;
            }
            else if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                RemoveStateMenuItem.Text = "Category " + SelectedState.Self.SelectedStateCategorySave.Name;
                RemoveStateMenuItem.Enabled = true;
            }
            else
            {
                RemoveStateMenuItem.Text = "<no state selected>";
                RemoveStateMenuItem.Enabled = false;
            }

            if (SelectedState.Self.SelectedElement != null && !(SelectedState.Self.SelectedElement is StandardElementSave))
            {
                RemoveElementMenuItem.Text = SelectedState.Self.SelectedElement.Name;
                RemoveElementMenuItem.Enabled = true;
            }
            else
            {
                RemoveElementMenuItem.Text = "<no element selected>";
                RemoveElementMenuItem.Enabled = false;
            }

            if(SelectedState.Self.SelectedBehaviorVariable != null)
            {
                RemoveVariableMenuItem.Text = SelectedState.Self.SelectedBehaviorVariable.ToString();
                RemoveVariableMenuItem.Enabled = true;
            }
            else
            {
                RemoveVariableMenuItem.Text = "<no behavior variable selected>";
                RemoveVariableMenuItem.Enabled = false;
            }

        }


        private void RemoveElementClicked(object sender, EventArgs e)
        {
            EditingManager.Self.RemoveSelectedElement();
        }

        private void RemoveStateOrCategoryClicked(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedStateSave != null)
            {
                GumCommands.Self.Edit.RemoveState(
                    SelectedState.Self.SelectedStateSave, SelectedState.Self.SelectedStateContainer);
            }
            else if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                GumCommands.Self.Edit.RemoveStateCategory(
                    SelectedState.Self.SelectedStateCategorySave, SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer);
            }
        }
    }

}
