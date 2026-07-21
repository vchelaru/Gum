using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Undo;
using System.Diagnostics;
using System.Linq;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Messages;
using Gum.Services.Dialogs;
using CommunityToolkit.Mvvm.Messaging;

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
        private readonly IProjectManager _projectManager;
        private readonly IMessenger _messenger;
        private readonly MenuStripStateLogic _menuStripStateLogic;

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
        private MenuItem _thirdPartyLicensesMenuItem;
        private MenuItem _documentationMenuItem;
        private MenuItem _saveAllMenuItem;
        private MenuItem _newProjectMenuItem;
        private MenuItem _findFileReferencesMenuItem;
        private MenuItem _pluginsMenuItem;
        private MenuItem _managePluginsMenuItem;
        private MenuItem _undoMenuItem;
        private MenuItem _redoMenuItem;
        private MenuItem _standardsPaletteMenuItem;

        #endregion

        public MenuStripManager(
            ISelectedState selectedState,
            IUndoManager undoManager,
            IEditCommands editCommands,
            IDialogService dialogService,
            IFileCommands fileCommands,
            IProjectManager projectManager,
            IMessenger messenger)
        {
            _selectedState = selectedState;
            _undoManager = undoManager;
            _editCommands = editCommands;
            _dialogService = dialogService;
            _fileCommands = fileCommands;
            _projectManager = projectManager;
            _messenger = messenger;
            _menuStripStateLogic = new MenuStripStateLogic(selectedState, projectManager);
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
            _thirdPartyLicensesMenuItem = new MenuItem();
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
                _dialogService.Show<PluginsDialogViewModel>();
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
            _saveAllMenuItem.Click += (_, _) => SaveProject(saveAll: true);

            _aboutMenuItem.Header = "About...";
            _aboutMenuItem.Click += (_, _) =>
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                _dialogService.ShowMessage("Gum version " + version, "About");
            };

            const string thirdPartyNoticesUrl =
                "https://github.com/vchelaru/Gum/blob/main/THIRD-PARTY-NOTICES.txt";
            _thirdPartyLicensesMenuItem.Header = "Third-Party Licenses...";
            _thirdPartyLicensesMenuItem.ToolTip = "Licenses and attributions for third-party components Gum redistributes";
            _thirdPartyLicensesMenuItem.Click += (_, _) =>
            {
                // The notices file ships next to the executable (see Gum.csproj). Fall back to
                // the copy on GitHub if it can't be found locally.
                string localPath = System.IO.Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
                string target = System.IO.File.Exists(localPath) ? localPath : thirdPartyNoticesUrl;
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
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
            _helpMenuItem.Items.Add(_thirdPartyLicensesMenuItem);
            _helpMenuItem.Items.Add(_documentationMenuItem);


            _fileMenuItem = new MenuItem();
            _fileMenuItem.Header = "File";

            _fileMenuItem.Items.Add(_newProjectMenuItem);
            Add(_fileMenuItem, "Load Project...", () => _projectManager.LoadProject());
            // Load Recent is inserted at index 2 by MainRecentFilesPlugin

            AddSeparator(_fileMenuItem);

            Add(_fileMenuItem, "Save Project", () => SaveProject(saveAll: false));
            _fileMenuItem.Items.Add(_saveAllMenuItem);

            AddSeparator(_fileMenuItem);

            Add(_fileMenuItem, "Export", null);

            _menu.Items.Add(_fileMenuItem);
            _menu.Items.Add(_editMenuItem);

            Add(_viewMenuItem, "Theming", () =>
            {
                _dialogService.Show<ThemingDialogViewModel>();
            });

            // Experimental: replace the Standard tree folder with a chip palette at the bottom of the
            // Project panel. Opt-in while experimental; persisted in the global settings file. The
            // settings file is loaded after PopulateMenu, so read it defensively here; RefreshUI() syncs
            // the checkmark once settings are available.
            _standardsPaletteMenuItem = new MenuItem
            {
                Header = "Standards palette (experimental)",
                IsCheckable = true,
                IsChecked = _projectManager.EffectiveUseStandardsPalette
            };
            // WPF toggles IsChecked before Click fires for a checkable item.
            _standardsPaletteMenuItem.Click += (_, _) =>
            {
                _projectManager.UseStandardsPalette = _standardsPaletteMenuItem.IsChecked;
                _projectManager.SaveGeneralSettings();
                _messenger.Send(new StandardsPaletteSettingChangedMessage(_projectManager.EffectiveUseStandardsPalette));
            };
            _viewMenuItem.Items.Add(_standardsPaletteMenuItem);

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

        private void SaveProject(bool saveAll)
        {
            if (ObjectFinder.Self.GumProjectSave == null)
            {
                _dialogService.ShowMessage("There is no project loaded.  Either load a project or create a new project before saving");
            }
            else
            {
                // Don't do an auto save, force it!
                _fileCommands.ForceSaveProject(saveAll);
            }
        }

        public void RefreshUI()
        {
            MenuStripRefreshState state = _menuStripStateLogic.GetRefreshState();

            // The settings file loads after PopulateMenu, so keep the checkmark in sync here.
            if (_standardsPaletteMenuItem != null)
            {
                _standardsPaletteMenuItem.IsChecked = state.StandardsPaletteChecked;
            }

            _removeStateMenuItem.Header = state.RemoveStateHeader;
            _removeStateMenuItem.IsEnabled = state.RemoveStateEnabled;

            _removeElementMenuItem.Header = state.RemoveElementHeader;
            _removeElementMenuItem.IsEnabled = state.RemoveElementEnabled;

            _removeVariableMenuItem.Header = state.RemoveVariableHeader;
            _removeVariableMenuItem.IsEnabled = state.RemoveVariableEnabled;
        }


        private void RemoveElementClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            _editCommands.DeleteSelection();
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
                _editCommands.AskToDeleteStateCategory(
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

            var parts = menuAndSubmenus.ToList();
            string menuName = parts.Last();

            var menuItem = new MenuItem { Header = menuName };

            string topMenuName = parts.First();

            var currentParent =
                _menu.Items.OfType<MenuItem>().FirstOrDefault(
                    item => item.Header as string == topMenuName);

            if (currentParent == null)
            {
                currentParent = new MenuItem { Header = topMenuName };

                // Don't call Add - this will put the menu item after the "Help" menu item, which should be last
                int indexToInsertAt = _menu.Items.Count - 1;
                _menu.Items.Insert(indexToInsertAt, currentParent);
            }

            // Walk through intermediate submenus (e.g., "File" > "Export" > "Export as Image")
            for (int i = 1; i < parts.Count - 1; i++)
            {
                var submenu = currentParent.Items.OfType<MenuItem>()
                    .FirstOrDefault(item => item.Header as string == parts[i]);

                if (submenu == null)
                {
                    submenu = new MenuItem { Header = parts[i] };
                    currentParent.Items.Add(submenu);
                }

                currentParent = submenu;
            }

            currentParent.Items.Add(menuItem);

            ApplyLayout(topMenuName);

            return menuItem;
        }

        // Stable per-menu layout. Items in each inner list form a group; separators are
        // inserted between groups. Items not listed here fall through to a trailing group
        // (separator + items in insertion order) so third-party plugins remain visible.
        private static readonly Dictionary<string, string[][]> _menuLayouts = new()
        {
            ["Content"] = new[]
            {
                new[] { "Find file references..." },
                new[] { "Add Forms Components", "Import from .gumx..." },
                new[]
                {
                    "Clear Font Cache",
                    "Re-create missing font files",
                    "Force re-create all font files",
                    "View Font Cache",
                },
            },
        };

        /// <summary>
        /// Re-orders the children of the given top-level menu according to the layout
        /// table and re-inserts group separators. Safe to call repeatedly; no-op if the
        /// menu name has no layout entry. Call this after any direct manipulation of
        /// a managed menu's <c>Items</c> collection (e.g. removing an item) so that
        /// the declared group order is restored.
        /// </summary>
        public void ApplyLayout(string topMenuName)
        {
            if (!_menuLayouts.TryGetValue(topMenuName, out var groups))
            {
                return;
            }

            var parent = _menu.Items.OfType<MenuItem>()
                .FirstOrDefault(item => item.Header as string == topMenuName);
            if (parent == null)
            {
                return;
            }

            var existing = parent.Items.OfType<MenuItem>().ToList();
            var byHeader = existing.ToDictionary(mi => mi.Header as string ?? "", mi => mi);

            parent.Items.Clear();

            var placed = new HashSet<MenuItem>();
            bool anyEmitted = false;
            foreach (var group in groups)
            {
                var groupItems = new List<MenuItem>();
                foreach (var header in group)
                {
                    if (byHeader.TryGetValue(header, out var mi))
                    {
                        groupItems.Add(mi);
                        placed.Add(mi);
                    }
                }
                if (groupItems.Count == 0)
                {
                    continue;
                }
                if (anyEmitted)
                {
                    parent.Items.Add(new Separator());
                }
                foreach (var mi in groupItems)
                {
                    parent.Items.Add(mi);
                }
                anyEmitted = true;
            }

            var leftovers = existing.Where(mi => !placed.Contains(mi)).ToList();
            if (leftovers.Count > 0)
            {
                if (anyEmitted)
                {
                    parent.Items.Add(new Separator());
                }
                foreach (var mi in leftovers)
                {
                    parent.Items.Add(mi);
                }
            }
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
