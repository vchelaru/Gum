using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The amber warnings (an existing code project, a folder that is not empty) and the red refusal (the project
/// already exists) of the New Project dialog over real files on disk, and that fixing the input clears them.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiBannerTests
{
    private static bool IsExistingCodeProjectBannerShown(NewProjectUiFixture fixture) => fixture.IsShown(NewProjectDialogText.WarningHeading);

    private static bool IsNotEmptyBannerShown(NewProjectUiFixture fixture) => fixture.IsShown(NewProjectDialogText.TargetNotEmptyHeading);

    private static bool IsRefusalBannerShown(NewProjectUiFixture fixture) => fixture.IsShown(NewProjectDialogText.RefusedHeading);

    [AvaloniaTheory]
    [InlineData("Other.csproj")]
    [InlineData("Other.sln")]
    [InlineData("Other.slnx")]
    public void ACodeProjectInTheLocationFolder_ShowsTheAmberBanner_AndOkStaysOn(string existingFile)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        File.WriteAllText(Path.Combine(fixture.Location, existingFile), "<Project />");

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Settle();

        IsExistingCodeProjectBannerShown(fixture).ShouldBeTrue();
        fixture.IsShown($"Found {existingFile} in {NewProjectDisplayPath.Normalize(fixture.Location)}.").ShouldBeTrue();
        IsRefusalBannerShown(fixture).ShouldBeFalse();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue("an existing code project warns and does not block");
    }

    [AvaloniaFact]
    public void ACodeProjectInAParentFolder_ShowsTheBanner_NamingThatFolder()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string parent = fixture.MakeFolder("Out", "deeper");
        File.WriteAllText(Path.Combine(parent, "Parent.csproj"), "<Project />");
        string nested = fixture.MakeFolder("Out", "deeper", "still");

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.TypeLocation(nested);
        fixture.Settle();

        fixture.IsShown($"Found Parent.csproj in {NewProjectDisplayPath.Normalize(parent)}.").ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();

        fixture.Dialog.TypeLocation(fixture.Location);
        fixture.Settle();
        IsExistingCodeProjectBannerShown(fixture).ShouldBeFalse("Location is now a sibling branch, not under the code project");
    }

    [AvaloniaFact]
    public void TheExistingCodeProjectBanner_ComesAndGoesWithCreateCSharpProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        File.WriteAllText(Path.Combine(fixture.Location, "Other.csproj"), "<Project />");
        fixture.Settle();
        IsExistingCodeProjectBannerShown(fixture).ShouldBeFalse("a Gum-only project does not care about code projects");

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        IsExistingCodeProjectBannerShown(fixture).ShouldBeTrue();

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, false);
        IsExistingCodeProjectBannerShown(fixture).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ACodeProjectInsideObjOrBin_IsNotTakenForTheUsersProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string obj = fixture.MakeFolder("Out", "obj");
        File.WriteAllText(Path.Combine(obj, "Generated.csproj"), "<Project />");

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.TypeLocation(obj);
        fixture.Settle();

        IsExistingCodeProjectBannerShown(fixture).ShouldBeFalse("build output is skipped");
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ANonEmptyTargetFolder_ShowsTheAmberBanner_AndOkStaysOn(bool codeProject)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string target = fixture.MakeFolder("Out", "Game");
        File.WriteAllText(Path.Combine(target, "notes.txt"), "mine");

        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, codeProject);
        fixture.Settle();

        IsNotEmptyBannerShown(fixture).ShouldBeTrue();
        fixture.IsShown(NewProjectDialogText.TargetNotEmptyBody(NewProjectDisplayPath.Normalize(target))).ShouldBeTrue();
        IsRefusalBannerShown(fixture).ShouldBeFalse();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();

        fixture.Dialog.TypeName("Other");
        fixture.Settle();
        IsNotEmptyBannerShown(fixture).ShouldBeFalse("another name points at a folder that does not exist");
    }

    [AvaloniaFact]
    public void AnEmptyExistingTargetFolder_ShowsNoWarning()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.MakeFolder("Out", "Game");

        fixture.Dialog.TypeName("Game");
        fixture.Settle();

        IsNotEmptyBannerShown(fixture).ShouldBeFalse();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void CreatingIntoANonEmptyFolder_KeepsTheFilesAlreadyThere()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        string target = fixture.MakeFolder("Out", "Game");
        File.WriteAllText(Path.Combine(target, "notes.txt"), "mine");
        File.WriteAllText(Path.Combine(fixture.Location, "Other.csproj"), "<Project />");

        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Settle();
        IsNotEmptyBannerShown(fixture).ShouldBeTrue();
        IsExistingCodeProjectBannerShown(fixture).ShouldBeTrue();
        fixture.Dialog.ClickOk();

        File.ReadAllText(Path.Combine(target, "notes.txt")).ShouldBe("mine");
        File.Exists(Path.Combine(target, "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData(".gumj", false)]
    [InlineData(".gumx", false)]
    [InlineData(".gumj", true)]
    [InlineData(".gumx", true)]
    public void AnExistingProjectFileOfEitherFormat_ShowsTheRedRefusal_AndOkIsOff_ForEitherChosenFormat(string existingExtension, bool chooseXml)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string existing = Path.Combine(fixture.MakeFolder("Out", "Game"), "Game" + existingExtension);
        File.WriteAllText(existing, "keep me");

        fixture.Dialog.TypeName("Game");
        if (chooseXml)
        {
            fixture.Dialog.ChooseFileFormat(NewProjectDialogViewModel.XmlFormat);
        }
        fixture.Settle();

        IsRefusalBannerShown(fixture).ShouldBeTrue();
        fixture.IsShownStartingWith("Project already exists").ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);
        fixture.Dialog.Result.ShouldBeNull("Enter does not get past the refusal");
        File.ReadAllText(existing).ShouldBe("keep me");
    }

    [AvaloniaTheory]
    [InlineData("name")]
    [InlineData("location")]
    [InlineData("codeProject")]
    public void FixingTheInput_ClearsTheRefusal_AndTheNotEmptyWarning_AndBringsOkBack(string fix)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        File.WriteAllText(Path.Combine(fixture.MakeFolder("Out", "Game"), "Game.gumj"), "keep me");
        string otherLocation = fixture.MakeFolder("Elsewhere");
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        IsRefusalBannerShown(fixture).ShouldBeTrue();
        IsNotEmptyBannerShown(fixture).ShouldBeTrue("the folder holds the project file");

        switch (fix)
        {
            case "name":
                fixture.Dialog.TypeName("Fresh");
                break;
            case "location":
                fixture.Dialog.TypeLocation(otherLocation);
                break;
            default:
                // A C# project creates a different set of files, and none of them exists yet.
                fixture.Settle();
                fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
                break;
        }
        fixture.Settle();

        IsRefusalBannerShown(fixture).ShouldBeFalse();
        fixture.ViewModel.RefusalText.ShouldBeEmpty();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
        IsNotEmptyBannerShown(fixture).ShouldBe(fix == "codeProject", "only the same folder stays non-empty");
    }

    [AvaloniaTheory]
    [InlineData("Game.csproj", "Project already exists")]
    [InlineData("Content/GumProject/GumProject.gumj", "Gum project already exists")]
    [InlineData("Content/GumProject/GumProject.gumx", "Gum project already exists")]
    public void ACodeProjectOverExistingCodeProjectFiles_ShowsTheRefusal(string existingFile, string expectedStart)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string path = Path.Combine(fixture.GameFolder("Game"), existingFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "keep me");

        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Settle();

        IsRefusalBannerShown(fixture).ShouldBeTrue();
        fixture.IsShownStartingWith(expectedStart).ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();

        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, false);
        fixture.Settle();
        IsRefusalBannerShown(fixture).ShouldBeFalse("Gum-only looks for Game/Game.gumj, which is not there");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TheRefusalAndTheTwoAmberWarnings_CanShowTogether_WithoutHidingEachOther()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        File.WriteAllText(Path.Combine(fixture.Location, "Other.csproj"), "<Project />");
        
        string target = fixture.MakeFolder("Out", "Game");
        File.WriteAllText(Path.Combine(target, "Game.csproj"), "keep me");

        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Settle();

        IsExistingCodeProjectBannerShown(fixture).ShouldBeTrue();
        IsNotEmptyBannerShown(fixture).ShouldBeTrue();
        IsRefusalBannerShown(fixture).ShouldBeTrue();
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
    }
}
