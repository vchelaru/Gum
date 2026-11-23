using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #8410: <c>X.IgnoredByParentLayout = ...</c> only compiles against runtimes that have the property
/// (syntax version 8 and up), so codegen omits it below that.
/// </summary>
public class CodeGeneratorIgnoredByParentLayoutTests : BaseTestClass
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

    private string Generate(int syntaxVersion)
    {
        StandardElementSave container = Project.StandardElements.First(item => item.Name == "Container");
        if (!container.DefaultState!.Variables.Any(v => v.Name == "IgnoredByParentLayout"))
        {
            container.DefaultState!.Variables.Add(new VariableSave { Name = "IgnoredByParentLayout", Type = "bool", Value = false, SetsValue = true });
        }
        ComponentSave component = new ComponentSave { Name = "Unit", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Outline", BaseType = "Container", ParentContainer = component });
        component.DefaultState!.Variables.Add(new VariableSave { Name = "Outline.IgnoredByParentLayout", Type = "bool", Value = true, SetsValue = true });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
            SyntaxVersion = syntaxVersion.ToString(),
        };
        return CreateCodeGenerator().GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    [Fact]
    public void GetGeneratedCodeForElement_InstanceIgnoredByParentLayout_IsEmittedAtSyntaxVersion8()
    {
        Generate(syntaxVersion: 8).ShouldContain("Outline.IgnoredByParentLayout = true;");
    }

    [Fact]
    public void GetGeneratedCodeForElement_InstanceIgnoredByParentLayout_IsOmittedBelowSyntaxVersion8()
    {
        Generate(syntaxVersion: 7).ShouldNotContain("IgnoredByParentLayout");
    }
}
