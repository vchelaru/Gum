using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CodeGeneratorExposedStateVariableTests : BaseTestClass
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

        return new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            elementSettingsManager,
            directoryProvider);
    }

    [Fact]
    public void GetGeneratedCodeForElement_ExposedUncategorizedStateVariable_DoesNotGenerateTheProperty()
    {
        GumProjectSave project = Project;

        ComponentSave item = new ComponentSave { Name = "Item", BaseType = "Container" };
        item.States.Add(new StateSave { Name = "Default", ParentContainer = item });
        item.States.Add(new StateSave { Name = "Highlighted", ParentContainer = item });
        project.Components.Add(item);

        ComponentSave holder = new ComponentSave { Name = "Holder", BaseType = "Container" };
        StateSave holderDefault = new StateSave { Name = "Default", ParentContainer = holder };
        holderDefault.Variables.Add(new VariableSave
        {
            Name = "ItemInstance.State",
            Type = "State",
            ExposedAsName = "ItemState",
            SetsValue = true,
        });
        holder.States.Add(holderDefault);
        holder.Instances.Add(new InstanceSave { Name = "ItemInstance", BaseType = "Item", ParentContainer = holder });
        project.Components.Add(holder);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                holder,
                elementSettings: null!,
                new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

            code.ShouldNotContain("ItemState");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
