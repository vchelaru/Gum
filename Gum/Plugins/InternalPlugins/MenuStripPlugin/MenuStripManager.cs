using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Undo;
using Gum.Gui.Forms;
using System.Diagnostics;
using System.Linq;
using Gum.Commands;
using Gum.Dialogs;
using Gum.ToolCommands;
using Gum.Services.Dialogs;

namespace Gum.Managers
{
    public class MenuStripManager
    {
        #region Fields

        private readonly ISelectedState _selectedState;
        private readonly IUndoManager _undoManager;
        private readonly IEditCommands _editCommands;
        private readonly IDialogService _dialogService;
        private readonly IFileCommands _fileCommands;
        private readonly ProjectCommands _projectCommands;
        private readonly IProjectManager _projectManager;

        private Menu _menu;

        private MenuItem fileMenuItem;
        private MenuItem editMenuItem;
        private MenuItem viewMenuItem;
        private MenuItem contentMenuItem;
        private MenuItem helpMenuItem;

        private MenuItem RemoveStateMenuItem;
        private MenuItem RemoveElementMenuItem;
        private MenuItem RemoveVariableMenuItem;
        private MenuItem aboutMenuItem;
        private MenuItem documentationMenuItem;
        private MenuItem saveAllMenuItem;
        private MenuItem newProjectMenuItem;
        private MenuItem findFileReferencesMenuItem;
        private MenuItem pluginsMenuItem;
        private MenuItem managePluginsMenuItem;
        private MenuItem undoMenuItem;
        private MenuItem redoMenuItem;

        #endregion

        public MenuStripManager(
            ISelectedState selectedState,
            IUndoManager undoManager,
            IEditCommands editCommands,
            IDialogService dialogService,
            IFileCommands fileCommands,
            ProjectCommands projectCommands,
            IProjectManager projectManager)
        {
            _selectedState = selectedState;
            _undoManager = undoManager;
            _editCommands = editCommands;
            _dialogService = dialogService;
            _fileCommands = fileCommands;
            _projectCommands = projectCommands;
            _projectManager = projectManager;
        }

