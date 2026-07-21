using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Services.Dialogs;
using Gum.Settings;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainRecentFilesPlugin into the headless Gum.Presentation
/// assembly (ADR-0005 Phase 3) so this business logic is unit testable.
/// </summary>
public class RecentFilesLogicTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly RecentFilesLogic _logic;

    public RecentFilesLogicTests()
    {
        _logic = new RecentFilesLogic(_projectManager.Object, _fileCommands.Object, _dialogService.Object);
    }

    private static RecentProjectReference MakeRecentProject(string path, bool isFavorite)
    {
        return new RecentProjectReference { AbsoluteFileName = path, IsFavorite = isFavorite };
    }

    [Fact]
    public void GetFavoriteProjects_ReturnsOnlyFavorites()
    {
        List<RecentProjectReference> recentProjects = new()
        {
            MakeRecentProject(@"C:\ProjectA.gumx", isFavorite: true),
            MakeRecentProject(@"C:\ProjectB.gumx", isFavorite: false),
            MakeRecentProject(@"C:\ProjectC.gumx", isFavorite: true),
        };
        _projectManager.Setup(x => x.RecentProjects).Returns(recentProjects);

        List<RecentProjectReference> result = _logic.GetFavoriteProjects().ToList();

        result.Count.ShouldBe(2);
        result.ShouldContain(recentProjects[0]);
        result.ShouldContain(recentProjects[2]);
    }

    [Fact]
    public void GetNonFavoriteProjectsForMenu_CapsAtMaxCount()
    {
        List<RecentProjectReference> recentProjects = Enumerable.Range(0, 7)
            .Select(i => MakeRecentProject($@"C:\Project{i}.gumx", isFavorite: false))
            .ToList();
        _projectManager.Setup(x => x.RecentProjects).Returns(recentProjects);

        List<RecentProjectReference> result = _logic.GetNonFavoriteProjectsForMenu(maxCount: 5).ToList();

        result.Count.ShouldBe(5);
        result.ShouldBe(recentProjects.Take(5));
    }

    [Fact]
    public void LoadProject_DelegatesToFileCommands()
    {
        _logic.LoadProject(@"C:\SomeProject.gumx");

        _fileCommands.Verify(x => x.LoadProject(@"C:\SomeProject.gumx"), Times.Once);
    }

    [Fact]
    public void ShowLoadRecentDialog_Confirmed_LoadsSelectedProjectAndPersistsFavoriteChanges()
    {
        List<RecentProjectReference> recentProjects = new()
        {
            MakeRecentProject(@"C:\ProjectA.gumx", isFavorite: false),
            MakeRecentProject(@"C:\ProjectB.gumx", isFavorite: false),
        };
        _projectManager.Setup(x => x.RecentProjects).Returns(recentProjects);

        _dialogService
            .Setup(x => x.Show(It.IsAny<LoadRecentViewModel>()))
            .Callback<LoadRecentViewModel>(vm =>
            {
                RecentItemViewModel itemA = vm.FilteredItems.Single(item => item.FullPath == @"C:\ProjectA.gumx");
                itemA.IsFavorite = true;
                vm.SelectedItem = itemA;
            })
            .Returns(true);

        _logic.ShowLoadRecentDialog();

        _fileCommands.Verify(x => x.LoadProject(@"C:\ProjectA.gumx"), Times.Once);
        recentProjects[0].IsFavorite.ShouldBeTrue();
        _fileCommands.Verify(x => x.SaveGeneralSettings(), Times.Once);
    }

    [Fact]
    public void ShowLoadRecentDialog_Cancelled_DoesNotLoadButStillPersistsFavoriteChanges()
    {
        List<RecentProjectReference> recentProjects = new()
        {
            MakeRecentProject(@"C:\ProjectA.gumx", isFavorite: false),
        };
        _projectManager.Setup(x => x.RecentProjects).Returns(recentProjects);

        _dialogService
            .Setup(x => x.Show(It.IsAny<LoadRecentViewModel>()))
            .Callback<LoadRecentViewModel>(vm =>
            {
                vm.FilteredItems.Single().IsFavorite = true;
            })
            .Returns(false);

        _logic.ShowLoadRecentDialog();

        _fileCommands.Verify(x => x.LoadProject(It.IsAny<string>()), Times.Never);
        recentProjects[0].IsFavorite.ShouldBeTrue();
        _fileCommands.Verify(x => x.SaveGeneralSettings(), Times.Once);
    }
}
