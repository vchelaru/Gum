using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// Ops that change a value another row reads leave the project propagation-consistent, like the tool's
/// Variables tab does (#8383).
/// </summary>
public class EditReferenceCascadeTests : BaseTestClass
{
    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private void Edit(string projectPath, string json)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        CliTestHelper result = CliTestHelper.Run("edit", projectPath, path);
        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
    }

    private static GumProjectSave Load(string projectPath) => new ProjectLoader().Load(projectPath).Project!;

    private void AssertConsistent(string projectPath)
    {
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        CliTestHelper.Run("check-references", projectPath).ExitCode.ShouldBe(0);

        Dictionary<string, string> before = ReadFiles(projectPath);
        CliTestHelper.Run("resave", projectPath).ExitCode.ShouldBe(0);
        ReadFiles(projectPath).ShouldBe(before);
    }

    private static Dictionary<string, string> ReadFiles(string projectPath) =>
        Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, File.ReadAllText);

    private const string MainMenuSetup = """
        { "version": 1, "operations": [
          { "op": "addScreen", "name": "MainMenu" },
          { "op": "addInstance", "element": "MainMenu", "name": "PlayButton", "type": "Container" },
          { "op": "addInstance", "element": "MainMenu", "name": "QuitButton", "type": "Container" },
          { "op": "setVariable", "element": "MainMenu", "name": "PlayButton.Width", "value": 300 },
          { "op": "setVariableReferences", "element": "MainMenu", "instance": "QuitButton", "lines": [ "Width = PlayButton.Width / 2" ] }
        ] }
        """;

    [Fact]
    public void SetVariable_UpdatesAnInstanceRowInTheSameStateThatReadsIt()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, MainMenuSetup);
        Load(projectPath).Screens.Single().DefaultState!.GetValue("QuitButton.Width").ShouldBe(150f);

        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "setVariable", "element": "MainMenu", "name": "PlayButton.Width", "value": 100 }
            ] }
            """);

        Load(projectPath).Screens.Single().DefaultState!.GetValue("QuitButton.Width").ShouldBe(50f);
        AssertConsistent(projectPath);
    }

    [Fact]
    public void SetVariable_InTheSameBatchAsTheReferenceRow_UpdatesTheReader()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addScreen", "name": "MainMenu" },
              { "op": "addInstance", "element": "MainMenu", "name": "PlayButton", "type": "Container" },
              { "op": "addInstance", "element": "MainMenu", "name": "QuitButton", "type": "Container" },
              { "op": "setVariable", "element": "MainMenu", "name": "PlayButton.Width", "value": 300 },
              { "op": "setVariableReferences", "element": "MainMenu", "instance": "QuitButton", "lines": [ "Width = PlayButton.Width / 2" ] },
              { "op": "setVariable", "element": "MainMenu", "name": "PlayButton.Width", "value": 100 }
            ] }
            """);

        Load(projectPath).Screens.Single().DefaultState!.GetValue("QuitButton.Width").ShouldBe(50f);
        AssertConsistent(projectPath);
    }

    [Fact]
    public void SetVariable_UpdatesAnotherStateOfTheSameElementThatReadsIt()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel", "baseType": "Container" },
              { "op": "addInstance", "element": "Panel", "name": "PlayButton", "type": "Container" },
              { "op": "addCategory", "element": "Panel", "name": "Size" },
              { "op": "addState", "element": "Panel", "category": "Size", "name": "Big" },
              { "op": "setVariableReferences", "element": "Panel", "category": "Size", "state": "Big", "lines": [ "Height = PlayButton.Width * 2" ] },
              { "op": "setVariable", "element": "Panel", "name": "PlayButton.Width", "value": 40 }
            ] }
            """);

        Load(projectPath).Components.Single().Categories.Single().States.Single().GetValue("Height").ShouldBe(80f);
        AssertConsistent(projectPath);
    }

    [Fact]
    public void SetVariable_UpdatesAnotherElementThatReadsIt()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Bar" },
              { "op": "addInstance", "element": "Bar", "name": "Total", "type": "Container" },
              { "op": "addComponent", "name": "Hud" },
              { "op": "setVariable", "element": "Bar", "name": "Total.Width", "value": 10 },
              { "op": "setVariableReferences", "element": "Hud", "lines": [ "Width = Components/Bar.Total.Width + 1" ] },
              { "op": "setVariable", "element": "Bar", "name": "Total.Width", "value": 70 }
            ] }
            """);

        Load(projectPath).Components.Single(item => item.Name == "Hud").DefaultState!.GetValue("Width").ShouldBe(71f);
        AssertConsistent(projectPath);
    }

    [Fact]
    public void ClearVariable_UpdatesTheRowsThatReadIt()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, MainMenuSetup);

        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "clearVariable", "element": "MainMenu", "name": "PlayButton.Width" }
            ] }
            """);

        GumProjectSave project = Load(projectPath);
        StateSave state = project.Screens.Single().DefaultState!;
        state.GetVariableSave("PlayButton.Width").ShouldBeNull();
        state.GetValue("QuitButton.Width").ShouldBe(75f);
        AssertConsistent(projectPath);
    }

    private const string PanelWithStateReader = """
        { "version": 1, "operations": [
          { "op": "addComponent", "name": "Panel", "baseType": "Container" },
          { "op": "addInstance", "element": "Panel", "name": "C", "type": "Container" },
          { "op": "addCategory", "element": "Panel", "name": "Size" },
          { "op": "addState", "element": "Panel", "category": "Size", "name": "Big" },
          { "op": "setVariable", "element": "Panel", "name": "C.Width", "value": 500 },
          { "op": "setVariableReferences", "element": "Panel", "category": "Size", "state": "Big", "lines": [ "Height = C.Width * 2" ] }
        ] }
        """;

    [Fact]
    public void SetVariable_UpdatesAStateRowThatReadsAnInstanceVariable()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, PanelWithStateReader);

        Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "setVariable", "element": "Panel", "name": "C.Width", "value": 40 } ] }
            """);

        Load(projectPath).Components.Single().Categories.Single().States.Single().GetValue("Height").ShouldBe(80f);
        AssertConsistent(projectPath);
    }

    [Fact]
    public void ClearVariable_UpdatesAnotherElementThatReadsIt()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Bar" },
              { "op": "addInstance", "element": "Bar", "name": "Total", "type": "Container" },
              { "op": "addComponent", "name": "Hud" },
              { "op": "setVariable", "element": "Bar", "name": "Total.Width", "value": 10 },
              { "op": "setVariableReferences", "element": "Hud", "lines": [ "Width = Components/Bar.Total.Width + 1" ] },
              { "op": "clearVariable", "element": "Bar", "name": "Total.Width" }
            ] }
            """);

        Load(projectPath).Components.Single(item => item.Name == "Hud").DefaultState!.GetValue("Width").ShouldBe(151f);
        AssertConsistent(projectPath);
    }
}
