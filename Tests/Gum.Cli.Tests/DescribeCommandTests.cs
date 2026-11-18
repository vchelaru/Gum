using System.Text.Json;
using Gum.Cli.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.Cli.Tests;

public class DescribeCommandTests : BaseTestClass
{
    private string CreateHudProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        string operationsPath = Path.Combine(TempDirectory, "setup.json");
        File.WriteAllText(operationsPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" },
              { "op": "exposeVariable", "element": "Hud", "instance": "Label", "variable": "Text", "name": "LabelText" },
              { "op": "addVariable", "element": "Hud", "name": "Score", "type": "int" },
              { "op": "addCategory", "element": "Hud", "name": "Size" },
              { "op": "addState", "element": "Hud", "category": "Size", "name": "Big" },
              { "op": "addScreen", "name": "Game" },
              { "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, operationsPath).ExitCode.ShouldBe(0);
        return projectPath;
    }

    private static string[] GetListedNames(string projectPath, params string[] describeArguments)
    {
        CliTestHelper result = CliTestHelper.Run(new[] { "describe", projectPath }.Concat(describeArguments).Append("--json").ToArray());
        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        return document.RootElement.GetProperty("variables").EnumerateArray()
            .Select(item => item.GetProperty("name").GetString()!)
            .ToArray();
    }

    [Fact]
    public void Describe_EveryListedName_IsAcceptedBySetVariableAndAMisspellingIsNot()
    {
        string projectPath = CreateHudProject();
        string[] hudNames = GetListedNames(projectPath, "Hud");
        string[] labelNames = GetListedNames(projectPath, "Hud", "--instance", "Label");
        string[] hudInstanceNames = GetListedNames(projectPath, "Game", "--instance", "HudInstance");
        hudNames.ShouldContain("LabelText");
        hudNames.ShouldContain("Score");
        hudNames.ShouldContain("SizeState");
        labelNames.ShouldContain("Label.Text");
        hudInstanceNames.ShouldContain("HudInstance.SizeState");
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        IEnumerable<object> operations = hudNames.Concat(labelNames).Select(name => new { op = "setVariable", element = "Hud", name, value = (object?)null })
            .Concat(hudInstanceNames.Select(name => new { op = "setVariable", element = "Game", name, value = (object?)null }));

        ProjectEditResult result = EditCommand.CreateEditService().Apply(project,
            JsonSerializer.Serialize(new { version = 1, operations }));
        ProjectEditResult misspelled = EditCommand.CreateEditService().Apply(project,
            """{ "version": 1, "operations": [ { "op": "setVariable", "element": "Hud", "name": "Label.Txet", "value": null } ] }""");

        result.Succeeded.ShouldBeTrue(result.Message);
        misspelled.Failure.ShouldBe(ProjectEditFailure.OperationFailed);
    }

    [Fact]
    public void Describe_Json_HasTheDocumentedLayout()
    {
        string projectPath = CreateHudProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Hud", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        JsonElement root = document.RootElement;
        root.GetProperty("element").GetString().ShouldBe("Hud");
        root.GetProperty("instance").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("type").GetString().ShouldBe("Hud");
        JsonElement sizeState = root.GetProperty("variables").EnumerateArray().Single(item => item.GetProperty("name").GetString() == "SizeState");
        sizeState.GetProperty("type").GetString().ShouldBe("Size");
        sizeState.GetProperty("values").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "Big" });
        JsonElement score = root.GetProperty("variables").EnumerateArray().Single(item => item.GetProperty("name").GetString() == "Score");
        score.TryGetProperty("values", out _).ShouldBeFalse();
        root.GetProperty("states").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "Default" });
        JsonElement size = root.GetProperty("categories").EnumerateArray().Single();
        size.GetProperty("name").GetString().ShouldBe("Size");
        size.GetProperty("states").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "Big" });
    }

    [Fact]
    public void Describe_HumanOutput_ListsVariablesWithTypesAndValues()
    {
        string projectPath = CreateHudProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Hud", "--instance", "Label");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("Label.Text");
        result.StandardOutput.ShouldContain("Left, Center, Right");
        result.StandardOutput.ShouldContain("Size: Big");
    }

    [Theory]
    [InlineData("Nope", null, "Nope")]
    [InlineData("Hud", "Nope", "Nope")]
    public void Describe_UnknownElementOrInstance_ExitsWith2(string element, string? instance, string expectedInMessage)
    {
        string projectPath = CreateHudProject();

        CliTestHelper result = instance == null
            ? CliTestHelper.Run("describe", projectPath, element)
            : CliTestHelper.Run("describe", projectPath, element, "--instance", instance);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain(expectedInMessage);
    }

    [Fact]
    public void EditSchema_Json_ListsEveryOpWithItsFields()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        document.RootElement.GetProperty("version").GetInt32().ShouldBe(1);
        JsonElement setVariable = document.RootElement.GetProperty("operations").EnumerateArray()
            .Single(item => item.GetProperty("op").GetString() == "setVariable");
        setVariable.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "element", "name", "value" });
        setVariable.GetProperty("optional").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "state", "category" });
        document.RootElement.GetProperty("operations").EnumerateArray()
            .Select(item => item.GetProperty("op").GetString()).ShouldContain("removeInstance");
        JsonElement extract = document.RootElement.GetProperty("operations").EnumerateArray()
            .Single(item => item.GetProperty("op").GetString() == "extractToComponent");
        extract.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(new[] { "element", "instance", "name", "replace" });
    }

    [Fact]
    public void EditSchema_HumanOutput_ListsOpsAndFields()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("setVariable");
        result.StandardOutput.ShouldContain("[state]");
    }

    [Fact]
    public void Edit_WithoutSchemaOrArguments_ExitsWith2()
    {
        CliTestHelper result = CliTestHelper.Run("edit");

        result.ExitCode.ShouldBe(2);
    }
}
