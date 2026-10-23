using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gum.Cli.Tests;

/// <summary>
/// Compiles the FullyInCode output of the Forms template and constructs every generated control,
/// so generated code that touches its children before they exist fails here rather than in a game.
/// </summary>
public class FullyInCodeFormsConstructionTests : IDisposable
{
    private readonly string _tempDirectory;

    public FullyInCodeFormsConstructionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliFullyInCode_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Constructor_EveryGeneratedFormsControl_DoesNotThrow()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new FormsTemplateCreator().Create(gumxPath);
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>" +
            "<PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4\" />" +
            "</ItemGroup></Project>");

        // codegen replaces settings without a CodeProjectRoot with auto-detected FindByName ones.
        CliTestHelper.Run("codegen-init", gumxPath).ExitCode.ShouldBe(0);
        CodeOutputProjectSettingsManager settingsManager = new CodeOutputProjectSettingsManager(
            new SilentLogger(), new FixedProjectDirectoryProvider(_tempDirectory + Path.DirectorySeparatorChar));
        CodeOutputProjectSettings settings = settingsManager.CreateOrLoadSettingsForProject();
        settings.ObjectInstantiationType = ObjectInstantiationType.FullyInCode;
        settingsManager.WriteSettingsForProject(settings);

        CliTestHelper.Run("codegen", gumxPath).ExitCode.ShouldBe(0);

        Assembly generated = Compile(Directory.GetFiles(_tempDirectory, "*.cs", SearchOption.AllDirectories));

        // Gum.Cli.Tests also loads RaylibGum and the Forms placeholders, so the MonoGame runtime is reached by name.
        Type systemManagersType = MonoGameGum.GetType("RenderingLibrary.SystemManagers", throwOnError: true)!;
        PropertyInfo defaultProperty = systemManagersType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!;
        if (defaultProperty.GetValue(null) == null)
        {
            object systemManagers = Activator.CreateInstance(systemManagersType)!;
            systemManagersType.GetProperty("Renderer")!.SetValue(systemManagers,
                Activator.CreateInstance(MonoGameGum.GetType("RenderingLibrary.Graphics.Renderer", throwOnError: true)!));
            defaultProperty.SetValue(null, systemManagers);
        }
        // The hooks SystemManagers.Initialize installs, minus the graphics device. Other tests in this
        // process may have left another runtime's hooks installed.
        Type customSetProperty = MonoGameGum.GetTypes().Single(item => item.Name == "CustomSetPropertyOnRenderable");
        Type renderableCreator = MonoGameGum.GetType("Gum.Renderables.RenderableCreator", throwOnError: true)!;
        Install(typeof(global::Gum.Wireframe.GraphicalUiElement), "SetPropertyOnRenderable", customSetProperty, "SetPropertyOnRenderable");
        Install(typeof(global::Gum.Wireframe.GraphicalUiElement), "UpdateFontFromProperties", customSetProperty, "UpdateToFontValues");
        Install(typeof(global::GumRuntime.ElementSaveExtensions), "CustomCreateGraphicalComponentFunc", renderableCreator, "HandleCreateGraphicalComponent");

        // No project loaded: generated code then skips the standard-element lookups, which need a renderer.
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
        Type frameworkElementType = GumCommon.GetType("Gum.Forms.Controls.FrameworkElement", throwOnError: true)!;

        List<string> failures = new List<string>();
        int constructed = 0;
        foreach (Type type in generated.GetTypes().Where(item =>
            frameworkElementType.IsAssignableFrom(item) && !item.IsAbstract && item.GetConstructor(Type.EmptyTypes) != null))
        {
            try
            {
                object control = Activator.CreateInstance(type)!;
                constructed++;
                if (type.Name == "ButtonClose")
                {
                    // The Forms control applied its state once the children existed.
                    type.GetProperty("ButtonCategoryState")!.GetValue(control)!.ToString().ShouldBe("Enabled");
                }
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                string frames = string.Join(" <- ", (inner.StackTrace ?? "").Split('\n').Take(6).Select(line => line.Trim()));
                failures.Add($"{type.Name}: {inner.GetType().Name} {inner.Message} {frames}");
            }
        }

        failures.ShouldBeEmpty();
        constructed.ShouldBeGreaterThan(10);
    }

    private static Assembly GumCommon => Assembly.Load("GumCommon");

    private static Assembly MonoGameGum => Assembly.Load("MonoGameGum");

    private static Assembly Compile(IEnumerable<string> paths)
    {

        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        List<SyntaxTree> trees = paths
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToList();

        // Only the framework and the MonoGame runtime: RaylibGum and the Forms placeholders declare the same type names.
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        HashSet<string> referencePaths = new HashSet<string>(
            Directory.GetFiles(frameworkDirectory, "*.dll").Where(IsManagedAssembly),
            StringComparer.OrdinalIgnoreCase)
        {
            GumCommon.Location,
            MonoGameGum.Location,
            Assembly.Load("MonoGame.Framework").Location,
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "FullyInCodeGenerated_" + Guid.NewGuid().ToString("N"),
            trees,
            referencePaths.Where(File.Exists).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Annotations));

        using MemoryStream stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        result.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .Take(10)
            .ShouldBeEmpty();

        return Assembly.Load(stream.ToArray());
    }

    private static void Install(Type owner, string memberName, Type source, string methodName)
    {
        MethodInfo method = source.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        FieldInfo? field = owner.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            field.SetValue(null, Delegate.CreateDelegate(field.FieldType, method));
            return;
        }
        PropertyInfo property = owner.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static)!;
        property.SetValue(null, Delegate.CreateDelegate(property.PropertyType, method));
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
