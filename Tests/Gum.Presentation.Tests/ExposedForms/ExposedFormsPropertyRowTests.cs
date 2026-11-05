using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.ExposedForms;

/// <summary>
/// The Variables tab rows for a behavior's Forms property that a component exposes: the inner instance's own
/// row, and the row the exposure adds to every instance of the component, for every Forms property the
/// shipped behaviors declare.
/// </summary>
public class ExposedFormsPropertyRowTests : BaseTestClass
{
    private ElementSaveDisplayer CreateDisplayer(ExposedFormsProject world, ElementSave selected)
    {
        AutoMocker mocker = new AutoMocker();
        mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(selected.DefaultState);
        mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(selected);
        mocker.Use(StandardElementsManager.Self);
        Gum.Reflection.TypeManager typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        mocker.Use(typeManager);
        mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());
        mocker.Use<IVariableSaveLogic>(mocker.CreateInstance<VariableSaveLogic>());
        mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(world.Project);
        return mocker.CreateInstance<ElementSaveDisplayer>();
    }

    private static List<VariableGridEntry> Rows(ElementSaveDisplayer displayer, ElementSave owner, InstanceSave? instance) =>
        displayer.GetCategories(owner, instance, owner.DefaultState!, null).SelectMany(category => category.Members).ToList();

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void InnerInstance_ShowsARowForEachFormsProperty(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Row);

        Rows(displayer, world.Row, world.Inner).Count(row => row.Name == "Inner." + property).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void InnerInstance_ShowsOneRowForAnExposedFormsProperty(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Row);

        Rows(displayer, world.Row, world.Inner).Count(row => row.Name == "Inner." + property).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ContainerInstance_ShowsOneRowUnderTheExposedName(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Screen);

        List<VariableGridEntry> rows = Rows(displayer, world.Screen, world.RowInstance);

        rows.Count(row => row.Name == "RowInstance." + ExposedFormsProject.ExposedName).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ContainerInstance_DoesNotShowTheInnerInstancesUnexposedFormsProperty(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Screen);

        List<VariableGridEntry> rows = Rows(displayer, world.Screen, world.RowInstance);

        rows.ShouldNotContain(row => row.Name == "RowInstance." + property || row.Name == "RowInstance.Inner." + property);
        rows.ShouldNotContain(row => row.Name == "RowInstance." + ExposedFormsProject.ExposedName);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ContainerInstance_ShowsTheExposedRowOfAnInheritedExposure(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        ComponentSave derived = world.AddComponent("DerivedRow", world.Row.Name);
        InstanceSave derivedInstance = ExposedFormsProject.AddInstance(world.Screen, "DerivedInstance", derived.Name);
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Screen);

        List<VariableGridEntry> rows = Rows(displayer, world.Screen, derivedInstance);

        rows.Count(row => row.Name == "DerivedInstance." + ExposedFormsProject.ExposedName).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ContainerInstance_ShowsTheExposedRowOfANestedExposure(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        ComponentSave panel = world.AddComponent("Panel", "Container");
        InstanceSave rowInPanel = ExposedFormsProject.AddInstance(panel, "RowInPanel", world.Row.Name);
        world.Expose(panel, rowInPanel, ExposedFormsProject.ExposedName, "PanelValue");
        InstanceSave panelInstance = ExposedFormsProject.AddInstance(world.Screen, "PanelInstance", panel.Name);
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Screen);

        List<VariableGridEntry> rows = Rows(displayer, world.Screen, panelInstance);

        rows.Count(row => row.Name == "PanelInstance.PanelValue").ShouldBe(1);
    }

}
