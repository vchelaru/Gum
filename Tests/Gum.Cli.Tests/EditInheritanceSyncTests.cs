using System.Xml.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #8365: edit keeps components that derive from another component in sync with it, as the tool's
/// InheritanceLogic does. Instances are read from the saved files: the loader repairs missing DefinedByBase
/// instances in memory, which would hide what edit wrote.
/// </summary>
public class EditInheritanceSyncTests : BaseTestClass
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

    private static List<(string Name, string BaseType, bool DefinedByBase)> SavedInstances(string projectPath, string component) =>
        XDocument.Load(Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", component + ".gucx"))
            .Descendants("Instance")
            .Select(item => (item.Attribute("Name")!.Value, item.Attribute("BaseType")!.Value,
                item.Attribute("DefinedByBase")?.Value == "true"))
            .ToList();

    private static string[] InheritedNames(string projectPath, string component) =>
        SavedInstances(projectPath, component).Where(item => item.DefinedByBase).Select(item => item.Name).ToArray();

    private const string BaseWithTwoInstances = """
        { "op": "addComponent", "name": "Base" },
        { "op": "addInstance", "element": "Base", "name": "Background", "type": "NineSlice" },
        { "op": "addInstance", "element": "Base", "name": "Label", "type": "Text" }
        """;

    [Fact]
    public void AddComponent_WithComponentBase_CopiesTheBaseInstancesAsDefinedByBase()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [
              {{BaseWithTwoInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Base" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstances(projectPath, "Derived").ShouldBe(new[]
        {
            ("Background", "NineSlice", true),
            ("Label", "Text", true),
        });
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void SetAndClearVariable_OnInheritedInstance_Apply()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [
              {{BaseWithTwoInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Base" },
              { "op": "setVariable", "element": "Derived", "name": "Background.Width", "value": 77 },
              { "op": "setVariable", "element": "Derived", "name": "Label.Text", "value": "Hi" },
              { "op": "clearVariable", "element": "Derived", "name": "Label.Text" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave derived = project.GetComponentSave("Derived")!;
        derived.DefaultState.GetValue("Background.Width").ShouldBe(77f);
        derived.DefaultState.GetVariableSave("Label.Text").ShouldBeNull();
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void AddInstance_ToBase_AddsItToEveryDerivedComponentInOrder()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [
              {{BaseWithTwoInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Base" },
              { "op": "addComponent", "name": "GrandChild", "baseType": "Derived" },
              { "op": "addInstance", "element": "Base", "name": "Kid", "type": "Sprite" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        InheritedNames(projectPath, "Derived").ShouldBe(new[] { "Background", "Label", "Kid" });
        InheritedNames(projectPath, "GrandChild").ShouldBe(new[] { "Background", "Label", "Kid" });
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void RemoveInstance_FromBase_RemovesItFromDerivedComponents()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [
              {{BaseWithTwoInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Base" },
              { "op": "removeInstance", "element": "Base", "name": "Background" }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        InheritedNames(projectPath, "Derived").ShouldBe(new[] { "Label" });
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void RemoveInstance_InheritedInstanceOnDerived_IsRefusedLikeTheTool()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [
              {{BaseWithTwoInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Base" },
              { "op": "removeInstance", "element": "Derived", "name": "Background" }
            ] }
            """);

        result.ExitCode.ShouldBe(1);
        (result.StandardOutput + result.StandardError).ShouldContain("defined in a base object");
    }

    [Fact]
    public void ExtractToComponent_Replace_KeepsDerivedComponentsOfTheSourceInSync()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Box", "type": "Container" },
              { "op": "addInstance", "element": "Panel", "name": "Inside", "type": "Text", "parent": "Box" },
              { "op": "addComponent", "name": "DerivedPanel", "baseType": "Panel" },
              { "op": "extractToComponent", "element": "Panel", "instance": "Box", "name": "BoxPart", "replace": true }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstances(projectPath, "DerivedPanel").ShouldBe(new[] { ("Box", "BoxPart", true) });
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
    }

    [Fact]
    public void ExtractToComponent_Replace_RepointsReferencesAndDerivedValuesOfAnExposedVariable()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Panel" },
              { "op": "addInstance", "element": "Panel", "name": "Box", "type": "Container" },
              { "op": "addInstance", "element": "Panel", "name": "Inside", "type": "Text", "parent": "Box" },
              { "op": "addInstance", "element": "Panel", "name": "Caption", "type": "Text" },
              { "op": "exposeVariable", "element": "Panel", "instance": "Inside", "variable": "Text", "name": "InsideText" },
              { "op": "setVariableReferences", "element": "Panel", "instance": "Caption", "lines": [ "Text = Inside.Text" ] },
              { "op": "addComponent", "name": "DerivedPanel", "baseType": "Panel" },
              { "op": "setVariable", "element": "DerivedPanel", "name": "Inside.Text", "value": "Derived" },
              { "op": "extractToComponent", "element": "Panel", "instance": "Box", "name": "BoxPart", "replace": true }
            ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project.ShouldNotBeNull();
        ElementSave panel = project.Components.Single(item => item.Name == "Panel");
        panel.DefaultState.ShouldNotBeNull();
        panel.DefaultState.VariableLists.Single(item => item.Name == "Caption.VariableReferences").ValueAsIList
            .ShouldBe(new object[] { "Text = Box.InsideText" });
        ElementSave derived = project.Components.Single(item => item.Name == "DerivedPanel");
        derived.DefaultState.ShouldNotBeNull();
        derived.DefaultState.GetValue("Box.InsideText").ShouldBe("Derived");
        derived.DefaultState.GetVariableSave("Inside.Text").ShouldBeNull();
    }
}
