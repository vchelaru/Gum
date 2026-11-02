using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #7059, #7210: a project the tool saved never makes GUM0016 fire. The tool gives every derived element the
/// DefinedByBase copies of its base's instances (<see cref="InheritanceLogic"/>), and saving writes them.
/// </summary>
public class ToolSavedBaseInstanceTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "gum-" + Guid.NewGuid().ToString("N"));

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ProjectSavedAfterBaseTypeSetAndBaseInstanceAdded_ShouldNotReportGum0016()
    {
        // Arrange
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave buttonBase = AddComponent(project, "ButtonBase", "Container");
        buttonBase.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = buttonBase });
        buttonBase.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = buttonBase });

        InheritanceLogic inheritanceLogic = new InheritanceLogic(
            new Mock<IFileCommands>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            new Mock<IBaseTypeVariablePruner>().Object);

        // The tool path when a user picks a Base Type in the dropdown.
        ComponentSave chosenBase = AddComponent(project, "ChosenBaseButton", "Container");
        chosenBase.BaseType = "ButtonBase";
        inheritanceLogic.HandleElementBaseType(chosenBase);

        // The tool path when a user adds an instance to a base that already has derived elements.
        ComponentSave existingDerived = AddComponent(project, "ExistingDerivedButton", "ButtonBase");
        inheritanceLogic.HandleElementBaseType(existingDerived);
        InstanceSave icon = new InstanceSave { Name = "Icon", BaseType = "Sprite", ParentContainer = buttonBase };
        buttonBase.Instances.Add(icon);
        inheritanceLogic.HandleInstanceAdded(buttonBase, icon);

        string gumxPath = Path.Combine(_directory, "Project.gumx");
        Directory.CreateDirectory(_directory);

        // Act
        project.Save(gumxPath, saveElements: true);
        ProjectLoadResult load = new ProjectLoader().Load(gumxPath);

        // Assert
        load.Success.ShouldBeTrue();
        load.Project!.GetComponentSave("ChosenBaseButton")!.Instances
            .Where(instance => instance.DefinedByBase).Select(instance => instance.Name)
            .ShouldBe(new[] { "TextInstance", "Background", "Icon" }, ignoreOrder: true);
        load.Project.GetComponentSave("ExistingDerivedButton")!.Instances
            .Select(instance => instance.Name).ShouldContain("Icon");
        load.LoadErrors.ShouldNotContain(error => error.Code == "GUM0016");
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }
}
