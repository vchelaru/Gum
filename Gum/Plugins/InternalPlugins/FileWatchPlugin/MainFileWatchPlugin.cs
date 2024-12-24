using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Timers;
using ToolsUtilities;

namespace Gum.Plugins.FileWatchPlugin;

[Export(typeof(PluginBase))]
public class MainFileWatchPlugin : InternalPlugin
{
    #region Fields/Properties

    System.Timers.Timer timer;

    FileWatchViewModel viewModel;

    PluginTab pluginTab;
    System.Windows.Forms.ToolStripMenuItem showFileWatchMenuItem;

    #endregion

    public override void StartUp()
    {
        var control = new FileWatchControl();

        viewModel = new FileWatchViewModel();
        control.DataContext = viewModel;

        pluginTab = GumCommands.Self.GuiCommands.AddControl(control, "File Watch", TabLocation.RightBottom);
        pluginTab.Hide();

        pluginTab.TabHidden += HandleTabHidden;
        pluginTab.TabShown += HandleTabShown;

        showFileWatchMenuItem = this.AddMenuItem("View", "Show File Watch");
        showFileWatchMenuItem.Click += HandleShowFileWatch;

        const int millisecondsTimerFrequency = 200;
        timer = new System.Timers.Timer(millisecondsTimerFrequency);
        timer.Elapsed += HandleElapsed;
        timer.Start();

        AssignEvents();
    }

    private void AssignEvents()
    {
        this.ProjectLoad += HandleProjectLoad;
        this.ProjectLocationSet += HandleProjectLocationSet;
    }

    private void HandleProjectLocationSet(FilePath path)
    {
        FileWatchLogic.Self.HandleProjectLoaded();
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        if(save.FullFileName == null)
        {
            FileWatchLogic.Self.HandleProjectUnloaded();
        }
        else
        {
            FileWatchLogic.Self.HandleProjectLoaded();
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
        var isShown = GumCommands.Self.GuiCommands.IsTabVisible(pluginTab);
        if(isShown)
        {
            pluginTab.Hide();
        }
        else
        {
            pluginTab.Show();
        }
    }

    private void HandleElapsed(object sender, ElapsedEventArgs e)
    {
        GumCommands.Self.GuiCommands.DoOnUiThread(() =>
        {
            // stuff stuff
            var fileWatchManager = FileWatchManager.Self;

            if(fileWatchManager.CurrentFilePathsWatching == null)
            {
                return;
            }

            string filePathsWatchingText = "";
            
            if(fileWatchManager.Enabled)
            {
                filePathsWatchingText = $"File paths watching ({fileWatchManager.CurrentFilePathsWatching?.Count ?? 0}):";
                foreach(var item in fileWatchManager.CurrentFilePathsWatching)
                {
                    filePathsWatchingText += $"\n\t{item}";
                }
                viewModel.WatchFolderInformation = filePathsWatchingText;
            }
            else
            {
                viewModel.WatchFolderInformation = "File watching is disabled";
            }

            viewModel.NumberOfFilesToFlush = fileWatchManager.ChangedFilesWaitingForFlush.Count.ToString();

            if (fileWatchManager.TimeToNextFlush.TotalSeconds <= 0)
            {
                viewModel.TimeToNextFlush = "Waiting for file change";
            }
            else
            {
                viewModel.TimeToNextFlush = ToTimeString( fileWatchManager.TimeToNextFlush);
            }

            const int maxOfFilesToShow = 15;

            var filesToDisplay = fileWatchManager.ChangedFilesWaitingForFlush.Take(maxOfFilesToShow).ToArray();

            string nextFilesInfo = null;
            foreach(var item in filesToDisplay)
            {
                nextFilesInfo += item.FullPath + "\n";
            }
            viewModel.NextFilesToFlush = nextFilesInfo;
        });
    }

    string ToTimeString(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"m\:ss\:ff");
    }
}
