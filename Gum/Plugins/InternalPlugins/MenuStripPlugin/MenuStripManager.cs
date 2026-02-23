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

        private MenuItem _fileMenuItem;
        private MenuItem _editMenuItem;
        private MenuItem _viewMenuItem;
        private MenuItem _contentMenuItem;
        private MenuItem _helpMenuItem;

        private MenuItem _removeStateMenuItem;
        private MenuItem _removeElementMenuItem;
        private MenuItem _removeVariableMenuItem;
        private MenuItem _aboutMenuItem;
        private MenuItem _documentationMenuItem;
        private MenuItem _saveAllMenuItem;
        private MenuItem _newProjectMenuItem;
        private MenuItem _findFileReferencesMenuItem;
        private MenuItem _pluginsMenuItem;
        private MenuItem _managePluginsMenuItem;
        private MenuItem _undoMenuItem;
        private MenuItem _redoMenuItem;

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
            _menu.Items.Clear();

            // Load Recent handled in MainRecentFilesPlugin

            #region Local Functions
            MenuItem Add(MenuItem parent, string text, Action clickEvent)
            {
                var mi = new MenuItem();
                mi.Header = text;
                if (clickEvent != null)
                {
                    mi.Click += (_, _) => clickEvent();
                }
                parent.Items.Add(mi);
                return mi;
            }

            void AddSeparator(MenuItem parent)
            {
                parent.Items.Add(new Separator());
            }
            #endregion

            _removeElementMenuItem = new MenuItem();
            _removeStateMenuItem = new MenuItem();
            _removeVariableMenuItem = new MenuItem();
            _aboutMenuItem = new MenuItem();
            _documentationMenuItem = new MenuItem();
            _saveAllMenuItem = new MenuItem();
            _newProjectMenuItem = new MenuItem();
            _contentMenuItem = new MenuItem();
            _findFileReferencesMenuItem = new MenuItem();
            _pluginsMenuItem = new MenuItem();
            _managePluginsMenuItem = new MenuItem();

            _editMenuItem = new MenuItem();
            _editMenuItem.Header = "Edit";

            // InputGestureText is display-only in WPF; actual keyboard bindings
            // are handled by HotkeyManager.
            _undoMenuItem = Add(_editMenuItem, "Undo", _undoManager.PerformUndo);
            _undoMenuItem.InputGestureText = "Ctrl+Z";
            _undoMenuItem.IsEnabled = false;

            _redoMenuItem = Add(_editMenuItem, "Redo", _undoManager.PerformRedo);
            _redoMenuItem.InputGestureText = "Ctrl+Y";
            _redoMenuItem.IsEnabled = false;

            _undoManager.UndosChanged += HandleUndosChanged;

            AddSeparator(_editMenuItem);

            var addMenuItem = Add(_editMenuItem, "Add", null);
            var removeMenuItem = Add(_editMenuItem, "Remove", null);

            removeMenuItem.Items.Add(_removeElementMenuItem);
            removeMenuItem.Items.Add(_removeStateMenuItem);
            removeMenuItem.Items.Add(_removeVariableMenuItem);

            Add(addMenuItem, "Screen", () => _dialogService.Show<AddScreenDialogViewModel>());
            Add(addMenuItem, "Component", () => _dialogService.Show<AddComponentDialogViewModel>());
            Add(addMenuItem, "Instance", () => _dialogService.Show<AddInstanceDialogViewModel>());
            Add(addMenuItem, "State", () => _dialogService.Show<AddStateDialogViewModel>());

            _removeElementMenuItem.Header = "Element";
            _removeElementMenuItem.Click += RemoveElementClicked;

            _removeStateMenuItem.Header = "State";
            _removeStateMenuItem.Click += RemoveStateOrCategoryClicked;

            _removeVariableMenuItem.Header = "Variable";
            _removeVariableMenuItem.Click += HandleRemoveBehaviorVariableClicked;


            _newProjectMenuItem.Header = "New Project";
            _newProjectMenuItem.Click += (_, _) =>
            {
                _fileCommands.NewProject();
                _fileCommands.ForceSaveProject();
            };

            _pluginsMenuItem.Header = "Plugins";
            _pluginsMenuItem.Items.Add(_managePluginsMenuItem);
            _managePluginsMenuItem.Header = "Manage Plugins";
            _managePluginsMenuItem.Click += (_, _) =>
            {
                PluginsWindow pluginsWindow = new PluginsWindow();
                pluginsWindow.Show();
            };


            _findFileReferencesMenuItem.Header = "Find file references...";
            _findFileReferencesMenuItem.Click += (_, _) =>
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


            _contentMenuItem.Header = "Content";
            _contentMenuItem.Items.Add(_findFileReferencesMenuItem);

            _saveAllMenuItem.Header = "Save All";
            _saveAllMenuItem.Click += (_, _) =>
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

            _aboutMenuItem.Header = "About...";
            _aboutMenuItem.Click += (_, _) =>
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                _dialogService.ShowMessage("Gum version " + version, "About");
            };

            string documentationLink = "https://docs.flatredball.com/gum";
            _documentationMenuItem.Header = $"View Docs ({documentationLink})";
            _documentationMenuItem.ToolTip = "External link to Gum documentation";
            _documentationMenuItem.Click += (_, _) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = documentationLink,
                    UseShellExecute = true
                });
            };

            _viewMenuItem = new MenuItem();
            _viewMenuItem.Header = "View";


            _helpMenuItem = new MenuItem();
            _helpMenuItem.Header = "Help";
            _helpMenuItem.Items.Add(_aboutMenuItem);
            _helpMenuItem.Items.Add(_documentationMenuItem);


            _fileMenuItem = new MenuItem();
            _fileMenuItem.Header = "File";

            Add(_fileMenuItem, "Load Project...", () => _projectManager.LoadProject());

            Add(_fileMenuItem, "Save Project", () =>
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

            Add(_fileMenuItem, "Theming", () =>
            {
                _dialogService.Show<ThemingDialogViewModel>();
            });

            _fileMenuItem.Items.Add(_saveAllMenuItem);
            _fileMenuItem.Items.Add(_newProjectMenuItem);

            _menu.Items.Add(_fileMenuItem);
            _menu.Items.Add(_editMenuItem);
            _menu.Items.Add(_viewMenuItem);
            _menu.Items.Add(_contentMenuItem);
            _menu.Items.Add(_pluginsMenuItem);
            _menu.Items.Add(_helpMenuItem);

            RefreshUI();
        }

        private void HandleUndosChanged(object? sender, UndoOperationEventArgs e)
        {
            UpdateUndoRedoEnabled();
        }

        // UndosChanged can fire from background threads; marshal to the UI thread for WPF controls.
        private void UpdateUndoRedoEnabled()
        {
            if (_undoMenuItem?.Dispatcher.CheckAccess() == false)
            {
                _undoMenuItem.Dispatcher.BeginInvoke(UpdateUndoRedoEnabled);
                return;
            }
            if (_undoMenuItem != null) _undoMenuItem.IsEnabled = _undoManager.CanUndo();
            if (_redoMenuItem != null) _redoMenuItem.IsEnabled = _undoManager.CanRedo();
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
                _removeStateMenuItem.Header = "State " + _selectedState.SelectedStateSave.Name;
                _removeStateMenuItem.IsEnabled = true;
            }
            else if (_selectedState.SelectedStateCategorySave != null)
            {
                _removeStateMenuItem.Header = "Category " + _selectedState.SelectedStateCategorySave.Name;
                _removeStateMenuItem.IsEnabled = true;
            }
            else
            {
                _removeStateMenuItem.Header = "<no state selected>";
                _removeStateMenuItem.IsEnabled = false;
            }

            if (_selectedState.SelectedElement != null && !(_selectedState.SelectedElement is StandardElementSave))
            {
                _removeElementMenuItem.Header = _selectedState.SelectedElement.Name;
                _removeElementMenuItem.IsEnabled = true;
            }
            else
            {
                _removeElementMenuItem.Header = "<no element selected>";
                _removeElementMenuItem.IsEnabled = false;
            }

            if (_selectedState.SelectedBehaviorVariable != null)
            {
                _removeVariableMenuItem.Header = _selectedState.SelectedBehaviorVariable.ToString();
                _removeVariableMenuItem.IsEnabled = true;
            }
            else
            {
                _removeVariableMenuItem.Header = "<no behavior variable selected>";
                _removeVariableMenuItem.IsEnabled = false;
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
#if DEBUG
            if (_menu == null)
            {
                throw new InvalidOperationException("PopulateMenu must be called before AddMenuItem.");
            }
#endif

            string menuName = menuAndSubmenus.Last();

            var menuItem = new MenuItem { Header = menuName };

            string menuNameToAddTo = menuAndSubmenus.First();

            var menuToAddTo =
                _menu.Items.OfType<MenuItem>().FirstOrDefault(
                    item => item.Header as string == menuNameToAddTo);

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
#if DEBUG
            if (_menu == null)
            {
                throw new InvalidOperationException("PopulateMenu must be called before GetItem.");
            }
#endif

            foreach (var item in _menu.Items.OfType<MenuItem>())
            {
                if (item.Header as string == name)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
