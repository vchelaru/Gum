using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;
using RenderBlend = Gum.RenderingLibrary.Blend;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #8263: <c>Blend.Subtract</c> only exists on runtimes at syntax version 8 or later, so below that
/// codegen writes a comment in place of the assignment. Other Blend values are unaffected.
/// </summary>
public class CodeGeneratorBlendSubtractTests : BaseTestClass
{
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

        return new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }

    private void DeclareBlendOnSprite()
    {
        StandardElementSave sprite = Project.StandardElements.First(item => item.Name == "Sprite");
        if (sprite.DefaultState.Variables.All(item => item.Name != "Blend"))
        {
            sprite.DefaultState.Variables.Add(new VariableSave
            {
                Name = "Blend", Type = "Blend", Value = RenderBlend.Normal, SetsValue = true,
            });
        }
    }

    private string Generate(RenderBlend blend, int syntaxVersion, bool inCategoryState = false)
    {
        DeclareBlendOnSprite();
        ComponentSave main = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        main.States.Add(new StateSave { Name = "Default", ParentContainer = main });
        main.Instances.Add(new InstanceSave { Name = "Instance", BaseType = "Sprite", ParentContainer = main });
        VariableSave variable = new VariableSave { Name = "Instance.Blend", Type = "Blend", Value = blend, SetsValue = true };
        if (inCategoryState)
        {
            StateSaveCategory category = new StateSaveCategory { Name = "Modes" };
            StateSave state = new StateSave { Name = "Dim", ParentContainer = main };
            state.Variables.Add(variable);
            category.States.Add(state);
            main.Categories.Add(category);
        }
        else
        {
            main.DefaultState.Variables.Add(variable);
        }
        Project.Components.Add(main);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
            SyntaxVersion = syntaxVersion.ToString(),
        };

        return CreateCodeGenerator().GetGeneratedCodeForElement(main, new CodeOutputElementSettings(), settings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetGeneratedCodeForElement_Subtract_IsCommentedOutBelowSyntaxVersion8(bool inCategoryState)
    {
        string code = Generate(RenderBlend.Subtract, syntaxVersion: 7, inCategoryState);

        code.ShouldNotContain("Blend.Subtract;");
        code.ShouldContain("// Blend.Subtract needs runtime syntax version 8");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetGeneratedCodeForElement_Subtract_IsEmittedAtSyntaxVersion8(bool inCategoryState)
    {
        string code = Generate(RenderBlend.Subtract, syntaxVersion: 8, inCategoryState);

        code.ShouldContain("Instance.Blend = global::Gum.RenderingLibrary.Blend.Subtract;");
        code.ShouldNotContain("needs runtime syntax version 8");
    }

    [Fact]
    public void GetGeneratedCodeForElement_OtherBlendValues_AreEmittedBelowSyntaxVersion8()
    {
        string code = Generate(RenderBlend.Additive, syntaxVersion: 7);

        code.ShouldContain("Instance.Blend = global::Gum.RenderingLibrary.Blend.Additive;");
    }
}
