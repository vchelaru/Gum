using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Which window owns (is disabled by) a dialog the service opens (#5538).</summary>
public class DialogOwnershipTests
{
    [AvaloniaFact]
    public void DialogOpenedFromADialog_IsOwnedByThatDialog_AndOwnershipReturnsToItOnceTheInnerCloses()
    {
        // Avalonia's ShowDialog disables only the owner, so a prompt owned by the main window left
        // the dialog that raised it clickable.
        AvaloniaDialogService service = TestAppBuilder.Services.GetRequiredService<AvaloniaDialogService>();
        DialogWindow? outer = null;
        Window? firstInnerOwner = null;
        Window? secondInnerOwner = null;
        int openAfterFirstInnerClosed = -1;

        Dispatcher.UIThread.Post(() =>
        {
            outer = service.OpenDialogs.Single();
            firstInnerOwner = ShowAndCloseInner(service);
            openAfterFirstInnerClosed = service.OpenDialogs.Count;
            secondInnerOwner = ShowAndCloseInner(service);
            outer.Close();
        });
        service.ShowMessage("Outer");

        outer.ShouldNotBeNull();
        firstInnerOwner.ShouldBeSameAs(outer);
        openAfterFirstInnerClosed.ShouldBe(1);
        secondInnerOwner.ShouldBeSameAs(outer);
        service.OpenDialogs.ShouldBeEmpty();
    }

    private static Window? ShowAndCloseInner(AvaloniaDialogService service)
    {
        Window? owner = null;
        Dispatcher.UIThread.Post(() =>
        {
            DialogWindow inner = service.OpenDialogs[^1];
            owner = inner.Owner as Window;
            inner.Close();
        });
        service.ShowMessage("Inner");
        return owner;
    }
}
