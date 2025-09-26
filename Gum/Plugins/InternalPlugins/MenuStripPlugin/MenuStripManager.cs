using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Undo;
using Gum.Gui.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using FlatRedBall.Glue.Themes;
using Gum.Commands;
using Gum.Dialogs;
using Gum.ToolCommands;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Themes;

namespace Gum.Managers
{
    public class MenuStripManager : IRecipient<UiBaseFontSizeChangedMessage>, IRecipient<ThemeChangedMessage>
    {
        #region Fields

        private readonly IGuiCommands _guiCommands;
        private readonly ISelectedState _selectedState;
        private readonly IUndoManager _undoManager;
        private readonly EditCommands _editCommands;
        private readonly IDialogService _dialogService;
        private readonly IFileCommands _fileCommands;
        private readonly ProjectCommands _projectCommands;
        private readonly IMessenger _messenger;

        private MenuStrip _menuStrip;

        private ToolStripMenuItem fileToolStripMenuItem;

        private ToolStripMenuItem editToolStripMenuItem;

        private ToolStripMenuItem viewToolStripMenuItem;

        private ToolStripMenuItem contentToolStripMenuItem;

        private ToolStripMenuItem helpToolStripMenuItem;

        private ToolStripMenuItem RemoveStateMenuItem;
        private ToolStripMenuItem RemoveElementMenuItem;
        private ToolStripMenuItem RemoveVariableMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem documentationToolStripMenuItem;
        private ToolStripMenuItem saveAllToolStripMenuItem;
        private ToolStripMenuItem newProjectToolStripMenuItem;
        private ToolStripMenuItem findFileReferencesToolStripMenuItem;
        private ToolStripMenuItem pluginsToolStripMenuItem;
        private ToolStripMenuItem managePluginsToolStripMenuItem;


        #endregion

        public MenuStripManager(IGuiCommands guiCommands,
            ISelectedState selectedState,
            IUndoManager undoManager,
            EditCommands editCommands,
            IDialogService dialogService,
            IFileCommands fileCommands,
            ProjectCommands projectCommands,
            IMessenger messenger)
        {
            _guiCommands = guiCommands;
            _selectedState = selectedState;
            _undoManager = undoManager;
            _editCommands = editCommands;
            _dialogService = dialogService;
            _fileCommands = fileCommands;
            _projectCommands = projectCommands;
            _messenger = messenger;
            messenger.RegisterAll(this);
        }

        public MenuStrip CreateMenuStrip()
        {
            // Load Recent handled in MainRecentFilesPlugin

            #region Local Functions
            ToolStripMenuItem Add(ToolStripMenuItem parent, string text, Action clickEvent)
            {
                var tsmi = new ToolStripMenuItem();
                tsmi.Text = text;
                if (clickEvent != null)
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
            this.documentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findFileReferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managePluginsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.editToolStripMenuItem = new ToolStripMenuItem();
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Text = "Edit";

            var undoMenuItem = Add(editToolStripMenuItem, "Undo", _undoManager.PerformUndo);
            undoMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));

            var redoMenuItem = Add(editToolStripMenuItem, "Redo", _undoManager.PerformRedo);
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


            Add(addToolStripMenuItem, "Screen", () => _dialogService.Show<AddScreenDialogViewModel>());
            Add(addToolStripMenuItem, "Component", () => _dialogService.Show<AddComponentDialogViewModel>());
            Add(addToolStripMenuItem, "Instance", () => _dialogService.Show<AddInstanceDialogViewModel>());
            Add(addToolStripMenuItem, "State", () => _dialogService.Show<AddStateDialogViewModel>());

            // 
            // RemoveElementMenuItem
            // 
            this.RemoveElementMenuItem.Name = "RemoveElementMenuItem";
            this.RemoveElementMenuItem.Text = "Element";
            this.RemoveElementMenuItem.Click += RemoveElementClicked;

            // 
            // RemoveStateMenuItem
            // 
            this.RemoveStateMenuItem.Name = "RemoveStateMenuItem";
            this.RemoveStateMenuItem.Text = "State";
            this.RemoveStateMenuItem.Click += RemoveStateOrCategoryClicked;

            // 
            // RemoveVariableMenuItem
            // 
            this.RemoveVariableMenuItem.Name = "RemoveVariableMenuItem";
            this.RemoveVariableMenuItem.Text = "Variable";
            this.RemoveVariableMenuItem.Click += HanldeRemoveBehaviorVariableClicked;


            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += (not, used) =>
            {
                _fileCommands.NewProject();
                _fileCommands.ForceSaveProject();
            };

