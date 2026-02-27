using Gum.Mvvm;

namespace Gum.Plugins.FileWatchPlugin;

public class FileWatchViewModel : ViewModel
{
    public string WatchFolderInformation
    {
        get => Get<string>();
        set => Set(value);
    }

    public string NumberOfFilesToFlush
    {
        get => Get<string>();
        set => Set(value);
    }
    public string TimeToNextFlush
    {
        get => Get<string>();
        set => Set(value);
    }
    public string NextFilesToFlush
    {
        get => Get<string>();
        set => Set(value);
    }

    public string IgnoredFilesInformation
    {
        get => Get<string>();
        set => Set(value);
    }

    public bool PrintFileChangesToOutput
    {
        get => Get<bool>();
        set => Set(value);
    }
}
