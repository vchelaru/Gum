﻿using Gum;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Mvvm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TextureCoordinateSelectionPlugin.Models;
using ToolsUtilities;

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

    private ProjectManager _projectManager;
    private readonly IFileCommands _fileCommands;
    private readonly FileWatchManager _fileWatchManager;
    private readonly IGuiCommands _guiCommands;

    [DependsOn(nameof(IsSnapToGridChecked))]
    public bool IsSnapToGridComboBoxEnabled => IsSnapToGridChecked;

    bool _isSavingSuppressed = false;

    public MainControlViewModel(
        ProjectManager projectManager,
        IFileCommands fileCommands,
        FileWatchManager fileWatchManager,
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

    private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        ////////////////////////////Early Out////////////////////////////////
        if (_isSavingSuppressed)
        {
            return;
        }
        ////////////////////////////End Early Out////////////////////////////

        if (e.PropertyName == nameof(IsSnapToGridChecked) ||
            e.PropertyName == nameof(SelectedSnapToGridValue))
        {
            SaveSettings();
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
            if (_projectManager.GumProjectSave != null)
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