            // 
            // pluginsToolStripMenuItem
            // 
            this.pluginsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.managePluginsToolStripMenuItem});
            this.pluginsToolStripMenuItem.Name = "pluginsToolStripMenuItem";
            this.pluginsToolStripMenuItem.Text = "Plugins";
            // 
            // managePluginsToolStripMenuItem
            // 
            this.managePluginsToolStripMenuItem.Name = "managePluginsToolStripMenuItem";
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
            this.findFileReferencesToolStripMenuItem.Text = "Find file references...";
            this.findFileReferencesToolStripMenuItem.Click += (not, used) =>
            {
                string message = "Enter entire or partial file name:";
                string title = "Find file references";

                if (_dialogService.GetUserString(message, title) is { } result)
                {
                    var elements = ObjectFinder.Self.GetElementsReferencing(result);

                    message = "File referenced by:";

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
                    _dialogService.ShowMessage(message);
                }

            };


            // 
            // contentToolStripMenuItem
            // 
            this.contentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.findFileReferencesToolStripMenuItem});
            this.contentToolStripMenuItem.Name = "contentToolStripMenuItem";
            this.contentToolStripMenuItem.Text = "Content";

            // 
            // saveAllToolStripMenuItem
            // 
            this.saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            this.saveAllToolStripMenuItem.Text = "Save All";
            this.saveAllToolStripMenuItem.Click += (not, used) =>
            {
                if (ObjectFinder.Self.GumProjectSave == null)
                {
                    _dialogService.ShowMessage("There is no project loaded.  Either load a project or create a new project before saving");
                }
                else
                {
                    // Don't do an auto save, force it!
                    _fileCommands.ForceSaveProject(true);
                }
            };

            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += (not, used) => _dialogService.ShowMessage("Gum version " + Application.ProductVersion, "About");

            string documentationLink = "https://docs.flatredball.com/gum";
            this.documentationToolStripMenuItem.Name = "documentationToolStripMenuItem";
            this.documentationToolStripMenuItem.Text = $"View Docs ({documentationLink})";
            this.documentationToolStripMenuItem.ToolTipText = "External link to Gum documentation";
            this.documentationToolStripMenuItem.Click += (not, used) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = documentationLink,
                    UseShellExecute = true
                });
            };

            this.viewToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem.Text = "View";

            //Add(viewToolStripMenuItem, "Hide Tools", () =>
            //{
            //    _guiCommands.HideTools();
            //});

            //Add(viewToolStripMenuItem, "Show Tools", () =>
            //{
            //    _guiCommands.ShowTools();
            //});




            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.aboutToolStripMenuItem, this.documentationToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Text = "Help";


            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            Add(fileToolStripMenuItem, "Load Project...", () => ProjectManager.Self.LoadProject());

            Add(fileToolStripMenuItem, "Save Project", () =>
            {
                if (ObjectFinder.Self.GumProjectSave == null)
                {
                    _dialogService.ShowMessage("There is no project loaded.  Either load a project or create a new project before saving");
                }
                else
                {
                    // Don't do an auto save, force it!
                    _fileCommands.ForceSaveProject();
                }
            });

            Add(fileToolStripMenuItem, "Theming", () =>
            {
                Locator.GetRequiredService<IDialogService>().Show<ThemingDialogViewModel>();
            });

            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {

            this.saveAllToolStripMenuItem,
            this.newProjectToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Text = "File";


            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._menuStrip.Renderer = GetCurrentThemeRenderer();

            // 
            // menuStrip1
            // 
            this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.editToolStripMenuItem,
                this.viewToolStripMenuItem,
                this.contentToolStripMenuItem,
                this.pluginsToolStripMenuItem,
                this.helpToolStripMenuItem});
            this._menuStrip.Location = new System.Drawing.Point(0, 0);
            this._menuStrip.Name = "menuStrip1";
            this._menuStrip.TabIndex = 0;
            this._menuStrip.Text = "menuStrip1";



            RefreshUI();
            _menuStrip.Font = new Font("Verdana", DefaultFontSize);
            return this._menuStrip;
        }

        private FrbMenuStripRenderer? GetCurrentThemeRenderer(float? fontSize = null)
        {
            if (System.Windows.Application.Current is { } app &&
                app.TryFindResource("Frb.Colors.Background") is System.Windows.Media.Color bgColor &&
                app.TryFindResource("Frb.Colors.Foreground") is System.Windows.Media.Color fgColor &&
                app.TryFindResource("Frb.Colors.Primary") is System.Windows.Media.Color primaryColor)
            {
                System.Drawing.Color bg = System.Drawing.Color.FromArgb(bgColor.A, bgColor.R, bgColor.G, bgColor.B);
                System.Drawing.Color fg = System.Drawing.Color.FromArgb(fgColor.A, fgColor.R, fgColor.G, fgColor.B);
                System.Drawing.Color primary = System.Drawing.Color.FromArgb(primaryColor.A, primaryColor.R, primaryColor.G, primaryColor.B);

                Font font = new("Verdana", fontSize ?? DefaultFontSize, FontStyle.Regular);
                _menuStrip.ForeColor = fg;
                _menuStrip.BackColor = bg;
                _menuStrip.Font = font;

                return new(bg, fg, primary, font);
            }



            return null;
        }

        private void HanldeRemoveBehaviorVariableClicked(object sender, EventArgs e)
        {
            _editCommands.RemoveBehaviorVariable(
                _selectedState.SelectedBehavior,
                _selectedState.SelectedBehaviorVariable);
        }

        public void RefreshUI()
        {
            if (_selectedState.SelectedStateSave != null && _selectedState.SelectedStateSave.Name != "Default")
            {
                RemoveStateMenuItem.Text = "State " + _selectedState.SelectedStateSave.Name;
                RemoveStateMenuItem.Enabled = true;
            }
            else if (_selectedState.SelectedStateCategorySave != null)
            {
                RemoveStateMenuItem.Text = "Category " + _selectedState.SelectedStateCategorySave.Name;
                RemoveStateMenuItem.Enabled = true;
            }
            else
            {
                RemoveStateMenuItem.Text = "<no state selected>";
                RemoveStateMenuItem.Enabled = false;
            }

            if (_selectedState.SelectedElement != null && !(_selectedState.SelectedElement is StandardElementSave))
            {
                RemoveElementMenuItem.Text = _selectedState.SelectedElement.Name;
                RemoveElementMenuItem.Enabled = true;
            }
            else
            {
                RemoveElementMenuItem.Text = "<no element selected>";
                RemoveElementMenuItem.Enabled = false;
            }

            if (_selectedState.SelectedBehaviorVariable != null)
            {
                RemoveVariableMenuItem.Text = _selectedState.SelectedBehaviorVariable.ToString();
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
            _projectCommands.RemoveElement(_selectedState.SelectedElement);
            _selectedState.SelectedElement = null;
        }

        private void RemoveStateOrCategoryClicked(object sender, EventArgs e)
        {
            if (_selectedState.SelectedStateSave != null)
            {
                _editCommands.AskToDeleteState(
                    _selectedState.SelectedStateSave, _selectedState.SelectedStateContainer);
            }
            else if (_selectedState.SelectedStateCategorySave != null)
            {
                _editCommands.RemoveStateCategory(
                    _selectedState.SelectedStateCategorySave, _selectedState.SelectedStateContainer);
            }
        }

        const int DefaultFontSize = 9;


        void IRecipient<UiBaseFontSizeChangedMessage>.Receive(UiBaseFontSizeChangedMessage message)
        {
            float fontSize = (DefaultFontSize / 12f) * (float)message.Size;

            _menuStrip.Font = new System.Drawing.Font(_menuStrip.Font.FontFamily, fontSize);
            _menuStrip.Renderer = GetCurrentThemeRenderer(fontSize);
            _menuStrip.Invalidate();
        }

        public ToolStripMenuItem AddMenuItem(IEnumerable<string> menuAndSubmenus)
        {
            string menuName = menuAndSubmenus.Last();

            ToolStripMenuItem menuItem = new ToolStripMenuItem(menuName);

            string menuNameToAddTo = menuAndSubmenus.First();

            var menuToAddTo =
                _menuStrip.Items.Cast<ToolStripMenuItem>().FirstOrDefault(
                    item => item.Text == menuNameToAddTo);
            //true);

            if (menuToAddTo == null)
            {
                menuToAddTo = new ToolStripMenuItem(menuNameToAddTo);

                // Don't call Add - this will put the menu item after the "Help" menu item, which should be last
                //MenuStrip.Items.Add(menuToAddTo);

                int indexToInsertAt = _menuStrip.Items.Count - 1;
                _menuStrip.Items.Insert(indexToInsertAt, menuToAddTo);
            }


            menuToAddTo.DropDownItems.Add(menuItem);
            return menuItem;

        }

        public ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in _menuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
        {
            _menuStrip.Renderer = GetCurrentThemeRenderer();
            _menuStrip.Invalidate();
        }
    }
}
