using System.Xml.Linq;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #8359: edit changes the base type of an element or an instance, and sets an instance's IsSlot and Locked, through
/// the logic the tool runs when its Base Type row changes. Results are read from the saved files.
/// </summary>
public class EditBaseTypeTests : BaseTestClass
{
    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private CliTestHelper Edit(string projectPath, string json)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return CliTestHelper.Run("edit", projectPath, path);
    }

    private static XDocument Load(string projectPath, string folder, string extension, string name) =>
        XDocument.Load(Path.Combine(Path.GetDirectoryName(projectPath)!, folder, name + extension));

    private static XElement SavedInstance(string projectPath, string component, string instance) =>
        Load(projectPath, "Components", ".gucx", component).Descendants("Instance")
            .Single(item => item.Attribute("Name")!.Value == instance);

    private static string[] SavedVariableNames(string projectPath, string component) =>
        Load(projectPath, "Components", ".gucx", component).Descendants("Variable")
            .Select(item => item.Attribute("Name")!.Value).ToArray();

    private static string? SavedFlag(XElement instance, string name) =>
        instance.Attribute(name)?.Value ?? instance.Element(name)?.Value;

    [Fact]
    public void SetBaseType_OnInstance_ChangesTypeAndPrunesVariablesTheNewTypeLacks()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Thing", "type": "Text" },
              { "op": "setVariable", "element": "Panel", "name": "Thing.Text", "value": "Hi" },
              { "op": "setVariable", "element": "Panel", "name": "Thing.Width", "value": 40 },
              { "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "Container" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstance(projectPath, "Panel", "Thing").Attribute("BaseType")!.Value.ShouldBe("Container");
        string[] variables = SavedVariableNames(projectPath, "Panel");
        variables.ShouldNotContain("Thing.Text");
        variables.ShouldContain("Thing.Width");
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void SetBaseType_OnComponent_PrunesVariablesTheNewBaseLacks()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Label", "baseType": "Text" },
              { "op": "setVariable", "element": "Label", "name": "Text", "value": "Hi" },
              { "op": "setVariable", "element": "Label", "name": "Width", "value": 40 },
              { "op": "setBaseType", "element": "Label", "baseType": "Container" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        Load(projectPath, "Components", ".gucx", "Label").Root!.Element("BaseType")!.Value.ShouldBe("Container");
        string[] variables = SavedVariableNames(projectPath, "Label");
        variables.ShouldNotContain("Text");
        variables.ShouldContain("Width");
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void SetBaseType_OnComponent_ReplacesInheritedInstancesWithTheNewBasesInstances()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "BaseA" },
              { "op": "addInstance", "element": "BaseA", "name": "FromA", "type": "Text" },
              { "op": "addComponent", "name": "BaseB" },
              { "op": "addInstance", "element": "BaseB", "name": "FromB", "type": "Sprite" },
              { "op": "addComponent", "name": "Derived", "baseType": "BaseA" },
              { "op": "setBaseType", "element": "Derived", "baseType": "BaseB" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        XElement[] instances = Load(projectPath, "Components", ".gucx", "Derived").Descendants("Instance").ToArray();
        instances.Select(item => item.Attribute("Name")!.Value).ShouldBe(new[] { "FromB" });
        SavedFlag(instances[0], "DefinedByBase").ShouldBe("true");
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void SetBaseType_OnInstanceOfBase_FollowsInDerivedComponents()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "BaseComp" },
              { "op": "addInstance", "element": "BaseComp", "name": "Kid", "type": "Text" },
              { "op": "addComponent", "name": "Derived", "baseType": "BaseComp" },
              { "op": "setBaseType", "element": "BaseComp", "instance": "Kid", "baseType": "Sprite" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstance(projectPath, "Derived", "Kid").Attribute("BaseType")!.Value.ShouldBe("Sprite");
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void SetBaseType_ThenAnUnrelatedEdit_ResavesTheSameFile()
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Thing", "type": "Text" },
              { "op": "setVariable", "element": "Panel", "name": "Thing.Text", "value": "Hi" },
              { "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "Container" }
            ] }
            """).ExitCode.ShouldBe(0);
        string file = Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "Panel.gucx");
        string before = File.ReadAllText(file);

        Edit(projectPath, """
            { "version": 1, "operations": [ { "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": 321 } ] }
            """).ExitCode.ShouldBe(0);

        File.ReadAllText(file).ShouldBe(before);
    }

    [Theory]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "baseType": "NoSuchType" }""", "NoSuchType")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "baseType": "Panel" }""", "circular")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "baseType": "" }""", "Container")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "baseType": "Home" }""", "Screen")]
    [InlineData("""{ "op": "setBaseType", "element": "Home", "baseType": "Panel" }""", "Screen")]
    [InlineData("""{ "op": "setBaseType", "element": "Home", "baseType": "Home" }""", "circular")]
    [InlineData("""{ "op": "setBaseType", "element": "Text", "baseType": "Container" }""", "standard element")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "NoSuchType" }""", "NoSuchType")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "Home" }""", "Screens")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "" }""", "type")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "instance": "Missing", "baseType": "Sprite" }""", "Missing")]
    [InlineData("""{ "op": "setBaseType", "element": "Derived", "instance": "Thing", "baseType": "Sprite" }""", "Panel")]
    [InlineData("""{ "op": "setBaseType", "element": "Panel", "instance": "Thing", "baseType": "Outer" }""", "circular")]
    public void SetBaseType_Refuses(string operation, string messagePart)
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addScreen", "name": "Home" },
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Thing", "type": "Text" },
              { "op": "addComponent", "name": "Outer" },
              { "op": "addInstance", "element": "Outer", "name": "Inner", "type": "Panel" },
              { "op": "addComponent", "name": "Derived", "baseType": "Panel" }
            ] }
            """).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, $$"""{ "version": 1, "operations": [ {{operation}} ] }""");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(messagePart);
    }

    [Fact]
    public void SetInstanceProperty_SetsLockedAndIsSlot()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Body", "type": "Container" },
              { "op": "setInstanceProperty", "element": "Panel", "instance": "Body", "property": "Locked", "value": true },
              { "op": "setInstanceProperty", "element": "Panel", "instance": "Body", "property": "IsSlot", "value": true }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        XElement body = SavedInstance(projectPath, "Panel", "Body");
        SavedFlag(body, "Locked").ShouldBe("true");
        SavedFlag(body, "IsSlot").ShouldBe("true");
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);

        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "setInstanceProperty", "element": "Panel", "instance": "Body", "property": "Locked", "value": false }
            ] }
            """).ExitCode.ShouldBe(0);
        SavedFlag(SavedInstance(projectPath, "Panel", "Body"), "Locked").ShouldBeNull();
    }

    [Theory]
    [InlineData("""{ "op": "setInstanceProperty", "element": "Home", "instance": "Body", "property": "IsSlot", "value": true }""", "Component")]
    [InlineData("""{ "op": "setInstanceProperty", "element": "Panel", "instance": "Body", "property": "Visible", "value": true }""", "Locked, IsSlot")]
    [InlineData("""{ "op": "setInstanceProperty", "element": "Panel", "instance": "Missing", "property": "Locked", "value": true }""", "Missing")]
    [InlineData("""{ "op": "setInstanceProperty", "element": "Derived", "instance": "Body", "property": "Locked", "value": true }""", "Panel")]
    public void SetInstanceProperty_Refuses(string operation, string messagePart)
    {
        string projectPath = CreateEmptyProject();
        Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addScreen", "name": "Home" },
              { "op": "addInstance", "element": "Home", "name": "Body", "type": "Container" },
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Body", "type": "Container" },
              { "op": "addComponent", "name": "Derived", "baseType": "Panel" }
            ] }
            """).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, $$"""{ "version": 1, "operations": [ {{operation}} ] }""");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(messagePart);
    }
}
