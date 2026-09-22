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
}
