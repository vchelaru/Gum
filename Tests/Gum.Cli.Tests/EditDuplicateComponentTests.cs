using System.Security.Cryptography;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// <c>duplicateComponent</c> copies a component the way the tool's Duplicate does: the whole element under a new
/// name, plus its animation and code settings files.
/// </summary>
public class EditDuplicateComponentTests : BaseTestClass
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

    private void EditOk(string projectPath, string operations)
    {
        CliTestHelper result = Edit(projectPath, operations);
        result.ExitCode.ShouldBe(0, result.StandardOutput + result.StandardError);
    }

    private static string Folder(string projectPath) => Path.GetDirectoryName(projectPath)!;

    private static GumProjectSave Load(string projectPath)
    {
        GumProjectSave project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static void ShouldBeCleanAndStable(string projectPath)
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

    [Fact]
    public void DuplicateComponent_CopiesInstancesStatesCategoriesVariablesAndBehaviors()
    {
        string projectPath = CreateEmptyProject();
        CliTestHelper.Run("add-forms", projectPath).ExitCode.ShouldBe(0);
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addBehavior", "element": "Card", "behavior": "ButtonBehavior" },
            { "op": "addInstance", "element": "Card", "name": "Body", "type": "Container" },
            { "op": "addInstance", "element": "Card", "name": "Title", "type": "Text", "parent": "Body" },
            { "op": "setVariable", "element": "Card", "name": "Title.Width", "value": 40 },
            { "op": "addVariable", "element": "Card", "name": "Score", "type": "int", "value": 3 },
            { "op": "addCategory", "element": "Card", "name": "Size" },
            { "op": "addState", "element": "Card", "name": "Big", "category": "Size" },
            { "op": "setVariable", "element": "Card", "name": "Width", "value": 200, "category": "Size", "state": "Big" }
            """);

        EditOk(projectPath, """{ "op": "duplicateComponent", "name": "Card", "newName": "CardCopy" }""");

        GumProjectSave project = Load(projectPath);
        ComponentSave source = project.GetComponentSave("Card")!;
        ComponentSave copy = project.GetComponentSave("CardCopy")!;
        copy.ShouldNotBeNull();
        project.ComponentReferences.Select(item => item.Name).ShouldContain("CardCopy");
        File.Exists(Path.Combine(Folder(projectPath), "Components", "CardCopy.gucx")).ShouldBeTrue();
        copy.Instances.Select(item => (item.Name, item.BaseType)).ShouldBe(source.Instances.Select(item => (item.Name, item.BaseType)));
        copy.Behaviors.Select(item => item.BehaviorName).ShouldBe(source.Behaviors.Select(item => item.BehaviorName));
        copy.Categories.Select(item => item.Name).ShouldBe(source.Categories.Select(item => item.Name));
        copy.Categories.Single(item => item.Name == "Size").States.Single(item => item.Name == "Big")
            .GetValue("Width").ShouldBe(200f);
        copy.DefaultState.GetValue("Title.Width").ShouldBe(40f);
        copy.DefaultState.GetValue("Score").ShouldBe(3);
        copy.DefaultState.GetValue("Title.Parent").ShouldBe("Body");
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void DuplicateComponent_InAFolder_KeepsTheFolderNamedByNewName()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """{ "op": "addComponent", "name": "Cards/Card" }""");

        EditOk(projectPath, """{ "op": "duplicateComponent", "name": "Cards/Card", "newName": "Cards/Card2" }""");

        Load(projectPath).GetComponentSave("Cards/Card2").ShouldNotBeNull();
        File.Exists(Path.Combine(Folder(projectPath), "Components", "Cards", "Card2.gucx")).ShouldBeTrue();
        ShouldBeCleanAndStable(projectPath);
    }

    [Fact]
    public void DuplicateComponent_CopiesTheAnimationFileAndCodeSettingsWithoutTheFileNameOverride()
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """{ "op": "addComponent", "name": "Card" }""");
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(new AnimationSave { Name = "Intro" });
        string components = Path.Combine(Folder(projectPath), "Components");
        animations.Save(Path.Combine(components, "CardAnimations.ganx"));
        File.WriteAllText(Path.Combine(components, "Card.codsj"), """{ "GeneratedFileName": "Shared.cs", "UsingStatements": "using Foo;" }""");

        EditOk(projectPath, """{ "op": "duplicateComponent", "name": "Card", "newName": "CardCopy" }""");

        ElementAnimationsSave.Load(Path.Combine(components, "CardCopyAnimations.ganx"))
            .Animations.Select(item => item.Name).ShouldBe(new[] { "Intro" });
        File.Exists(Path.Combine(components, "CardAnimations.ganx")).ShouldBeTrue();
        string settings = File.ReadAllText(Path.Combine(components, "CardCopy.codsj"));
        settings.ShouldContain("using Foo;");
        settings.ShouldNotContain("Shared.cs");
        File.ReadAllText(Path.Combine(components, "Card.codsj")).ShouldContain("Shared.cs");
        ShouldBeCleanAndStable(projectPath);
    }

    [Theory]
    [InlineData("""{ "op": "duplicateComponent", "name": "Nope", "newName": "Other" }""", "no Component named Nope")]
    [InlineData("""{ "op": "duplicateComponent", "name": "Card", "newName": "Taken" }""", "Taken")]
    [InlineData("""{ "op": "duplicateComponent", "name": "Menu", "newName": "Other" }""", "no Component named Menu")]
    [InlineData("""{ "op": "duplicateComponent", "name": "Text", "newName": "Other" }""", "Standard elements cannot be duplicated")]
    public void DuplicateComponent_BadInput_IsRefusedAndWritesNothing(string operation, string expected)
    {
        string projectPath = CreateEmptyProject();
        EditOk(projectPath, """
            { "op": "addComponent", "name": "Card" },
            { "op": "addComponent", "name": "Taken" },
            { "op": "addScreen", "name": "Menu" }
            """);
        Dictionary<string, string> before = Snapshot(projectPath);

        CliTestHelper result = Edit(projectPath, operation);

        result.ExitCode.ShouldNotBe(0);
        (result.StandardOutput + result.StandardError).ShouldContain(expected);
        Snapshot(projectPath).ShouldBe(before);
    }
}
