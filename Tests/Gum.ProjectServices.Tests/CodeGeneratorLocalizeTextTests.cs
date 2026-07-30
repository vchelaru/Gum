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
/// LocalizeText (issue #4133) is a plain bool property on TextRuntime, not a method-mapped
/// dispatch name like TextNoTranslate, so codegen needs no special-casing - a set value emits a
/// plain bool assignment the same as any other property.
/// </summary>
public class CodeGeneratorLocalizeTextTests : BaseTestClass
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
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private static CodeOutputProjectSettings CreateMonoGame() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
    };

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    private static void AddVariable(ElementSave element, string name, object? value, string type) =>
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = name,
            Value = value,
            SetsValue = true,
            Type = type,
        });

    /// <summary>
    /// Mirrors StandardElementsManager defining LocalizeText on the Text standard element.
    /// Codegen only emits an instance variable whose root is defined on the base element.
    /// </summary>
    private void RegisterTextLocalizeTextVariable()
    {
        StandardElementSave text = Project.StandardElements.First(item => item.Name == "Text");
        AddVariable(text, "LocalizeText", true, "bool");
    }

    [Fact]
    public void GetCodeForInstance_LocalizeTextFalse_EmitsPlainBoolAssignment()
    {
        GumProjectSave project = Project;
        RegisterTextLocalizeTextVariable();

        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave textInstance = new InstanceSave
        {
            Name = "MyText",
            BaseType = "Text",
            ParentContainer = main,
        };
        main.Instances.Add(textInstance);
        AddVariable(main, "MyText.LocalizeText", false, "bool");

        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(textInstance, main, CreateMonoGame());

            code.ShouldContain("this.MyText.LocalizeText = false;");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
