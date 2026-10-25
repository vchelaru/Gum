using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Generated category state properties must build without compiler warnings: one that replaces a
/// same-named member of the Forms base class is marked <c>new</c> (CS0108, #6784), and a category
/// without states emits no switch (CS1522, #6785).
/// </summary>
public class CodeGeneratorStatePropertyTests : BaseTestClass
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

        Mock<ISyntaxVersionDetectionService> detection = new Mock<ISyntaxVersionDetectionService>();
        detection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(new SyntaxVersionResult { Version = 4 });

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            syntaxVersionDetectionService: detection.Object);
    }

    private string Generate(
        string behaviorName,
        string categoryName,
        string[] stateNames,
        ObjectInstantiationType instantiationType)
    {
        ComponentSave component = new ComponentSave { Name = "Controls/Thing", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = component });
        }
        component.Categories.Add(category);
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            ObjectInstantiationType = instantiationType,
            RootNamespace = "MyGame",
        };

        return CreateCodeGenerator().GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    [InlineData(ObjectInstantiationType.FindByName)]
    public void GetGeneratedCodeForElement_StatePropertyHidingFormsConstant_IsMarkedNew(ObjectInstantiationType instantiationType)
    {
        string code = Generate("MenuItemBehavior", "MenuItemCategory", new[] { "Enabled" }, instantiationType);

        code.ShouldContain("public new MenuItemCategory? MenuItemCategoryState");
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    [InlineData(ObjectInstantiationType.FindByName)]
    public void GetGeneratedCodeForElement_StatePropertyNotHidingAnything_IsNotMarkedNew(ObjectInstantiationType instantiationType)
    {
        string code = Generate("ButtonBehavior", "OtherCategory", new[] { "Enabled" }, instantiationType);

        code.ShouldContain("public OtherCategory? OtherCategoryState");
        code.ShouldNotContain("public new OtherCategory?");
    }

    [Fact]
    public void GetGeneratedCodeForElement_CategoryWithStates_EmitsSwitch()
    {
        string code = Generate("ButtonBehavior", "OtherCategory", new[] { "Enabled", "Disabled" }, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("switch (value)");
        code.ShouldContain("case OtherCategory.Enabled:");
        code.ShouldContain("case OtherCategory.Disabled:");
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    [InlineData(ObjectInstantiationType.FindByName)]
    public void GetGeneratedCodeForElement_CategoryWithoutStates_EmitsNoSwitch(ObjectInstantiationType instantiationType)
    {
        string code = Generate("ButtonBehavior", "EmptyCategory", System.Array.Empty<string>(), instantiationType);

        code.ShouldContain("public EmptyCategory? EmptyCategoryState");
        code.ShouldNotContain("switch (value)");
        code.ShouldNotContain("appliedDynamically");
    }
}
