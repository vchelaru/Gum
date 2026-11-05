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
    public void DerivedControlInstance_ShowsTheInheritedBehaviorsFormsProperty(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        ComponentSave derivedControl = world.AddComponent("Controls/DerivedControl", world.Control.Name);
        InstanceSave derivedInstance = ExposedFormsProject.AddInstance(world.Screen, "DerivedControlInstance", derivedControl.Name);
        ElementSaveDisplayer displayer = CreateDisplayer(world, world.Screen);

        List<VariableGridEntry> rows = Rows(displayer, world.Screen, derivedInstance);

        rows.Count(row => row.Name == "DerivedControlInstance." + property).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void DerivedControl_SelectedItself_ShowsTheInheritedBehaviorsFormsProperty(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        ComponentSave derivedControl = world.AddComponent("Controls/DerivedControl", world.Control.Name);
        ElementSaveDisplayer displayer = CreateDisplayer(world, derivedControl);

        List<VariableGridEntry> rows = Rows(displayer, derivedControl, null);

        rows.Count(row => row.Name == property).ShouldBe(1);
    }
}
