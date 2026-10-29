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
/// Each test runs <c>gumcli codegen</c> on a project and compiles the output against the MonoGame runtime.
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

    #region Harness

    private string CreateGame(ObjectInstantiationType instantiationType)
    {
        File.WriteAllText(Path.Combine(TempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup>" +
            "<ItemGroup><PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" /></ItemGroup></Project>");
        string gumx = Path.Combine(TempDirectory, "Content", "GumProject", "GumProject.gumx");
        CliTestHelper.Run("new", gumx, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("codegen-init", gumx).ExitCode.ShouldBe(0);

        string gumFolder = Path.GetDirectoryName(gumx)! + Path.DirectorySeparatorChar;
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(gumFolder));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.OutputLibrary = OutputLibrary.GumForms;
        settings.ObjectInstantiationType = instantiationType;
        settingsManager.WriteSettingsForProject(settings);
        return gumx;
    }

    private static CliTestHelper Codegen(string gumx) => CliTestHelper.Run("codegen", gumx);

    private static void EditProject(string gumx, Action<GumProjectSave> edit)
    {
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        edit(project);
        project.Save(gumx, saveElements: true);
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach ((string instanceName, string instanceBaseType) in instances)
        {
            component.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceBaseType, ParentContainer = component });
        }
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    /// <summary>Compiles every .cs under the game folder the way the game's build would; returns the errors.</summary>
    private List<string> Compile()
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        List<SyntaxTree> trees = Directory.GetFiles(TempDirectory, "*.cs", SearchOption.AllDirectories)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToList();

        // Only the framework and the MonoGame runtime: RaylibGum and the Forms placeholders declare the same type names.
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        HashSet<string> referencePaths = new HashSet<string>(
            Directory.GetFiles(frameworkDirectory, "*.dll").Where(IsManagedAssembly),
            StringComparer.OrdinalIgnoreCase)
        {
            Assembly.Load("GumCommon").Location,
            Assembly.Load("MonoGameGum").Location,
            Assembly.Load("MonoGame.Framework").Location,
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "MemberNames_" + Guid.NewGuid().ToString("N"),
            trees,
            referencePaths.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Annotations));

        using MemoryStream stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        return result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .Take(10)
            .ToList();
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
            return true;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private class SilentLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }

    #endregion
}
