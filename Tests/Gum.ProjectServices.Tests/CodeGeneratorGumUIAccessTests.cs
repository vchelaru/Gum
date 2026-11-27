using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Docs reach Gum through a <c>GumUI</c> property and call <c>AddToRoot</c> (an extension in
/// Gum.Forms.Controls), so code pasted into a generated element's custom file must resolve both.
/// The project-wide fallback file imports them, which also covers custom files written before this.
/// </summary>
public class CodeGeneratorGumUIAccessTests : BaseTestClass
{
    private const string GumUIImport = "global using static global::MyGame.GumUIAccess;";
    private const string FormsExtensionsImport = "global using static global::Gum.Forms.Controls.FrameworkElementExt;";
    private const string GumUIProperty = "internal static global::Gum.GumService GumUI => global::Gum.GumService.Default;";

    private static CodeGenerationNameVerifier CreateNameVerifier()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        return new CodeGenerationNameVerifier(mockNameVerifier.Object);
    }

    private static CodeGenerator CreateCodeGenerator(int syntaxVersion, int? cSharpVersion = null)
    {
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        Mock<ISyntaxVersionDetectionService> syntaxDetection = new Mock<ISyntaxVersionDetectionService>();
        syntaxDetection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(new SyntaxVersionResult { Version = syntaxVersion });

        Mock<ICSharpVersionDetectionService> cSharpDetection = new Mock<ICSharpVersionDetectionService>();
        cSharpDetection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(cSharpVersion);

        return new CodeGenerator(
            CreateNameVerifier(),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            syntaxVersionDetectionService: syntaxDetection.Object,
            cSharpVersionDetectionService: cSharpDetection.Object);
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = Project;
        return screen;
    }

    private static CodeOutputProjectSettings CreateSettings(OutputLibrary outputLibrary) => new CodeOutputProjectSettings
    {
        OutputLibrary = outputLibrary,
        ObjectInstantiationType = ObjectInstantiationType.FindByName,
        RootNamespace = "MyGame",
    };

    [Fact]
    public void GenerateStandardElementsFallbackCode_GumForms_ImportsGumUIAndFormsExtensions()
    {
        string code = CreateCodeGenerator(syntaxVersion: 3)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(OutputLibrary.GumForms))!;

        code.ShouldContain(GumUIImport);
        code.ShouldContain(FormsExtensionsImport);
        code.ShouldContain(GumUIProperty);
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    public void GenerateStandardElementsFallbackCode_LegacyUnifiedRuntime_ImportsOnlyGumUI(OutputLibrary outputLibrary)
    {
        // Legacy output derives from GraphicalUiElement, whose AddToRoot is an instance method.
        string code = CreateCodeGenerator(syntaxVersion: 3)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(outputLibrary))!;

        code.ShouldContain(GumUIImport);
        code.ShouldNotContain(FormsExtensionsImport);
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_BelowSyntaxVersion3_DoesNotImportGumUI()
    {
        // Gum.GumService does not exist before syntax version 3.
        string code = CreateCodeGenerator(syntaxVersion: 2)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(OutputLibrary.GumForms))!;

        code.ShouldNotContain("GumUI");
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_CSharp9_DoesNotImportGumUI()
    {
        // global using needs C# 10 (Unity is on 9).
        string code = CreateCodeGenerator(syntaxVersion: 3, cSharpVersion: 9)
            .GenerateStandardElementsFallbackCode(Project, CreateSettings(OutputLibrary.GumForms))!;

        code.ShouldNotContain("global using");
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_NoRootNamespace_ImportsFromTheGlobalNamespace()
    {
        CodeOutputProjectSettings settings = CreateSettings(OutputLibrary.GumForms);
        settings.RootNamespace = "";

        string code = CreateCodeGenerator(syntaxVersion: 3)
            .GenerateStandardElementsFallbackCode(Project, settings)!;

        code.ShouldContain("global using static global::GumUIAccess;");
    }

    [Fact]
    public void GetCustomCodeForElement_GumForms_ImportsFormsControls()
    {
        ScreenSave screen = AddScreen("TitleScreen");
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(CreateCodeGenerator(syntaxVersion: 3), CreateNameVerifier());

        string code = customCodeGenerator.GetCustomCodeForElement(
            screen, new CodeOutputElementSettings(), CreateSettings(OutputLibrary.GumForms));

        code.ShouldContain("using Gum.Forms.Controls;");
    }

    [Fact]
    public void GetCustomCodeForElement_FormsControlsAlreadyInCommonUsings_ImportsItOnce()
    {
        ScreenSave screen = AddScreen("TitleScreen");
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(CreateCodeGenerator(syntaxVersion: 3), CreateNameVerifier());
        CodeOutputProjectSettings settings = CreateSettings(OutputLibrary.GumForms);
        settings.CommonUsingStatements = "using Gum.Forms.Controls;\r\nusing System.Linq;\r\n";

        string code = customCodeGenerator.GetCustomCodeForElement(screen, new CodeOutputElementSettings(), settings);

        code.Split("using Gum.Forms.Controls;").Length.ShouldBe(2);
    }

    [Fact]
    public void GetCustomCodeForElement_LegacyMonoGame_DoesNotImportFormsControls()
    {
        ScreenSave screen = AddScreen("TitleScreen");
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(CreateCodeGenerator(syntaxVersion: 3), CreateNameVerifier());

        string code = customCodeGenerator.GetCustomCodeForElement(
            screen, new CodeOutputElementSettings(), CreateSettings(OutputLibrary.MonoGame));

        code.ShouldNotContain("Gum.Forms.Controls");
    }
}