        public void PopulateMenu(Menu menu)
        {
            _menu = menu;

            // Load Recent handled in MainRecentFilesPlugin

            #region Local Functions
            MenuItem Add(MenuItem parent, string text, Action clickEvent)
            {
                var mi = new MenuItem();
                mi.Header = text;
                if (clickEvent != null)
                {
                    mi.Click += (not, used) => clickEvent();
                }
                parent.Items.Add(mi);
                return mi;
            }

            void AddSeparator(MenuItem parent)
            {
                parent.Items.Add(new Separator());
            }
            #endregion

            this.RemoveElementMenuItem = new MenuItem();
            this.RemoveStateMenuItem = new MenuItem();
            this.RemoveVariableMenuItem = new MenuItem();
            this.aboutMenuItem = new MenuItem();
            this.documentationMenuItem = new MenuItem();
            this.saveAllMenuItem = new MenuItem();
            this.newProjectMenuItem = new MenuItem();
            this.contentMenuItem = new MenuItem();
            this.findFileReferencesMenuItem = new MenuItem();
            this.pluginsMenuItem = new MenuItem();
            this.managePluginsMenuItem = new MenuItem();

            this.editMenuItem = new MenuItem();
            this.editMenuItem.Header = "Edit";

            undoMenuItem = Add(editMenuItem, "Undo", _undoManager.PerformUndo);
            undoMenuItem.InputGestureText = "Ctrl+Z";
            undoMenuItem.IsEnabled = false;

            redoMenuItem = Add(editMenuItem, "Redo", _undoManager.PerformRedo);
            redoMenuItem.InputGestureText = "Ctrl+Y";
            redoMenuItem.IsEnabled = false;

            _undoManager.UndosChanged += (_, __) => UpdateUndoRedoEnabled();

            AddSeparator(editMenuItem);

            var addMenuItem = Add(editMenuItem, "Add", null);
            var removeMenuItem = Add(editMenuItem, "Remove", null);

            removeMenuItem.Items.Add(this.RemoveElementMenuItem);
            removeMenuItem.Items.Add(this.RemoveStateMenuItem);
            removeMenuItem.Items.Add(this.RemoveVariableMenuItem);

            Add(addMenuItem, "Screen", () => _dialogService.Show<AddScreenDialogViewModel>());
            Add(addMenuItem, "Component", () => _dialogService.Show<AddComponentDialogViewModel>());
            Add(addMenuItem, "Instance", () => _dialogService.Show<AddInstanceDialogViewModel>());
            Add(addMenuItem, "State", () => _dialogService.Show<AddStateDialogViewModel>());

            //
            // RemoveElementMenuItem
            //
            this.RemoveElementMenuItem.Header = "Element";
            this.RemoveElementMenuItem.Click += RemoveElementClicked;

            //
            // RemoveStateMenuItem
            //
            this.RemoveStateMenuItem.Header = "State";
            this.RemoveStateMenuItem.Click += RemoveStateOrCategoryClicked;

            //
            // RemoveVariableMenuItem
            //
            this.RemoveVariableMenuItem.Header = "Variable";
            this.RemoveVariableMenuItem.Click += HandleRemoveBehaviorVariableClicked;


            //
            // newProjectMenuItem
            //
            this.newProjectMenuItem.Header = "New Project";
            this.newProjectMenuItem.Click += (not, used) =>
            {
                _fileCommands.NewProject();
                _fileCommands.ForceSaveProject();
            };

            //
            // pluginsMenuItem
            //
            this.pluginsMenuItem.Items.Add(this.managePluginsMenuItem);
            this.pluginsMenuItem.Header = "Plugins";
            //
            // managePluginsMenuItem
            //
            this.managePluginsMenuItem.Header = "Manage Plugins";
            this.managePluginsMenuItem.Click += (not, used) =>
            {
                PluginsWindow pluginsWindow = new PluginsWindow();
                pluginsWindow.Show();
            };


            //
            // findFileReferencesMenuItem
            //
            this.findFileReferencesMenuItem.Header = "Find file references...";
            this.findFileReferencesMenuItem.Click += (not, used) =>
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
            // contentMenuItem
            //
            this.contentMenuItem.Items.Add(this.findFileReferencesMenuItem);
            this.contentMenuItem.Header = "Content";

            //
            // saveAllMenuItem
            //
            this.saveAllMenuItem.Header = "Save All";
            this.saveAllMenuItem.Click += (not, used) =>
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
            // aboutMenuItem
            //
            this.aboutMenuItem.Header = "About...";
            this.aboutMenuItem.Click += (not, used) =>
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                _dialogService.ShowMessage("Gum version " + version, "About");
            };

