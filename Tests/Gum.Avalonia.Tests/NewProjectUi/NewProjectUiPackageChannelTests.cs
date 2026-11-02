using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.ProjectServices;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The "Gum packages" radio group of the New Project dialog (#7356), driven with real clicks in the head's own window:
/// Stable by default, the Preview warning, Local Gum source availability, and the command line each pick gives.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiPackageChannelTests
{
    private static string Game(NewProjectUiFixture fixture) => Path.Combine(fixture.Location, "MyGame");

    [AvaloniaFact]
    public void TheGroup_DefaultsToStable_WithNoWarning()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);

        fixture.Settle();
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Stable).IsChecked.ShouldBe(true);
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Preview).IsChecked.ShouldBe(false);
        fixture.Dialog.ToggleLabeled(NewProjectUiText.UseLocalSource).IsChecked.ShouldBe(false);
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.PreviewWarning).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ClickingPreview_ChecksOnlyIt_ShowsTheWarning_AndAddsThePreviewFlag()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("kni");

        fixture.Dialog.SetChecked(NewProjectUiText.Preview, true);

        fixture.Dialog.ToggleLabeled(NewProjectUiText.Stable).IsChecked.ShouldBe(false);
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.PreviewWarning).ShouldBeTrue();
        fixture.Dialog.OpenCommandLine().ShouldContain("--platform kni --preview");

        fixture.Dialog.SetChecked(NewProjectUiText.Stable, true);

        fixture.Settle();
        fixture.IsShown(NewProjectUiText.PreviewWarning).ShouldBeFalse();
        fixture.Dialog.CommandLine.ShouldNotContain("--preview");
    }

    [AvaloniaFact]
    public void WithNoGumCheckout_LocalGumSourceIsDisabled_ButPreviewIsNot()
    {
        Mock<INewProjectService> service = new Mock<INewProjectService>();
        service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);
        service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation { Resolved = new ResolvedNewProject { Name = "Game" } });
        using NewProjectUiFixture fixture = new NewProjectUiFixture(service.Object);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);

        fixture.Dialog.ToggleLabeled(NewProjectUiText.UseLocalSource).IsEffectivelyEnabled.ShouldBeFalse();
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Preview).IsEffectivelyEnabled.ShouldBeTrue();
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Stable).IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void KniSkia_ChecksLocalGumSource_AndDisablesStableAndPreview()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.SetChecked(NewProjectUiText.Preview, true);

        fixture.Dialog.ChoosePlatform("kniskia");

        fixture.Dialog.ToggleLabeled(NewProjectUiText.UseLocalSource).IsChecked.ShouldBe(true);
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Preview).IsEffectivelyEnabled.ShouldBeFalse();
        fixture.Dialog.ToggleLabeled(NewProjectUiText.Stable).IsEffectivelyEnabled.ShouldBeFalse();
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.PreviewWarning).ShouldBeFalse();
    }
}
