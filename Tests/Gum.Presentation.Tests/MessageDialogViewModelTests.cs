using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for MessageDialogViewModel, relocated out of Gum.csproj into
/// the headless Gum.Presentation assembly (ADR-0005, #3754). Its View stays in the Gum tool
/// assembly, paired via [Dialog(typeof(MessageDialogViewModel))] on MessageDialogView - see
/// DialogViewResolverTests (GumToolUnitTests) for the cross-assembly resolution pin.
/// </summary>
public class MessageDialogViewModelTests
{
    [Fact]
    public void Constructor_DefaultsAffirmativeAndNegativeText_FromDialogViewModelBase()
    {
        MessageDialogViewModel viewModel = new();

        viewModel.AffirmativeText.ShouldBe("OK");
        viewModel.NegativeText.ShouldBe("Cancel");
    }

    [Fact]
    public void TitleAndMessage_RoundTrip()
    {
        MessageDialogViewModel viewModel = new()
        {
            Title = "Gum",
            Message = "Something went wrong.",
        };

        viewModel.Title.ShouldBe("Gum");
        viewModel.Message.ShouldBe("Something went wrong.");
    }
}
