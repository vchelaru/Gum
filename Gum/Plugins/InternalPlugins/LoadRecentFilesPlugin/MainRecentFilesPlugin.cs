using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainRecentFilesPlugin : InternalPlugin
    {
        ToolStripMenuItem recentFilesMenuItem;
        public override void StartUp()
        {
            recentFilesMenuItem = this.AddMenuItemTo("Load Recent", null, "File", preferredIndex: 2);

            RefreshMenuItems();

            this.ProjectLoad += HandleProjectLoad;
        }

        private void HandleProjectLoad(GumProjectSave obj)
        {
            RefreshMenuItems();
        }

        private void RefreshMenuItems()
        {
            var recentFiles = ProjectManager.Self.GeneralSettingsFile?.RecentProjects;


            recentFilesMenuItem.DropDownItems.Clear();

            if(recentFiles == null)
            {
                return;
            }

            foreach (var item in recentFiles.Where(item => item.IsFavorite))
            {
                var filePath = item.FilePath;
                string name = GetDisplayedNameForGumxFilePath(filePath);

                recentFilesMenuItem.DropDownItems.Add(
                    name,
                    null,
                    (not, used) => _fileCommands.LoadProject(filePath.FullPath));
            }

            var nonFavorites = recentFiles.Where(item => !item.IsFavorite).ToArray();

            var hasNonFavorites = nonFavorites.Length > 0;

            if (hasNonFavorites)
            {
                recentFilesMenuItem.DropDownItems.Add("-");

                foreach (var item in nonFavorites.Take(5))
                {
                    var filePath = item.FilePath;

                    string name = GetDisplayedNameForGumxFilePath(filePath);

                    recentFilesMenuItem.DropDownItems.Add(
                        name, 
                        null, 
                        (not, used) => _fileCommands.LoadProject(filePath.FullPath));
                }


            }

            recentFilesMenuItem.DropDownItems.Add("More...", null, HandleLoadRecentClicked);
        }

        private static string GetDisplayedNameForGumxFilePath(FilePath filePath)
        {
            var name = filePath.RemoveExtension().FileNameNoPath;

            // It's common to have lots of same-named projects so let's see if this is in a csproj somewhere:
            var parentDirectory = filePath.GetDirectoryContainingThis();
            if (parentDirectory != null)
            {
                string? foundCsproj = null;
                while (parentDirectory?.Exists() == true)
                {
                    foundCsproj = System.IO.Directory.GetFiles(parentDirectory.FullPath, "*.csproj").FirstOrDefault();

                    if (foundCsproj == null)
                    {
                        parentDirectory = parentDirectory.GetDirectoryContainingThis();
                    }
                    else
                    {
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(foundCsproj))
                {
                    var fullPath = new FilePath(foundCsproj);
                    name += $" ({fullPath.FileNameNoPath})";
                }
            }

            return name;
        }

        private async void HandleLoadRecentClicked(object sender, EventArgs e)
        {
            var viewModel = new LoadRecentViewModel();
            var recentFiles = ProjectManager.Self.GeneralSettingsFile.RecentProjects;
            viewModel.AllItems.Clear();
            if (recentFiles != null)
            {
                foreach (var recentFile in recentFiles)
                {
                    var vm = new RecentItemViewModel()
                    {
                        FullPath = recentFile.FilePath.FullPath,
                        IsFavorite = recentFile.IsFavorite
                    };
                    viewModel.AllItems.Add(vm);

                }
            }

            viewModel.RefreshFilteredItems();

            if (_dialogService.Show(viewModel))
            {
                var fileToLoad = viewModel.SelectedItem.FullPath;

                _fileCommands.LoadProject(fileToLoad);

            }

            if (recentFiles != null)
            {
                foreach (var item in viewModel.FilteredItems)
                {
                    var matching = recentFiles.FirstOrDefault(candidate => candidate.FilePath == item.FullPath);

                    if (matching != null)
                    {
                        matching.IsFavorite = item.IsFavorite;
                    }
                }
                _fileCommands.SaveGeneralSettings();
            }

        }
    }
}
