using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The Folders section of the New Project dialog: the root path, folders and project file for a Gum-only project and
/// for each platform's code project, Expand all and Collapse all, and a very long location.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiFoldersTests
{
    private static readonly string Separator = Path.DirectorySeparatorChar.ToString();

    public static IEnumerable<object[]> Platforms() =>
        new[] { "monogame", "kni", "kniskia", "raylib", "stride", "silknet" }.Select(name => new object[] { name });

    [AvaloniaFact]
    public void GumOnly_ShowsTheNewFolderAsTheRoot_ThePlainGumFolders_AndTheProjectFile()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();

        List<string> rows = fixture.FolderRows();

        rows.ShouldBe(new[]
        {
            NewProjectUiText.Folder(Path.Combine(fixture.Location, "Game")),
            "Behaviors" + Separator, "Components" + Separator, "Fonts" + Separator, "Screens" + Separator, "Standards" + Separator,
            "Game.gumj",
        }, ignoreOrder: true);
        rows.ShouldNotContain("Content" + Separator);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Platforms))]
    public void EachPlatformsCodeProject_ShowsTheGameFolder_ContentAndTheGumProjectFolders(string platform)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform(platform);

        List<string> rows = fixture.FolderRows();

        rows.ShouldContain(NewProjectUiText.Folder(Path.Combine(fixture.Location, "Game")));
        foreach (string expected in new[] { "Content", "GumProject", "Behaviors", "Components", "Fonts", "Screens", "Standards" })
        {
            rows.ShouldContain(expected + Separator, $"{expected} for {platform}");
        }
        rows.ShouldContain("GumProject.gumj");
        rows.Count.ShouldBe(9, "root, Content, GumProject, five Gum folders and the project file");
    }

    [AvaloniaFact]
    public void Collapse_all_ClosesEveryFolder_AndExpand_all_OpensThemAgain()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.OpenSection(NewProjectDialogView.FoldersExpanderName);
        fixture.TreeRowTexts().Count.ShouldBeGreaterThan(1, "the section opens with the folders showing");

        fixture.ClickButton("Collapse all");
        fixture.TreeRowTexts().Count.ShouldBe(1, "only the root row is left");
        fixture.ViewModel.FolderTree.Single().IsExpanded.ShouldBeFalse();

        fixture.ClickButton("Expand all");
        List<string> rows = fixture.TreeRowTexts();
        rows.ShouldContain("GumProject.gumj");
        rows.ShouldContain("Screens" + Separator);
        rows.Count.ShouldBe(9);
    }

    [AvaloniaFact]
    public void TheFoldersTreeFollowsNameLocationAndFormat_WhileTheSectionIsOpen()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.OpenSection(NewProjectDialogView.FoldersExpanderName);
        string elsewhere = fixture.MakeFolder("Elsewhere");

        fixture.Dialog.TypeName("Renamed");
        fixture.Dialog.TypeLocation(elsewhere);
        fixture.Dialog.ChooseFileFormat(NewProjectUiText.XmlFormat);

        List<string> rows = fixture.TreeRowTexts();
        rows.ShouldContain(NewProjectUiText.Folder(Path.Combine(elsewhere, "Renamed")));
        rows.ShouldContain("Renamed.gumx");
        rows.ShouldNotContain("Renamed.gumj");
        fixture.ViewModel.FolderTree.Single().Children.Where(child => child.IsMainFile).Select(child => child.Name).ShouldBe(new[] { "Renamed.gumx" });
    }

    [AvaloniaFact]
    public void ABlankNameShowsTheDefaultName_AndTheTreeDoesNotCrash()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.OpenSection(NewProjectDialogView.FoldersExpanderName);
        fixture.Window.Click(fixture.Dialog.NameBox);
        fixture.Window.Press(global::Avalonia.Input.Key.A, global::Avalonia.Input.PhysicalKey.A, global::Avalonia.Input.RawInputModifiers.Control);
        fixture.Window.Press(global::Avalonia.Input.Key.Back, global::Avalonia.Input.PhysicalKey.Backspace);

        fixture.TreeRowTexts().ShouldContain("MyGame.gumj", "the tree falls back to the default name while OK is off");
        fixture.Settle();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void AVeryLongLocation_ScrollsTheTreeSideways_WithoutClippingTheRoot_OrWideningTheWindow()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string longLocation = fixture.Location;
        for (int i = 0; i < 8; i++)
        {
            longLocation = Path.Combine(longLocation, "a_rather_long_folder_name_" + i);
        }
        fixture.Dialog.TypeLocation(longLocation);
        double widthBefore = fixture.Window.Window.Bounds.Width;
        fixture.Dialog.OpenFolders();

        TreeView tree = fixture.Window.Find<TreeView>();
        ScrollViewer scroller = tree.GetVisualDescendants().OfType<ScrollViewer>().First();
        scroller.Extent.Width.ShouldBeGreaterThan(scroller.Viewport.Width, "the path is longer than the box");
        scroller.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.ScrollBar>()
            .Any(bar => bar.Orientation == global::Avalonia.Layout.Orientation.Horizontal && bar.IsEffectivelyVisible && bar.Bounds.Width > 0).ShouldBeTrue("a horizontal bar offers the rest");
        fixture.Window.Window.Bounds.Width.ShouldBe(widthBefore, 0.5, "a long path does not widen the dialog");
        fixture.Settle();
        fixture.BoundsOf(fixture.Dialog.OkButton).Right.ShouldBeLessThanOrEqualTo(fixture.Window.Window.Bounds.Width);

        scroller.Offset = new Vector(scroller.Extent.Width - scroller.Viewport.Width, 0);
        fixture.Window.Layout();
        TextBlock root = tree.GetVisualDescendants().OfType<TextBlock>().First();
        ScrollContentPresenter presenter = scroller.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
        fixture.BoundsOf(root).Right.ShouldBeLessThanOrEqualTo(fixture.BoundsOf(presenter).Right + 1, "the end of the path can be scrolled into view");
        root.Text.ShouldBe(NewProjectUiText.Folder(Path.Combine(longLocation, "MyGame")), "the whole path is in the row, not cut off");
    }

    [AvaloniaFact]
    public void TheFoldersListedForAGumOnlyProject_AreTheOnesTheToolWritesBesideTheProjectFile()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        List<string> folders = new List<string>();
        List<string> mainFiles = new List<string>();
        NewProjectUiFixture.CollectLayout(fixture.ViewModel.FolderTree.Single(), folders, mainFiles);

        folders.ShouldBe(new[] { "Behaviors", "Components", "Fonts", "Screens", "Standards" }, ignoreOrder: true);
        mainFiles.ShouldBe(new[] { "Game.gumj" });
    }
}
