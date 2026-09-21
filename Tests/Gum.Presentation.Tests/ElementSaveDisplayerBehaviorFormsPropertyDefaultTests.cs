using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Repro for issue #4903: an enum-typed <see cref="BehaviorSave.FormsProperties"/> default
/// (e.g. StackPanelBehavior's Orientation, declared as a raw XML string) should surface through
/// <see cref="ElementSaveDisplayer.GetCategories"/> as a value matching the row's declared enum
/// type, the same way a normal authored enum variable would - not as the raw string, which the
/// combo box's boxed-enum item list never matches.
/// </summary>
public class ElementSaveDisplayerBehaviorFormsPropertyDefaultTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;

    public ElementSaveDisplayerBehaviorFormsPropertyDefaultTests()
    {
        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);

        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.Use(StandardElementsManager.Self);

        var typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);

        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());

        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    [Fact]
    public void GetCategories_ShouldReturnBoxedEnumDefault_ForUnauthoredEnumFormsProperty()
    {
        var behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Name = "Orientation",
            Type = "Orientation",
            Value = "Vertical"
        });
        _project.Behaviors.Add(behavior);

        var component = new ComponentSave { Name = "MyStackPanel" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });
        var componentDefaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(componentDefaultState);
        _project.Components.Add(component);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(componentDefaultState);
        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement)
            .Returns(component);

        var categories = _displayer.GetCategories(component, null, componentDefaultState, null);
        var orientationEntry = categories.SelectMany(c => c.Members).FirstOrDefault(m => m.Name == "Orientation");

        orientationEntry.ShouldNotBeNull("the behavior's FormsProperty should surface a grid row even when unauthored");
        orientationEntry.GetValue(component).ShouldBe(Gum.Forms.Controls.Orientation.Vertical,
            "the combo box selects by matching a boxed enum instance against its Enum.GetValues item list - a raw string never matches, so the default renders as unselected/blank");
    }
}
