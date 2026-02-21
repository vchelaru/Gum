using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views;
using Gum.Services;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin
{
    [Export(typeof(PluginBase))]
    internal class MainRecentFilesPlugin : InternalPlugin
    {
        MenuItem recentFilesMenuItem;
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
            var recentFiles = Locator.GetRequiredService<IProjectManager>().GeneralSettingsFile?.RecentProjects;


            recentFilesMenuItem.Items.Clear();

            if(recentFiles == null)
            {
                return;
            }

            foreach (var item in recentFiles.Where(item => item.IsFavorite))
            {
                var filePath = item.FilePath;
                string name = GetDisplayedNameForGumxFilePath(filePath);

                var mi = new MenuItem { Header = name };
                mi.Click += (not, used) => _fileCommands.LoadProject(filePath.FullPath);
                recentFilesMenuItem.Items.Add(mi);
            }

            var nonFavorites = recentFiles.Where(item => !item.IsFavorite).ToArray();

            var hasNonFavorites = nonFavorites.Length > 0;

            if (hasNonFavorites)
            {
                recentFilesMenuItem.Items.Add(new Separator());

                foreach (var item in nonFavorites.Take(5))
                {
                    var filePath = item.FilePath;

                    string name = GetDisplayedNameForGumxFilePath(filePath);

                    var mi = new MenuItem { Header = name };
                    mi.Click += (not, used) => _fileCommands.LoadProject(filePath.FullPath);
                    recentFilesMenuItem.Items.Add(mi);
                }


            }

            var moreItem = new MenuItem { Header = "More..." };
            moreItem.Click += HandleLoadRecentClicked;
            recentFilesMenuItem.Items.Add(moreItem);
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

        private async void HandleLoadRecentClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            var viewModel = new LoadRecentViewModel();
            var recentFiles = Locator.GetRequiredService<IProjectManager>().GeneralSettingsFile.RecentProjects;
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
