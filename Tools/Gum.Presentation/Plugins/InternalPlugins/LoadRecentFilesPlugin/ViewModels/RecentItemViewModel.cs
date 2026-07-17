using Gum.Mvvm;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels
{
    public class RecentItemViewModel : ViewModel
    {
        public string FullPath
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FullPath))]
        public string StrippedName => !string.IsNullOrEmpty(FullPath)
            ? FileManager.RemovePath(FullPath)
            : "";

        public bool IsFavorite
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
