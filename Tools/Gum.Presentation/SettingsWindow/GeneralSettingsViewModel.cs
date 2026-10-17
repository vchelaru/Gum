using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Managers;
using Gum.Messages;
using Gum.Mvvm;
using Gum.Services;
using ToolsUtilities;

namespace Gum.SettingsWindow;

/// <summary>
/// The Application > General page: the options that belong to this machine rather than the open
/// project. Each change is written through and saved at once.
/// </summary>
public class GeneralSettingsViewModel : ViewModel
{
    private readonly IProjectManager _projectManager;
    private readonly IMessenger _messenger;
    private readonly IFileSystemRevealService _fileSystemRevealService;

    /// <summary>Creates the view model, filled from the current settings.</summary>
    public GeneralSettingsViewModel(
        IProjectManager projectManager,
        IMessenger messenger,
        IFileSystemRevealService fileSystemRevealService)
    {
        _projectManager = projectManager;
        _messenger = messenger;
        _fileSystemRevealService = fileSystemRevealService;

        SetWithoutNotifying(_projectManager.AutoSave, nameof(AutoSave));
        SetWithoutNotifying(_projectManager.EffectiveUseStandardsPalette, nameof(UseStandardsPalette));

        OpenSettingsFolderCommand = new RelayCommand(
            () => _fileSystemRevealService.OpenFolder(FileManager.UserApplicationDataForThisApplication));
    }

    /// <summary>Whether the project saves whenever something changes.</summary>
    public bool AutoSave
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                _projectManager.AutoSave = value;
                _projectManager.SaveGeneralSettings();
            }
        }
    }

    /// <summary>Whether the standard types show as a chip palette in the Project panel.</summary>
    public bool UseStandardsPalette
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                _projectManager.UseStandardsPalette = value;
                _projectManager.SaveGeneralSettings();
                _messenger.Send(new StandardsPaletteSettingChangedMessage(_projectManager.EffectiveUseStandardsPalette));
            }
        }
    }

    /// <summary>Opens the folder holding Gum's global settings files.</summary>
    public RelayCommand OpenSettingsFolderCommand { get; }
}
