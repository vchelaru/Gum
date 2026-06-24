using Gum.Mvvm;

// This ViewModel was relocated out of Gum.csproj into the headless Gum.Presentation
// assembly (ADR-0005) as the first vertical slice. Its namespace is intentionally
// kept as Gum.Plugins.FileWatchPlugin so consumers in the tool need no edits — the
// move is a "which assembly compiles this file" change, not a rename (mirrors the
// #3229 spike, where ImportTreeNodeViewModel kept its ImportFromGumxPlugin namespace).
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
