using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeGenerationFileLocationsService"/>: where generated and custom code files land.
/// </summary>
public class CodeGenerationFileLocationsServiceTests : BaseTestClass
{
    private readonly string _projectDirectory = Path.Combine(Path.GetTempPath(), "GumFileLocations") + Path.DirectorySeparatorChar;

    [Fact]
    public void GetGeneratedFileName_ShouldUseCodeProjectRoot_WhenGeneratedCodeFolderIsEmpty()
    {
        ComponentSave component = CreateComponent("Buttons/IconButton");
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings { CodeProjectRoot = "Code/", OutputLibrary = OutputLibrary.MonoGameForms };

        FilePath? generated = CreateService().GetGeneratedFileName(
            component, new CodeOutputElementSettings(), projectSettings, VisualApi.Gum);

        generated.ShouldBe(new FilePath(Path.Combine(_projectDirectory, "Code", "Components", "Buttons", "IconButton.Generated.cs")));
    }

    [Theory]
    [InlineData("Gum/Generated")]
    [InlineData("Gum/Generated/")]
    [InlineData("Gum\\Generated\\")]
    public void GetGeneratedFileName_ShouldPlaceFilesUnderGeneratedCodeFolder_RelativeToCodeProjectRoot(string generatedCodeFolder)
    {
        ComponentSave component = CreateComponent("Buttons/IconButton");
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = "Code/",
            GeneratedCodeFolder = generatedCodeFolder,
            OutputLibrary = OutputLibrary.MonoGameForms
        };
        CodeGenerationFileLocationsService service = CreateService();

        FilePath? generated = service.GetGeneratedFileName(component, new CodeOutputElementSettings(), projectSettings, VisualApi.Gum);
        FilePath? custom = service.GetCustomCodeFileName(component, new CodeOutputElementSettings(), projectSettings, VisualApi.Gum);
        FilePath? fallback = service.GetStandardElementsFallbackFileName(projectSettings);

        string outputFolder = Path.Combine(_projectDirectory, "Code", "Gum", "Generated");
        generated.ShouldBe(new FilePath(Path.Combine(outputFolder, "Components", "Buttons", "IconButton.Generated.cs")));
        custom.ShouldBe(new FilePath(Path.Combine(outputFolder, "Components", "Buttons", "IconButton.cs")));
        fallback.ShouldBe(new FilePath(Path.Combine(outputFolder, "StandardElements.Generated.cs")));
    }

    [Fact]
    public void GetGeneratedFileName_ShouldReturnNull_WhenOnlyGeneratedCodeFolderIsSet()
    {
        // The folder is relative to the code project root, so without a root nothing is generated.
        ComponentSave component = CreateComponent("IconButton");
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings { GeneratedCodeFolder = "Generated" };
        CodeGenerationFileLocationsService service = CreateService();

        service.GetGeneratedFileName(component, new CodeOutputElementSettings(), projectSettings, VisualApi.Gum).ShouldBeNull();
        service.GetStandardElementsFallbackFileName(projectSettings).ShouldBeNull();
    }

    [Fact]
    public void Serialize_ShouldOmitGeneratedCodeFolder_WhenEmpty()
    {
        // Existing .codsj files must not change on resave when the setting is never used.
        string empty = JsonConvert.SerializeObject(new CodeOutputProjectSettings());
        string set = JsonConvert.SerializeObject(new CodeOutputProjectSettings { GeneratedCodeFolder = "Generated/" });

        empty.ShouldNotContain("GeneratedCodeFolder");
        JsonConvert.DeserializeObject<CodeOutputProjectSettings>(set)!.GeneratedCodeFolder.ShouldBe("Generated/");
    }

    private CodeGenerationFileLocationsService CreateService()
    {
        ObjectFinder.Self.GumProjectSave = Project;

        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier nameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(_projectDirectory);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        CodeGenerator codeGenerator = new CodeGenerator(
            nameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);

        return new CodeGenerationFileLocationsService(codeGenerator, nameVerifier, directoryProvider);
    }

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.ParentContainer = component;
        component.States.Add(defaultState);
        return component;
    }
}
