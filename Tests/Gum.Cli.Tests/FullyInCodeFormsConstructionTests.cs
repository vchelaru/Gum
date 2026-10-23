using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gum.Cli.Tests;

/// <summary>
/// Compiles the FullyInCode output of the Forms template (plus components derived from template
/// controls) and constructs the generated controls, so generated code that touches its children
/// before they exist, or builds them in the wrong order, fails here rather than in a game.
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
        Assembly generated = GenerateAndCompile();
        Type frameworkElementType = GumCommon.GetType("Gum.Forms.Controls.FrameworkElement", throwOnError: true)!;

        List<string> failures = new List<string>();
        int constructed = 0;
        foreach (Type type in generated.GetTypes().Where(item =>
            frameworkElementType.IsAssignableFrom(item) && !item.IsAbstract && item.GetConstructor(Type.EmptyTypes) != null))
        {
            try
            {
                Activator.CreateInstance(type);
                constructed++;
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

    [Fact]
    public void Constructor_GeneratedButton_AppliesStateWithChildrenParented()
    {
        object button = Construct(GenerateAndCompile(), "ButtonClose");

        Get(button, "ButtonCategoryState")!.ToString().ShouldBe("Enabled");
        Get(button, "Background.Parent").ShouldBeSameAs(Get(button, "Visual"));
    }

    [Theory]
    [InlineData("ButtonCloseDerived", "Background")]
    [InlineData("TextBoxDerived", "TextInstance")]
    [InlineData("WindowDerived", "InnerPanelInstance")]
    public void Constructor_DerivedComponent_BuildsEveryLevelUnderItsVisual(string typeName, string baseChildName)
    {
        object control = Construct(GenerateAndCompile(), typeName);
        object visual = Get(control, "Visual")!;

        Call(visual, "GetGraphicalUiElementByName", baseChildName).ShouldNotBeNull();
        Get(control, "Extra.Parent").ShouldBeSameAs(visual);
        // Defaults from both levels: the derived width wins over the base's, the derived child keeps its own.
        Get(visual, "Width").ShouldBe(150f);
        Get(control, "Extra.Width").ShouldBe(17f);
    }

    [Fact]
    public void Constructor_DerivedButton_AppliesStateAfterDerivedDefaults()
    {
        object button = Construct(GenerateAndCompile(), "ButtonCloseDerived");
        File.ReadAllText(Directory.GetFiles(_tempDirectory, "ButtonCloseDerived.Generated.cs", SearchOption.AllDirectories).Single())
            .ShouldContain("this.FocusedIndicator.Visible = true;");

        // The derived default shows FocusedIndicator; the Enabled state, applied after the defaults, hides it.
        Get(button, "FocusedIndicator.Visible").ShouldBe(false);
        Get(button, "FocusedIndicator.Parent").ShouldBeSameAs(Get(button, "Visual"));
    }

    [Fact]
    public void Constructor_ExistingVisual_BindsToItsChildrenWithoutDuplicates()
    {
        Type type = GenerateAndCompile().GetTypes().Single(item => item.Name == "ButtonClose");
        object first = Activator.CreateInstance(type)!;
        object visual = Get(first, "Visual")!;
        type.GetProperty("Visual")!.SetValue(first, null);

        object second = Activator.CreateInstance(type, visual)!;

        Get(second, "Background").ShouldBeSameAs(Call(visual, "GetGraphicalUiElementByName", "Background"));
        Get(second, "Icon.Visual").ShouldBeSameAs(Call(visual, "GetGraphicalUiElementByName", "Icon"));
        ((ICollection)Get(visual, "Children")!).Count.ShouldBe(3);
    }

    private static object Construct(Assembly generated, string typeName) =>
        Activator.CreateInstance(generated.GetTypes().Single(item => item.Name == typeName))!;

    // Generated classes are internal to the compiled assembly, so members are read by reflection.
    private static object? Get(object? target, string path)
    {
        foreach (string name in path.Split('.'))
        {
            target = target!.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(target);
        }
        return target;
    }

    private static object? Call(object target, string methodName, string argument) =>
        target.GetType().GetMethod(methodName, new[] { typeof(string) })!.Invoke(target, new object[] { argument });

    private Assembly GenerateAndCompile()
    {
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        new FormsTemplateCreator().Create(gumxPath);
        AddDerivedComponents(gumxPath);
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

        CliTestHelper codegen = CliTestHelper.Run("codegen", gumxPath);
        codegen.ExitCode.ShouldBe(0, codegen.StandardError + codegen.StandardOutput);

        Assembly generated = Compile(Directory.GetFiles(_tempDirectory, "*.cs", SearchOption.AllDirectories));
        InstallHeadlessRuntime();
        return generated;
    }

    // Each derived component adds a child and overrides a default at both levels.
    private static void AddDerivedComponents(string gumxPath)
    {
        GumProjectSave project = GumProjectSave.Load(gumxPath, out _)!;
        (string BaseName, string DerivedName)[] pairs =
        {
            ("Controls/ButtonClose", "Controls/ButtonCloseDerived"),
            ("Controls/TextBox", "Controls/TextBoxDerived"),
            ("Controls/Window", "Controls/WindowDerived"),
        };
        foreach ((string baseName, string derivedName) in pairs)
        {
            ComponentSave component = new ComponentSave { Name = derivedName, BaseType = baseName };
            StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
            component.States.Add(defaultState);
            component.Instances.Add(new InstanceSave { Name = "Extra", BaseType = "Container", ParentContainer = component });
            defaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 150f, SetsValue = true });
            defaultState.Variables.Add(new VariableSave { Name = "Extra.Width", Type = "float", Value = 17f, SetsValue = true });
            if (baseName == "Controls/ButtonClose")
            {
                // A base child the derived level overrides is stored as a DefinedByBase instance, as the tool saves it.
                component.Instances.Add(new InstanceSave { Name = "FocusedIndicator", BaseType = "NineSlice", DefinedByBase = true, ParentContainer = component });
                defaultState.Variables.Add(new VariableSave { Name = "FocusedIndicator.Visible", Type = "bool", Value = true, SetsValue = true });
            }
            project.Components.Add(component);
            project.ComponentReferences.Add(new ElementReference { Name = derivedName, ElementType = ElementType.Component });
        }
        project.Save(gumxPath, saveElements: true);
    }

    private static void InstallHeadlessRuntime()
    {
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
