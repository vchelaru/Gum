using System.Xml.Linq;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #8360: edit reorders instances and states and moves a state between categories, through the list moves the
/// tool's reorder commands use. Order is read from the saved files, and every success is also check-clean and
/// byte-identical to what resave writes.
/// </summary>
public class EditReorderTests : BaseTestClass
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

    private static XDocument ComponentFile(string projectPath, string component) =>
        XDocument.Load(Path.Combine(Path.GetDirectoryName(projectPath)!, "Components", component + ".gucx"));

    private static string[] SavedInstances(string projectPath, string component) =>
        ComponentFile(projectPath, component).Descendants("Instance").Select(item => item.Attribute("Name")!.Value).ToArray();

    private static string[] SavedStates(string projectPath, string component, string? category)
    {
        XElement root = ComponentFile(projectPath, component).Root!;
        IEnumerable<XElement> states = category == null
            ? root.Elements("State")
            : root.Elements("Category").Single(item => item.Element("Name")!.Value == category).Elements("State");
        return states.Select(item => item.Element("Name")!.Value).ToArray();
    }

    private void ShouldBeCheckCleanAndResaveStable(string projectPath)
    {
        CliTestHelper.Run("check", projectPath).ExitCode.ShouldBe(0);
        string directory = Path.GetDirectoryName(projectPath)!;
        Dictionary<string, string> before = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, File.ReadAllText);
        CliTestHelper.Run("resave", projectPath).ExitCode.ShouldBe(0);
        foreach ((string file, string text) in before)
        {
            File.ReadAllText(file).ShouldBe(text, $"{Path.GetFileName(file)} changed when resaved");
        }
    }

    private const string FourInstances = """
        { "op": "addComponent", "name": "Panel" },
        { "op": "addInstance", "element": "Panel", "name": "A", "type": "Container" },
        { "op": "addInstance", "element": "Panel", "name": "B", "type": "Container" },
        { "op": "addInstance", "element": "Panel", "name": "C", "type": "Container" },
        { "op": "addInstance", "element": "Panel", "name": "D", "type": "Container" }
        """;

    private string[] MoveInstanceAndRead(string moveFields)
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "moveInstance", "element": "Panel", {{moveFields}} } ] }
            """);
        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        ShouldBeCheckCleanAndResaveStable(projectPath);
        return SavedInstances(projectPath, "Panel");
    }

    [Fact]
    public void MoveInstance_ToFront_PutsItLast() =>
        MoveInstanceAndRead("\"name\": \"A\", \"toFront\": true").ShouldBe(new[] { "B", "C", "D", "A" });

    [Fact]
    public void MoveInstance_ToBack_PutsItFirst() =>
        MoveInstanceAndRead("\"name\": \"D\", \"toBack\": true").ShouldBe(new[] { "D", "A", "B", "C" });

    [Fact]
    public void MoveInstance_BeforeALaterSibling_LandsDirectlyBeforeIt() =>
        MoveInstanceAndRead("\"name\": \"A\", \"before\": \"D\"").ShouldBe(new[] { "B", "C", "A", "D" });

    [Fact]
    public void MoveInstance_AfterAnEarlierSibling_LandsDirectlyAfterIt() =>
        MoveInstanceAndRead("\"name\": \"D\", \"after\": \"A\"").ShouldBe(new[] { "A", "D", "B", "C" });

    [Fact]
    public void MoveInstance_AfterTheSiblingItAlreadyFollows_ChangesNothing() =>
        MoveInstanceAndRead("\"name\": \"B\", \"after\": \"A\"").ShouldBe(new[] { "A", "B", "C", "D" });

    [Theory]
    [InlineData(0, new[] { "D", "A", "B", "C" })]
    [InlineData(1, new[] { "A", "D", "B", "C" })]
    [InlineData(3, new[] { "A", "B", "C", "D" })]
    public void MoveInstance_Index_PutsItAtThatPositionAmongItsSiblings(int index, string[] expected) =>
        MoveInstanceAndRead($"\"name\": \"D\", \"index\": {index}").ShouldBe(expected);

    [Fact]
    public void MoveInstance_IndexForward_CountsTheFinalPosition() =>
        MoveInstanceAndRead("\"name\": \"A\", \"index\": 2").ShouldBe(new[] { "B", "C", "A", "D" });

    [Fact]
    public void MoveInstance_Index_CountsOnlyChildrenOfTheSameParent()
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "addInstance", "element": "Panel", "name": "P", "type": "Container", "parent": "B" },
              { "op": "addInstance", "element": "Panel", "name": "Q", "type": "Container", "parent": "B" },
              { "op": "addInstance", "element": "Panel", "name": "R", "type": "Container", "parent": "B" },
              { "op": "moveInstance", "element": "Panel", "name": "R", "index": 0 } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstances(projectPath, "Panel").ShouldBe(new[] { "A", "B", "C", "D", "R", "P", "Q" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveInstance_KeepsTheValuesItHad()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "setVariable", "element": "Panel", "name": "A.Width", "value": 33 },
              { "op": "moveInstance", "element": "Panel", "name": "A", "toFront": true } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        ComponentFile(projectPath, "Panel").Descendants("Variable").Select(item => item.Attribute("Name")!.Value).ShouldContain("A.Width");
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveInstance_InABaseComponent_MovesTheInheritedCopiesToo()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Panel" },
              { "op": "moveInstance", "element": "Panel", "name": "A", "toFront": true } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedInstances(projectPath, "Panel").ShouldBe(new[] { "B", "C", "D", "A" });
        SavedInstances(projectPath, "Derived").ShouldBe(new[] { "B", "C", "D", "A" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveInstance_InheritedInstance_IsRefusedNamingTheBase()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "addComponent", "name": "Derived", "baseType": "Panel" },
              { "op": "moveInstance", "element": "Derived", "name": "A", "toFront": true } ] }
            """);

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain("Move it in Panel");
    }

    [Fact]
    public void MoveInstance_AnchorWithADifferentParent_IsRefusedListingTheSiblings()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "addInstance", "element": "Panel", "name": "Child", "type": "Container", "parent": "B" },
              { "op": "moveInstance", "element": "Panel", "name": "A", "before": "Child" } ] }
            """);

        result.ExitCode.ShouldNotBe(0);
        string output = result.StandardOutput + result.StandardError;
        output.ShouldContain("different parents");
        output.ShouldContain("B, C, D");
    }

    [Theory]
    [InlineData("\"name\": \"A\"", "needs one of")]
    [InlineData("\"name\": \"A\", \"before\": \"B\", \"toFront\": true", "only one of")]
    [InlineData("\"name\": \"A\", \"before\": \"A\"", "itself")]
    [InlineData("\"name\": \"A\", \"after\": \"Nope\"", "no instance named Nope")]
    [InlineData("\"name\": \"A\", \"toFront\": false", "must be true")]
    [InlineData("\"name\": \"Nope\", \"toFront\": true", "no instance named Nope")]
    [InlineData("\"name\": \"A\", \"index\": 4", "index 4 is out of range: A has 4 siblings counting itself, so use 0 to 3")]
    [InlineData("\"name\": \"A\", \"index\": -1", "out of range")]
    [InlineData("\"name\": \"A\", \"index\": 1, \"toBack\": true", "only one of before, after, toFront, toBack, index")]
    [InlineData("\"name\": \"A\", \"index\": 1.5", "\"index\" must be a whole number")]
    public void MoveInstance_BadPosition_IsRefusedWithAMessage(string fields, string expected)
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{FourInstances}},
              { "op": "moveInstance", "element": "Panel", {{fields}} } ] }
            """);

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(expected);
    }

    private const string TwoCategories = """
        { "op": "addComponent", "name": "Panel" },
        { "op": "addInstance", "element": "Panel", "name": "Box", "type": "Container" },
        { "op": "addCategory", "element": "Panel", "name": "Size" },
        { "op": "addState", "element": "Panel", "name": "Small", "category": "Size" },
        { "op": "addState", "element": "Panel", "name": "Medium", "category": "Size" },
        { "op": "addState", "element": "Panel", "name": "Large", "category": "Size" },
        { "op": "addCategory", "element": "Panel", "name": "Mood" },
        { "op": "addState", "element": "Panel", "name": "Happy", "category": "Mood" },
        { "op": "addState", "element": "Panel", "name": "Sad", "category": "Mood" },
        { "op": "addState", "element": "Panel", "name": "Loose1" },
        { "op": "addState", "element": "Panel", "name": "Loose2" }
        """;

    private (string ProjectPath, CliTestHelper Result) RunMoveState(string fields)
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{TwoCategories}},
              { "op": "moveState", "element": "Panel", {{fields}} } ] }
            """);
        return (projectPath, result);
    }

    [Fact]
    public void MoveState_ToStartInACategory_PutsItFirst()
    {
        (string projectPath, CliTestHelper result) = RunMoveState("\"name\": \"Large\", \"category\": \"Size\", \"toStart\": true");

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedStates(projectPath, "Panel", "Size").ShouldBe(new[] { "Large", "Small", "Medium" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_AfterASiblingInACategory_LandsDirectlyAfterIt()
    {
        (string projectPath, CliTestHelper result) = RunMoveState("\"name\": \"Small\", \"category\": \"Size\", \"after\": \"Medium\"");

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedStates(projectPath, "Panel", "Size").ShouldBe(new[] { "Medium", "Small", "Large" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_AmongUncategorizedStates_KeepsDefaultFirst()
    {
        (string projectPath, CliTestHelper result) = RunMoveState("\"name\": \"Loose2\", \"after\": \"Default\"");

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedStates(projectPath, "Panel", null).ShouldBe(new[] { "Default", "Loose2", "Loose1" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_BeforeDefault_IsRefused()
    {
        (_, CliTestHelper result) = RunMoveState("\"name\": \"Loose2\", \"before\": \"Default\"");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain("Nothing can go before Default");
    }

    [Fact]
    public void MoveState_TheDefaultState_IsRefused()
    {
        (_, CliTestHelper result) = RunMoveState("\"name\": \"Default\", \"toEnd\": true");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain("default state");
    }

    [Fact]
    public void MoveState_ToAnotherCategory_AppendsItThere()
    {
        (string projectPath, CliTestHelper result) = RunMoveState("\"name\": \"Medium\", \"category\": \"Size\", \"toCategory\": \"Mood\"");

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedStates(projectPath, "Panel", "Size").ShouldBe(new[] { "Small", "Large" });
        SavedStates(projectPath, "Panel", "Mood").ShouldBe(new[] { "Happy", "Sad", "Medium" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_ToAnotherCategoryBeforeASibling_LandsThere()
    {
        (string projectPath, CliTestHelper result) = RunMoveState(
            "\"name\": \"Medium\", \"category\": \"Size\", \"toCategory\": \"Mood\", \"before\": \"Sad\"");

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        SavedStates(projectPath, "Panel", "Mood").ShouldBe(new[] { "Happy", "Medium", "Sad" });
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_ToAnotherCategory_PropagatesTheDestinationsVariablesToIt()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{TwoCategories}},
              { "op": "setVariable", "element": "Panel", "name": "Box.Width", "value": 50, "state": "Happy", "category": "Mood" },
              { "op": "moveState", "element": "Panel", "name": "Medium", "category": "Size", "toCategory": "Mood" } ] }
            """);

        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        XElement mood = ComponentFile(projectPath, "Panel").Root!.Elements("Category").Single(item => item.Element("Name")!.Value == "Mood");
        foreach (XElement state in mood.Elements("State"))
        {
            state.Descendants("Variable").Select(item => item.Attribute("Name")!.Value).ShouldContain("Box.Width", $"state {state.Element("Name")!.Value}");
        }
        ShouldBeCheckCleanAndResaveStable(projectPath);
    }

    [Fact]
    public void MoveState_FromUncategorizedIntoACategory_IsRefused()
    {
        (_, CliTestHelper result) = RunMoveState("\"name\": \"Loose1\", \"toCategory\": \"Mood\"");

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain("only between categories");
    }

    [Fact]
    public void MoveState_ToACategoryHoldingTheSameName_IsRefused()
    {
        string projectPath = CreateEmptyProject();

        CliTestHelper result = Edit(projectPath, $$"""
            { "version": 1, "operations": [ {{TwoCategories}},
              { "op": "addState", "element": "Panel", "name": "Small", "category": "Mood" },
              { "op": "moveState", "element": "Panel", "name": "Small", "category": "Size", "toCategory": "Mood" } ] }
            """);

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain("cannot move to Mood");
    }

    [Theory]
    [InlineData("\"name\": \"Small\", \"category\": \"Size\"", "needs one of")]
    [InlineData("\"name\": \"Small\", \"category\": \"Size\", \"after\": \"Medium\", \"toEnd\": true", "only one of")]
    [InlineData("\"name\": \"Small\", \"category\": \"Size\", \"after\": \"Happy\"", "has no state named Happy")]
    [InlineData("\"name\": \"Small\", \"category\": \"Size\", \"toCategory\": \"Nope\"", "no category named Nope")]
    [InlineData("\"name\": \"Small\", \"category\": \"Mood\", \"toStart\": true", "has no state named Small")]
    [InlineData("\"name\": \"Small\", \"category\": \"Size\", \"after\": \"Small\"", "itself")]
    public void MoveState_BadInput_IsRefusedWithAMessage(string fields, string expected)
    {
        (_, CliTestHelper result) = RunMoveState(fields);

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(expected);
    }

    [Fact]
    public void MoveState_ToAnotherCategory_WhenABehaviorNeedsTheState_IsRefusedNamingTheBehavior()
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper.Run("add-forms", projectPath).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, """
            { "version": 1, "operations": [
              { "op": "addComponent", "name": "MyButton" },
              { "op": "addBehavior", "element": "MyButton", "behavior": "ButtonBehavior" },
              { "op": "addCategory", "element": "MyButton", "name": "Other" },
              { "op": "moveState", "element": "MyButton", "category": "ButtonCategory", "name": "Pushed", "toCategory": "Other" }
            ] }
            """);

        result.ExitCode.ShouldNotBe(0);
        string output = result.StandardOutput + result.StandardError;
        output.ShouldContain("needed by the following behavior(s)");
        output.ShouldContain("ButtonBehavior");
    }

    [Fact]
    public void Schema_ListsTheMoveOperations()
    {
        CliTestHelper result = CliTestHelper.Run("edit", "--schema");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("moveInstance");
        result.StandardOutput.ShouldContain("moveState");
    }
}
