using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.ProjectServices;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The Command line section of the New Project dialog: Copy puts exactly the shown command on the clipboard, and the
/// shown command, handed to the service <c>gumcli new</c> runs, creates exactly what the dialog creates.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiCommandLineTests
{
    // The request gumcli new builds from a command line (NewCommand.Execute): the path, then the options.
    private static NewProjectRequest ParseCommand(string command)
    {
        List<string> tokens = Regex.Matches(command, "\"([^\"]*)\"|\\S+").Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Value).ToList();
        tokens[0].ShouldBe("gumcli");
        tokens[1].ShouldBe("new");
        string? path = tokens.Count > 2 && !tokens[2].StartsWith("--") ? tokens[2] : null;
        string template = "forms";
        string? theme = null;
        string? platform = null;
        bool noRestore = false;
        bool sourceLinked = false;
        for (int i = 2; i < tokens.Count; i++)
        {
            switch (tokens[i])
            {
                case "--template":
                    template = tokens[++i];
                    break;
                case "--theme":
                    theme = tokens[++i];
                    break;
                case "--platform":
                    platform = tokens[++i];
                    break;
                case "--no-restore":
                    noRestore = true;
                    break;
                case "--source-linked":
                    sourceLinked = true;
                    break;
            }
        }
        return new NewProjectRequest { Path = path, Template = template, Theme = theme, Platform = platform, SourceLinked = sourceLinked, Restore = !noRestore };
    }

    private static string? ClipboardAfterCopy(NewProjectUiFixture fixture)
    {
        fixture.Window.Click(fixture.Window.Find<global::Avalonia.Controls.Button>(button => button.Name == NewProjectDialogView.CopyButtonName));
        fixture.WaitUntil(() => fixture.Window.ClipboardText() != null, "the clipboard text");
        return fixture.Window.ClipboardText();
    }

    [AvaloniaFact]
    public void Copy_PutsExactlyTheShownCommandOnTheClipboard_InEachState_AndNeverAStaleOne()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.OpenCommandLine();
        List<string> copied = new List<string>();

        copied.Add(ClipboardAfterCopy(fixture)!);
        copied[0].ShouldBe(fixture.Dialog.CommandLine);

        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("raylib");
        fixture.Dialog.SetChecked(NewProjectDialogText.RestorePackages, false);
        copied.Add(ClipboardAfterCopy(fixture)!);
        copied[1].ShouldBe(fixture.Dialog.CommandLine);
        copied[1].ShouldContain("--platform raylib --no-restore");

        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, false);
        fixture.Dialog.ChooseTheme("Bubblegum");
        fixture.Dialog.TypeName("Another Name");
        copied.Add(ClipboardAfterCopy(fixture)!);
        copied[2].ShouldBe(fixture.Dialog.CommandLine);
        copied[2].ShouldContain("--theme Bubblegum");
        copied.Distinct().Count().ShouldBe(3);
    }

    [AvaloniaFact]
    public void Copy_StillCopiesWhileTheNameIsInvalid_TheCommandThatIsShown()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.OpenCommandLine();
        fixture.Dialog.TypeName("bad/name");

        string? copied = ClipboardAfterCopy(fixture);

        copied.ShouldBe(fixture.Dialog.CommandLine);
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
    }

    public static IEnumerable<object[]> CodeProjectCases()
    {
        yield return new object[] { "raylib", "Standard", true };
        yield return new object[] { "monogame", "Bubblegum", true };
        yield return new object[] { "kni", "Standard", false };
        yield return new object[] { "kniskia", "Standard", true };
        yield return new object[] { "silknet", "Bubblegum", true };
        yield return new object[] { "stride", "Standard", false };
    }

    [AvaloniaTheory]
    [MemberData(nameof(CodeProjectCases))]
    public void TheShownCommand_RunThroughTheNewProjectService_CreatesExactlyWhatTheDialogCreates(string platform, string theme, bool forms)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectDialogText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform(platform);
        fixture.Dialog.SetChecked(NewProjectDialogText.IncludeForms, forms);
        if (forms)
        {
            fixture.Dialog.ChooseTheme(theme);
        }
        string command = fixture.Dialog.OpenCommandLine();
        INewProjectService service = fixture.Service;

        // gumcli new: validate, then create the files.
        NewProjectValidation validation = service.Validate(ParseCommand(command));
        validation.Error.ShouldBeNull(validation.Error?.Message);
        NewProjectFilesResult byCommand = service.CreateFilesAsync(validation.Resolved!, CancellationToken.None).GetAwaiter().GetResult();
        byCommand.Success.ShouldBeTrue(byCommand.ErrorMessage);
        string commandGameFolder = fixture.GameFolder("Game");
        string commandCopy = Path.Combine(fixture.Root, "ByCommand");
        Directory.Move(commandGameFolder, commandCopy);

        // The dialog, into the same place.
        fixture.Dialog.ClickOk();

        List<string> byDialogFiles = NewProjectUiFixture.RelativeFiles(commandGameFolder);
        List<string> byCommandFiles = NewProjectUiFixture.RelativeFiles(commandCopy);
        byDialogFiles.ShouldBe(byCommandFiles, "the same files");
        foreach (string file in byCommandFiles)
        {
            File.ReadAllBytes(Path.Combine(commandGameFolder, file)).ShouldBe(File.ReadAllBytes(Path.Combine(commandCopy, file)), $"{file} has the same bytes");
        }
    }

    [AvaloniaTheory]
    [InlineData(false, "Standard")]
    [InlineData(true, "Standard")]
    [InlineData(true, "Bubblegum")]
    public void TheShownGumOnlyCommand_NamesTheProjectFileTheDialogCreates_AndTheServiceWritesIt(bool xml, string theme)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        if (xml)
        {
            fixture.Dialog.ChooseFileFormat(NewProjectDialogViewModel.XmlFormat);
        }
        fixture.Dialog.ChooseTheme(theme);
        string command = fixture.Dialog.OpenCommandLine();
        NewProjectRequest request = ParseCommand(command);

        NewProjectValidation validation = fixture.Service.Validate(request);
        validation.Error.ShouldBeNull(validation.Error?.Message);
        NewProjectFilesResult files = fixture.Service.CreateFilesAsync(validation.Resolved!, CancellationToken.None).GetAwaiter().GetResult();

        files.Success.ShouldBeTrue(files.ErrorMessage);
        Path.GetFullPath(files.GumProjectPath!).ShouldBe(Path.GetFullPath(fixture.ViewModel.GumProjectPath));
        File.Exists(fixture.ViewModel.GumProjectPath).ShouldBeTrue();
        Path.GetExtension(files.GumProjectPath).ShouldBe(xml ? ".gumx" : ".gumj");
        request.Theme.ShouldBe(theme == "Standard" ? null : theme);

        fixture.Dialog.TypeName("Other");
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.IsShownStartingWith("Project already exists").ShouldBeTrue("the dialog now refuses the project the command made");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse();
    }

    [AvaloniaTheory]
    [InlineData("Games With Spaces")]
    [InlineData("Games (copy)")]
    [InlineData("Games & More")]
    public void ALocationWithSpecialCharacters_IsQuotedInTheCommand_AndTheServiceReadsItBack(string folderName)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string location = fixture.MakeFolder(folderName);
        fixture.Dialog.TypeName("Game");
        fixture.Dialog.TypeLocation(location);
        string command = fixture.Dialog.OpenCommandLine();

        NewProjectRequest request = ParseCommand(command);

        request.Path.ShouldBe(NewProjectDisplayPath.Normalize(Path.Combine(location, "Game", "Game.gumj")));
        fixture.Service.Validate(request).Error.ShouldBeNull();
    }
}
