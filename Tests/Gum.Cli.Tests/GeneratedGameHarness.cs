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
/// Makes a game folder with a Gum project and a csproj, runs <c>gumcli codegen</c> on it and compiles the
/// output against the MonoGame runtime the way the game's build would.
/// </summary>
internal static class GeneratedGameHarness
{
    public static string CreateGame(string tempDirectory, ObjectInstantiationType instantiationType, OutputLibrary outputLibrary = OutputLibrary.GumForms)
    {
        File.WriteAllText(Path.Combine(tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame</RootNamespace></PropertyGroup>" +
            "<ItemGroup><PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" /></ItemGroup></Project>");
        string gumx = Path.Combine(tempDirectory, "Content", "GumProject", "GumProject.gumx");
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

    public static CliTestHelper Codegen(string gumx) => CliTestHelper.Run("codegen", gumx);

    public static void EditProject(string gumx, Action<GumProjectSave> edit)
    {
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        edit(project);
        project.Save(gumx, saveElements: true);
    }

    public static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params (string Name, string BaseType)[] instances)
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
    public static List<string> Compile(string tempDirectory)
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        List<SyntaxTree> trees = Directory.GetFiles(tempDirectory, "*.cs", SearchOption.AllDirectories)
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
}
