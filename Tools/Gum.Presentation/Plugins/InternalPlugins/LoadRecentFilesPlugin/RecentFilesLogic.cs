using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Services.Dialogs;
using Gum.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin;

/// <summary>
/// Business logic behind the "Load Recent" menu, relocated out of the WPF-hosted
/// <c>MainRecentFilesPlugin</c> (ADR-0005 Phase 3) so it can be unit tested headlessly. The plugin
/// stays a thin wrapper: it builds the actual WPF <c>MenuItem</c>s and forwards clicks here.
/// </summary>
public class RecentFilesLogic
{
    private readonly IProjectManager _projectManager;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;

    public RecentFilesLogic(IProjectManager projectManager, IFileCommands fileCommands, IDialogService dialogService)
    {
        _projectManager = projectManager;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Recent projects the user has favorited, in their stored order.
    /// </summary>
    public IEnumerable<RecentProjectReference> GetFavoriteProjects() =>
        _projectManager.RecentProjects.Where(item => item.IsFavorite);

    /// <summary>
    /// The most recently opened non-favorited projects, capped at <paramref name="maxCount"/>.
    /// </summary>
    public IEnumerable<RecentProjectReference> GetNonFavoriteProjectsForMenu(int maxCount = 5) =>
        _projectManager.RecentProjects.Where(item => !item.IsFavorite).Take(maxCount);

    /// <summary>
    /// Loads the project at the given path.
    /// </summary>
    public void LoadProject(string filePath) => _fileCommands.LoadProject(filePath);

    /// <summary>
    /// Computes the display name for a recent project's file path, disambiguating same-named
    /// projects by appending the name of the nearest containing .csproj, if any.
    /// </summary>
    public static string GetDisplayedNameForGumxFilePath(FilePath filePath)
    {
        string name = filePath.RemoveExtension().FileNameNoPath;

        // It's common to have lots of same-named projects so let's see if this is in a csproj somewhere:
        FilePath? parentDirectory = filePath.GetDirectoryContainingThis();
        if (parentDirectory != null)
        {
            string? foundCsproj = null;
            while (parentDirectory?.Exists() == true)
            {
                foundCsproj = Directory.GetFiles(parentDirectory.FullPath, "*.csproj").FirstOrDefault();

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
                FilePath fullPath = new FilePath(foundCsproj);
                name += $" ({fullPath.FileNameNoPath})";
            }
        }

        return name;
    }

    /// <summary>
    /// Builds and populates the "Load Recent" dialog's view model from the current recent-projects
    /// list.
    /// </summary>
    public LoadRecentViewModel BuildLoadRecentViewModel()
    {
        LoadRecentViewModel viewModel = new();

        foreach (RecentProjectReference recentFile in _projectManager.RecentProjects)
        {
            viewModel.AllItems.Add(new RecentItemViewModel
            {
                FullPath = recentFile.FilePath.FullPath,
                IsFavorite = recentFile.IsFavorite
            });
        }

        viewModel.RefreshFilteredItems();

        return viewModel;
    }

    /// <summary>
    /// Shows the "Load Recent" dialog, loads the selected project if the user confirms, and
    /// persists any favorite toggles the user made regardless of whether they confirmed.
    /// </summary>
    public void ShowLoadRecentDialog()
    {
        LoadRecentViewModel viewModel = BuildLoadRecentViewModel();

        if (_dialogService.Show(viewModel))
        {
            _fileCommands.LoadProject(viewModel.SelectedItem.FullPath);
        }

        IReadOnlyList<RecentProjectReference> recentFiles = _projectManager.RecentProjects;
        foreach (RecentItemViewModel item in viewModel.FilteredItems)
        {
            RecentProjectReference? matching = recentFiles.FirstOrDefault(candidate => candidate.FilePath == item.FullPath);
            if (matching != null)
            {
                matching.IsFavorite = item.IsFavorite;
            }
        }

        _fileCommands.SaveGeneralSettings();
    }
}
