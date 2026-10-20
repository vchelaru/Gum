using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #6248: the user's <c>CommonUsingStatements</c> / element <c>UsingStatements</c> are copied
/// into generated files, so a <c>using MonoGameGum.GueDeriving;</c> saved in a .codsj brings the
/// old namespace back. At the syntax versions where generated code itself uses the neutral
/// <c>Gum.*</c> names, codegen translates those legacy usings. Older versions keep them.
/// </summary>
public class CodeGeneratorLegacyUsingStatementsTests : BaseTestClass
{
    private const string LegacyUsings =
        "using Gum.Converters;\r\nusing MonoGameGum.GueDeriving;\r\nusing MonoGameGum;\r\nusing System.Linq;\r\n";

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private IReadOnlyList<string> Collect(
        string commonUsings,
        int syntaxVersion,
        OutputLibrary outputLibrary = OutputLibrary.GumForms,
        string? elementUsings = null)
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            RootNamespace = "MyGame",
            CommonUsingStatements = commonUsings,
        };

        CodeOutputElementSettings? elementSettings = elementUsings == null
            ? null
            : new CodeOutputElementSettings { UsingStatements = elementUsings };

        return CreateCodeGenerator().CollectUsingNamespaces(
            component,
            elementSettings,
            settings,
            syntaxVersion);
    }

    [Fact]
    public void CollectUsingNamespaces_Version3_TranslatesLegacyMonoGameGumUsings()
    {
        IReadOnlyList<string> usings = Collect(LegacyUsings, syntaxVersion: 3);

        usings.ShouldNotContain("MonoGameGum");
        usings.ShouldNotContain("MonoGameGum.GueDeriving");
        usings.Count(item => item == "Gum").ShouldBe(1);
        usings.Count(item => item == "Gum.GueDeriving").ShouldBe(1);
        usings.ShouldContain("System.Linq");
        usings.ShouldContain("Gum.Converters");
    }

    [Fact]
    public void CollectUsingNamespaces_Version2_TranslatesGueDerivingButKeepsMonoGameGum()
    {
        // Below 3 generated code still imports MonoGameGum (the AddChild crutch); from 1 on it
        // uses Gum.GueDeriving.
        IReadOnlyList<string> usings = Collect(LegacyUsings, syntaxVersion: 2);

        usings.ShouldContain("MonoGameGum");
        usings.ShouldNotContain("MonoGameGum.GueDeriving");
        usings.ShouldContain("Gum.GueDeriving");
    }

    [Fact]
    public void CollectUsingNamespaces_Version0_KeepsLegacyUsings()
    {
        IReadOnlyList<string> usings = Collect(LegacyUsings, syntaxVersion: 0);

        usings.ShouldContain("MonoGameGum");
        usings.ShouldContain("MonoGameGum.GueDeriving");
        usings.ShouldNotContain("Gum.GueDeriving");
    }

    [Fact]
    public void CollectUsingNamespaces_Version1_TranslatesSkiaGueDeriving()
    {
        IReadOnlyList<string> usings = Collect(
            "using SkiaGum.GueDeriving;\r\n",
            syntaxVersion: 1,
            outputLibrary: OutputLibrary.Skia);

        usings.ShouldNotContain("SkiaGum.GueDeriving");
        usings.Count(item => item == "Gum.GueDeriving").ShouldBe(1);
    }

    [Fact]
    public void CollectUsingNamespaces_Version3_DoesNotTouchLookalikeNamespaces()
    {
        IReadOnlyList<string> usings = Collect(
            "using MonoGameGumCodeGeneration.Components;\r\nusing MonoGameGum.Forms.Controls;\r\n",
            syntaxVersion: 3);

        usings.ShouldContain("MonoGameGumCodeGeneration.Components");
        usings.ShouldContain("MonoGameGum.Forms.Controls");
    }

    [Fact]
    public void CollectUsingNamespaces_Version3_TranslatesElementLevelLegacyUsings()
    {
        IReadOnlyList<string> usings = Collect(
            "using System.Linq;",
            syntaxVersion: 3,
            elementUsings: "using MonoGameGum.GueDeriving;");

        usings.ShouldNotContain("MonoGameGum.GueDeriving");
        usings.ShouldContain("Gum.GueDeriving");
    }

    [Fact]
    public void CollectUsingNamespaces_Version3_AcceptsUsingsWithoutKeywordOrSemicolon()
    {
        IReadOnlyList<string> usings = Collect("MonoGameGum.GueDeriving\nMonoGameGum", syntaxVersion: 3);

        usings.ShouldNotContain("MonoGameGum");
        usings.ShouldNotContain("MonoGameGum.GueDeriving");
    }

    [Fact]
    public void CollectUsingNamespaces_Version3_DoesNotModifySettings()
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host" };
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            CommonUsingStatements = LegacyUsings,
        };

        CreateCodeGenerator().CollectUsingNamespaces(component, elementSettings: null, settings, resolvedSyntaxVersion: 3);

        settings.CommonUsingStatements.ShouldBe(LegacyUsings);
    }

    [Fact]
    public void GetCustomCodeForElement_Version3_TranslatesLegacyUsingsInStub()
    {
        string stub = GetStub(syntaxVersion: "3");

        stub.ShouldNotContain("MonoGameGum");
        stub.ShouldContain("using Gum;");
        stub.ShouldContain("using Gum.GueDeriving;");
        stub.ShouldContain("using System.Linq;");
    }

    [Fact]
    public void GetCustomCodeForElement_Version0_KeepsLegacyUsingsInStub()
    {
        string stub = GetStub(syntaxVersion: "0");

        stub.ShouldContain("using MonoGameGum.GueDeriving;");
        stub.ShouldContain("using MonoGameGum;");
    }

    private string GetStub(string syntaxVersion)
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            RootNamespace = "MyGame",
            CommonUsingStatements = LegacyUsings,
            SyntaxVersion = syntaxVersion,
        };

        CodeGenerator generator = CreateCodeGenerator();
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(
            generator,
            new CodeGenerationNameVerifier(mockNameVerifier.Object));

        return customCodeGenerator.GetCustomCodeForElement(component, elementSettings: null!, settings);
    }
}
