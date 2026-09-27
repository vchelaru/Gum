using System.Globalization;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The same project must generate the same code on every machine, whatever its culture.
/// </summary>
public class CodeGeneratorCultureTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }

    private static string GenerateUnder(string cultureName, ElementSave element, CodeOutputProjectSettings settings)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            return CreateCodeGenerator().GetGeneratedCodeForElement(element, new CodeOutputElementSettings(), settings);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    // cs-CZ sorts "ch" after "h" (using order), tr-TR lowercases "I" to a dotless "ı" (state
    // field names), and sv-SE writes negative numbers with U+2212 instead of "-" (values).
    [Theory]
    [InlineData("cs-CZ")]
    [InlineData("sv-SE")]
    [InlineData("tr-TR")]
    public void GetGeneratedCodeForElement_MatchesEnUsOutput(string cultureName)
    {
        StandardElementSave text = Project.StandardElements.Single(item => item.Name == "Text");
        text.DefaultState!.Variables.Add(new VariableSave { Name = "MaxLettersToShow", Type = "int", Value = 0, SetsValue = true });

        ComponentSave hero = new ComponentSave { Name = "Characters/Hero", BaseType = "Container" };
        ComponentSave healthBar = new ComponentSave { Name = "Hud/HealthBar", BaseType = "Container" };
        ComponentSave host = new ComponentSave { Name = "Host", BaseType = "Container" };
        foreach (ComponentSave component in new[] { hero, healthBar, host })
        {
            component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        }
        host.Instances.Add(new InstanceSave { Name = "HeroInstance", BaseType = "Characters/Hero", ParentContainer = host });
        host.Instances.Add(new InstanceSave { Name = "HealthBarInstance", BaseType = "Hud/HealthBar", ParentContainer = host });
        host.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = host });
        host.DefaultState!.Variables.Add(new VariableSave { Name = "Label.MaxLettersToShow", Type = "int", Value = -3, SetsValue = true });
        StateSaveCategory category = new StateSaveCategory { Name = "Items" };
        category.States.Add(new StateSave { Name = "Open", ParentContainer = host });
        host.Categories.Add(category);

        Project.Components.Add(hero);
        Project.Components.Add(healthBar);
        Project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
            AppendFolderToNamespace = true,
        };

        string expected = GenerateUnder("en-US", host, settings);
        string actual = GenerateUnder(cultureName, host, settings);

        // Guard that the en-US baseline exercises all three culture-sensitive spots.
        expected.ShouldContain($"using MyGame.Components.Characters;{Environment.NewLine}using MyGame.Components.Hud;");
        expected.ShouldContain("_itemsState");
        expected.ShouldContain("MaxLettersToShow = -3;");
        actual.ShouldBe(expected);
    }
}
