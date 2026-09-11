using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.BaseClasses;
using Gum.Services.Dialogs;
using Gum.Settings;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin
{
    // As of ADR-0005 Phase 3, the display/filtering/dialog logic for the "Load Recent" menu lives in
    // RecentFilesLogic (Gum.Presentation) so it can be unit tested headlessly. This plugin builds only
    // the menu entries (through the shared menu model) and forwards clicks into that logic.
    [Export(typeof(PluginBase))]
    internal class MainRecentFilesPlugin : CorePriorityPlugin
    {
        private MenuItemModel _recentFilesMenuItem = null!;
        private readonly RecentFilesLogic _recentFilesLogic;

        [ImportingConstructor]
        public MainRecentFilesPlugin(IProjectManager projectManager, IFileCommands fileCommands, IDialogService dialogService)
        {
            _recentFilesLogic = new RecentFilesLogic(projectManager, fileCommands, dialogService);
        }

        public override void StartUp()
        {
            // Just after "Load Project...", before the first separator.
            MenuItemModel fileMenu = Menu!.GetItem("File")!;
            _recentFilesMenuItem = new MenuItemModel("Load Recent");
            fileMenu.Items.Insert(System.Math.Min(2, fileMenu.Items.Count), _recentFilesMenuItem);

            RefreshMenuItems();

            this.ProjectLoad += HandleProjectLoad;
        }

        private void HandleProjectLoad(GumProjectSave obj)
        {
            RefreshMenuItems();
        }

        private void RefreshMenuItems()
        {
            _recentFilesMenuItem.Items.Clear();

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
                    _recentFilesMenuItem.Items.Add(MenuItemModel.Separator());
                }
                foreach (var item in nonFavorites)
                {
                    AddMenuItemFor(item);
                }
            }

            _recentFilesMenuItem.Items.Add(MenuItemModel.Separator());
            _recentFilesMenuItem.Items.Add(new MenuItemModel("More...", HandleLoadRecentClicked));
        }

        private void AddMenuItemFor(RecentProjectReference item)
        {
            var filePath = item.FilePath;
            string name = RecentFilesLogic.GetDisplayedNameForGumxFilePath(filePath);
            _recentFilesMenuItem.Items.Add(new MenuItemModel(name, () => _recentFilesLogic.LoadProject(filePath.FullPath)));
        }

        private void HandleLoadRecentClicked()
        {
            _recentFilesLogic.ShowLoadRecentDialog();
            RefreshMenuItems();
        }
    }
}
