using System.Security.Cryptography;
using System.Xml.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #8358: the rename ops run the tool's rename, so nothing that names the old name is left behind. Each test renames
/// something and then expects <c>check</c> to stay clean, which fails when a reference of that kind dangles.
/// </summary>
public class EditRenameTests : BaseTestClass
{
    private string CreateEmptyProject()
    {
        string projectPath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        return projectPath;
    }

    private CliTestHelper Edit(string projectPath, string operations)
    {
        string path = Path.Combine(TempDirectory, "ops-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{ \"version\": 1, \"operations\": [" + operations + "] }");
        return CliTestHelper.Run("edit", projectPath, path);
    }

    private CliTestHelper EditOk(string projectPath, string operations)
    {
        CliTestHelper result = Edit(projectPath, operations);
        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
        return result;
    }

    private static string Folder(string projectPath) => Path.GetDirectoryName(projectPath)!;

    private static GumProjectSave Load(string projectPath)
    {
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private void ShouldBeCleanAndStable(string projectPath)
    {
        CliTestHelper check = CliTestHelper.Run("check", projectPath);
        check.ExitCode.ShouldBe(0, check.StandardOutput + check.StandardError);

        Dictionary<string, string> before = Snapshot(projectPath);
        CliTestHelper.Run("resave", projectPath).ExitCode.ShouldBe(0);
        Snapshot(projectPath).ShouldBe(before);
    }

    private static Dictionary<string, string> Snapshot(string projectPath) =>
        Directory.GetFiles(Folder(projectPath), "*", SearchOption.AllDirectories)
            .ToDictionary(file => file, file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));

