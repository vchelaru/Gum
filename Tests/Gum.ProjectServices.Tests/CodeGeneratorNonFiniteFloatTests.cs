using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #7090: a non-finite float must be emitted as float.NaN / float.PositiveInfinity / float.NegativeInfinity,
/// not as the invalid literals "NaNf" / "Infinityf".
/// </summary>
public class CodeGeneratorNonFiniteFloatTests : BaseTestClass
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

    private string Generate(OutputLibrary library, float value)
    {
        StandardElementSave container = Project.StandardElements.Single(item => item.Name == "Container");
        container.DefaultState!.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 0f, SetsValue = true });

        ComponentSave host = new ComponentSave { Name = "Host", BaseType = "Container" };
        host.States.Add(new StateSave { Name = "Default", ParentContainer = host });
        host.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Container", ParentContainer = host });
        host.DefaultState!.Variables.Add(new VariableSave { Name = "Inner.Width", Type = "float", Value = value, SetsValue = true });
        Project.Components.Add(host);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = library,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
            RootNamespace = "MyGame",
        };
        return CreateCodeGenerator().GetGeneratedCodeForElement(host, new CodeOutputElementSettings(), settings);
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, float.NaN, "float.NaN")]
    [InlineData(OutputLibrary.MonoGame, float.PositiveInfinity, "float.PositiveInfinity")]
    [InlineData(OutputLibrary.MonoGame, float.NegativeInfinity, "float.NegativeInfinity")]
    [InlineData(OutputLibrary.GumForms, float.NaN, "float.NaN")]
    [InlineData(OutputLibrary.GumForms, float.PositiveInfinity, "float.PositiveInfinity")]
    [InlineData(OutputLibrary.GumForms, float.NegativeInfinity, "float.NegativeInfinity")]
    public void GetGeneratedCodeForElement_NonFiniteFloat_EmitsFloatConstants(OutputLibrary library, float value, string expected)
    {
        Generate(library, value).ShouldContain($"Width = {expected};", Case.Sensitive);
    }

    [Fact]
    public void GetGeneratedCodeForElement_FiniteFloat_KeepsFSuffix()
    {
        Generate(OutputLibrary.MonoGame, 1.5f).ShouldContain("Width = 1.5f;", Case.Sensitive);
    }
}
