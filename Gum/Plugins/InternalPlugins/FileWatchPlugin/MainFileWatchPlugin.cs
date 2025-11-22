using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Timers;
using Gum.Services;
using ToolsUtilities;

namespace Gum.Plugins.FileWatchPlugin;

[Export(typeof(PluginBase))]
public class MainFileWatchPlugin : InternalPlugin
{
    #region Fields/Properties

    PeriodicUiTimer refreshDisplayTimer;

    FileWatchViewModel viewModel;

    PluginTab pluginTab;
    System.Windows.Forms.ToolStripMenuItem showFileWatchMenuItem;
    private FileWatchManager _fileWatchManager;
    private FileWatchLogic _fileWatchLogic;

    #endregion

    public override void StartUp()
    {
        var control = new FileWatchControl();

        viewModel = new FileWatchViewModel();
        control.DataContext = viewModel;

        _fileWatchManager = Locator.GetRequiredService<FileWatchManager>(); 
        _fileWatchLogic = FileWatchLogic.Self;

        pluginTab = _tabManager.AddControl(control, "File Watch", TabLocation.RightBottom);
        pluginTab.Hide();

        pluginTab.TabHidden += HandleTabHidden;
        pluginTab.TabShown += HandleTabShown;
        pluginTab.CanClose = true;

        showFileWatchMenuItem = this.AddMenuItem("View", "Show File Watch");
        showFileWatchMenuItem.Click += HandleShowFileWatch;

        const int millisecondsTimerFrequency = 200;
        refreshDisplayTimer = Locator.GetRequiredService<PeriodicUiTimer>();
        refreshDisplayTimer.Tick += HandleRefreshDisplayTimerElapsed;
        refreshDisplayTimer.Start(TimeSpan.FromMilliseconds(millisecondsTimerFrequency));

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ProjectLoad += HandleProjectLoad;
        this.ProjectLocationSet += HandleProjectLocationSet;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
    {
        if (element == null)
        {
            return;
        }

        var fullVariableName = variableName;
        if (instance != null)
        {
            fullVariableName = instance.Name + "." + variableName;
        }

        var rootVarible = ObjectFinder.Self.GetRootVariable(fullVariableName, element);

        if (rootVarible?.IsFile == true)
        {
            // need to update the file paths
            _fileWatchLogic.RefreshRootDirectory();
        }
    }

    private void HandleProjectLocationSet(FilePath path)
    {
        _fileWatchLogic.HandleProjectLoaded();
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        if (save.FullFileName == null)
        {
            _fileWatchLogic.HandleProjectUnloaded();
        }
        else
        {
            _fileWatchLogic.HandleProjectLoaded();
        }
    }

    private void HandleTabShown()
    {
        showFileWatchMenuItem.Text = "Hide File Watch";
    }

    private void HandleTabHidden()
    {
        showFileWatchMenuItem.Text = "Show File Watch";
    }

    private void HandleShowFileWatch(object sender, EventArgs e)
    {
        pluginTab.IsVisible = !pluginTab.IsVisible;
    }

    private void HandleRefreshDisplayTimerElapsed()
    {
        if (_fileWatchManager.CurrentFilePathWatching == null)
        {
            return;
        }

        string filePathsWatchingText = "";

        if (_fileWatchManager.Enabled)
        {
            filePathsWatchingText = $"File path watching ({_fileWatchManager.CurrentFilePathWatching}):";

            viewModel.WatchFolderInformation = filePathsWatchingText;
        }
        else
        {
            viewModel.WatchFolderInformation = "File watching is disabled";
        }

        viewModel.NumberOfFilesToFlush = _fileWatchManager.ChangedFilesWaitingForFlush.Count.ToString();

        if (_fileWatchManager.TimeToNextFlush.TotalSeconds <= 0)
        {
            viewModel.TimeToNextFlush = "Waiting for file change";
        }
        else
        {
            viewModel.TimeToNextFlush = "File flush in: " + ToTimeString(_fileWatchManager.TimeToNextFlush);
        }

        const int maxOfFilesToShow = 15;

        try
        {
            var filesToDisplay = _fileWatchManager.ChangedFilesWaitingForFlush.Take(maxOfFilesToShow).ToArray();

            string nextFilesInfo = string.Empty;

            if (filesToDisplay.Length > 0)
            {
                nextFilesInfo += "Next files to flush:\n";
            }

            foreach (var item in filesToDisplay)
            {
                nextFilesInfo += item.FullPath + "\n";
            }
            viewModel.NextFilesToFlush = nextFilesInfo;


            viewModel.IgnoredFilesInformation = string.Empty;
            var now = DateTime.Now;
            var activeIgnores = _fileWatchManager.TimedChangesToIgnore.Where(item => item.Value > now).ToArray();
            if (activeIgnores.Length > 0)
            {
                viewModel.IgnoredFilesInformation += $"Ignoring {activeIgnores.Length} files:\n";

                foreach (var kvp in activeIgnores)
                {
                    var timeSpan = kvp.Value - now;
                    viewModel.IgnoredFilesInformation += $"{kvp.Key.FullPath} - Ignored for {ToTimeString(timeSpan)}\n";
                }
            }


        }
        catch
        {
            // This can happen if there's a file that changes right when this happens. no biggie, we'll get it next time....
        }
    }

    string ToTimeString(TimeSpan timeSpan)
    {
        // it's never going to be minutes, so let's just shorten it to seconds:
        //return timeSpan.ToString(@"m\:ss\:ff");
        return timeSpan.ToString(@"ss\:ff");
    }
}