    private static void WriteAnimations(string projectPath, string subfolder, string elementName, params AnimationSave[] animations)
    {
        ElementAnimationsSave save = new ElementAnimationsSave();
        save.Animations.AddRange(animations);
        string path = Path.Combine(Folder(projectPath), subfolder, elementName + ".ganx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        save.Save(path);
    }

    #region Element

    [Fact]
    public void RenameElement_MovesToFolder_UpdatesBaseTypeInstanceTypeAndFile()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addComponent", "name": "FancyCard", "baseType": "Card" },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Main", "type": "Card" }
            """);

        EditOk(projectPath, """{ "op": "renameElement", "name": "Card", "newName": "Cards/Panel" }""");

        GumProjectSave project = Load(projectPath);
        project.GetComponentSave("Card").ShouldBeNull();
        project.GetComponentSave("Cards/Panel").ShouldNotBeNull();
        project.GetComponentSave("FancyCard")!.BaseType.ShouldBe("Cards/Panel");
        project.GetScreenSave("Menu")!.GetInstance("Main")!.BaseType.ShouldBe("Cards/Panel");
        File.Exists(Path.Combine(Folder(projectPath), "Components", "Card.gucx")).ShouldBeFalse();
        File.Exists(Path.Combine(Folder(projectPath), "Components", "Cards", "Panel.gucx")).ShouldBeTrue();
        project.ComponentReferences.Select(item => item.Name).ShouldContain("Cards/Panel");
        project.ComponentReferences.Select(item => item.Name).ShouldNotContain("Card");
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameElement_FollowsFavoritesAndAnimationAndCodeSettingsFiles()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """{ "op": "addComponent", "name": "Card" }""");
        GumProjectSave project = Load(projectPath);
        project.FavoriteComponents.Add("Card");
        project.Save(projectPath, saveElements: true);
        WriteAnimations(projectPath, "Components", "Card", new AnimationSave { Name = "Intro" });
        File.WriteAllText(Path.Combine(Folder(projectPath), "Components", "Card.codsj"), "{}");

        EditOk(projectPath, """{ "op": "renameElement", "name": "Card", "newName": "Panel" }""");

        Load(projectPath).FavoriteComponents.ShouldBe(new[] { "Panel" });
        string components = Path.Combine(Folder(projectPath), "Components");
        File.Exists(Path.Combine(components, "Card.ganx")).ShouldBeFalse();
        File.Exists(Path.Combine(components, "Panel.ganx")).ShouldBeTrue();
        File.Exists(Path.Combine(components, "Card.codsj")).ShouldBeFalse();
        File.Exists(Path.Combine(components, "Panel.codsj")).ShouldBeTrue();
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameElement_ReferencesInVariableReferenceLines_AreRewritten()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addInstance", "element": "Card", "name": "Box", "type": "ColoredRectangle" },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Main", "type": "Card" },
            { "op": "addInstance", "element": "Menu", "name": "Follower", "type": "ColoredRectangle" },
            { "op": "setVariableReferences", "element": "Menu", "instance": "Follower", "lines": ["Width = Components/Card.Width"] }
            """);

        EditOk(projectPath, """{ "op": "renameElement", "name": "Card", "newName": "Panel" }""");

        Load(projectPath).GetScreenSave("Menu")!.DefaultState.VariableLists
            .SelectMany(list => list.ValueAsIList.Cast<string>())
            .ShouldContain(line => line.Contains("Components/Panel.Width"));
        ShouldBeCleanAndStable(projectPath);
    }

    [Theory]
    [InlineData("""{ "op": "renameElement", "name": "Text", "newName": "Words" }""", "standard element")]
    [InlineData("""{ "op": "renameElement", "name": "Card", "newName": "card" }""", "letter case")]
    [InlineData("""{ "op": "renameElement", "name": "Card", "newName": "Other" }""", "Other")]
    public void RenameElement_Refusals_SayWhatToChange(string operation, string expected)
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addComponent", "name": "Other" }
            """);
        Dictionary<string, string> before = Snapshot(projectPath);

        CliTestHelper result = Edit(projectPath, operation);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain(expected);
        Snapshot(projectPath).ShouldBe(before);
    }

    #endregion

    #region Instance

    [Fact]
    public void RenameInstance_RenamesVariablesParentsAnimationsAndInheritedCopies()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Base" },
            { "op": "addInstance", "element": "Base", "name": "Holder", "type": "Container" },
            { "op": "addInstance", "element": "Base", "name": "Label", "type": "Text", "parent": "Holder" },
            { "op": "setVariable", "element": "Base", "name": "Label.Text", "value": "Hi" },
            { "op": "addComponent", "name": "Derived", "baseType": "Base" }
            """);
        WriteAnimations(projectPath, "Components", "Base",
            new AnimationSave { Name = "Intro", Animations = { new AnimationReferenceSave { Name = "Holder.Fade" } } });

        EditOk(projectPath, """{ "op": "renameInstance", "element": "Base", "name": "Holder", "newName": "Frame" }""");

        GumProjectSave project = Load(projectPath);
        ComponentSave baseComponent = project.GetComponentSave("Base")!;
        baseComponent.GetInstance("Frame").ShouldNotBeNull();
        baseComponent.GetInstance("Holder").ShouldBeNull();
        baseComponent.DefaultState.GetValue("Label.Parent").ShouldBe("Frame");
        project.GetComponentSave("Derived")!.GetInstance("Frame").ShouldNotBeNull();
        ElementAnimationsSave.Load(Path.Combine(Folder(projectPath), "Components", "Base.ganx"))
            .Animations[0].Animations[0].Name.ShouldBe("Frame.Fade");
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameInstance_RewritesVariableReferencesAndExposedVariables()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addInstance", "element": "Card", "name": "Title", "type": "Text" },
            { "op": "addInstance", "element": "Card", "name": "Box", "type": "ColoredRectangle" },
            { "op": "exposeVariable", "element": "Card", "instance": "Title", "variable": "Text", "name": "Caption" },
            { "op": "setVariableReferences", "element": "Card", "instance": "Box", "lines": ["Width = Title.Width"] }
            """);

        EditOk(projectPath, """{ "op": "renameInstance", "element": "Card", "name": "Title", "newName": "Heading" }""");

        ComponentSave card = Load(projectPath).GetComponentSave("Card")!;
        card.DefaultState.GetVariableSave("Heading.Text")!.ExposedAsName.ShouldBe("Caption");
        card.DefaultState.VariableLists.SelectMany(list => list.ValueAsIList.Cast<string>())
            .ShouldContain(line => line.Contains("Heading.Width"));
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameInstance_InheritedOrDuplicateName_IsRefused()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Base" },
            { "op": "addInstance", "element": "Base", "name": "A", "type": "Container" },
            { "op": "addInstance", "element": "Base", "name": "B", "type": "Container" },
            { "op": "addComponent", "name": "Derived", "baseType": "Base" }
            """);

        Edit(projectPath, """{ "op": "renameInstance", "element": "Base", "name": "A", "newName": "B" }""").ExitCode.ShouldBe(1);
        CliTestHelper inherited = Edit(projectPath, """{ "op": "renameInstance", "element": "Derived", "name": "A", "newName": "C" }""");
        inherited.ExitCode.ShouldBe(1);
        inherited.StandardError.ShouldContain("Rename it there");
    }

    #endregion

    #region State and category

    [Fact]
    public void RenameState_UpdatesStateSelectorsOnOtherElementsAndKeyframes()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Toggle" },
            { "op": "addCategory", "element": "Toggle", "name": "OnOff" },
            { "op": "addState", "element": "Toggle", "category": "OnOff", "name": "Off" },
            { "op": "addState", "element": "Toggle", "category": "OnOff", "name": "On" },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Switch", "type": "Toggle" },
            { "op": "setVariable", "element": "Menu", "name": "Switch.OnOffState", "value": "On" }
            """);
        WriteAnimations(projectPath, "Components", "Toggle", new AnimationSave
        {
            Name = "Flip",
            States = { new AnimatedStateSave { StateName = "OnOff/On" }, new AnimatedStateSave { StateName = "OnOff/Off" } }
        });

        EditOk(projectPath, """{ "op": "renameState", "element": "Toggle", "category": "OnOff", "name": "On", "newName": "Enabled" }""");

        GumProjectSave project = Load(projectPath);
        project.GetScreenSave("Menu")!.DefaultState.GetValue("Switch.OnOffState").ShouldBe("Enabled");
        project.GetComponentSave("Toggle")!.Categories[0].States.Select(item => item.Name).ShouldContain("Enabled");
        ElementAnimationsSave.Load(Path.Combine(Folder(projectPath), "Components", "Toggle.ganx"))
            .Animations[0].States.Select(item => item.StateName).ShouldBe(new[] { "OnOff/Enabled", "OnOff/Off" });
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameCategory_RenamesStateVariablesOnInstancesAndKeyframes()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Toggle" },
            { "op": "addCategory", "element": "Toggle", "name": "OnOff" },
            { "op": "addState", "element": "Toggle", "category": "OnOff", "name": "On" },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Switch", "type": "Toggle" },
            { "op": "setVariable", "element": "Menu", "name": "Switch.OnOffState", "value": "On" }
            """);
        WriteAnimations(projectPath, "Components", "Toggle", new AnimationSave
        {
            Name = "Flip",
            States = { new AnimatedStateSave { StateName = "OnOff/On" } }
        });

        EditOk(projectPath, """{ "op": "renameCategory", "element": "Toggle", "name": "OnOff", "newName": "Power" }""");

        GumProjectSave project = Load(projectPath);
        VariableSave variable = project.GetScreenSave("Menu")!.DefaultState.GetVariableSave("Switch.PowerState")!;
        variable.Type.ShouldBe("Power");
        variable.Value.ShouldBe("On");
        project.GetComponentSave("Toggle")!.DefaultState.GetVariableSave("PowerState").ShouldNotBeNull();
        ElementAnimationsSave.Load(Path.Combine(Folder(projectPath), "Components", "Toggle.ganx"))
            .Animations[0].States[0].StateName.ShouldBe("Power/On");
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameState_OfDefaultState_IsRefused()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """{ "op": "addComponent", "name": "Card" }""");

        CliTestHelper result = Edit(projectPath, """{ "op": "renameState", "element": "Card", "name": "Default", "newName": "Start" }""");

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("default state");
    }

    #endregion

    #region Variable

    [Fact]
    public void RenameVariable_Custom_RenamesOnDerivedAndOnInstances()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addVariable", "element": "Card", "name": "Score", "type": "int", "value": 3 },
            { "op": "addComponent", "name": "Fancy", "baseType": "Card" },
            { "op": "setVariable", "element": "Fancy", "name": "Score", "value": 9 },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Main", "type": "Card" },
            { "op": "setVariable", "element": "Menu", "name": "Main.Score", "value": 5 }
            """);

        EditOk(projectPath, """{ "op": "renameVariable", "element": "Card", "name": "Score", "newName": "Points" }""");

        GumProjectSave project = Load(projectPath);
        project.GetComponentSave("Card")!.DefaultState.GetVariableSave("Points").ShouldNotBeNull();
        project.GetComponentSave("Card")!.DefaultState.GetVariableSave("Score").ShouldBeNull();
        project.GetComponentSave("Fancy")!.DefaultState.GetVariableSave("Points")!.Value.ShouldBe(9);
        project.GetScreenSave("Menu")!.DefaultState.GetVariableSave("Main.Points")!.Value.ShouldBe(5);
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameVariable_Exposed_RenamesTheExposedNameWhereItIsSet()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addInstance", "element": "Card", "name": "Title", "type": "Text" },
            { "op": "exposeVariable", "element": "Card", "instance": "Title", "variable": "Text", "name": "Caption" },
            { "op": "addScreen", "name": "Menu" },
            { "op": "addInstance", "element": "Menu", "name": "Main", "type": "Card" },
            { "op": "setVariable", "element": "Menu", "name": "Main.Caption", "value": "Hello" }
            """);

        EditOk(projectPath, """{ "op": "renameVariable", "element": "Card", "name": "Caption", "newName": "Heading" }""");

        GumProjectSave project = Load(projectPath);
        project.GetComponentSave("Card")!.DefaultState.GetVariableSave("Title.Text")!.ExposedAsName.ShouldBe("Heading");
        project.GetScreenSave("Menu")!.DefaultState.GetVariableSave("Main.Heading")!.Value.ShouldBe("Hello");
        ShouldBeCleanAndStable(projectPath);
    }

    #endregion

    #region Behavior

    [Fact]
    public void RenameBehavior_UpdatesComponentReferencesProjectReferenceAndFile()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """{ "op": "addComponent", "name": "Card" }""");
        GumProjectSave project = Load(projectPath);
        BehaviorSave behavior = new BehaviorSave { Name = "Selectable" };
        string behaviorFolder = Path.Combine(Folder(projectPath), "Behaviors");
        Directory.CreateDirectory(behaviorFolder);
        behavior.Save(Path.Combine(behaviorFolder, "Selectable.behx"));
        project.BehaviorReferences.Add(new BehaviorReference { Name = "Selectable" });
        project.GetComponentSave("Card")!.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "Selectable" });
        project.Save(projectPath, saveElements: true);

        EditOk(projectPath, """{ "op": "renameBehavior", "name": "Selectable", "newName": "Pickable" }""");

        project = Load(projectPath);
        project.Behaviors.Select(item => item.Name).ShouldBe(new[] { "Pickable" });
        project.BehaviorReferences.Select(item => item.Name).ShouldBe(new[] { "Pickable" });
        project.GetComponentSave("Card")!.Behaviors.Select(item => item.BehaviorName).ShouldBe(new[] { "Pickable" });
        File.Exists(Path.Combine(behaviorFolder, "Selectable.behx")).ShouldBeFalse();
        File.Exists(Path.Combine(behaviorFolder, "Pickable.behx")).ShouldBeTrue();
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void RenameBehavior_StandardFormsBehavior_IsRefused()
    {
        string projectPath = Path.Combine(TempDirectory, "Forms", "Forms.gumx");
        CliTestHelper.Run("new", projectPath).ExitCode.ShouldBe(0);

        CliTestHelper result = Edit(projectPath, """{ "op": "renameBehavior", "name": "ButtonBehavior", "newName": "Tappable" }""");

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("standard Gum Forms behavior");
    }

    #endregion

    [Fact]
    public void Schema_ListsTheRenameOps()
    {
        string schema = CliTestHelper.Run("edit", "--schema").StandardOutput;

        foreach (string op in new[] { "renameElement", "renameInstance", "renameState", "renameCategory", "renameVariable", "renameBehavior" })
        {
            schema.ShouldContain(op);
        }
    }
}
