using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The platform drop-down of the New Project dialog, driven with the arrow keys in the head's own window: what each of
/// the six platforms shows, forces and enables, and that a create writes that platform's files.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiPlatformTests
{
    public static IEnumerable<object[]> Platforms() =>
        new[] { "monogame", "kni", "kniskia", "raylib", "stride", "silknet" }.Select(name => new object[] { name });

    private static string? ExpectedNote(string platform) => platform switch
    {
        "stride" => NewProjectDialogText.StridePlatformNote,
        "kniskia" => NewProjectDialogText.KniSkiaPlatformNote,
        "silknet" => NewProjectDialogText.SilkNetPlatformNote,
        _ => null,
    };

    private static void StartCodeProject(NewProjectUiFixture fixture, string platform)
    {
        fixture.Dialog.TypeName("Game");
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform(platform);
        fixture.Settle();
    }

    [AvaloniaFact]
    public void PlatformDropDown_ListsTheSixPlatformsInOrder_AndStartsOnMonoGame()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);

        fixture.Dialog.PlatformBox.ItemCount.ShouldBe(6);
        fixture.ViewModel.Platforms.Select(option => option.Name).ShouldBe(new[] { "monogame", "kni", "kniskia", "raylib", "stride", "silknet" });
        fixture.Dialog.PlatformBox.SelectedValue.ShouldBe("monogame");
        fixture.Dialog.PlatformBox.IsEffectivelyVisible.ShouldBeTrue();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Platforms))]
    public void SelectingAPlatform_UpdatesTheCommandLine_TheNote_AndTheLocalSourceBox(string platform)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Service.IsSourceLinkedAvailable.ShouldBeTrue("these tests run inside a Gum checkout");
        StartCodeProject(fixture, platform);

        fixture.Dialog.OpenCommandLine().ShouldContain($"--platform {platform}");
        fixture.ViewModel.PlatformName.ShouldBe(platform);

        string? note = ExpectedNote(platform);
        if (note == null)
        {
            fixture.IsShownStartingWith("Runs on").ShouldBeFalse();
            fixture.ViewModel.PlatformNote.ShouldBeEmpty();
        }
        else
        {
            fixture.IsShown(note).ShouldBeTrue($"the {platform} note shows");
        }

        CheckBox localSource = fixture.Dialog.CheckBoxLabeled(NewProjectDialogText.UseLocalSource);
        if (platform == "kniskia")
        {
            localSource.IsChecked.ShouldBe(true, "KNI with Skia always links the source");
            localSource.IsEffectivelyEnabled.ShouldBeFalse("and the box cannot be changed");
            fixture.Dialog.CommandLine.ShouldContain("--source-linked");
        }
        else
        {
            localSource.IsChecked.ShouldBe(false);
            localSource.IsEffectivelyEnabled.ShouldBeTrue();
            fixture.Dialog.CommandLine.ShouldNotContain("--source-linked");
        }
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Platforms))]
    public void CreatingEachPlatform_WritesItsFiles_RunsTheRestoreOnTheCsproj_AndMatchesTheFoldersTheDialogShowed(string platform)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        List<string> restored = fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        StartCodeProject(fixture, platform);
        List<string> folders = new List<string>();
        List<string> mainFiles = new List<string>();
        fixture.FolderRows();
        NewProjectUiFixture.CollectLayout(fixture.ViewModel.FolderTree.Single(), folders, mainFiles);

        fixture.Dialog.ClickOk();

        string gameFolder = fixture.GameFolder("Game");
        string csproj = Path.Combine(gameFolder, "Game.csproj");
        restored.ShouldHaveSingleItem().ShouldBe(csproj);
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeTrue();
        File.Exists(csproj).ShouldBeTrue();
        File.Exists(Path.Combine(gameFolder, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(gameFolder, "Game1.cs")).ShouldBe(platform is "monogame" or "kni" or "kniskia");
        Path.GetFullPath(fixture.ViewModel.CreatedGumProjectPath!).ShouldBe(Path.Combine(gameFolder, "Content", "GumProject", "GumProject.gumj"));
        File.Exists(fixture.ViewModel.CreatedGumProjectPath!).ShouldBeTrue();

        // The Folders tree promised exactly these.
        folders.ShouldNotBeEmpty();
        foreach (string folder in folders)
        {
            Directory.Exists(Path.Combine(gameFolder, folder)).ShouldBeTrue($"{folder} was shown for {platform}");
        }
        mainFiles.ShouldBe(new[] { "Content/GumProject/GumProject.gumj" });
        string reference = File.ReadAllText(csproj);
        if (platform == "kniskia")
        {
            reference.ShouldContain("ProjectReference", Case.Sensitive, "KNI with Skia links the Gum source");
        }
        else
        {
            reference.ShouldContain("PackageReference", Case.Sensitive, "the other platforms use the NuGet package");
        }
    }

    [AvaloniaFact]
    public void KniSkiaWithNoGumCheckout_ShowsWhyOkIsOff_AndKniBringsOkBack()
    {
        Mock<INewProjectService> service = new Mock<INewProjectService>();
        service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);
        service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation { Resolved = new ResolvedNewProject { Name = "Game" } });
        using NewProjectUiFixture fixture = new NewProjectUiFixture(service.Object);
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);

        fixture.Dialog.CheckBoxLabeled(NewProjectDialogText.UseLocalSource).IsEffectivelyEnabled.ShouldBeFalse("no checkout, nothing to link");
        fixture.Dialog.ChoosePlatform("kniskia");
        fixture.Settle();

        fixture.Dialog.CheckBoxLabeled(NewProjectDialogText.UseLocalSource).IsChecked.ShouldBe(true);
        fixture.IsShown(NewProjectDialogText.NeedsLocalSource).ShouldBeTrue("the dialog says why OK is off");
        fixture.IsShown(NewProjectDialogText.KniSkiaPlatformNote).ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();

        fixture.Dialog.ChoosePlatform("kni");
        fixture.Settle();

        fixture.IsShown(NewProjectDialogText.NeedsLocalSource).ShouldBeFalse();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
        fixture.Dialog.CheckBoxLabeled(NewProjectDialogText.UseLocalSource).IsChecked.ShouldBe(false, "the forced tick does not stay behind");
    }

    [AvaloniaFact]
    public void TickingLocalSource_SurvivesAVisitToKniSkia()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.SetChecked(NewProjectDialogText.UseLocalSource, true);
        fixture.Dialog.OpenCommandLine().ShouldContain("--source-linked");

        fixture.Dialog.ChoosePlatform("kniskia");
        fixture.Dialog.ChoosePlatform("raylib");

        fixture.Dialog.CheckBoxLabeled(NewProjectDialogText.UseLocalSource).IsChecked.ShouldBe(true, "the user's own tick is still there");
        fixture.Dialog.CommandLine.ShouldContain("--platform raylib");
        fixture.Dialog.CommandLine.ShouldContain("--source-linked");
    }

    [AvaloniaFact]
    public void PlatformNote_FollowsThePlatform_AndGoesWhenTheCodeProjectIsUnticked()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        StartCodeProject(fixture, "stride");
        fixture.IsShown(NewProjectDialogText.StridePlatformNote).ShouldBeTrue();

        fixture.Dialog.ChoosePlatform("silknet");
        fixture.IsShown(NewProjectDialogText.StridePlatformNote).ShouldBeFalse();
        fixture.IsShown(NewProjectDialogText.SilkNetPlatformNote).ShouldBeTrue();

        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, false);
        fixture.IsShown(NewProjectDialogText.SilkNetPlatformNote).ShouldBeFalse("the platform choices are hidden with the code project");
        fixture.Dialog.PlatformBox.IsEffectivelyVisible.ShouldBeFalse();

        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.PlatformBox.SelectedValue.ShouldBe("silknet", "the platform is remembered");
        fixture.IsShown(NewProjectDialogText.SilkNetPlatformNote).ShouldBeTrue();
    }
}
