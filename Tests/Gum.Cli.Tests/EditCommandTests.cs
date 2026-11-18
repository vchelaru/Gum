using System.Text.Json;
using System.Xml.Linq;
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
    public void Edit_RemoveOperations_RemoveWhatTheToolRemovesAndPassCheck()
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Stack", "type": "Container" },
              { "op": "addInstance", "element": "Hud", "name": "Panel", "type": "Container", "parent": "Stack" },
              { "op": "addInstance", "element": "Hud", "name": "Child", "type": "Text", "parent": "Panel" },
              { "op": "setVariable", "element": "Hud", "name": "Child.Text", "value": "Hi" },
              { "op": "exposeVariable", "element": "Hud", "instance": "Child", "variable": "Text", "name": "ChildText" },
              { "op": "addVariable", "element": "Hud", "name": "Score", "type": "int" },
              { "op": "setVariable", "element": "Hud", "name": "Width", "value": 300 },
              { "op": "addCategory", "element": "Hud", "name": "Size" },
              { "op": "addState", "element": "Hud", "category": "Size", "name": "Big" },
              { "op": "addState", "element": "Hud", "category": "Size", "name": "Normal" },
              { "op": "addCategory", "element": "Hud", "name": "Mood" },
              { "op": "addState", "element": "Hud", "category": "Mood", "name": "Happy" },
              { "op": "addScreen", "name": "Game" },
              { "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.SizeState", "value": "Big" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.MoodState", "value": "Happy" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.Score", "value": 5 },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.ChildText", "value": "Yo" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "removeInstance", "element": "Hud", "name": "Panel" },
              { "op": "removeState", "element": "Hud", "category": "Size", "name": "Big" },
              { "op": "removeCategory", "element": "Hud", "name": "Mood" },
              { "op": "removeVariable", "element": "Hud", "name": "Score" },
              { "op": "clearVariable", "element": "Hud", "name": "Width" },
              { "op": "clearVariable", "element": "Hud", "name": "ChildText" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave hud = project.Components.Single(item => item.Name == "Hud");
        hud.Instances.Select(item => item.Name).ShouldBe(new[] { "Stack", "Child" });
        hud.GetDefaultStateOrThrow().GetValue("Child.Parent").ShouldBe("Stack");
        hud.Categories.Select(item => item.Name).ShouldBe(new[] { "Size" });
        hud.Categories[0].States.Select(item => item.Name).ShouldBe(new[] { "Normal" });
        hud.GetDefaultStateOrThrow().GetVariableSave("MoodState").ShouldBeNull();
        hud.GetDefaultStateOrThrow().GetVariableSave("Score").ShouldBeNull();
        hud.GetDefaultStateOrThrow().GetVariableSave("Width").ShouldBeNull();
        VariableSave childText = hud.GetDefaultStateOrThrow().GetVariableSave("Child.Text")!;
        childText.ExposedAsName.ShouldBe("ChildText");
        childText.SetsValue.ShouldBeFalse();
        ScreenSave game = project.Screens.Single(item => item.Name == "Game");
        game.GetDefaultStateOrThrow().GetVariableSave("HudInstance.SizeState").ShouldBeNull();
        game.GetDefaultStateOrThrow().GetVariableSave("HudInstance.MoodState").ShouldBeNull();
        game.GetDefaultStateOrThrow().GetVariableSave("HudInstance.Score").ShouldBeNull();
        game.GetDefaultStateOrThrow().GetValue("HudInstance.ChildText").ShouldBe("Yo");
    }

    [Theory]
    // The tool's Variables tab hands an enum or a number to the state, which saves as a number, so an edit
    // must store the same, and null for a nullable type. No standard element defines a nullable enum or a
    // double?, so Hud declares Direction (Orientation?) and Spacing (double?) itself.
    [InlineData("Stack.ChildrenLayout", "\"LeftToRightStack\"", "ChildrenLayout", "xsd:int", "2")]
    [InlineData("Direction", "\"Vertical\"", "Orientation?", "xsd:int", "1")]
    [InlineData("Direction", "1", "Orientation?", "xsd:int", "1")]
    [InlineData("Spacing", "2.5", "double?", "xsd:double", "2.5")]
    [InlineData("Label.MaxWidth", "100", "float?", "xsd:float", "100")]
    [InlineData("Label.MaxWidth", "null", "float?", null, null)]
    [InlineData("Label.MaxLettersToShow", "5", "int?", "xsd:int", "5")]
    public void Edit_SetVariable_StoresTheValueAsTheToolDoes(string name, string jsonValue, string expectedType,
        string? expectedXsiType, string? expectedText)
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Stack", "type": "Container" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string hudPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "Hud.gucx");
        XDocument hud = XDocument.Load(hudPath);
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        // A set MaxWidth, so the null case shows null clearing it.
        hud.Root!.Element("State")!.AddFirst(
            new XElement("Variable", new XAttribute("Type", "float?"), new XAttribute("Name", "Label.MaxWidth"), new XAttribute("SetsValue", "true"),
                new XElement("Value", new XAttribute(xsi + "type", "xsd:float"), "50")),
            new XElement("Variable", new XAttribute("Type", "Orientation?"), new XAttribute("Name", "Direction"), new XAttribute("SetsValue", "true"), new XAttribute("IsCustomVariable", "true")),
            new XElement("Variable", new XAttribute("Type", "double?"), new XAttribute("Name", "Spacing"), new XAttribute("SetsValue", "true"), new XAttribute("IsCustomVariable", "true")));
        hud.Save(hudPath);
        string operations = WriteOperations($$"""
            { "version": 1, "operations": [
              { "op": "setVariable", "element": "Hud", "name": "{{name}}", "value": {{jsonValue}} }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        XElement variable = XDocument.Load(hudPath)
            .Descendants("Variable").Single(item => (string?)item.Attribute("Name") == name);
        ((string?)variable.Attribute("Type")).ShouldBe(expectedType);
        XElement? value = variable.Element("Value");
        ((string?)value?.Attribute(xsi + "type")).ShouldBe(expectedXsiType);
        value?.Value.ShouldBe(expectedText);
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
    [InlineData("""{ "op": "removeInstance", "element": "Hud", "name": "Nope" }""", "Nope")]
    [InlineData("""{ "op": "removeState", "element": "Hud", "name": "Default" }""", "default state")]
    [InlineData("""{ "op": "removeCategory", "element": "Hud", "name": "Size" }""", "Size")]
    [InlineData("""{ "op": "removeVariable", "element": "Hud", "name": "Width" }""", "custom variable")]
    [InlineData("""{ "op": "clearVariable", "element": "Hud", "name": "Height" }""", "Height")]
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
