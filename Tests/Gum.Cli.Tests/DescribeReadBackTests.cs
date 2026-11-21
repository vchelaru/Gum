using Gum.DataTypes;
using Gum.ProjectServices;
using System.Text.Json;
using Shouldly;

namespace Gum.Cli.Tests;

public class DescribeReadBackTests : BaseTestClass
{
    private string CreateProject(string template, string operationsJson)
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", template).ExitCode.ShouldBe(0);
        string operationsPath = Path.Combine(TempDirectory, "setup.json");
        File.WriteAllText(operationsPath, operationsJson);
        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operationsPath);
        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        return projectPath;
    }

    private string CreateLayeredProject() => CreateProject("empty", """
        { "version": 1, "operations": [
          { "op": "addComponent", "name": "Base" },
          { "op": "addInstance", "element": "Base", "name": "Title", "type": "Text" },
          { "op": "addComponent", "name": "Hud", "baseType": "Base" },
          { "op": "addInstance", "element": "Hud", "name": "Panel", "type": "Container" },
          { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text", "parent": "Panel" },
          { "op": "exposeVariable", "element": "Hud", "instance": "Label", "variable": "Text", "name": "LabelText" },
          { "op": "setVariableReferences", "element": "Hud", "instance": "Label", "lines": [ "Text = Title.Text" ] },
          { "op": "addScreen", "name": "Game" }
        ] }
        """);

    private static JsonElement Describe(string projectPath, params string[] arguments)
    {
        CliTestHelper result = CliTestHelper.Run(new[] { "describe", projectPath }.Concat(arguments).Append("--json").ToArray());
        result.ExitCode.ShouldBe(0, result.StandardError);
        return JsonDocument.Parse(result.StandardOutput).RootElement;
    }

    [Fact]
    public void Describe_NoElement_ListsEveryElementWithKindAndBaseType()
    {
        string projectPath = CreateLayeredProject();

        JsonElement root = Describe(projectPath);

        JsonElement[] elements = root.GetProperty("elements").EnumerateArray().ToArray();
        JsonElement hud = elements.Single(item => item.GetProperty("name").GetString() == "Hud");
        hud.GetProperty("kind").GetString().ShouldBe("component");
        hud.GetProperty("baseType").GetString().ShouldBe("Base");
        elements.Single(item => item.GetProperty("name").GetString() == "Game").GetProperty("kind").GetString().ShouldBe("screen");
        elements.Single(item => item.GetProperty("name").GetString() == "Text").GetProperty("kind").GetString().ShouldBe("standard");
    }

    [Fact]
    public void Describe_NoElement_HumanOutputGroupsByKind()
    {
        string projectPath = CreateLayeredProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("Screens:");
        result.StandardOutput.ShouldContain("  Game");
        result.StandardOutput.ShouldContain("Components:");
        result.StandardOutput.ShouldContain("  Hud (Base)");
        result.StandardOutput.ShouldContain("Standards:");
    }

    [Fact]
    public void Describe_NoElementWithInstance_ExitsWith2()
    {
        string projectPath = CreateLayeredProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "--instance", "Label");

        result.ExitCode.ShouldBe(2);
    }

    [Fact]
    public void Describe_UnknownElement_PointsAtTheListing()
    {
        string projectPath = CreateLayeredProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Nope");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Nope");
        result.StandardError.ShouldContain("gumcli describe");
    }

    [Fact]
    public void Describe_Element_ReportsBaseTypeAndInstances()
    {
        string projectPath = CreateLayeredProject();

        JsonElement root = Describe(projectPath, "Hud");

        root.GetProperty("baseType").GetString().ShouldBe("Base");
        JsonElement[] instances = root.GetProperty("instances").EnumerateArray().ToArray();
        instances.Select(item => item.GetProperty("name").GetString()).ShouldBe(new[] { "Title", "Panel", "Label" }, ignoreOrder: true);
        JsonElement label = instances.Single(item => item.GetProperty("name").GetString() == "Label");
        label.GetProperty("type").GetString().ShouldBe("Text");
        label.GetProperty("parent").GetString().ShouldBe("Panel");
        label.GetProperty("definedByBase").GetBoolean().ShouldBeFalse();
        label.GetProperty("locked").GetBoolean().ShouldBeFalse();
        JsonElement title = instances.Single(item => item.GetProperty("name").GetString() == "Title");
        title.GetProperty("definedByBase").GetBoolean().ShouldBeTrue();
        title.GetProperty("parent").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public void Describe_Element_ReportsExposedVariablesAndReferences()
    {
        string projectPath = CreateLayeredProject();

        JsonElement root = Describe(projectPath, "Hud");

        JsonElement exposed = root.GetProperty("exposedVariables").EnumerateArray().Single();
        exposed.GetProperty("name").GetString().ShouldBe("LabelText");
        exposed.GetProperty("variable").GetString().ShouldBe("Label.Text");
        JsonElement references = root.GetProperty("variableReferences").EnumerateArray().Single();
        references.GetProperty("instance").GetString().ShouldBe("Label");
        references.GetProperty("state").GetString().ShouldBe("Default");
        references.GetProperty("lines").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "Text = Title.Text" });
    }

    [Fact]
    public void Describe_Element_HumanOutputKeepsVariablesFirstThenListsTheRest()
    {
        string projectPath = CreateLayeredProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Hud");

        result.ExitCode.ShouldBe(0, result.StandardError);
        string output = result.StandardOutput;
        output.IndexOf("Variables:").ShouldBeLessThan(output.IndexOf("Instances:"));
        output.ShouldContain("Base type: Base");
        output.ShouldContain("Label (Text) in Panel");
        output.ShouldContain("Title (Text) inherited");
        output.ShouldContain("LabelText -> Label.Text");
        output.ShouldContain("Label: Text = Title.Text");
    }

    [Fact]
    public void Describe_Instance_DoesNotAddTheElementDetails()
    {
        string projectPath = CreateLayeredProject();

        JsonElement root = Describe(projectPath, "Hud", "--instance", "Label");

        root.TryGetProperty("instances", out _).ShouldBeFalse();
        root.TryGetProperty("baseType", out _).ShouldBeFalse();
    }

    [Fact]
    public void Describe_ElementWithBehaviors_ListsThem()
    {
        string projectPath = Path.Combine(TempDirectory, "Forms", "Forms.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "forms").ExitCode.ShouldBe(0);

        JsonElement root = Describe(projectPath, "Controls/ButtonStandard");

        root.GetProperty("behaviors").EnumerateArray().Select(item => item.GetString()).ShouldContain("ButtonBehavior");
    }

    [Fact]
    public void Describe_LockedInstance_IsReportedLocked()
    {
        string projectPath = CreateLayeredProject();
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.Components.Single(item => item.Name == "Hud").GetInstance("Label")!.Locked = true;
        project.Save(projectPath, saveElements: true);

        JsonElement root = Describe(projectPath, "Hud");
        JsonElement[] instances = root.GetProperty("instances").EnumerateArray().ToArray();

        instances.Single(item => item.GetProperty("name").GetString() == "Label").GetProperty("locked").GetBoolean().ShouldBeTrue();
        instances.Single(item => item.GetProperty("name").GetString() == "Panel").GetProperty("locked").GetBoolean().ShouldBeFalse();
        CliTestHelper.Run("describe", projectPath, "Hud").StandardOutput.ShouldContain("Label (Text) in Panel locked");
    }

    [Fact]
    public void Describe_ReferenceInCategoryState_ReportsCategoryAndStateAndHeading()
    {
        string projectPath = CreateProject("empty", """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Title", "type": "Text" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" },
              { "op": "addCategory", "element": "Hud", "name": "Size" },
              { "op": "addState", "element": "Hud", "category": "Size", "name": "Big" },
              { "op": "setVariableReferences", "element": "Hud", "instance": "Label", "category": "Size", "state": "Big", "lines": [ "Text = Title.Text" ] }
            ] }
            """);

        JsonElement reference = Describe(projectPath, "Hud").GetProperty("variableReferences").EnumerateArray().Single();
        CliTestHelper text = CliTestHelper.Run("describe", projectPath, "Hud");

        reference.GetProperty("category").GetString().ShouldBe("Size");
        reference.GetProperty("state").GetString().ShouldBe("Big");
        text.StandardOutput.ShouldContain("References:");
        text.StandardOutput.ShouldContain("Label [Size/Big]: Text = Title.Text");
    }

    [Fact]
    public void Describe_NoElement_ListsBehaviors()
    {
        string projectPath = Path.Combine(TempDirectory, "Forms", "Forms.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "forms").ExitCode.ShouldBe(0);

        JsonElement[] elements = Describe(projectPath).GetProperty("elements").EnumerateArray().ToArray();

        elements.Single(item => item.GetProperty("name").GetString() == "ButtonBehavior").GetProperty("kind").GetString().ShouldBe("behavior");
        CliTestHelper.Run("describe", projectPath).StandardOutput.ShouldContain("Behaviors:");
    }
}
