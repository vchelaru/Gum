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

    private string CreateGame(ObjectInstantiationType instantiationType) => GeneratedGameHarness.CreateGame(TempDirectory, instantiationType);

    private static CliTestHelper Codegen(string gumx) => GeneratedGameHarness.Codegen(gumx);

    private static void EditProject(string gumx, Action<GumProjectSave> edit) => GeneratedGameHarness.EditProject(gumx, edit);

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances) =>
        GeneratedGameHarness.AddComponent(project, name, baseType, instances);

    private List<string> Compile() => GeneratedGameHarness.Compile(TempDirectory);
}