            string documentationLink = "https://docs.flatredball.com/gum";
            this.documentationMenuItem.Header = $"View Docs ({documentationLink})";
            this.documentationMenuItem.ToolTip = "External link to Gum documentation";
            this.documentationMenuItem.Click += (not, used) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = documentationLink,
                    UseShellExecute = true
                });
            };

            this.viewMenuItem = new MenuItem();
            viewMenuItem.Header = "View";


            this.helpMenuItem = new MenuItem();
            this.helpMenuItem.Items.Add(this.aboutMenuItem);
            this.helpMenuItem.Items.Add(this.documentationMenuItem);
            this.helpMenuItem.Header = "Help";


            this.fileMenuItem = new MenuItem();

            Add(fileMenuItem, "Load Project...", () => _projectManager.LoadProject());

            Add(fileMenuItem, "Save Project", () =>
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

            Add(fileMenuItem, "Theming", () =>
            {
                _dialogService.Show<ThemingDialogViewModel>();
            });

            //
            // fileMenuItem
            //
            this.fileMenuItem.Items.Add(this.saveAllMenuItem);
            this.fileMenuItem.Items.Add(this.newProjectMenuItem);
            this.fileMenuItem.Header = "File";

            _menu.Items.Add(this.fileMenuItem);
            _menu.Items.Add(this.editMenuItem);
            _menu.Items.Add(this.viewMenuItem);
            _menu.Items.Add(this.contentMenuItem);
            _menu.Items.Add(this.pluginsMenuItem);
            _menu.Items.Add(this.helpMenuItem);

            RefreshUI();
        }

        private void UpdateUndoRedoEnabled()
        {
            if (undoMenuItem != null) undoMenuItem.IsEnabled = _undoManager.CanUndo();
            if (redoMenuItem != null) redoMenuItem.IsEnabled = _undoManager.CanRedo();
        }

        private void HandleRemoveBehaviorVariableClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            if(_selectedState.SelectedBehavior != null && _selectedState.SelectedBehaviorVariable != null)
            {
                _editCommands.RemoveBehaviorVariable(
                    _selectedState.SelectedBehavior,
                    _selectedState.SelectedBehaviorVariable);
            }
        }

        public void RefreshUI()
        {
            if (_selectedState.SelectedStateSave != null && _selectedState.SelectedStateSave.Name != "Default")
            {
                RemoveStateMenuItem.Header = "State " + _selectedState.SelectedStateSave.Name;
                RemoveStateMenuItem.IsEnabled = true;
            }
            else if (_selectedState.SelectedStateCategorySave != null)
            {
                RemoveStateMenuItem.Header = "Category " + _selectedState.SelectedStateCategorySave.Name;
                RemoveStateMenuItem.IsEnabled = true;
            }
            else
            {
                RemoveStateMenuItem.Header = "<no state selected>";
                RemoveStateMenuItem.IsEnabled = false;
            }

            if (_selectedState.SelectedElement != null && !(_selectedState.SelectedElement is StandardElementSave))
            {
                RemoveElementMenuItem.Header = _selectedState.SelectedElement.Name;
                RemoveElementMenuItem.IsEnabled = true;
            }
            else
            {
                RemoveElementMenuItem.Header = "<no element selected>";
                RemoveElementMenuItem.IsEnabled = false;
            }

            if (_selectedState.SelectedBehaviorVariable != null)
            {
                RemoveVariableMenuItem.Header = _selectedState.SelectedBehaviorVariable.ToString();
                RemoveVariableMenuItem.IsEnabled = true;
            }
            else
            {
                RemoveVariableMenuItem.Header = "<no behavior variable selected>";
                RemoveVariableMenuItem.IsEnabled = false;
            }

        }


        private void RemoveElementClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            _projectCommands.RemoveElement(_selectedState.SelectedElement);
            _selectedState.SelectedElement = null;
        }

        private void RemoveStateOrCategoryClicked(object? sender, System.Windows.RoutedEventArgs e)
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

        public MenuItem AddMenuItem(IEnumerable<string> menuAndSubmenus)
        {
            string menuName = menuAndSubmenus.Last();

            var menuItem = new MenuItem { Header = menuName };

            string menuNameToAddTo = menuAndSubmenus.First();

            var menuToAddTo =
                _menu.Items.OfType<MenuItem>().FirstOrDefault(
                    item => (string)item.Header == menuNameToAddTo);

            if (menuToAddTo == null)
            {
                menuToAddTo = new MenuItem { Header = menuNameToAddTo };

                // Don't call Add - this will put the menu item after the "Help" menu item, which should be last
                int indexToInsertAt = _menu.Items.Count - 1;
                _menu.Items.Insert(indexToInsertAt, menuToAddTo);
            }


            menuToAddTo.Items.Add(menuItem);
            return menuItem;

        }

        public MenuItem GetItem(string name)
        {
            foreach (var item in _menu.Items.OfType<MenuItem>())
            {
                if ((string)item.Header == name)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
