using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using System.Reflection;

namespace Gum.Cli.Tests;

/// <summary>
/// A member of a generated class cannot be named like its enclosing type (CS0542), so an instance, custom
/// variable or exposed variable named like its own component must still generate a class that builds.
/// Each test runs <c>gumcli codegen</c> on a project and compiles the output against the MonoGame runtime (<see cref="GeneratedGameHarness"/>).
/// </summary>
public class GeneratedMemberNameCollisionTests : BaseTestClass
{
    [Theory]
    [InlineData(ObjectInstantiationType.FindByName)]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    public void Codegen_InstanceNamedLikeItsOwnComponent_GeneratesCompilableClass(ObjectInstantiationType instantiationType)
    {
        string gumx = CreateGame(instantiationType);
        EditProject(gumx, project => AddComponent(project, "Score", "Container", ("Score", "Text")));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_InstanceNamedLikeItsOwnComponentInAFolder_GeneratesCompilableClass()
    {
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project =>
        {
            AddComponent(project, "Hud/Score Panel", "Container", ("Score Panel", "Text"));
            AddComponent(project, "Hud/Header", "Container", ("Header", "Text"), ("Child", "Hud/Score Panel"));
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_ExposedAndCustomVariablesNamedLikeTheirComponent_GeneratesCompilableClass()
    {
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project =>
        {
            ComponentSave exposed = AddComponent(project, "Lives", "Container", ("Label", "Text"));
            exposed.DefaultState!.Variables.Add(new VariableSave
            {
                Name = "Label.Text", Type = "string", ExposedAsName = "Lives", SetsValue = false,
            });
            ComponentSave custom = AddComponent(project, "Coins", "Container", ("Label", "Text"));
            custom.DefaultState!.Variables.Add(new VariableSave { Name = "Coins", Type = "int", Value = 3, SetsValue = true, IsCustomVariable = true });
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_InstancesNamedLikeMembersOfTheFormsBaseClass_GeneratesCompilableClass()
    {
        // The tool rejects names of standard variables (Width, Visible, Parent, Name); these it accepts.
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project => AddComponent(project, "Header", "Container",
            ("Visual", "Container"), ("IsEnabled", "Text"), ("IsFocused", "Text"), ("Dispose", "Text"),
            ("ToString", "Text"), ("GetHashCode", "Text"), ("Tag", "Text"), ("Children", "Container"),
            ("CurrentState", "Text"), ("CustomInitialize", "Text"), ("Visual_", "Text")));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FindByName)]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    public void Codegen_InstanceAndVariablesNamedLikeACategory_GeneratesCompilableClass(ObjectInstantiationType instantiationType)
    {
        // The category generates a nested enum named "Size"; a member of the same name would collide with it (CS0102).
        string gumx = CreateGame(instantiationType);
        EditProject(gumx, project =>
        {
            ComponentSave panel = AddComponent(project, "Panel", "Container", ("Size", "Text"), ("Label", "Text"));
            AddCategory(panel, "Size", "Small", "Large");
            AddCategory(panel, "Mode", "On", "Off");
            AddCategory(panel, "Level", "Low", "High");
            panel.DefaultState!.Variables.Add(new VariableSave
            {
                Name = "Label.Text", Type = "string", ExposedAsName = "Mode", SetsValue = false,
            });
            panel.DefaultState.Variables.Add(new VariableSave { Name = "Level", Type = "int", Value = 3, SetsValue = true, IsCustomVariable = true });
        });

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_StateNamedLikeItsCategory_GeneratesCompilableClass()
    {
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project => AddCategory(AddComponent(project, "Panel", "Container", ("Label", "Text")), "Mode", "Mode", "Other"));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FindByName)]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    public void Codegen_CategoryNamedLikeItsComponent_BlocksThatElementUnderGumForms(ObjectInstantiationType instantiationType)
    {
        // Gum Forms names the class "Mode", so the nested enum "Mode" is CS0542. The enum is the public type of the
        // ModeState property, so the element is reported instead of renamed.
        string gumx = CreateGame(instantiationType);
        EditProject(gumx, project =>
        {
            AddCategory(AddComponent(project, "Mode", "Container", ("Label", "Text")), "Mode", "On", "Off");
            AddComponent(project, "Fine", "Container", ("Label", "Text"));
        });

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("error: Mode: Category \"Mode\" generates");
        Directory.GetFiles(TempDirectory, "Mode.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
        Directory.GetFiles(TempDirectory, "Fine.Generated.cs", SearchOption.AllDirectories).ShouldNotBeEmpty();
        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_CategoryNamedLikeItsComponent_StillGeneratesWhereTheClassHasARuntimeSuffix()
    {
        // The legacy MonoGame output names the class "ModeRuntime", so the enum "Mode" compiles and must not be blocked.
        string gumx = CreateGame(ObjectInstantiationType.FindByName, OutputLibrary.MonoGame);
        EditProject(gumx, project => AddCategory(AddComponent(project, "Mode", "Container", ("Label", "Text")), "Mode", "On", "Off"));

        Codegen(gumx).ExitCode.ShouldBe(0);

        Compile().ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_CategoryNamedLikeItsComponentThatIsNeverGenerated_IsNotReported()
    {
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project => AddCategory(AddComponent(project, "Mode", "Container"), "Mode", "On"));
        string gumFolder = Path.GetDirectoryName(gumx)! + Path.DirectorySeparatorChar;
        ComponentSave mode = GumProjectSave.Load(gumx, out _)!.Components.Single();
        new CodeOutputElementSettingsManager(new FixedProjectDirectoryProvider(gumFolder))
            .WriteSettingsForElement(mode, new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.NeverGenerate });

        Codegen(gumx).ExitCode.ShouldBe(0);
    }

    [Theory]
    [InlineData(OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode, "Mode", "ModeState")]
    [InlineData(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName, "Mode", "ModeState")]
    public void Codegen_TwoCategoriesGeneratingTheSameName_BlocksTheElement(
        OutputLibrary library, ObjectInstantiationType instantiationType, string firstCategory, string secondCategory)
    {
        // Category "Mode" generates the property ModeState and category "ModeState" an enum of that name (CS0102).
        string gumx = CreateGame(instantiationType, library);
        EditProject(gumx, project =>
        {
            ComponentSave panel = AddComponent(project, "Panel", "Container", ("Label", "Text"));
            AddCategory(panel, firstCategory, "On");
            AddCategory(panel, secondCategory, "On");
        });

        CliTestHelper result = Codegen(gumx);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("error: Panel: Category");
        Directory.GetFiles(TempDirectory, "Panel.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void Check_CategoryNamedLikeItsComponent_IsNotReported()
    {
        // Whether the name clashes depends on the output library, which only codegen knows.
        string gumx = CreateGame(ObjectInstantiationType.FullyInCode);
        EditProject(gumx, project => AddCategory(AddComponent(project, "Mode", "Container"), "Mode", "On"));

        CliTestHelper.Run("check", gumx).ExitCode.ShouldBe(0);
    }

    #region Harness

    private static void AddCategory(ComponentSave component, string categoryName, params string[] stateNames)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = component });
        }
        component.Categories.Add(category);
    }

    private string CreateGame(ObjectInstantiationType instantiationType, OutputLibrary outputLibrary = OutputLibrary.GumForms) =>
        GeneratedGameHarness.CreateGame(TempDirectory, instantiationType, outputLibrary);

    private static CliTestHelper Codegen(string gumx) => GeneratedGameHarness.Codegen(gumx);

    private static void EditProject(string gumx, Action<GumProjectSave> edit) => GeneratedGameHarness.EditProject(gumx, edit);

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances) =>
        GeneratedGameHarness.AddComponent(project, name, baseType, instances);

    private List<string> Compile() => GeneratedGameHarness.Compile(TempDirectory);

    #endregion
}
