using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;

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
                var name = item.FilePath.RemoveExtension().FileNameNoPath;
                recentFilesMenuItem.DropDownItems.Add(
                    name,
                    null, 
                    (not, used) => _fileCommands.LoadProject(item.FilePath.FullPath));
            }

            var nonFavorites = recentFiles.Where(item => !item.IsFavorite).ToArray();

            var hasNonFavorites = nonFavorites.Length > 0;

            if (hasNonFavorites)
            {
                recentFilesMenuItem.DropDownItems.Add("-");

                foreach (var item in nonFavorites.Take(5))
                {
                    var name = item.FilePath.RemoveExtension().FileNameNoPath;

                    recentFilesMenuItem.DropDownItems.Add(
                        name, 
                        null, 
                        (not, used) => _fileCommands.LoadProject(item.FilePath.FullPath));
                }


            }

            recentFilesMenuItem.DropDownItems.Add("More...", null, HandleLoadRecentClicked);
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

            var window = new LoadRecentWindow();

            window.DataContext = viewModel;

            var result = window.ShowDialog();

            if (result == true)
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
