using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// A dialog never grows past the screen: a tall view scrolls inside the window and the buttons
/// stay reachable, as the WPF <c>DialogWindow</c> caps its height to the owner and scrolls its content.
/// </summary>
public class DialogWindowSizingTests
{
    [AvaloniaFact]
    public void TallView_ScrollsInsideTheWindow_KeepingTheButtonsOnScreen()
    {
        // A rename-state prompt listing hundreds of affected references (#4796).
        string message = "Enter new state name\n\n" + string.Join("\n", Enumerable.Range(0, 300).Select(i => $"Component{i} Instance{i}.State"));
        GetUserStringDialogViewModel viewModel = new GetUserStringDialogViewModel { Message = message, AffirmativeText = "Ok", NegativeText = "Cancel" };
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.FitHeightToScreen(owner: null);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Screen screen = window.Screens.Primary!;
        double screenHeight = screen.WorkingArea.Height / screen.Scaling;
        window.Bounds.Height.ShouldBeLessThanOrEqualTo(screenHeight);

        // Only the message scrolls; the text box and the buttons stay pinned in view beneath it.
        ScrollViewer scroller = window.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Extent.Height > s.Viewport.Height);
        scroller.GetVisualDescendants().OfType<TextBox>().ShouldBeEmpty();
        ShouldBeInsideWindow(window.GetVisualDescendants().OfType<TextBox>().First(), window);
        ShouldBeInsideWindow(window.GetVisualDescendants().OfType<Button>().First(button => button.Name == DialogWindow.AffirmativeButtonName), window);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollContentFalse_LeavesTheViewToScrollItself()
    {
        // Twin of WPF's Dialog.ScrollContent="False": the view gets the bounded height, not a scroller.
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Message = "m", AffirmativeText = "Ok" };
        Border view = new Border { Height = 5000 };
        DialogWindow.SetScrollContent(view, false);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.MaxHeight = 400;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.GetVisualDescendants().OfType<ScrollViewer>().ShouldBeEmpty();
        window.Bounds.Height.ShouldBeLessThanOrEqualTo(400);
        window.Close();
    }

    private static void ShouldBeInsideWindow(Control control, Window window)
    {
        Point origin = control.TranslatePoint(new Point(0, 0), window)!.Value;
        control.Bounds.Height.ShouldBeGreaterThan(0);
        origin.Y.ShouldBeGreaterThanOrEqualTo(0);
        (origin.Y + control.Bounds.Height).ShouldBeLessThanOrEqualTo(window.Bounds.Height);
    }
}
