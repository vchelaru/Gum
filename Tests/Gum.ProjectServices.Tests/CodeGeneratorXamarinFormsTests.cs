using System.Text.RegularExpressions;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CodeGeneratorXamarinFormsTests : BaseTestClass
{
    [Fact]
    public void GetGeneratedCodeForElement_Constructor_OpensWithIndentedBrace()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeGenerator codeGenerator = new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);

        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "IsXamarinFormsControl", Type = "bool", Value = true, SetsValue = true });
        component.States.Add(defaultState);
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        string code = codeGenerator.GetGeneratedCodeForElement(
            component,
            new CodeOutputElementSettings(),
            new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.XamarinForms, RootNamespace = "MyGame" });

        code.ShouldContain("var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;");
        Regex.IsMatch(code, @"^\d+\{", RegexOptions.Multiline).ShouldBeFalse(code);
    }
}
