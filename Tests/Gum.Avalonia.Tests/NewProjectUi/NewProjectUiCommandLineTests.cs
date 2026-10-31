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

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("raylib");
        fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, false);
        copied.Add(ClipboardAfterCopy(fixture)!);
        copied[1].ShouldBe(fixture.Dialog.CommandLine);
        copied[1].ShouldContain("--platform raylib --no-restore");

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, false);
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
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform(platform);
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, forms);
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
            fixture.Dialog.ChooseFileFormat(NewProjectUiText.XmlFormat);
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

        request.Path.ShouldBe(Path.Combine(location, "Game", "Game.gumj"));
        fixture.Service.Validate(request).Error.ShouldBeNull();
    }

    private static string Game(NewProjectUiFixture fixture) => Path.Combine(fixture.Location, "MyGame");

    private static string GumOnlyPath(NewProjectUiFixture fixture) => Path.Combine(fixture.Location, "MyGame", "MyGame.gumj");

    [AvaloniaFact]
    public void NoRestore_AppearsOnlyWhenRestoreIsUntickedWithACodeProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.OpenCommandLine().ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\"");

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.CheckBoxLabeled(NewProjectUiText.RestorePackages).IsChecked.ShouldBe(true);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform monogame");

        fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform monogame --no-restore");

        fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, true);
        fixture.Dialog.CommandLine.ShouldNotContain("--no-restore");

        // Unticked, then the C# project goes away: a Gum-only project has no restore to skip.
        fixture.Dialog.SetChecked(NewProjectUiText.RestorePackages, false);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\"");
    }

    [AvaloniaFact]
    public void SourceLinked_AppearsForATickedLocalSource_AndForKniSkia_ButNeverForAGumOnlyProject()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.ChoosePlatform("kni");
        fixture.Dialog.OpenCommandLine().ShouldBe($"gumcli new \"{Game(fixture)}\" --platform kni");

        fixture.Dialog.SetChecked(NewProjectUiText.UseLocalSource, true);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform kni --source-linked");

        fixture.Dialog.SetChecked(NewProjectUiText.UseLocalSource, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform kni");

        fixture.Dialog.ChoosePlatform("kniskia");
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform kniskia --source-linked");

        // A tick left behind does not leak into a Gum-only command.
        fixture.Dialog.ChoosePlatform("kni");
        fixture.Dialog.SetChecked(NewProjectUiText.UseLocalSource, true);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\"");
    }

    [AvaloniaFact]
    public void TemplateEmpty_AppearsExactlyWhenFormsIsUnticked()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.OpenCommandLine().ShouldNotContain("--template");

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\" --template empty");

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform monogame --template empty");

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, true);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{Game(fixture)}\" --platform monogame");
    }

    [AvaloniaFact]
    public void Theme_AppearsForANamedThemeWithForms_NotForStandard_AndNotWithFormsOff()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.OpenCommandLine().ShouldNotContain("--theme");

        fixture.Dialog.ChooseTheme("Bubblegum");
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\" --theme Bubblegum");

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\" --template empty");

        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, true);
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\" --theme Bubblegum");

        fixture.Dialog.ChooseTheme("Standard");
        fixture.Dialog.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(fixture)}\"");
    }
}
