using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

public class DialogWindowDriverTests
{
    [AvaloniaFact]
    public void Press_ThatClosesTheDialogOnALaterDispatcherPass_DoesNotReleaseTheKeyOnTheClosedWindow()
    {
        ChoiceDialogViewModel viewModel = new ChoiceDialogViewModel { Title = "Conflict", Message = "Pick one." };
        viewModel.SetOptions(new Dictionary<int, string> { [0] = "Skip" });
        using DialogWindowDriver dialog = new DialogWindowDriver(viewModel);
        // Enter accepts through a dispatcher job, not synchronously inside the key press.
        dialog.Window.KeyDown += (_, _) => Dispatcher.UIThread.Post(dialog.Window.Close, DispatcherPriority.ApplicationIdle);

        dialog.Press(Key.Enter, PhysicalKey.Enter);

        dialog.IsOpen.ShouldBeFalse();
    }
}
