using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Generated code must run in Unity, which compiles with C# 9 and never calls module
/// initializers (#5503). Startup registration uses Unity's hook behind UNITY_5_3_OR_NEWER, and
/// projects below C# 10 get block namespaces.
/// </summary>
public class CodeGeneratorUnityCompatibilityTests : BaseTestClass
{
    private const string StartupHookPrefix =
        "#if UNITY_5_3_OR_NEWER";
    private const string UnityAttribute =
        "[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]";

    private static CodeGenerator CreateCodeGenerator(int? languageVersion)
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        Mock<ISyntaxVersionDetectionService> syntaxDetection = new Mock<ISyntaxVersionDetectionService>();
        syntaxDetection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(new SyntaxVersionResult { Version = 4 });

        Mock<ICSharpVersionDetectionService> languageDetection = new Mock<ICSharpVersionDetectionService>();
        languageDetection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(languageVersion);

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            syntaxVersionDetectionService: syntaxDetection.Object,
            cSharpVersionDetectionService: languageDetection.Object);
    }

    private static CodeOutputProjectSettings CreateSettings(OutputLibrary outputLibrary) => new CodeOutputProjectSettings
    {
        OutputLibrary = outputLibrary,
        RootNamespace = "MyGame",
        AppendFolderToNamespace = true,
    };

    private string GenerateHost(int? languageVersion, OutputLibrary outputLibrary)
    {
        ComponentSave host = new ComponentSave { Name = "Host", BaseType = "Container" };
        host.States.Add(new StateSave { Name = "Default", ParentContainer = host });
        host.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = host });
        Project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = Project;

        return CreateCodeGenerator(languageVersion).GetGeneratedCodeForElement(
            host, new CodeOutputElementSettings(), CreateSettings(outputLibrary));
    }

    private static void ShouldUseUnityHookOrModuleInitializer(string code, string moduleInitializerAttribute)
    {
        string[] lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .ToArray();
        int start = Array.IndexOf(lines, StartupHookPrefix);
        start.ShouldBeGreaterThanOrEqualTo(0, code);
        lines[start + 1].ShouldBe(UnityAttribute);
        lines[start + 2].ShouldBe("#else");
        lines[start + 3].ShouldBe(moduleInitializerAttribute);
        lines[start + 4].ShouldBe("#endif");
        lines.Count(line => line.Contains("ModuleInitializer]")).ShouldBe(1);
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.MonoGameForms)]
    public void GetGeneratedCodeForElement_RegistersWithUnityHookOrModuleInitializer(OutputLibrary outputLibrary)
    {
        string code = GenerateHost(languageVersion: null, outputLibrary);

        ShouldUseUnityHookOrModuleInitializer(code, "[System.Runtime.CompilerServices.ModuleInitializer]");
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_RegistersWithUnityHookOrModuleInitializer()
    {
        string? code = CreateCodeGenerator(languageVersion: null)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(OutputLibrary.MonoGame));

        code.ShouldNotBeNull();
        ShouldUseUnityHookOrModuleInitializer(code, "[ModuleInitializer]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(10)]
    [InlineData(14)]
    public void GetGeneratedCodeForElement_CSharp10OrUnknown_UsesFileScopedNamespace(int? languageVersion)
    {
        string code = GenerateHost(languageVersion, OutputLibrary.MonoGame);

        code.ShouldContain("namespace MyGame.Components;" + Environment.NewLine);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    public void GetGeneratedCodeForElement_BelowCSharp10_UsesBlockNamespace(int languageVersion)
    {
        string code = GenerateHost(languageVersion, OutputLibrary.MonoGame);

        code.ShouldNotContain("namespace MyGame.Components;");
        code.ShouldContain("namespace MyGame.Components" + Environment.NewLine + "{" + Environment.NewLine + "    partial class HostRuntime");
        code.TrimEnd().ShouldEndWith("    }" + Environment.NewLine + "}");
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_BelowCSharp10_UsesBlockNamespace()
    {
        string? code = CreateCodeGenerator(languageVersion: 9)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(OutputLibrary.MonoGame));

        code.ShouldNotBeNull();
        code.ShouldNotContain("namespace MyGame;");
        code.ShouldContain("namespace MyGame" + Environment.NewLine + "{" + Environment.NewLine + "    internal static class StandardElementsCodeGenRegistration");
        code.TrimEnd().ShouldEndWith("    }" + Environment.NewLine + "}");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.MonoGameForms)]
    public void GeneratedCode_AtCSharp9_UsesNoNewerLanguageFeatures(OutputLibrary outputLibrary)
    {
        string elementCode = GenerateHost(languageVersion: 9, outputLibrary);
        string? fallbackCode = CreateCodeGenerator(languageVersion: 9)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(outputLibrary));
        fallbackCode.ShouldNotBeNull();

        // Unity defines UNITY_5_3_OR_NEWER, so compile that branch too. The Gum runtime and
        // UnityEngine aren't referenced, so only language-version diagnostics are checked.
        foreach (string[] symbols in new[] { Array.Empty<string>(), new[] { "UNITY_5_3_OR_NEWER" } })
        {
            CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols);
            List<SyntaxTree> trees = new List<SyntaxTree>
            {
                CSharpSyntaxTree.ParseText(elementCode, parseOptions),
                CSharpSyntaxTree.ParseText(fallbackCode, parseOptions),
            };
            CSharpCompilation compilation = CSharpCompilation.Create(
                "GeneratedAtCSharp9",
                trees,
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            List<Diagnostic> languageVersionErrors = compilation.GetDiagnostics()
                .Where(d => d.GetMessage().Contains("language version", StringComparison.OrdinalIgnoreCase))
                .ToList();

            languageVersionErrors.ShouldBeEmpty(string.Join(Environment.NewLine, languageVersionErrors));
        }
    }
}
