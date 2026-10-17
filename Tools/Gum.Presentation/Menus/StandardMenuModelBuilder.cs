using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Gum.Undo;
using ToolsUtilities;

namespace Gum.Menus;

/// <summary>
/// Builds the tool's standard menus (File, Edit, View, Content, Help) into a
/// <see cref="MenuModel"/> and keeps the selection-dependent items current through
/// <see cref="RefreshUI"/>. Plugins add their own items on top through the model.
/// </summary>
public class StandardMenuModelBuilder
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IEditCommands _editCommands;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectManager _projectManager;
    private readonly IMessenger _messenger;
    private readonly IFileSystemRevealService _fileSystemRevealService;
    private readonly IDispatcher _dispatcher;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly MenuStripStateLogic _menuStripStateLogic;

    private MenuItemModel? _undoMenuItem;
    private MenuItemModel? _redoMenuItem;
    private MenuItemModel? _removeElementMenuItem;
    private MenuItemModel? _removeStateMenuItem;
    private MenuItemModel? _removeVariableMenuItem;
    private MenuStepper? _zoomStepper;

    /// <summary>Creates the builder over the services the standard items act on.</summary>
    public StandardMenuModelBuilder(
        ISelectedState selectedState,
        IUndoManager undoManager,
        IEditCommands editCommands,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IProjectManager projectManager,
        IMessenger messenger,
        IFileSystemRevealService fileSystemRevealService,
        IDispatcher dispatcher,
        IHotkeyManager hotkeyManager,
        IUiSettingsService uiSettingsService)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _editCommands = editCommands;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _projectManager = projectManager;
        _messenger = messenger;
        _fileSystemRevealService = fileSystemRevealService;
        _dispatcher = dispatcher;
        _hotkeyManager = hotkeyManager;
        _uiSettingsService = uiSettingsService;
        // The hotkeys and the Theming dialog change the same size, so the row follows the setting.
        _messenger.Register<UiBaseFontSizeChangedMessage>(this, (_, _) => RefreshZoomText());
        _menuStripStateLogic = new MenuStripStateLogic(selectedState);
    }

    /// <summary>The model built by <see cref="Build"/>.</summary>
    public MenuModel Model { get; } = new MenuModel();

    private void RefreshZoomText()
    {
        if (_zoomStepper != null)
        {
            double percent = Math.Round(_uiSettingsService.BaseFontSize / UiSettingsService.DefaultBaseFontSize * 100);
            _zoomStepper.ValueText = percent.ToString("0", CultureInfo.InvariantCulture) + "%";
        }
    }

    /// <summary>Populates <see cref="Model"/>. Load Recent is inserted by the recent-files plugin.</summary>
    public MenuModel Build()
    {
        Model.TopLevelItems.Clear();

        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("New Project", async () =>
        {
            try
            {
                await _fileCommands.NewProjectAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error creating new project:\n{ex.Message}");
            }
        }));
        file.Items.Add(new MenuItemModel("Load Project...", async () =>
        {
            try
            {
                await _projectManager.LoadProjectAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error loading project:\n{ex.Message}");
            }
        }));
        file.Items.Add(new MenuItemModel("Close Project", async () =>
        {
            try
            {
                await _fileCommands.CloseProjectAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error closing project:\n{ex.Message}");
            }
        }));
        file.Items.Add(MenuItemModel.Separator());
        file.Items.Add(new MenuItemModel("Save Project", () => SaveProject(saveAll: false)));
        file.Items.Add(new MenuItemModel("Save All", () => SaveProject(saveAll: true)));
        file.Items.Add(MenuItemModel.Separator());
        file.Items.Add(new MenuItemModel("Export"));

        MenuItemModel edit = new MenuItemModel("Edit");
        _undoMenuItem = new MenuItemModel("Undo", _undoManager.PerformUndo) { Gesture = _hotkeyManager.Undo, IsEnabled = false };
        _redoMenuItem = new MenuItemModel("Redo", _undoManager.PerformRedo) { Gesture = _hotkeyManager.Redo, IsEnabled = false };
        edit.Items.Add(_undoMenuItem);
        edit.Items.Add(_redoMenuItem);
        _undoManager.UndosChanged += (_, _) => _dispatcher.Post(UpdateUndoRedoEnabled);
        edit.Items.Add(MenuItemModel.Separator());

        MenuItemModel add = new MenuItemModel("Add");
        add.Items.Add(new MenuItemModel("Screen", _editCommands.ShowAddScreenDialog));
        add.Items.Add(new MenuItemModel("Component", _editCommands.ShowAddComponentDialog));
        add.Items.Add(new MenuItemModel("Instance", () => _dialogService.Show<AddInstanceDialogViewModel>()));
        add.Items.Add(new MenuItemModel("State", () => _dialogService.Show<AddStateDialogViewModel>()));
        edit.Items.Add(add);

        MenuItemModel remove = new MenuItemModel("Remove");
        _removeElementMenuItem = new MenuItemModel("Element", () => _editCommands.DeleteSelection());
        _removeStateMenuItem = new MenuItemModel("State", RemoveStateOrCategory);
        _removeVariableMenuItem = new MenuItemModel("Variable", RemoveBehaviorVariable);
        remove.Items.Add(_removeElementMenuItem);
        remove.Items.Add(_removeStateMenuItem);
        remove.Items.Add(_removeVariableMenuItem);
        edit.Items.Add(remove);
        edit.Items.Add(MenuItemModel.Separator());
        edit.Items.Add(new MenuItemModel("Settings...", () => _dialogService.Show<SettingsDialogViewModel>()));

        MenuItemModel view = new MenuItemModel("View");
        // Zooms the whole app (the UI font size), not the canvas, which has its own zoom controls.
        _zoomStepper = new MenuStepper(
            "Zoom", "Zoom In", "Zoom Out", "Actual Size",
            increase: () => _uiSettingsService.BaseFontSize += 1,
            decrease: () => _uiSettingsService.BaseFontSize -= 1,
            reset: () => _uiSettingsService.BaseFontSize = UiSettingsService.DefaultBaseFontSize);
        RefreshZoomText();
        view.Items.Add(new MenuItemModel("Zoom") { Stepper = _zoomStepper });
        view.Items.Add(new MenuItemModel("Focus on Selection",
            () => _messenger.Send(ZoomCanvasToFitSelectionMessage.FocusOnSelection()))
        {
            Gesture = _hotkeyManager.FocusSelection,
        });

        MenuItemModel content = new MenuItemModel("Content");
        content.Items.Add(new MenuItemModel("Find file references...", FindFileReferences));

        MenuItemModel help = new MenuItemModel("Help");
        help.Items.Add(new MenuItemModel("About...", ShowAbout));
        const string thirdPartyNoticesUrl = "https://github.com/vchelaru/Gum/blob/main/THIRD-PARTY-NOTICES.txt";
        help.Items.Add(new MenuItemModel("Third-Party Licenses...", () =>
        {
            // The notices file ships next to the executable; fall back to the copy on GitHub.
            string localPath = System.IO.Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
            if (System.IO.File.Exists(localPath))
            {
                _fileSystemRevealService.OpenFile(localPath);
            }
            else
            {
                _fileSystemRevealService.OpenUrl(thirdPartyNoticesUrl);
            }
        })
        { ToolTip = "Licenses and attributions for third-party components Gum redistributes" });
        const string documentationLink = "https://docs.flatredball.com/gum";
        help.Items.Add(new MenuItemModel($"View Docs ({documentationLink})", () => _fileSystemRevealService.OpenUrl(documentationLink))
        {
            ToolTip = "External link to Gum documentation",
        });
        help.Items.Add(new MenuItemModel("Open Settings Folder...", () =>
            _fileSystemRevealService.OpenFolder(FileManager.UserApplicationDataForThisApplication))
        {
            ToolTip = "Open the folder containing Gum's global settings files",
        });

        Model.TopLevelItems.Add(file);
        Model.TopLevelItems.Add(edit);
        Model.TopLevelItems.Add(view);
        Model.TopLevelItems.Add(content);
        Model.TopLevelItems.Add(help);

        RefreshUI();
        return Model;
    }

    /// <summary>Shows the version message behind Help > About; the macOS app menu's About Gum shows the same.</summary>
    public void ShowAbout()
    {
        _dialogService.ShowMessage("Gum version " + ToolVersion.Describe(Assembly.GetEntryAssembly()), "About");
    }

    /// <summary>Syncs the selection-dependent headers, enabled flags, and check marks.</summary>
    public void RefreshUI()
    {
        MenuStripRefreshState state = _menuStripStateLogic.GetRefreshState();

        if (_removeStateMenuItem != null)
        {
            _removeStateMenuItem.Header = state.RemoveStateHeader;
            _removeStateMenuItem.IsEnabled = state.RemoveStateEnabled;
        }
        if (_removeElementMenuItem != null)
        {
            _removeElementMenuItem.Header = state.RemoveElementHeader;
            _removeElementMenuItem.IsEnabled = state.RemoveElementEnabled;
        }
        if (_removeVariableMenuItem != null)
        {
            _removeVariableMenuItem.Header = state.RemoveVariableHeader;
            _removeVariableMenuItem.IsEnabled = state.RemoveVariableEnabled;
        }
        UpdateUndoRedoEnabled();
    }

    private void UpdateUndoRedoEnabled()
    {
        if (_undoMenuItem != null)
        {
            _undoMenuItem.IsEnabled = _undoManager.CanUndo();
        }
        if (_redoMenuItem != null)
        {
            _redoMenuItem.IsEnabled = _undoManager.CanRedo();
        }
    }

    private void RemoveBehaviorVariable()
    {
        if (_selectedState.SelectedBehavior != null && _selectedState.SelectedBehaviorVariable != null)
        {
            _editCommands.RemoveBehaviorVariable(_selectedState.SelectedBehavior, _selectedState.SelectedBehaviorVariable);
            RefreshUI();
        }
    }

    private void RemoveStateOrCategory()
    {
        // A selected state or category always has an owning element or behavior.
        if (_selectedState.SelectedStateContainer is not { } container)
        {
            return;
        }

        if (_selectedState.SelectedStateSave != null)
        {
            _editCommands.AskToDeleteState(_selectedState.SelectedStateSave, container);
        }
        else if (_selectedState.SelectedStateCategorySave != null)
        {
            _editCommands.AskToDeleteStateCategory(_selectedState.SelectedStateCategorySave, container);
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

    private void FindFileReferences()
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
                    message += "\n" + element;
                }
            }
            _dialogService.ShowMessage(message);
        }
    }
}
