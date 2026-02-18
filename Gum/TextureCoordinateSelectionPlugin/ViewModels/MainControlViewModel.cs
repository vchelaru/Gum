using Gum;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Mvvm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TextureCoordinateSelectionPlugin.Logic;
using TextureCoordinateSelectionPlugin.Models;
using ToolsUtilities;
using System.Windows;

namespace TextureCoordinateSelectionPlugin.ViewModels;

public class MainControlViewModel : ViewModel
{
    public bool IsSnapToGridChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public int SelectedSnapToGridValue
    {
        get => Get<int>();
        set => Set(value);
    }

    public List<int> AvailableSnapToGridValues
    {
        get; private set;
    } = new List<int>
    {
        4, 8, 12, 16, 24, 32, 48, 64
    };

    public IList<int> AvailableZoomLevels { get; set;} = new int[]
    {
        3200, 1600, 800, 400, 200, 100, 50, 25, 12
    };

    public int SelectedZoomLevel
    {
        get => Get<int>();
        set => Set(value);
    }

    private IProjectManager _projectManager;
    private readonly IFileCommands _fileCommands;
    private readonly IFileWatchManager _fileWatchManager;
    private readonly IGuiCommands _guiCommands;
    TextureCoordinateDisplayController? _displayController;

    [DependsOn(nameof(IsSnapToGridChecked))]
    public bool IsSnapToGridComboBoxEnabled => IsSnapToGridChecked;

    public List<ExposedTextureCoordinateSet>? AvailableExposedSources
    {
        get => Get<List<ExposedTextureCoordinateSet>?>();
        set => Set(value);
    }

    public ExposedTextureCoordinateSet? SelectedExposedSource
    {
        get => Get<ExposedTextureCoordinateSet?>();
        set => Set(value);
    }

    [DependsOn(nameof(AvailableExposedSources))]
    public Visibility ExposedSourceDropdownVisibility =>
        (AvailableExposedSources?.Count ?? 0) > 1 ? Visibility.Visible : Visibility.Collapsed;

    public void UpdateExposedSources(List<ExposedTextureCoordinateSet> sources, bool preserveSelection)
    {
        var previouslySelected = preserveSelection ? SelectedExposedSource : null;
        AvailableExposedSources = sources.Count > 0 ? sources : null;
        SelectedExposedSource = sources.FirstOrDefault(s =>
            s.SourceObjectName == previouslySelected?.SourceObjectName)
            ?? sources.FirstOrDefault();
    }

    bool _isSavingSuppressed = false;

    public void Initialize(TextureCoordinateDisplayController displayController)
    {
        _displayController = displayController;
    }

    public MainControlViewModel(
        IProjectManager projectManager,
        IFileCommands fileCommands,
        IFileWatchManager fileWatchManager,
        IGuiCommands guiCommands)
    {
        SelectedSnapToGridValue = 16;
        SelectedZoomLevel = 100;

        _projectManager = projectManager;
        _fileCommands = fileCommands;
        _fileWatchManager = fileWatchManager;
        _guiCommands = guiCommands;

        this.PropertyChanged += HandlePropertyChanged;

    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedZoomLevel):
                _displayController?.UpdateZoom(SelectedZoomLevel);
                break;
            case nameof(IsSnapToGridChecked):
            case nameof(SelectedSnapToGridValue):
                _displayController?.UpdateSnapGrid();
                if (!_isSavingSuppressed)
                {
                    SaveSettings();
                }
                break;
            case nameof(SelectedExposedSource):
                _displayController?.SetCurrentExposedSource(SelectedExposedSource);
                _displayController?.Refresh();
                break;
        }
    }

    internal void ZoomIn()
    {
        var index = AvailableZoomLevels.IndexOf(SelectedZoomLevel);
        if(index > 0)
        {
            SelectedZoomLevel = AvailableZoomLevels[index - 1];
        }
    }

    internal void ZoomOut()
    {
        var index = AvailableZoomLevels.IndexOf(SelectedZoomLevel);
        if(index < AvailableZoomLevels.Count - 1)
        {
            SelectedZoomLevel = AvailableZoomLevels[index + 1];
        }
    }

    FilePath? SettingsFilePath
    {
        get
        {
            if (!string.IsNullOrEmpty(_projectManager.GumProjectSave?.FullFileName))
            {
                var directory = FileManager.GetDirectory(_projectManager.GumProjectSave.FullFileName);
                return directory + "TextureCoordinateSettings.tcsj";
            }
            else
            {
                return null;
            }
        }
    }

    public void LoadSettings()
    {
        var sourceFile = SettingsFilePath;
        ////////////////////////////////////////Early Out////////////////////////////////////
        if(sourceFile == null)
        {
            return;
        }
        /////////////////////////////////////End Early Out////////////////////////////////////

        try
        {
            if(FileManager.FileExists(sourceFile.FullPath))
            {
                var contents = FileManager.FromFileText(sourceFile.FullPath);
                var model = JsonConvert.DeserializeObject<TextureCoordinateSettingsModel>(contents);

                _isSavingSuppressed = true;

                if (model != null)
                {
                    this.IsSnapToGridChecked = model.IsSnapToGridChecked;
                    this.SelectedSnapToGridValue = model.SelectedSnapToGridValue;
                }

                _isSavingSuppressed = false;

            }
        }
        catch(Exception ex)
        {
            _guiCommands.PrintOutput("Error loading Texture Coordinate Settings:\n" + ex.Message);
        }

    }

    void SaveSettings()
    {
        var destinationFile = SettingsFilePath;
        ////////////////////////////////////////Early Out////////////////////////////////////
        if(destinationFile == null)
        {
            return;
        }
        /////////////////////////////////////End Early Out////////////////////////////////////
        var model = new TextureCoordinateSettingsModel()
        {
            IsSnapToGridChecked = this.IsSnapToGridChecked,
            SelectedSnapToGridValue = this.SelectedSnapToGridValue
        };

        var contents = JsonConvert.SerializeObject(model, Formatting.Indented);

        _fileWatchManager.IgnoreNextChangeUntil(destinationFile);

        _fileCommands.SaveIfDiffers(destinationFile, contents);
    }
}
