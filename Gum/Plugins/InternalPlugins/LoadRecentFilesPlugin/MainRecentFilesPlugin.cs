using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Gum.Settings;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin
{
    // As of ADR-0005 Phase 3, the display/filtering/dialog logic for the "Load Recent" menu lives in
    // RecentFilesLogic (Gum.Presentation) so it can be unit tested headlessly. This plugin builds only
    // the actual WPF MenuItems and forwards clicks into that logic.
    [Export(typeof(PluginBase))]
    internal class MainRecentFilesPlugin : PriorityPlugin
    {
        MenuItem recentFilesMenuItem;
        private readonly RecentFilesLogic _recentFilesLogic;

        [ImportingConstructor]
        public MainRecentFilesPlugin(IProjectManager projectManager, IFileCommands fileCommands, IDialogService dialogService)
        {
            _recentFilesLogic = new RecentFilesLogic(projectManager, fileCommands, dialogService);
        }

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
            recentFilesMenuItem.Items.Clear();

            var favorites = _recentFilesLogic.GetFavoriteProjects().ToList();
            foreach (var item in favorites)
            {
                AddMenuItemFor(item);
            }

            var nonFavorites = _recentFilesLogic.GetNonFavoriteProjectsForMenu().ToList();
            if (nonFavorites.Count > 0)
            {
                if (favorites.Count > 0)
                {
                    recentFilesMenuItem.Items.Add(new Separator());
                }

                foreach (var item in nonFavorites)
                {
                    AddMenuItemFor(item);
                }
            }

            recentFilesMenuItem.Items.Add(new Separator());
            var moreItem = new MenuItem { Header = "More..." };
            moreItem.Click += HandleLoadRecentClicked;
            recentFilesMenuItem.Items.Add(moreItem);
        }

        private void AddMenuItemFor(RecentProjectReference item)
        {
            var filePath = item.FilePath;
            string name = RecentFilesLogic.GetDisplayedNameForGumxFilePath(filePath);

            var mi = new MenuItem { Header = name };
            mi.Click += (_, _) => _recentFilesLogic.LoadProject(filePath.FullPath);
            recentFilesMenuItem.Items.Add(mi);
        }

        private void HandleLoadRecentClicked(object? sender, System.Windows.RoutedEventArgs e)
        {
            _recentFilesLogic.ShowLoadRecentDialog();
            RefreshMenuItems();
        }
    }
}
