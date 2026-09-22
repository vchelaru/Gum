using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #4911: a Component's custom ("new") variable was generated as a bare auto-property
/// (<c>{ get; set; }</c>) with no notification wiring, so INotifyPropertyChanged.PropertyChanged
/// never fires for it - whether set directly in C# or through GraphicalUiElement.SetProperty
/// (which is how the tool applies a Variables-tab edit at runtime via TrySetCustomVariableOnThis's
/// reflection-based SetValue, since reflection still invokes the property's own setter).
/// </summary>
public class CodeGeneratorNewVariableNotifyPropertyChangedTests : BaseTestClass
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

    [Fact]
    public void GetGeneratedCodeForElement_CustomVariable_SetterRaisesNotifyPropertyChanged()
    {
        GumProjectSave project = Project;

        ComponentSave slider = new ComponentSave { Name = "Slider", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = slider };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "NumberOfMarks",
            Value = 5,
            Type = "int",
            IsCustomVariable = true,
        });
        slider.States.Add(defaultState);
        project.Components.Add(slider);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                slider,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

            code.ShouldContain("NotifyPropertyChanged();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetGeneratedCodeForElement_MonoGameForms_CustomVariable_SetterRaisesOnPropertyChanged()
    {
        // Under MonoGameForms, the generated class derives from Gum.Forms.Controls.FrameworkElement
        // (see CodeGenerator.GetInheritance), not GraphicalUiElement - it has OnPropertyChanged, not
        // NotifyPropertyChanged (issue #4921 CI: Build-Generated-Code, CS0103).
        GumProjectSave project = Project;

        ComponentSave slider = new ComponentSave { Name = "Slider", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = slider };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "NumberOfMarks",
            Value = 5,
            Type = "int",
            IsCustomVariable = true,
        });
        slider.States.Add(defaultState);
        project.Components.Add(slider);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                slider,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms, RootNamespace = "MyGame" });

            code.ShouldContain("OnPropertyChanged();");
            code.ShouldNotContain("NotifyPropertyChanged();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    // A custom variable named after a reserved C# keyword (e.g. "object") is escaped by
    // ToCSharpName to "@object" - the '@' must not leak into the backing field name, since '@'
    // is only legal as the first character of a C# identifier (CS1519 mid-identifier).
    [Fact]
    public void GetGeneratedCodeForElement_CustomVariableNamedReservedKeyword_GeneratesValidBackingField()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError reservedKeyword = CommonValidationError.ReservedCSharpKeyword;
        CommonValidationError none = CommonValidationError.None;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName("object", out whyNotValid, out reservedKeyword))
            .Returns(false);
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.Is<string>(name => name != "object"), out whyNotValid, out none))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();
        CodeGenerator codeGenerator = new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);

        GumProjectSave project = Project;

        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "object",
            Value = "hello",
            Type = "string",
            IsCustomVariable = true,
        });
        component.States.Add(defaultState);
        project.Components.Add(component);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = codeGenerator.GetGeneratedCodeForElement(
                component,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

            code.ShouldContain("public string @object");
            code.ShouldNotContain("_@object");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
