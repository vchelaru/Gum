using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using System;
using System.ComponentModel;
using System.Linq;
using ToolsUtilities;

// Kept in the same namespace as FileWatchViewModel (Gum.Plugins.FileWatchPlugin) so the tool-side
// MainFileWatchPlugin needs no new `using` to consume this class.
namespace Gum.Plugins.FileWatchPlugin;

/// <summary>
/// Owns MainFileWatchPlugin's WPF-free reactions to project/variable events and the File Watch debug
/// panel's display-refresh logic. Extracted from <c>MainFileWatchPlugin</c> (issue #3931): none of this
/// touches a WPF type, but every method used to read the plugin's own private fields rather than take
/// its dependencies as constructor parameters, which is what blocked the extraction until now.
///
/// The plugin still owns the real platform glue this class deliberately has no seam for: creating the
/// WPF <c>FileWatchControl</c>/tab/menu item, and the timer/event wiring itself.
/// </summary>
public class FileWatchPluginController
{
    private readonly IFileWatchManager _fileWatchManager;
    private readonly FileWatchLogic _fileWatchLogic;

    public FileWatchPluginController(IFileWatchManager fileWatchManager, FileWatchLogic fileWatchLogic)
    {
        _fileWatchManager = fileWatchManager;
        _fileWatchLogic = fileWatchLogic;
    }

    public void HandleViewModelPropertyChanged(FileWatchViewModel viewModel, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileWatchViewModel.PrintFileChangesToOutput))
        {
            _fileWatchManager.PrintFileChangesToOutput = viewModel.PrintFileChangesToOutput;
        }
    }

    public void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
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

        var rootVariable = ObjectFinder.Self.GetRootVariable(fullVariableName, element);

        if (rootVariable?.IsFile == true)
        {
            // need to update the file paths
            _fileWatchLogic.RefreshRootDirectory();
        }
    }

    public void HandleProjectLocationSet(FilePath path)
    {
        _fileWatchLogic.HandleProjectLoaded();
    }

    public void HandleProjectLoad(GumProjectSave save)
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

    public void RefreshDisplay(FileWatchViewModel viewModel)
    {
        if (_fileWatchManager.CurrentFilePathsWatching.Count() == 0)
        {
            return;
        }

        string filePathsWatchingText = "";

        if (_fileWatchManager.Enabled)
        {
            filePathsWatchingText = $"File path(s) watching:\n";
            foreach (var item in _fileWatchManager.CurrentFilePathsWatching)
            {
                filePathsWatchingText += "  " + item + "\n";
            }

            viewModel.WatchFolderInformation = filePathsWatchingText;
        }
        else
        {
            viewModel.WatchFolderInformation = "File watching is disabled";
        }

        viewModel.NumberOfFilesToFlush = _fileWatchManager.ChangedFilesWaitingForFlush.Count().ToString();

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

    private static string ToTimeString(TimeSpan timeSpan)
    {
        // it's never going to be minutes, so let's just shorten it to seconds:
        return timeSpan.ToString(@"ss\:ff");
    }
}
