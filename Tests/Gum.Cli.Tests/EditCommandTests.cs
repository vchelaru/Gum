using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class EditCommandTests : BaseTestClass
{
    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private string WriteOperations(string json)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    private Dictionary<string, string> ReadProjectFiles(string projectPath) =>
        Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, File.ReadAllText);

    [Fact]
    public void Edit_HudExample_SavesEveryOperationAndPassesCheck()
    {
        string projectPath = CreateEmptyProject();
        string operations = WriteOperations("""
            {
              "version": 1,
              "operations": [
                { "op": "addComponent", "name": "Counter", "baseType": "Text" },
                { "op": "addComponent", "name": "Hud" },
                { "op": "addInstance", "element": "Hud", "name": "Stack", "type": "Container" },
                { "op": "addInstance", "element": "Hud", "name": "Counter1", "type": "Counter", "parent": "Stack" },
                { "op": "setVariable", "element": "Hud", "name": "Stack.ChildrenLayout", "value": "TopToBottomStack" },
                { "op": "setVariable", "element": "Hud", "name": "Counter1.Text", "value": "0" },
                { "op": "exposeVariable", "element": "Hud", "instance": "Counter1", "variable": "Text", "name": "Counter1Text" },
                { "op": "addVariable", "element": "Hud", "name": "Score", "type": "int" },
                { "op": "addCategory", "element": "Hud", "name": "Size" },
                { "op": "addState", "element": "Hud", "category": "Size", "name": "Normal" },
                { "op": "addState", "element": "Hud", "category": "Size", "name": "Big" },
                { "op": "setVariable", "element": "Hud", "category": "Size", "state": "Big", "name": "Width", "value": 200 },
                { "op": "addScreen", "name": "Game" },
                { "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" },
                { "op": "setVariable", "element": "Game", "name": "HudInstance.SizeState", "value": "Big" },
                { "op": "setVariable", "element": "Game", "name": "HudInstance.Counter1Text", "value": "5" }
              ]
            }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave hud = project.Components.Single(item => item.Name == "Hud");
        hud.Instances.Select(item => item.Name).ShouldBe(new[] { "Stack", "Counter1" });
        hud.DefaultState.GetValue("Counter1.Parent").ShouldBe("Stack");
        hud.DefaultState.GetValue("Stack.ChildrenLayout").ShouldBe(ChildrenLayout.TopToBottomStack);
        hud.DefaultState.GetVariableSave("Counter1.Text")!.ExposedAsName.ShouldBe("Counter1Text");
        hud.DefaultState.GetVariableSave("Score")!.IsCustomVariable.ShouldBeTrue();
        StateSaveCategory size = hud.Categories.Single();
        size.States.Select(item => item.Name).ShouldBe(new[] { "Normal", "Big" });
        size.States[1].GetValue("Width").ShouldBe(200f);
        size.States[0].GetVariableSave("Width").ShouldNotBeNull();
        ScreenSave game = project.Screens.Single(item => item.Name == "Game");
        game.DefaultState.GetValue("HudInstance.SizeState").ShouldBe("Big");
        game.DefaultState.GetValue("HudInstance.Counter1Text").ShouldBe("5");
    }

    [Fact]
    public void Edit_VariableTheTypeDoesNotDefine_FailsAndWritesNothing()
    {
        string projectPath = CreateEmptyProject();
        Dictionary<string, string> before = ReadProjectFiles(projectPath);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "setVariable", "element": "Hud", "name": "Widht", "value": 10 }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("operations[1]");
        result.StandardError.ShouldContain("Widht");
        ReadProjectFiles(projectPath).ShouldBe(before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Edit_IntroducingACheckError_ReportsItLikeCheckAndWritesNothing(bool json)
    {
        string projectPath = CreateEmptyProject();
        Dictionary<string, string> before = ReadProjectFiles(projectPath);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Thing", "type": "DoesNotExist" }
            ] }
            """);

        CliTestHelper result = json
            ? CliTestHelper.Run("edit", projectPath, operations, "--json")
            : CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(1);
        ReadProjectFiles(projectPath).ShouldBe(before);
        if (json)
        {
            JsonElement[] errors = JsonSerializer.Deserialize<JsonElement[]>(result.StandardOutput)!;
            errors.ShouldContain(error => error.GetProperty("element").GetString() == "Hud"
                && error.GetProperty("severity").GetString() == "Error");
        }
        else
        {
            result.StandardOutput.ShouldContain("error: Hud:");
            result.StandardOutput.ShouldContain("DoesNotExist");
        }
    }

    [Fact]
    public void Edit_ProjectWithAnExistingError_SavesAndReportsOnlyNewErrors()
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Counter" },
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Counter1", "type": "Counter" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        File.Delete(Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "Counter.gucx"));
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(1);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        result.StandardOutput.ShouldNotContain("Counter1");
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "Hud.gucx")).ShouldContain("Label");
    }

    [Theory]
    [InlineData("""{ "version": 2, "operations": [] }""", "version")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "renameEverything" } ] }""", "renameEverything")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "addScreen", "name": "Game", "colour": "red" } ] }""", "colour")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "addScreen" } ] }""", "name")]
    [InlineData("""{ "version": 1, "operations": [ ], "extra": 1 }""", "extra")]
    [InlineData("""not json""", "operations file")]
    public void Edit_InvalidOperationsFile_ExitsWith2AndWritesNothing(string json, string expectedInMessage)
    {
        string projectPath = CreateEmptyProject();
        Dictionary<string, string> before = ReadProjectFiles(projectPath);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, WriteOperations(json));

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain(expectedInMessage);
        ReadProjectFiles(projectPath).ShouldBe(before);
    }

    [Theory]
    [InlineData("""{ "op": "addInstance", "element": "Missing", "name": "A", "type": "Text" }""", "Missing")]
    [InlineData("""{ "op": "addComponent", "name": "Text" }""", "Text")]
    [InlineData("""{ "op": "setVariable", "element": "Hud", "name": "Width", "value": "wide" }""", "wide")]
    [InlineData("""{ "op": "setVariable", "element": "Hud", "state": "Huge", "name": "Width", "value": 1 }""", "Huge")]
    [InlineData("""{ "op": "exposeVariable", "element": "Hud", "instance": "Label", "variable": "Nope", "name": "LabelNope" }""", "Nope")]
    [InlineData("""{ "op": "addVariable", "element": "Hud", "name": "Width", "type": "float" }""", "Width")]
    [InlineData("""{ "op": "addVariable", "element": "Hud", "name": "Speed", "type": "Vector2" }""", "Vector2")]
    [InlineData("""{ "op": "setVariable", "element": "Hud", "name": "Label.Widht", "value": 1 }""", "Label.Widht")]
    public void Edit_OperationThatCannotApply_ExitsWith1AndNamesTheProblem(string operation, string expectedInMessage)
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        Dictionary<string, string> before = ReadProjectFiles(projectPath);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath,
            WriteOperations("{ \"version\": 1, \"operations\": [ " + operation + " ] }"));

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("operations[0]");
        result.StandardError.ShouldContain(expectedInMessage);
        ReadProjectFiles(projectPath).ShouldBe(before);
    }
}
