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
    public void Edit_UnexposeVariable_ClearsTheExposedNameAndTheValuesSetThroughIt()
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text" },
              { "op": "setVariable", "element": "Hud", "name": "Label.Text", "value": "Hi" },
              { "op": "exposeVariable", "element": "Hud", "instance": "Label", "variable": "Text", "name": "LabelText" },
              { "op": "addScreen", "name": "Game" },
              { "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.LabelText", "value": "Yo" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.Width", "value": 50 }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "unexposeVariable", "element": "Hud", "name": "LabelText" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        VariableSave label = project.Components.Single(item => item.Name == "Hud").DefaultState.GetVariableSave("Label.Text")!;
        label.ExposedAsName.ShouldBeNull();
        label.Value.ShouldBe("Hi");
        ScreenSave game = project.Screens.Single(item => item.Name == "Game");
        game.DefaultState.GetVariableSave("HudInstance.LabelText").ShouldBeNull();
        game.DefaultState.GetValue("HudInstance.Width").ShouldBe(50f);
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

    [Fact]
    public void Edit_RemoveScreenAndComponent_RemovesThemAndDeletesTheirFiles()
    {
        string projectPath = CreateEmptyProject();
        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Controls/Hud" },
              { "op": "addComponent", "name": "Kept" },
              { "op": "addScreen", "name": "Game" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string hudFile = Path.Combine(projectDirectory, "Components", "Controls", "Hud.gucx");
        string gameFile = Path.Combine(projectDirectory, "Screens", "Game.gusx");
        File.Exists(hudFile).ShouldBeTrue();
        File.Exists(gameFile).ShouldBeTrue();
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "removeComponent", "name": "Controls/Hud" },
              { "op": "removeScreen", "name": "Game" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        File.Exists(hudFile).ShouldBeFalse();
        File.Exists(gameFile).ShouldBeFalse();
        File.Exists(Path.Combine(projectDirectory, "Components", "Kept.gucx")).ShouldBeTrue();
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.Components.Select(item => item.Name).ShouldBe(new[] { "Kept" });
        project.ComponentReferences.Select(item => item.Name).ShouldBe(new[] { "Kept" });
        project.Screens.ShouldBeEmpty();
        project.ScreenReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Edit_RemoveComponentThenAddItAgain_KeepsTheNewFile()
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [ { "op": "addComponent", "name": "Hud", "baseType": "Text" } ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "removeComponent", "name": "Hud" },
              { "op": "addComponent", "name": "Hud" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        File.Exists(Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", "Hud.gucx")).ShouldBeTrue();
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.Components.ShouldHaveSingleItem().BaseType.ShouldBe("Container");
    }

    [Theory]
    [InlineData("""{ "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" }""", "Game")]
    [InlineData("""{ "op": "addComponent", "name": "BigHud", "baseType": "Hud" }""", "BigHud")]
    public void Edit_RemoveComponentStillInUse_FailsAndKeepsItsFile(string usingOperation, string usingElement)
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations($$"""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addScreen", "name": "Game" },
              {{usingOperation}}
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        Dictionary<string, string> before = ReadProjectFiles(projectPath);
        string operations = WriteOperations("""
            { "version": 1, "operations": [ { "op": "removeComponent", "name": "Hud" } ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(1, result.StandardOutput + result.StandardError);
        result.StandardOutput.ShouldContain($"error: {usingElement}:");
        result.StandardOutput.ShouldContain("Hud");
        ReadProjectFiles(projectPath).ShouldBe(before);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Edit_ExtractToComponent_DoesWhatTheToolsCreateComponentDoesAndPassesCheck(bool replace)
    {
        string projectPath = CreateEmptyProject();
        string setup = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "Hud" },
              { "op": "addInstance", "element": "Hud", "name": "Panel", "type": "Container" },
              { "op": "addInstance", "element": "Hud", "name": "Label", "type": "Text", "parent": "Panel" },
              { "op": "addInstance", "element": "Hud", "name": "Caption", "type": "Text" },
              { "op": "setVariable", "element": "Hud", "name": "Panel.X", "value": 50 },
              { "op": "setVariable", "element": "Hud", "name": "Panel.Width", "value": 200 },
              { "op": "setVariable", "element": "Hud", "name": "Label.Text", "value": "Hi" },
              { "op": "exposeVariable", "element": "Hud", "instance": "Label", "variable": "Text", "name": "LabelText" },
              { "op": "addCategory", "element": "Hud", "name": "Size" },
              { "op": "addState", "element": "Hud", "category": "Size", "name": "Big" },
              { "op": "setVariable", "element": "Hud", "category": "Size", "state": "Big", "name": "Panel.Width", "value": 300 },
              { "op": "setVariable", "element": "Hud", "category": "Size", "state": "Big", "name": "Label.Text", "value": "Big" },
              { "op": "addScreen", "name": "Game" },
              { "op": "addInstance", "element": "Game", "name": "HudInstance", "type": "Hud" },
              { "op": "setVariable", "element": "Game", "name": "HudInstance.LabelText", "value": "Yo" }
            ] }
            """);
        CliTestHelper.Run("edit", projectPath, setup).ExitCode.ShouldBe(0);
        string operations = WriteOperations("{ \"version\": 1, \"operations\": [ { \"op\": \"extractToComponent\", "
            + "\"element\": \"Hud\", \"instance\": \"Panel\", \"name\": \"Controls/PanelComponent\", \"replace\": "
            + (replace ? "true" : "false") + " } ] }");

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave panelComponent = project.Components.Single(item => item.Name == "Controls/PanelComponent");
        panelComponent.BaseType.ShouldBe("Container");
        panelComponent.Instances.Select(item => item.Name).ShouldBe(new[] { "Label" });
        StateSave panelDefault = panelComponent.GetDefaultStateOrThrow();
        panelDefault.GetValue("Width").ShouldBe(200f);
        panelDefault.GetVariableSave("X")?.SetsValue.ShouldNotBe(true);
        panelDefault.GetVariableSave("Label.Parent").ShouldBeNull();
        panelDefault.GetValue("Label.Text").ShouldBe("Hi");
        panelDefault.GetVariableSave("Label.Text")!.ExposedAsName.ShouldBeNull();
        ComponentSave hud = project.Components.Single(item => item.Name == "Hud");
        StateSave hudDefault = hud.GetDefaultStateOrThrow();
        StateSave big = hud.Categories.Single().States.Single();
        StateSave gameDefault = project.Screens.Single(item => item.Name == "Game").GetDefaultStateOrThrow();
        if (replace)
        {
            hud.Instances.Select(item => item.Name + ":" + item.BaseType)
                .ShouldBe(new[] { "Panel:Controls/PanelComponent", "Caption:Text" });
            hudDefault.GetValue("Panel.X").ShouldBe(50f);
            hudDefault.GetVariableSave("Panel.Width").ShouldBeNull();
            hudDefault.Variables.ShouldNotContain(item => item.ExposedAsName == "LabelText");
            big.GetValue("Panel.Width").ShouldBe(300f);
            big.GetVariableSave("Label.Text").ShouldBeNull();
            gameDefault.GetVariableSave("HudInstance.LabelText").ShouldBeNull();
        }
        else
        {
            hud.Instances.Select(item => item.Name).ShouldBe(new[] { "Panel", "Label", "Caption" });
            hudDefault.GetVariableSave("Label.Text")!.ExposedAsName.ShouldBe("LabelText");
            big.GetValue("Label.Text").ShouldBe("Big");
            gameDefault.GetValue("HudInstance.LabelText").ShouldBe("Yo");
        }
    }

    [Fact]
    public void Edit_SetVariableReferences_StoresTheLinesAppliesThemAndAnEmptyListRemovesThem()
    {
        string projectPath = CreateEmptyProject();
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "SegmentBar" },
              { "op": "addVariable", "element": "SegmentBar", "name": "TotalHealth", "type": "int", "value": 10 },
              { "op": "addInstance", "element": "SegmentBar", "name": "Total", "type": "Sprite" },
              { "op": "setVariableReferences", "element": "SegmentBar", "instance": "Total", "lines": [ "Width = TotalHealth * 4" ] },
              { "op": "setVariableReferences", "element": "SegmentBar", "lines": [ "Height = Total.Width" ] }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        StateSave state = new ProjectLoader().Load(projectPath).Project!.Components.Single(item => item.Name == "SegmentBar").DefaultState!;
        state.GetVariableListSave("Total.VariableReferences")!.ValueAsIList.ShouldBe(new[] { "Width = TotalHealth * 4" });
        state.GetValue("Total.Width").ShouldBe(40f);
        state.GetVariableListSave("VariableReferences")!.ValueAsIList.ShouldBe(new[] { "Height = Total.Width" });
        state.GetValue("Height").ShouldBe(40f);

        string remove = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "setVariableReferences", "element": "SegmentBar", "instance": "Total", "lines": [ ] }
            ] }
            """);
        CliTestHelper removeResult = CliTestHelper.Run("edit", projectPath, remove);
        removeResult.ExitCode.ShouldBe(0, removeResult.StandardOutput + removeResult.StandardError);
        state = new ProjectLoader().Load(projectPath).Project!.Components.Single(item => item.Name == "SegmentBar").DefaultState!;
        state.GetVariableListSave("Total.VariableReferences").ShouldBeNull();
        state.GetVariableListSave("VariableReferences").ShouldNotBeNull();
    }

    [Fact]
    public void Edit_SetVariableReferences_UpdatesOtherStatesAndElementsThatReadTheChangedValue()
    {
        string projectPath = CreateEmptyProject();
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "SegmentBar" },
              { "op": "addVariable", "element": "SegmentBar", "name": "TotalHealth", "type": "int", "value": 10 },
              { "op": "addInstance", "element": "SegmentBar", "name": "Total", "type": "Sprite" },
              { "op": "addCategory", "element": "SegmentBar", "name": "Size" },
              { "op": "addState", "element": "SegmentBar", "category": "Size", "name": "Big" },
              { "op": "setVariableReferences", "element": "SegmentBar", "category": "Size", "state": "Big", "lines": [ "Height = Total.Width" ] },
              { "op": "addComponent", "name": "Hud" },
              { "op": "setVariableReferences", "element": "Hud", "lines": [ "Width = Components/SegmentBar.Total.Width" ] },
              { "op": "setVariableReferences", "element": "SegmentBar", "instance": "Total", "lines": [ "Width = TotalHealth * 4" ] }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.Components.Single(item => item.Name == "SegmentBar").Categories.Single().States.Single()
            .GetValue("Height").ShouldBe(40f);
        project.Components.Single(item => item.Name == "Hud").DefaultState!.GetValue("Width").ShouldBe(40f);
    }

    [Fact]
    public void Edit_SetProjectProperty_SavesTheCanvasSizeAndTextureFilter()
    {
        string projectPath = CreateEmptyProject();
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": 256 },
              { "op": "setProjectProperty", "name": "DefaultCanvasHeight", "value": 224 },
              { "op": "setProjectProperty", "name": "TextureFilter", "value": "Linear" }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        project.DefaultCanvasWidth.ShouldBe(256);
        project.DefaultCanvasHeight.ShouldBe(224);
        project.TextureFilter.ShouldBe("Linear");
    }

    [Fact]
    public void Edit_SetVariableReferences_ReadingAFormsProperty_AppliesTheBehaviorLinesLikeTheTool()
    {
        // The row reads the slider's Minimum (its behavior default, 0) and gives Disabled, but the button
        // behavior's tool-only line derives Enabled from IsEnabled at its default. As in the tool, the
        // instance's Forms value wins, so no category state is stored.
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath).ExitCode.ShouldBe(0);
        string operations = WriteOperations("""
            { "version": 1, "operations": [
              { "op": "addScreen", "name": "Game" },
              { "op": "addInstance", "element": "Game", "name": "Volume", "type": "Controls/Slider" },
              { "op": "addInstance", "element": "Game", "name": "Play", "type": "Controls/ButtonStandard" },
              { "op": "setVariableReferences", "element": "Game", "instance": "Play",
                "lines": [ "ButtonCategoryState = Volume.Minimum < 0 ? \"Enabled\" : \"Disabled\"" ] }
            ] }
            """);

        CliTestHelper result = CliTestHelper.Run("edit", projectPath, operations);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        StateSave game = new ProjectLoader().Load(projectPath).Project!.Screens.Single(item => item.Name == "Game").DefaultState!;
        game.GetVariableListSave("Play.VariableReferences").ShouldNotBeNull();
        game.GetVariableSave("Play.ButtonCategoryState").ShouldBeNull();
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
    [InlineData("""{ "version": 1, "operations": [ { "op": "extractToComponent", "element": "Hud", "instance": "Label", "name": "LabelComponent", "replace": "yes" } ] }""", "true or false")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "extractToComponent", "element": "Hud", "instance": "Label", "name": "LabelComponent" } ] }""", "replace")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "setVariableReferences", "element": "Hud", "lines": "Width = 4" } ] }""", "\"lines\"")]
    [InlineData("""{ "version": 1, "operations": [ { "op": "setVariableReferences", "element": "Hud", "lines": [ 4 ] } ] }""", "\"lines\"")]
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
    [InlineData("""{ "op": "removeScreen", "name": "Hud" }""", "no Screen named Hud")]
    [InlineData("""{ "op": "removeComponent", "name": "Nope" }""", "no Component named Nope")]
    [InlineData("""{ "op": "removeState", "element": "Hud", "name": "Default" }""", "default state")]
    [InlineData("""{ "op": "removeCategory", "element": "Hud", "name": "Size" }""", "Size")]
    [InlineData("""{ "op": "removeVariable", "element": "Hud", "name": "Width" }""", "custom variable")]
    [InlineData("""{ "op": "clearVariable", "element": "Hud", "name": "Height" }""", "Height")]
    [InlineData("""{ "op": "unexposeVariable", "element": "Hud", "name": "Nope" }""", "no variable exposed as Nope")]
    [InlineData("""{ "op": "extractToComponent", "element": "Hud", "instance": "Nope", "name": "NopeComponent", "replace": true }""", "Nope")]
    [InlineData("""{ "op": "extractToComponent", "element": "Hud", "instance": "Label", "name": "Text", "replace": true }""", "Text")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Hud", "instance": "Label", "lines": [ "Widht = 4" ] }""", "Widht")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Hud", "instance": "Label", "lines": [ "Width = Nope.Width" ] }""", "Nope")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Hud", "instance": "Label", "lines": [ "Width = \"wide\"" ] }""", "Width = \"wide\"")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Hud", "instance": "Label", "lines": [ "Width +" ] }""", "Width +")]
    [InlineData("""{ "op": "setVariableReferences", "element": "Hud", "instance": "Nope", "lines": [ "Width = 4" ] }""", "Nope")]
    [InlineData("""{ "op": "setProjectProperty", "name": "ShowRuler", "value": true }""", "DefaultCanvasWidth, DefaultCanvasHeight, TextureFilter")]
    [InlineData("""{ "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": 0 }""", "DefaultCanvasWidth")]
    [InlineData("""{ "op": "setProjectProperty", "name": "DefaultCanvasHeight", "value": -5 }""", "-5")]
    [InlineData("""{ "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": "wide" }""", "wide")]
    [InlineData("""{ "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": 12.5 }""", "12.5")]
    [InlineData("""{ "op": "setProjectProperty", "name": "TextureFilter", "value": "Blurry" }""", "Blurry")]
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
