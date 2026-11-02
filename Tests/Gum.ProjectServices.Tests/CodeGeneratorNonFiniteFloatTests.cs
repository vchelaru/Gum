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

    private string GeneratePolygon(OutputLibrary library, float x)
    {
        StandardElementSave polygon = new StandardElementSave { Name = "Polygon" };
        polygon.States.Add(new StateSave { Name = "Default", ParentContainer = polygon });
        Project.StandardElements.Add(polygon);

        ComponentSave host = new ComponentSave { Name = "Host", BaseType = "Container" };
        host.States.Add(new StateSave { Name = "Default", ParentContainer = host });
        host.Instances.Add(new InstanceSave { Name = "Shape", BaseType = "Polygon", ParentContainer = host });
        host.DefaultState!.VariableLists.Add(new VariableListSave<System.Numerics.Vector2>
        {
            Name = "Shape.Points",
            Type = "Vector2",
            Value = new List<System.Numerics.Vector2>
            {
                new System.Numerics.Vector2(x, 1f),
                new System.Numerics.Vector2(1.5f, 2f),
            }
        });
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
    [InlineData(OutputLibrary.Skia, float.NaN, "float.NaN")]
    [InlineData(OutputLibrary.Skia, float.PositiveInfinity, "float.PositiveInfinity")]
    [InlineData(OutputLibrary.Skia, float.NegativeInfinity, "float.NegativeInfinity")]
    public void GetGeneratedCodeForElement_NonFinitePolygonPoint_EmitsFloatConstants(OutputLibrary library, float x, string expected)
    {
        string code = GeneratePolygon(library, x);

        code.ShouldContain($"new System.Numerics.Vector2({expected}, 1f),", Case.Sensitive);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void GetGeneratedCodeForElement_FinitePolygonPoint_IsCultureInvariant(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            string code = GeneratePolygon(OutputLibrary.MonoGame, 0.5f);

            code.ShouldContain("new System.Numerics.Vector2(0.5f, 1f),", Case.Sensitive);
            code.ShouldContain("new System.Numerics.Vector2(1.5f, 2f),", Case.Sensitive);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
