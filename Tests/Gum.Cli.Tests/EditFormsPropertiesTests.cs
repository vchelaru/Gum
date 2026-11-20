using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// The Forms properties of a control's behaviors (a Slider's Minimum, a CheckBox's IsChecked), which the tool shows
/// in the Variables tab's Behavior category, are variables <c>describe</c> lists and <c>edit setVariable</c> sets.
/// </summary>
public class EditFormsPropertiesTests : BaseTestClass
{
    private string CreateFormsProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("add-forms", projectPath).ExitCode.ShouldBe(0);
        CliTestHelper setup = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addScreen", "name": "Settings" },
              { "op": "addInstance", "element": "Settings", "name": "VolumeSlider", "type": "Controls/Slider" },
              { "op": "addInstance", "element": "Settings", "name": "MuteCheck", "type": "Controls/CheckBox" },
              { "op": "addInstance", "element": "Settings", "name": "NameBox", "type": "Controls/TextBox" },
              { "op": "addInstance", "element": "Settings", "name": "Readout", "type": "Text" },
              { "op": "addComponent", "name": "FancySlider", "baseType": "Controls/Slider" }
            ] }
            """);
        setup.ExitCode.ShouldBe(0, setup.StandardOutput + setup.StandardError);
        return projectPath;
    }

    private CliTestHelper Edit(string projectPath, string operationsJson)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, operationsJson);
        return CliTestHelper.Run("edit", projectPath, path);
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

    private static VariableSave GetVariable(string projectPath, string element, string name)
    {
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ElementSave found = project.AllElements.Single(item => item.Name == element);
        return found.GetDefaultStateOrThrow().Variables.Single(item => item.Name == name);
    }

    [Fact]
    public void Edit_SetFormsProperties_StoresTypedValuesLikeTheToolAndPassesCheck()
    {
        string projectPath = CreateFormsProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "setVariable", "element": "Settings", "name": "VolumeSlider.Maximum", "value": 10 },
              { "op": "setVariable", "element": "Settings", "name": "VolumeSlider.Value", "value": 2.5 },
              { "op": "setVariable", "element": "Settings", "name": "MuteCheck.IsChecked", "value": true },
              { "op": "setVariable", "element": "Settings", "name": "NameBox.MaxLength", "value": 12 },
              { "op": "setVariable", "element": "Settings", "name": "NameBox.ToolTip", "value": "Your name" },
              { "op": "setVariable", "element": "Settings", "name": "NameBox.IsEnabled", "value": false }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        VariableSave maximum = GetVariable(projectPath, "Settings", "VolumeSlider.Maximum");
        maximum.Value.ShouldBe(10.0);
        maximum.Type.ShouldBe("double");
        GetVariable(projectPath, "Settings", "VolumeSlider.Value").Value.ShouldBe(2.5);
        GetVariable(projectPath, "Settings", "MuteCheck.IsChecked").Value.ShouldBe(true);
        GetVariable(projectPath, "Settings", "NameBox.MaxLength").Value.ShouldBe(12);
        GetVariable(projectPath, "Settings", "NameBox.ToolTip").Value.ShouldBe("Your name");
        GetVariable(projectPath, "Settings", "NameBox.IsEnabled").Value.ShouldBe(false);
    }

    [Fact]
    public void Edit_SetFormsProperties_ResavesIdentically()
    {
        string projectPath = CreateFormsProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "setVariable", "element": "Settings", "name": "VolumeSlider.Maximum", "value": 10 },
              { "op": "setVariable", "element": "Settings", "name": "MuteCheck.IsChecked", "value": true }
            ] }
            """).ExitCode.ShouldBe(0);
        string screenFile = Path.Combine(Path.GetDirectoryName(projectPath)!, "Screens", "Settings.gusx");
        string expected = File.ReadAllText(screenFile);

        CliTestHelper.Run("resave", projectPath).ExitCode.ShouldBe(0);

        File.ReadAllText(screenFile).ShouldBe(expected);
    }

    [Fact]
    public void Edit_SetFormsPropertyOnAComponentDerivedFromAControl_SetsItOnTheComponent()
    {
        string projectPath = CreateFormsProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "setVariable", "element": "FancySlider", "name": "Maximum", "value": 50 } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GetVariable(projectPath, "FancySlider", "Maximum").Value.ShouldBe(50.0);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Edit_SetFormsPropertyInACategoryState_WritesThatState()
    {
        string projectPath = CreateFormsProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addCategory", "element": "Settings", "name": "Mode" },
              { "op": "addState", "element": "Settings", "category": "Mode", "name": "Loud" },
              { "op": "setVariable", "element": "Settings", "category": "Mode", "state": "Loud", "name": "VolumeSlider.Value", "value": 9 }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ScreenSave settings = project.Screens.Single(item => item.Name == "Settings");
        settings.Categories.Single().States.Single().GetValue("VolumeSlider.Value").ShouldBe(9.0);
    }

    [Theory]
    [InlineData("VolumeSlider.Maximum", "\"loud\"", "double")]
    [InlineData("MuteCheck.IsChecked", "1", "bool")]
    [InlineData("NameBox.MaxLength", "1.5", "int")]
    [InlineData("VolumeSlider.Nope", "1", "Controls/Slider has no variable named Nope")]
    [InlineData("MuteCheck.Maximum", "1", "Controls/CheckBox has no variable named Maximum")]
    public void Edit_FormsPropertyThatCannotApply_FailsAndWritesNothing(string name, string value, string expectedInMessage)
    {
        string projectPath = CreateFormsProject();
        Dictionary<string, string> before = ReadProjectFiles(projectPath);

        CliTestHelper result = Edit(projectPath,
            "{ \"version\": 1, \"operations\": [ { \"op\": \"setVariable\", \"element\": \"Settings\", \"name\": \"" + name + "\", \"value\": " + value + " } ] }");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(expectedInMessage);
        ReadProjectFiles(projectPath).ShouldBe(before);
    }

    private static Dictionary<string, string> ReadProjectFiles(string projectPath) =>
        Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, File.ReadAllText);

    [Fact]
    public void Edit_ClearFormsProperty_RemovesTheValue()
    {
        string projectPath = CreateFormsProject();
        Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "setVariable", "element": "Settings", "name": "VolumeSlider.Maximum", "value": 10 } ] }
            """).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "clearVariable", "element": "Settings", "name": "VolumeSlider.Maximum" } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        new ProjectLoader().Load(projectPath).Project!.Screens.Single(item => item.Name == "Settings")
            .GetDefaultStateOrThrow().Variables.ShouldNotContain(item => item.Name == "VolumeSlider.Maximum");
    }

    [Fact]
    public void Edit_VariableReferenceThatReadsAFormsProperty_StillAppliesAndPassesCheck()
    {
        string projectPath = CreateFormsProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "setVariable", "element": "Settings", "name": "VolumeSlider.Maximum", "value": 10 },
              { "op": "setVariableReferences", "element": "Settings", "instance": "Readout", "lines": [ "Width = VolumeSlider.Maximum * 4" ] }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        Convert.ToDouble(GetVariable(projectPath, "Settings", "Readout.Width").Value).ShouldBe(40.0);
    }

    [Fact]
    public void Describe_ListsTheFormsPropertiesWithTheirTypes()
    {
        string projectPath = CreateFormsProject();

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Settings", "--instance", "VolumeSlider", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        Dictionary<string, string> types = document.RootElement.GetProperty("variables").EnumerateArray()
            .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("type").GetString()!);
        types["VolumeSlider.Maximum"].ShouldBe("double");
        types["VolumeSlider.Value"].ShouldBe("double");
        types["VolumeSlider.IsEnabled"].ShouldBe("bool");
        types["VolumeSlider.ToolTip"].ShouldBe("string");
        types.Keys.ShouldContain("VolumeSlider.Width");
    }

    [Fact]
    public void Describe_ListsAnEnumFormsPropertyWithItsValues()
    {
        string projectPath = CreateFormsProject();
        Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "addInstance", "element": "Settings", "name": "Stack", "type": "Controls/StackPanel" } ] }
            """).ExitCode.ShouldBe(0);

        CliTestHelper result = CliTestHelper.Run("describe", projectPath, "Settings", "--instance", "Stack", "--json");

        result.ExitCode.ShouldBe(0, result.StandardError);
        using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
        JsonElement orientation = document.RootElement.GetProperty("variables").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Stack.Orientation");
        orientation.GetProperty("type").GetString().ShouldBe("Orientation");
        orientation.GetProperty("values").EnumerateArray().Select(item => item.GetString()).ShouldBe(new[] { "Horizontal", "Vertical" });
    }

    [Fact]
    public void Describe_EveryListedFormsName_IsAcceptedBySetVariable()
    {
        string projectPath = CreateFormsProject();
        string[] sliderNames = GetListedNames(projectPath, "Settings", "--instance", "VolumeSlider");
        string[] checkNames = GetListedNames(projectPath, "Settings", "--instance", "MuteCheck");
        string[] fancyNames = GetListedNames(projectPath, "FancySlider");
        sliderNames.ShouldContain("VolumeSlider.Minimum");
        checkNames.ShouldContain("MuteCheck.IsChecked");
        fancyNames.ShouldContain("Maximum");
        IEnumerable<object> operations = sliderNames.Concat(checkNames)
                .Select(name => new { op = "setVariable", element = "Settings", name, value = (object?)null })
            .Concat(fancyNames.Select(name => new { op = "setVariable", element = "FancySlider", name, value = (object?)null }));

        CliTestHelper result = Edit(projectPath, JsonSerializer.Serialize(new { version = 1, operations }));

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
    }
}
