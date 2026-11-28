using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The custom code stub uses the same namespace form as the generated half of its class: file-scoped
/// from C# 10 (or when the language version is unknown), block-scoped below it (#8512).
/// </summary>
public class CustomCodeGeneratorNamespaceFormTests : BaseTestClass
{
    [Theory]
    [InlineData(10)]
    [InlineData(null)]
    public void GetCustomCodeForElement_CSharp10OrUnknown_UsesAFileScopedNamespaceLikeTheGeneratedFile(int? languageVersion)
    {
        string expected =
            "using Gum;\n" +
            "using Gum.Forms.Controls;\n" +
            "\n" +
            "namespace MyGame.Components;\n" +
            "\n" +
            "partial class Host\n" +
            "{\n" +
            "    GumService GumUI => GumService.Default;\n" +
            "\n" +
            "    partial void CustomInitialize()\n" +
            "    {\n" +
            "    \n" +
            "    }\n" +
            "}\n";

        (string stub, string generated) = Generate(languageVersion, rootNamespace: "MyGame");

        stub.ShouldBe(expected);
        generated.ShouldContain("namespace MyGame.Components;");
    }

    [Fact]
    public void GetCustomCodeForElement_BelowCSharp10_UsesABlockNamespaceLikeTheGeneratedFile()
    {
        string expected =
            "using Gum;\n" +
            "using Gum.Forms.Controls;\n" +
            "\n" +
            "namespace MyGame.Components\n" +
            "{\n" +
            "    partial class Host\n" +
            "    {\n" +
            "        GumService GumUI => GumService.Default;\n" +
            "\n" +
            "        partial void CustomInitialize()\n" +
            "        {\n" +
            "        \n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        (string stub, string generated) = Generate(languageVersion: 9, rootNamespace: "MyGame");

        stub.ShouldBe(expected);
        generated.ShouldContain("namespace MyGame.Components\n{");
    }

    [Fact]
    public void GetCustomCodeForElement_WithNoRootNamespace_DeclaresNoNamespace()
    {
        (string stub, _) = Generate(languageVersion: 10, rootNamespace: string.Empty);

        stub.ShouldNotContain("namespace");
        stub.ShouldContain("\npartial class Host\n{\n");
    }

    private (string Stub, string Generated) Generate(int? languageVersion, string rootNamespace)
    {
        ComponentSave component = new ComponentSave { Name = "Host", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            ObjectInstantiationType = ObjectInstantiationType.FindByName,
            RootNamespace = rootNamespace,
            CommonUsingStatements = string.Empty,
            SyntaxVersion = "3",
        };

        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        Mock<ICSharpVersionDetectionService> languageDetection = new Mock<ICSharpVersionDetectionService>();
        languageDetection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(languageVersion);

        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeGenerator generator = new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            cSharpVersionDetectionService: languageDetection.Object);
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(generator, codeGenNameVerifier);

        string stub = customCodeGenerator.GetCustomCodeForElement(component, new CodeOutputElementSettings(), settings);
        string generated = generator.GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
        return (stub.Replace("\r\n", "\n"), generated.Replace("\r\n", "\n"));
    }
}
