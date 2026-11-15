using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>The Code tab's Game tab: the code a game needs to match the Preview (issue #7261).</summary>
public class GameTabScenarioTests
{
    private static CodeTabHarness ScreenSelected(string? settings)
    {
        CodeTabHarness code = new CodeTabHarness(beforeLoad: settings == null ? null : folder =>
            File.WriteAllText(Path.Combine(folder, "ProjectCodeSettings.codsj"), settings));
        ScreenSave screen = code.Project.AddScreen("TitleScreen");
        code.Tree.SaveAll();
        code.Select(screen);
        return code;
    }

    [AvaloniaFact]
    public void GameTab_ForAnUnsetProject_ShowsTheMonoGameSetup_AndObjectRestoresTheElementCode()
    {
        using CodeTabHarness code = ScreenSelected(null);
        code.ShowsButton("Game").ShouldBeTrue();
        string objectCode = code.Preview;

        code.ClickButton("Game");

        code.Preview.ShouldContain("GumUI.Initialize(this, ");
        code.Preview.ShouldContain("var screen = new TitleScreen();");
        code.Preview.ShouldContain("GumUI.UseKeyboardDefaults();");

        code.ClickButton("Object");

        code.Preview.ShouldBe(objectCode);
        code.ClickButton("Game");
        code.ClickButton("State");
        code.Preview.ShouldContain("State Code for");
    }

    [AvaloniaFact]
    public void GameTab_ForARaylibProject_ShowsOnlyTheNote()
    {
        using CodeTabHarness code = ScreenSelected(
            """{ "CodeProjectRoot": "Code/", "OutputLibrary": 6, "ObjectInstantiationType": 1 }""");

        code.ClickButton("Game");

        code.Preview.ShouldContain("Preview runs on MonoGame");
        code.Preview.ShouldNotContain("GumUI.Initialize");
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GameTab_ForALegacyLibrary_IsHidden(int outputLibrary)
    {
        using CodeTabHarness code = ScreenSelected(
            "{ \"CodeProjectRoot\": \"Code/\", \"OutputLibrary\": " + outputLibrary + ", \"ObjectInstantiationType\": 0 }");

        code.ShowsButton("Game").ShouldBeFalse();
        code.ShowsButton("Object").ShouldBeTrue();
    }
}
