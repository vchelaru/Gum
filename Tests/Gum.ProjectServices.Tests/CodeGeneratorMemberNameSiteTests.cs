using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Output paths that name an instance as a member of the generated class must step around a member named
/// like the class itself (CS0542), the same as the instance declaration does.
/// </summary>
public class CodeGeneratorMemberNameSiteTests : BaseTestClass
{
    private string Generate(CodeOutputProjectSettings settings, Action<ComponentSave> edit)
    {
        ComponentSave component = new ComponentSave { Name = "Score", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Score", BaseType = "Text", ParentContainer = component });
        edit(component);
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        Mock<INameVerifier> nameVerifier = new Mock<INameVerifier>();
        string? whyNotValid = null;
        CommonValidationError error = default;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeGenerator generator = new CodeGenerator(
            new CodeGenerationNameVerifier(nameVerifier.Object), new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider), directoryProvider);

        return generator.GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    [Fact]
    public void GetGeneratedCodeForElement_SurfaceInvalidationOfInstanceNamedLikeItsComponent_UsesTheSteppedAroundMember()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Maui,
            ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
        };

        string code = Generate(settings, component =>
        {
            component.DefaultState!.Variables.Add(new VariableSave { Name = "IsXamarinFormsControl", Type = "bool", Value = true, SetsValue = true });
            StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
            StateSave state = new StateSave { Name = "Big", ParentContainer = component };
            state.Variables.Add(new VariableSave { Name = "Score.Rotation", Type = "float", Value = 5f, SetsValue = true });
            category.States.Add(state);
            component.Categories.Add(category);
        });

        code.ShouldContain("casted.Score_.EffectiveManagers?.InvalidateSurface();");
    }
}
