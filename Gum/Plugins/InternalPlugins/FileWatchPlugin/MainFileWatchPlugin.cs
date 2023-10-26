using Gum.Logic.FileWatch;
using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Timers;

namespace Gum.Plugins.FileWatchPlugin
{
    [Export(typeof(PluginBase))]
    public class MainFileWatchPlugin : InternalPlugin
    {
        System.Timers.Timer timer;

        FileWatchViewModel viewModel;

        public override void StartUp()
        {
            var control = new FileWatchControl();

            viewModel = new FileWatchViewModel();
            control.DataContext = viewModel;

            GumCommands.Self.GuiCommands.AddControl(control, "File Watch", TabLocation.RightBottom);

            const int millisecondsTimerFrequency = 200;
            timer = new System.Timers.Timer(millisecondsTimerFrequency);
            timer.Elapsed += HandleElapsed;
            timer.Start();
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

                var filePathsWatchingText = $"File paths watching ({fileWatchManager.CurrentFilePathsWatching?.Count ?? 0}):";
                foreach(var item in fileWatchManager.CurrentFilePathsWatching)
                {
                    filePathsWatchingText += $"\n\t{item}";
                }
                viewModel.WatchFolderInformation = filePathsWatchingText;

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
}
