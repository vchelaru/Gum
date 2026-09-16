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

        ScrollViewer scroller = window.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Content == view);
        scroller.Extent.Height.ShouldBeGreaterThan(scroller.Viewport.Height);

        Button ok = window.GetVisualDescendants().OfType<Button>().First(button => button.Name == DialogWindow.AffirmativeButtonName);
        Point origin = ok.TranslatePoint(new Point(0, 0), window)!.Value;
        (origin.Y + ok.Bounds.Height).ShouldBeLessThanOrEqualTo(window.Bounds.Height);
        ok.Bounds.Height.ShouldBeGreaterThan(0);
        window.Close();
    }
}
