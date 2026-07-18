using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using System.Collections.ObjectModel;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ImportBaseDialogViewModel's search filtering, relocated
/// out of Gum.csproj into the headless Gum.Presentation assembly (ADR-0005, #3754). The WPF
/// ICollectionView/CollectionViewSource filtering was rewritten as a plain filtered
/// ObservableCollection&lt;string&gt; since Gum.Presentation may never reference WPF.
/// </summary>
public class ImportBaseDialogViewModelTests
{
    private sealed class TestImportDialogViewModel : ImportBaseDialogViewModel
    {
        public override string Title => "Test Import";
        public override string BrowseFileFilter => "Test Files (*.test)|*.test";

        public TestImportDialogViewModel(IDialogService dialogService) : base(dialogService) { }
    }

    private static TestImportDialogViewModel CreateSut(params string[] unfilteredFiles)
    {
        TestImportDialogViewModel sut = new(Mock.Of<IDialogService>());
        sut.UnfilteredFiles.AddRange(unfilteredFiles);
        return sut;
    }

    [Fact]
    public void FilteredFiles_ShowsAllFiles_WhenSearchTextIsEmpty()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx", "Beta.gucx");

        sut.FilteredFiles.ShouldBe(new[] { "Alpha.gucx", "Beta.gucx" });
    }

    [Fact]
    public void FilteredFiles_FiltersToPartialMatch_WhenSearchTextIsSet()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx", "Beta.gucx", "AlphaBeta.gucx");

        sut.SearchText = "alpha";

        sut.FilteredFiles.ShouldBe(new[] { "Alpha.gucx", "AlphaBeta.gucx" });
    }

    [Fact]
    public void FilteredFiles_MatchesCaseInsensitively()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx");

        sut.SearchText = "ALPHA";

        sut.FilteredFiles.ShouldBe(new[] { "Alpha.gucx" });
    }

    [Fact]
    public void FilteredFiles_IsEmpty_WhenSearchTextMatchesNothing()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx", "Beta.gucx");

        sut.SearchText = "zzz";

        sut.FilteredFiles.ShouldBeEmpty();
    }

    [Fact]
    public void FilteredFiles_UpdatesWhenUnfilteredFilesChanges_AfterSearchTextIsSet()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx");
        sut.SearchText = "beta";

        sut.UnfilteredFiles.Add("Beta.gucx");

        sut.FilteredFiles.ShouldBe(new[] { "Beta.gucx" });
    }

    [Fact]
    public void FilteredFiles_RevertsToAllFiles_WhenSearchTextClears()
    {
        TestImportDialogViewModel sut = CreateSut("Alpha.gucx", "Beta.gucx");
        sut.SearchText = "alpha";

        sut.SearchText = "";

        sut.FilteredFiles.ShouldBe(new[] { "Alpha.gucx", "Beta.gucx" });
    }
}
