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

    private string CreateGame(ObjectInstantiationType instantiationType, OutputLibrary outputLibrary = OutputLibrary.GumForms)
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
        settings.OutputLibrary = outputLibrary;
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
