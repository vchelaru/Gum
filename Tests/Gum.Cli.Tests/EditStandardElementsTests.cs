using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>gumcli edit</c> on standard elements (Text, Sprite, ...): states, categories and variables, saved the way
/// the tool saves them.
/// </summary>
public class EditStandardElementsTests : BaseTestClass
{
    private string CopyShippedProject()
    {
        string? root = AppContext.BaseDirectory;
        while (root != null && !File.Exists(Path.Combine(root, "Gum.slnx")))
        {
            root = Path.GetDirectoryName(root);
        }
        root.ShouldNotBeNull();

        string source = Path.Combine(root, "Samples", "GameUiSamples", "Content", "GumProject");
        string destination = Path.Combine(TempDirectory, "Copy");
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return Path.Combine(destination, "GameUiSamplesGumProject.gumx");
    }

    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private CliTestHelper Edit(string projectPath, string operationsJson)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, operationsJson);
        return CliTestHelper.Run("edit", projectPath, path);
    }

    private static StandardElementSave Load(string projectPath, string name)
    {
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        return project.StandardElements.Single(item => item.Name == name);
    }

    private static string TextFile(string projectPath) =>
        Path.Combine(Path.GetDirectoryName(projectPath)!, "Standards", "Text.gutx");

    [Fact]
    public void Edit_AddStateAndSetVariablesOnText_SavesLikeAnExistingStateInTheCategory()
    {
        string projectPath = CopyShippedProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addState", "element": "Text", "name": "Gold", "category": "ColorCategory" },
              { "op": "setVariable", "element": "Text", "name": "Red", "value": 255, "state": "Gold", "category": "ColorCategory" },
              { "op": "setVariable", "element": "Text", "name": "Green", "value": 193, "state": "Gold", "category": "ColorCategory" },
              { "op": "setVariable", "element": "Text", "name": "Blue", "value": 53, "state": "Gold", "category": "ColorCategory" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        StandardElementSave text = Load(projectPath, "Text");
        StateSaveCategory colors = text.Categories.Single(item => item.Name == "ColorCategory");
        StateSave gold = colors.States.Single(item => item.Name == "Gold");
        StateSave gray = colors.States.Single(item => item.Name == "Gray");
        gold.Variables.Select(item => (item.Name, item.Type, item.Category, item.SetsValue))
            .ShouldBe(gray.Variables.Select(item => (item.Name, item.Type, item.Category, item.SetsValue)));
        gold.GetValue("Red").ShouldBe(255);
        gold.GetValue("Green").ShouldBe(193);
        gold.GetValue("Blue").ShouldBe(53);
    }

    [Fact]
    public void Edit_AddAndRemoveStateOnText_LeavesTheFileAsResaveWritesIt()
    {
        string projectPath = CopyShippedProject();
        CliTestHelper.Run("resave", projectPath).ExitCode.ShouldBe(0);
        string expected = File.ReadAllText(TextFile(projectPath));

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addState", "element": "Text", "name": "Gold", "category": "ColorCategory" },
              { "op": "removeState", "element": "Text", "name": "Gold", "category": "ColorCategory" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        File.ReadAllText(TextFile(projectPath)).ShouldBe(expected);
    }

    [Fact]
    public void Edit_CategoryAndClearVariableOnStandard_Apply()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addCategory", "element": "Sprite", "name": "Tints" },
              { "op": "addState", "element": "Sprite", "name": "Warm", "category": "Tints" },
              { "op": "setVariable", "element": "Sprite", "name": "Red", "value": 200, "state": "Warm", "category": "Tints" },
              { "op": "clearVariable", "element": "Sprite", "name": "Red", "state": "Warm", "category": "Tints" },
              { "op": "addCategory", "element": "Text", "name": "Extra" },
              { "op": "removeCategory", "element": "Text", "name": "Extra" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        StandardElementSave sprite = Load(projectPath, "Sprite");
        sprite.Categories.Single().States.Single().Name.ShouldBe("Warm");
        Load(projectPath, "Text").Categories.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{ "op": "addInstance", "element": "Text", "name": "Child", "type": "Container" }""")]
    [InlineData("""{ "op": "addVariable", "element": "Text", "name": "Score", "type": "int" }""")]
    [InlineData("""{ "op": "removeInstance", "element": "Text", "name": "Child" }""")]
    [InlineData("""{ "op": "removeVariable", "element": "Text", "name": "Score" }""")]
    [InlineData("""{ "op": "exposeVariable", "element": "Text", "instance": "Child", "variable": "X", "name": "ChildX" }""")]
    [InlineData("""{ "op": "unexposeVariable", "element": "Text", "name": "ChildX" }""")]
    [InlineData("""{ "op": "extractToComponent", "element": "Text", "instance": "Child", "name": "Part", "replace": false }""")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Text", "lines": [] }""")]
    public void Edit_OpThatNeedsInstancesOnStandard_IsRefusedAndNothingIsWritten(string operation)
    {
        string projectPath = CreateEmptyProject();
        string before = File.ReadAllText(TextFile(projectPath));

        CliTestHelper result = Edit(projectPath, "{ \"version\": 1, \"operations\": [ " + operation + " ] }");

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("Text is a standard element");
        result.StandardError.ShouldContain("Screen or Component");
        File.ReadAllText(TextFile(projectPath)).ShouldBe(before);
    }

    [Fact]
    public void Edit_RemoveComponentNamingAStandard_SaysItIsStandard()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "removeComponent", "name": "Text" } ] }
            """);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("Text is a standard element");
    }

    [Fact]
    public void Describe_Standard_ListsItsVariablesStatesAndCategories()
    {
        string projectPath = CopyShippedProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Text", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("\"Red\"");
        result.StandardOutput.ShouldContain("ColorCategory");
    }

    [Fact]
    public void Edit_UnknownElement_NamesStandardsInTheMessage()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "addState", "element": "Nope", "name": "X" } ] }
            """);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("no Screen, Component or standard element named Nope");
    }

    [Fact]
    public void EditSchema_Json_MarksTheOpsThatAcceptAStandardElement()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(result.StandardOutput);
        string[] standardOps = document.RootElement.GetProperty("operations").EnumerateArray()
            .Where(item => item.GetProperty("standardElements").GetBoolean())
            .Select(item => item.GetProperty("op").GetString()!)
            .Order()
            .ToArray();
        standardOps.ShouldBe(new[] { "addCategory", "addState", "clearVariable", "removeCategory", "removeState", "setVariable" });
    }

    [Fact]
    public void EditSchema_HumanOutput_ExplainsTheStandardElementMarker()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldContain("standard element");
    }

    [Fact]
    public void Edit_RemoveCategoryOnStandard_AlsoRemovesTheCategoryStateVariable()
    {
        string projectPath = CopyShippedProject();
        Load(projectPath, "Text").DefaultState.GetVariableSave("ColorCategoryState").ShouldNotBeNull();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "removeCategory", "element": "Text", "name": "ColorCategory" } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        StandardElementSave text = Load(projectPath, "Text");
        text.Categories.ShouldNotContain(item => item.Name == "ColorCategory");
        text.AllStates.SelectMany(item => item.Variables).ShouldNotContain(item => item.Name == "ColorCategoryState");
    }
}
