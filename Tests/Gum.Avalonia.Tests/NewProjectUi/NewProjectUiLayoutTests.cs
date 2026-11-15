using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The New Project dialog at the tool's zoom levels in both themes, with every banner, note and section showing: OK and
/// Cancel stay in the window and apart, no control sticks out of the window or lies on another one, and the sections
/// that scroll can be brought into view. Asserted on control bounds, not pixels.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiLayoutTests
{
    public static IEnumerable<object[]> ZoomAndTheme() =>
        new[] { 9d, 12d, 16d, 24d }.SelectMany(zoom => new[] { "light", "dark" }.Select(theme => new object[] { zoom, theme }));

    // Controls a person clicks or types in, not the parts inside them.
    private static List<Control> Interactive(NewProjectUiFixture fixture) =>
        fixture.Window.Window.GetVisualDescendants().OfType<Control>()
            .Where(control => control is TextBox or ComboBox or Button or CheckBox && control.IsEffectivelyVisible && control.Bounds.Width > 0 && control.Bounds.Height > 0)
            .Where(control => !control.GetVisualAncestors().Any(ancestor => ancestor is ComboBox or Button or CheckBox))
            .ToList();

    // Where a control is actually seen: the scrolling form clips what lies beyond its viewport.
    private static Rect? VisibleBounds(NewProjectUiFixture fixture, Control control)
    {
        Rect visible = fixture.BoundsOf(control);
        foreach (ScrollViewer scroller in control.GetVisualAncestors().OfType<ScrollViewer>())
        {
            visible = visible.Intersect(fixture.BoundsOf(scroller));
        }
        return visible.Width > 0 && visible.Height > 0 ? visible : null;
    }

    private static void AssertLaidOut(NewProjectUiFixture fixture, string state)
    {
        fixture.Window.Layout();
        Rect window = new Rect(fixture.Window.Window.Bounds.Size);
        Rect ok = fixture.BoundsOf(fixture.Dialog.OkButton);
        Rect cancel = fixture.BoundsOf(fixture.Dialog.CancelButton);

        fixture.Dialog.OkButton.IsEffectivelyVisible.ShouldBeTrue(state);
        fixture.Dialog.CancelButton.IsEffectivelyVisible.ShouldBeTrue(state);
        window.Contains(ok.TopLeft).ShouldBeTrue($"OK's top left is in the window ({state})");
        window.Contains(ok.BottomRight - new Vector(0.5, 0.5)).ShouldBeTrue($"OK is entirely in the window ({state}): {ok} in {window}");
        window.Contains(cancel.BottomRight - new Vector(0.5, 0.5)).ShouldBeTrue($"Cancel is entirely in the window ({state}): {cancel} in {window}");
        ok.Intersects(cancel).ShouldBeFalse($"OK and Cancel do not overlap ({state})");

        List<Control> controls = Interactive(fixture);
        controls.ShouldNotBeEmpty();
        foreach (Control control in controls)
        {
            Rect bounds = fixture.BoundsOf(control);
            bounds.Left.ShouldBeGreaterThanOrEqualTo(-0.5, $"{control.GetType().Name} sticks out on the left ({state})");
            bounds.Right.ShouldBeLessThanOrEqualTo(window.Width + 0.5, $"{control.GetType().Name} \"{(control as ContentControl)?.Content}\" sticks out on the right ({state}): {bounds} in {window}");
        }
        for (int i = 0; i < controls.Count; i++)
        {
            for (int j = i + 1; j < controls.Count; j++)
            {
                // A control scrolled out of view is clipped by the form's scroll viewer, so it cannot lie on the footer.
                Rect? a = VisibleBounds(fixture, controls[i]);
                Rect? b = VisibleBounds(fixture, controls[j]);
                if (a == null || b == null)
                {
                    continue;
                }
                Rect overlap = a.Value.Intersect(b.Value);
                bool overlaps = overlap.Width > 1 && overlap.Height > 1;
                overlaps.ShouldBeFalse($"{Describe(controls[i])} {a} overlaps {Describe(controls[j])} {b} ({state})");
            }
        }

        // Text the dialog wraps stays inside its column: a block wider than the window would be clipped.
        foreach (TextBlock block in fixture.Window.Window.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible && !string.IsNullOrEmpty(text.Text) && text.Bounds.Width > 0 && !text.GetVisualAncestors().OfType<TreeView>().Any()))
        {
            fixture.BoundsOf(block).Right.ShouldBeLessThanOrEqualTo(window.Width + 0.5, $"\"{block.Text}\" is clipped on the right ({state})");
        }
    }

    private static string Describe(Control control) => control switch
    {
        TextBox box => $"TextBox \"{box.Text}\"",
        ContentControl content => $"{control.GetType().Name} \"{(content.Content as string) ?? (content.Content as Control)?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text}\"",
        _ => control.GetType().Name,
    };

    [AvaloniaTheory]
    [MemberData(nameof(ZoomAndTheme))]
    public void WithEveryBannerAndSectionShowing_NothingIsClippedOrOverlapped_AndOkAndCancelStayReachable(double zoom, string theme)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetTheme(theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
        fixture.SetZoom(zoom);

        // A code project over an existing code project and a non-empty folder, with a platform note and both sections open.
        File.WriteAllText(Path.Combine(fixture.Location, "Other.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(fixture.MakeFolder("Out", "Game"), "notes.txt"), "mine");
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("stride");
        fixture.Settle();
        fixture.Dialog.OpenFolders();
        fixture.Dialog.OpenCommandLine();
        fixture.Settle();
        AssertLaidOut(fixture, $"code project, zoom {zoom}, {theme}");

        // Invalid name: the message sits beside the buttons.
        fixture.Dialog.TypeName("bad/name");
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.NameProblem("bad/name")).ShouldBeTrue();
        AssertLaidOut(fixture, $"invalid name, zoom {zoom}, {theme}");

        // The refusal: a Gum-only project over an existing project file.
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, false);
        fixture.Dialog.TypeName("Existing");
        File.WriteAllText(Path.Combine(fixture.MakeFolder("Out", "Existing"), "Existing.gumj"), "x");
        fixture.Dialog.TypeName("Existing2");
        fixture.Dialog.TypeName("Existing");
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.RefusedHeading).ShouldBeTrue();
        AssertLaidOut(fixture, $"refusal, zoom {zoom}, {theme}");

        Window window = fixture.Window.Window;
        Screen? screen = window.Screens.Primary;
        if (screen != null)
        {
            window.Bounds.Height.ShouldBeLessThanOrEqualTo(screen.WorkingArea.Height / screen.Scaling + 0.5, "the window fits the screen");
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(ZoomAndTheme))]
    public void WithRecommendedPackagesChecked_TheNoteAndTheChainedCommandLineFit_AndOkAndCancelStayReachable(double zoom, string theme)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetTheme(theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
        fixture.SetZoom(zoom);
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("monogame");
        fixture.ViewModel.IncludeRecommendedPackages = true;
        fixture.Settle();
        fixture.Dialog.OpenFolders();
        fixture.Dialog.OpenCommandLine();
        fixture.Settle();

        fixture.Dialog.CommandLine.ShouldContain("dotnet add");
        AssertLaidOut(fixture, $"recommended packages, zoom {zoom}, {theme}");
    }

    [AvaloniaTheory]
    [InlineData(9d)]
    [InlineData(24d)]
    public void ThePackagesFailurePanel_FitsAtTheExtremeZooms_WithItsThreeButtonsApart(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetZoom(zoom);
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.ViewModel.IncludeRecommendedPackages = true;
        fixture.Service.AddPackages = (_, _, _, _) => Task.FromResult(new Gum.ProjectServices.AddPackagesResult
        {
            Status = Gum.ProjectServices.RestoreStatus.Failed,
            FailedPackage = "Gum.Shapes.MonoGame",
            OutputTail = string.Join("\n", Enumerable.Range(0, 12).Select(i => $"error: could not write the project, line {i}"))
        });
        fixture.Settle();
        fixture.Dialog.ClickOkWithoutWaitingForClose();
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the packages failure");
        fixture.Service.AddPackages = null;

        AssertLaidOut(fixture, $"packages failure, zoom {zoom}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(ZoomAndTheme))]
    public void ThemePreview_IsLargeBesideANarrowDropDown_InsideTheWindowAndApartFromIt(double zoom, string theme)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetTheme(theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
        fixture.SetZoom(zoom);
        fixture.Settle();

        Image preview = fixture.Window.Window.GetVisualDescendants().OfType<Image>().First(image => image.IsEffectivelyVisible && image.Source != null);
        Rect window = new Rect(fixture.Window.Window.Bounds.Size);
        Rect previewBounds = fixture.BoundsOf(preview);
        Rect dropDown = fixture.BoundsOf(fixture.Dialog.ThemeBox);

        window.Contains(previewBounds.TopLeft).ShouldBeTrue($"the preview starts in the window ({zoom}, {theme})");
        previewBounds.Right.ShouldBeLessThanOrEqualTo(window.Width + 0.5, $"the preview is not clipped on the right ({zoom}, {theme})");
        previewBounds.Left.ShouldBeGreaterThanOrEqualTo(dropDown.Right, $"the drop-down ends before the preview starts ({zoom}, {theme})");
        previewBounds.Top.ShouldBe(dropDown.Top, 1, $"the preview top is flush with the drop-down ({zoom}, {theme})");

        if (zoom == 12)
        {
            previewBounds.Width.ShouldBeGreaterThanOrEqualTo(240, $"the preview is about 245 wide at 100%: {previewBounds}");
            previewBounds.Height.ShouldBeGreaterThanOrEqualTo(165, $"and about 170 tall: {previewBounds}");
            dropDown.Width.ShouldBeLessThanOrEqualTo(200, $"the drop-down is about 190 wide at 100%: {dropDown}");
            (previewBounds.Left - dropDown.Right).ShouldBe(20, 1, "with a 20 px gap at 100%");
        }
    }

    [AvaloniaTheory]
    [InlineData(9d)]
    [InlineData(24d)]
    public void TheRestoreFailurePanel_FitsAtTheExtremeZooms_WithItsThreeButtonsApart(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetZoom(zoom);
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.StandInRestore(NewProjectUiFixture.RestoreFailed(string.Join("\n", Enumerable.Range(0, 12).Select(i => $"error NU1101: Unable to find package number {i} in any source"))));
        fixture.Settle();
        fixture.Dialog.ClickOkWithoutWaitingForClose();
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the restore failure");

        AssertLaidOut(fixture, $"restore failure, zoom {zoom}");
    }

    [AvaloniaTheory]
    [InlineData(9d)]
    [InlineData(24d)]
    public void TheLastSection_CanBeScrolledIntoView_AtTheExtremeZooms(double zoom)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Window.Window.FitHeightToScreen(null);
        fixture.SetZoom(zoom);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.OpenFolders();
        fixture.Dialog.OpenCommandLine();

        SelectableTextBlock command = fixture.Window.Find<SelectableTextBlock>(text => text.Text != null && text.Text.StartsWith("gumcli new"));
        command.BringIntoView();
        fixture.Window.Layout();

        ScrollViewer scroller = command.GetVisualAncestors().OfType<ScrollViewer>().Last();
        Rect view = fixture.BoundsOf(scroller);
        Rect shown = fixture.BoundsOf(command);
        shown.Top.ShouldBeGreaterThanOrEqualTo(view.Top - 1, "the command line is not above the visible part");
        shown.Bottom.ShouldBeLessThanOrEqualTo(view.Bottom + 1, "or below it");
        fixture.Settle();
        fixture.BoundsOf(fixture.Dialog.OkButton).Top.ShouldBeGreaterThanOrEqualTo(view.Bottom - 1, "the buttons stay below the scrolling part");
    }
}
