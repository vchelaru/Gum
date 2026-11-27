using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The Code tab offers Auto and Manual setup for a project with a .csproj above it and no code
/// settings yet, from the moment the project loads, without the Settings window's Code generation
/// page ever being opened; and stops offering it once the settings exist.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CodeTabSetupPromptScenarioTests
{
    private const string GameCsproj =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>SetupGame</RootNamespace>
          </PropertyGroup>
        </Project>
        """;

    private static string SettingsFile(string gumFolder) => Path.Combine(gumFolder, "ProjectCodeSettings.codsj");

    [AvaloniaFact]
    public void OpenProjectWithCsprojAndNoCodeSettings_OffersSetup_AndAutoEndsIt()
    {
        using CodeTabHarness code = new CodeTabHarness(
            folder => File.WriteAllText(Path.Combine(folder, "SetupGame.csproj"), GameCsproj),
            openProjectSettingsPage: false);
        File.Exists(SettingsFile(code.Project.ProjectFolder)).ShouldBeFalse();
        ScreenSave screen = code.Project.AddScreen("MainScreen");
        code.Tree.SaveAll();
        code.Select(screen);

        code.ShowsButton("Auto").ShouldBeTrue("the project has a .csproj and no code settings");
        code.ShowsButton("Manual").ShouldBeTrue();
        code.View.SettingsGrid.IsVisible.ShouldBeFalse();

        code.ClickButton("Auto");

        code.ShowsButton("Auto").ShouldBeFalse("Auto filled in the settings");
        code.View.SettingsGrid.IsVisible.ShouldBeTrue();

        code.Reopen(openProjectSettingsPage: false);
        code.Select(code.Project.Project.Screens.Single(candidate => candidate.Name == "MainScreen"));

        File.Exists(SettingsFile(code.Project.ProjectFolder)).ShouldBeTrue();
        code.ShowsButton("Auto").ShouldBeFalse("the reopened project has code settings");
        code.View.SettingsGrid.IsVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void NewProjectInsideAGamesContentFolder_OffersAutoSetup()
    {
        using CodeTabHarness code = new CodeTabHarness(openProjectSettingsPage: false);
        ProjectTreeHarness tree = code.Tree;
        string game = Path.Combine(code.Project.ProjectFolder, "SetupGame");
        string content = Path.Combine(game, "Content");
        Directory.CreateDirectory(content);
        File.WriteAllText(Path.Combine(game, "SetupGame.csproj"), GameCsproj);
        tree.Dialogs.AnswerNextInWindow<NewProjectDialogViewModel>(window =>
        {
            NewProjectDialogDriver dialog = new NewProjectDialogDriver(window);
            dialog.TypeName("SetupUi");
            dialog.TypeLocation(content);
            dialog.SetChecked(NewProjectDialogText.CreateCodeProject, false);
            dialog.ClickOk();
        });

        tree.PickMainMenu("File", "New Project");
        tree.WaitUntil(() => tree.SelectedState.SelectedScreen != null
            && TestAppBuilder.Services.GetRequiredService<IProjectManager>().GumProjectSave?.FullFileName?.Contains("SetupUi") == true,
            TimeSpan.FromSeconds(90), "the new project");
        code.Select(tree.SelectedState.SelectedScreen!);

        code.ShowsButton("Auto").ShouldBeTrue("the game's .csproj is above the new project, which has no code settings");
    }
}
