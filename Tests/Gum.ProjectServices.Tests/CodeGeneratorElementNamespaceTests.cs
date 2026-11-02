using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The namespace segments codegen derives from an element's folder must be C# identifiers whatever
/// the folder is called.
/// </summary>
public class CodeGeneratorElementNamespaceTests
{
    private static CodeGenerator CreateCodeGenerator()
    {
        CodeGenerationNameVerifier verifier = new CodeGenerationNameVerifier(new Mock<INameVerifier>().Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        return new CodeGenerator(
            verifier,
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }

    [Theory]
    [InlineData("Menus/Main", "MyGame.Screens.Menus")]
    [InlineData("2DLevels/Level1", "MyGame.Screens._2DLevels")]
    [InlineData("Level Select/Level1", "MyGame.Screens.Level_Select")]
    [InlineData("Menus/event/Main", "MyGame.Screens.Menus.@event")]
    [InlineData("World-1/Main", "MyGame.Screens.World_1")]
    public void GetElementNamespace_WithAFolderInTheElementName_AppendsOnlyValidSegments(
        string elementName, string expectedNamespace)
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            RootNamespace = "MyGame",
            AppendFolderToNamespace = true,
        };

        string result = CreateCodeGenerator().GetElementNamespace(elementName, typeof(ScreenSave), null, settings);

        result.ShouldBe(expectedNamespace);
    }
}
