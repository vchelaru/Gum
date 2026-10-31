using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// #7285: the line under Location that names the project folder (made, reused, or Location itself), shown in full and
/// wrapped, and the matching summary in the collapsed Folders header.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiFolderNoticeTests
{
    private const string FoldersSummaryName = "PART_FoldersSummary";
    private const string FolderNoticeName = "PART_FolderNotice";
    private static readonly string Separator = Path.DirectorySeparatorChar.ToString();

    private static TextBlock? Notice(NewProjectUiFixture fixture)
    {
        fixture.Window.Layout();
        return fixture.Window.Window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(block => block.Name == FolderNoticeName && block.IsEffectivelyVisible);
    }

    private static string TextOf(TextBlock block) =>
        block.Inlines is { Count: > 0 } inlines ? string.Concat(inlines.OfType<Run>().Select(run => run.Text)) : block.Text ?? string.Empty;

    private static string NoticeText(NewProjectUiFixture fixture)
    {
        TextBlock? notice = Notice(fixture);
        notice.ShouldNotBeNull("the line under Location is showing");
        return TextOf(notice) + " | " + PathBlock(fixture)!.Text;
    }

    private static NewProjectDialogView.FolderPathPanel? PathBlock(NewProjectUiFixture fixture)
    {
        fixture.Window.Layout();
        return fixture.Window.Window.GetVisualDescendants().OfType<NewProjectDialogView.FolderPathPanel>().FirstOrDefault(block => block.Name == "PART_FolderNoticePath" && block.IsEffectivelyVisible);
    }

    // How many folder pieces share the path's first row; one means the first line is just the drive designator.
    private static int FirstRowPieces(NewProjectDialogView.FolderPathPanel path) =>
        path.Children.Count(piece => Math.Abs(piece.Bounds.Top - path.Children[0].Bounds.Top) < 1);

    private static int Rows(NewProjectDialogView.FolderPathPanel path) =>
        path.Children.Select(piece => Math.Round(piece.Bounds.Top)).Distinct().Count();

    private static TextBlock? Summary(NewProjectUiFixture fixture)
    {
        fixture.Window.Layout();
        return fixture.Window.Window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(block => block.Name == FoldersSummaryName && block.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void ANewFolder_IsNamedInFull_RightUnderLocation()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("CodeProj3");
        fixture.Settle();

        NoticeText(fixture).ShouldBe($"Creates a new folder | {fixture.Location}{Separator}CodeProj3{Separator}");
        Rect location = fixture.BoundsOf(fixture.Dialog.LocationBox);
        Rect notice = fixture.BoundsOf(Notice(fixture)!);
        notice.Top.ShouldBeGreaterThanOrEqualTo(location.Bottom - 0.5, "below the Location field");
        (notice.Top - location.Bottom).ShouldBeLessThan(location.Height, "directly under it, not further down the dialog");
        Rect path = fixture.BoundsOf(PathBlock(fixture)!);
        path.Top.ShouldBeGreaterThanOrEqualTo(notice.Bottom - 0.5, "the path is on its own line under the heading");
        path.Left.ShouldBe(notice.Left, 0.5, "and starts at the same left edge");
        FirstRowPieces(PathBlock(fixture)!).ShouldBeGreaterThan(1, "never a lone drive designator on the first line");
    }

    [AvaloniaFact]
    public void AnExistingFolder_SaysItIsUsed()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.MakeFolder(Path.Combine("Out", "Taken"));
        fixture.Dialog.TypeName("Taken");
        fixture.Settle();

        NoticeText(fixture).ShouldBe($"Uses the existing folder | {fixture.Location}{Separator}Taken{Separator}");
    }

    [AvaloniaFact]
    public void ALocationThatIsTheProjectFolder_SaysSo_ForAFolderThatExists_AndOneThatDoesNot()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string existing = fixture.MakeFolder(Path.Combine("Out", "Here"));
        fixture.Dialog.TypeName("Here");
        fixture.Dialog.TypeLocation(existing);
        fixture.Settle();
        NoticeText(fixture).ShouldBe($"Uses this folder as the project folder (it already exists) | {existing}{Separator}");

        string missing = Path.Combine(fixture.Location, "Later");
        fixture.Dialog.TypeName("Later");
        fixture.Dialog.TypeLocation(missing);
        fixture.Settle();
        NoticeText(fixture).ShouldBe($"Uses this folder as the project folder (it will be created) | {missing}{Separator}");
    }

    [AvaloniaFact]
    public void TheLineFollowsTheNameAsItIsTyped()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("First");
        fixture.Settle();
        NoticeText(fixture).ShouldEndWith($"{Separator}First{Separator}");

        fixture.Dialog.TypeName("Second");
        fixture.Settle();

        NoticeText(fixture).ShouldEndWith($"{Separator}Second{Separator}");
    }

    [AvaloniaFact]
    public void WithAMessageUpInTheFooter_TheLineIsHidden()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Click(fixture.Dialog.NameBox);
        fixture.Window.Press(Key.A, PhysicalKey.A, RawInputModifiers.Control);
        fixture.Window.Press(Key.Back, PhysicalKey.Backspace);
        fixture.Settle();

        fixture.ViewModel.ValidationMessage.ShouldNotBeEmpty();
        Notice(fixture).ShouldBeNull();
        Summary(fixture).ShouldBeNull();
    }

    [AvaloniaTheory]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(24)]
    public void ALongPath_IsNeverAbbreviated_Wraps_AndKeepsTheWindowUsable(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        // A path with no spaces in it, as most are; a space would give the text rules a break to use.
        string location = OperatingSystem.IsWindows() ? @"C:\Users\You\Documents" : "/home/you/documents";
        for (int i = 0; i < 6; i++)
        {
            location = Path.Combine(location, "AVeryLongFolderNameNumber" + i);
        }
        fixture.Dialog.TypeName("TheProject");
        fixture.Dialog.TypeLocation(location);
        fixture.SetZoom(zoom);
        fixture.Settle();

        NewProjectDialogView.FolderPathPanel notice = PathBlock(fixture)!;
        notice.ShouldNotBeNull();
        string text = notice.Text!;
        text.ShouldBe($"{location}{Separator}TheProject{Separator}", "the whole path, with nothing added to it");
        text.Any(c => char.IsControl(c) || c == (char)0x200B).ShouldBeFalse("no zero-width or control characters, so a copy is the plain path");
        string.Concat(notice.Children.OfType<TextBlock>().Select(piece => piece.Text)).ShouldBe(text, "the pieces add up to the path, nothing cut or added");
        FirstRowPieces(notice).ShouldBeGreaterThan(1, "the first line is more than a drive designator");
        Rows(notice).ShouldBeGreaterThan(1, $"a path this long wraps to more lines at zoom {zoom}");

        Rect window = new Rect(fixture.Window.Window.Bounds.Size);
        Rect bounds = fixture.BoundsOf(notice);
        bounds.Right.ShouldBeLessThanOrEqualTo(window.Width + 0.5, "not clipped on the right");
        fixture.Window.Layout();
        Rect ok = fixture.BoundsOf(fixture.Dialog.OkButton);
        Rect cancel = fixture.BoundsOf(fixture.Dialog.CancelButton);
        window.Contains(ok.TopLeft).ShouldBeTrue("OK stays in the window");
        window.Contains(cancel.TopLeft).ShouldBeTrue("Cancel stays in the window");
        ok.Right.ShouldBeLessThanOrEqualTo(window.Width + 0.5);
    }

    [AvaloniaTheory]
    [InlineData(12)]
    [InlineData(24)]
    public void AFolderNameLongerThanTheLine_WrapsInsideItself_AndStaysInTheWindow(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string location = Path.Combine(OperatingSystem.IsWindows() ? @"C:\Users\You" : "/home/you", new string('x', 200));
        fixture.Dialog.TypeName("TheProject");
        fixture.Dialog.TypeLocation(location);
        fixture.SetZoom(zoom);
        fixture.Settle();

        NewProjectDialogView.FolderPathPanel path = PathBlock(fixture)!;
        path.ShouldNotBeNull();
        fixture.BoundsOf(path).Right.ShouldBeLessThanOrEqualTo(fixture.Window.Window.Bounds.Width + 0.5);
        path.Children.Max(piece => piece.Bounds.Right).ShouldBeLessThanOrEqualTo(path.Bounds.Width + 0.5, "no piece runs out of the panel");
        path.Text.ShouldBe($"{location}{Separator}TheProject{Separator}");
    }

    [AvaloniaFact]
    public void TheLine_IsNotFocusable_AndAddsNoTabStop()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Settle();

        TextBlock notice = Notice(fixture)!;
        notice.ShouldNotBeNull();
        notice.Focusable.ShouldBeFalse();
        notice.GetSelfAndVisualDescendants().OfType<InputElement>().Any(element => element.Focusable && element.IsTabStop).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void TheCollapsedFoldersHeader_SummarizesTheFolder_AndTheSummaryGoesWhenItOpens()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("CodeProj3");
        fixture.Settle();

        TextBlock summary = Summary(fixture)!;
        summary.ShouldNotBeNull("the collapsed header carries the summary");
        TextOf(summary).ShouldBe($"Creates CodeProj3{Separator} in Out");
        summary.Focusable.ShouldBeFalse();

        fixture.OpenSection(NewProjectDialogView.FoldersExpanderName);
        Summary(fixture).ShouldBeNull("the expanded tree already says it");
    }

    [AvaloniaFact]
    public void TheFoldersSummary_SaysTheExistingFolderIsUsed()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.MakeFolder(Path.Combine("Out", "Taken"));
        fixture.Dialog.TypeName("Taken");
        fixture.Settle();

        TextOf(Summary(fixture)!).ShouldBe("Uses the existing folder");
    }

    [AvaloniaTheory]
    [InlineData(9)]
    [InlineData(24)]
    public void TheFoldersHeader_StaysInsideTheWindow_AtEveryZoom(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("CodeProj3AndAVeryLongProjectNameToFillTheHeaderRow");
        fixture.Dialog.TypeLocation(fixture.MakeFolder("AParentFolderWithAnExtremelyLongNameThatDoesNotFitBesideTheHeading"));
        fixture.SetZoom(zoom);
        fixture.Settle();

        TextBlock summary = Summary(fixture)!;
        summary.ShouldNotBeNull();
        fixture.BoundsOf(summary).Right.ShouldBeLessThanOrEqualTo(fixture.Window.Window.Bounds.Width + 0.5, $"zoom {zoom}");
    }
}
