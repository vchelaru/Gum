using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// A behavior-declared Forms property (IsEnabled) that a component exposes from an inner instance is
/// labeled "Exposed as ..." on that instance's row, the same as an exposed plain variable (#7641).
/// </summary>
public class ElementSaveDisplayerExposedFormsPropertyTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly ComponentSave _row;
    private readonly InstanceSave _button;
    private readonly ComponentSave _buttonComponent;

    public ElementSaveDisplayerExposedFormsPropertyTests()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        project.Behaviors.Add(behavior);

        _buttonComponent = new ComponentSave { Name = "Controls/Button", BaseType = "Container" };
        _buttonComponent.States.Add(new StateSave { Name = "Default", ParentContainer = _buttonComponent });
        _buttonComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        project.Components.Add(_buttonComponent);

        _row = new ComponentSave { Name = "OptionsRow", BaseType = "Container" };
        StateSave rowDefault = new StateSave { Name = "Default", ParentContainer = _row };
        _row.States.Add(rowDefault);
        _button = new InstanceSave { Name = "ButtonInstance1", BaseType = "Controls/Button", ParentContainer = _row };
        _row.Instances.Add(_button);
        rowDefault.Variables.Add(new VariableSave
        {
            Name = "ButtonInstance1.IsEnabled",
            Type = "bool",
            SetsValue = false,
            ExposedAsName = "IsEnabled",
        });
        rowDefault.Variables.Add(new VariableSave
        {
            Name = "ButtonInstance1.Width",
            Type = "float",
            SetsValue = false,
            ExposedAsName = "ButtonWidth",
        });
        project.Components.Add(_row);

        ObjectFinder.Self.GumProjectSave = project;

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(rowDefault);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_row);
        _mocker.Use(StandardElementsManager.Self);
        Gum.Reflection.TypeManager typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);
        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());
        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());
        _mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    private VariableGridEntry GetRow(string name) =>
        _displayer.GetCategories(_row, _button, _row.DefaultState, null)
            .SelectMany(category => category.Members)
            .Single(member => member.Name == name);

    [Fact]
    public void ExposedPlainVariable_OnInnerInstance_ShowsExposedAsSubtext()
    {
        (GetRow("ButtonInstance1.Width").DetailText ?? "").ShouldContain("Exposed as ButtonWidth");
    }

    [Fact]
    public void ExposedFormsProperty_OnInnerInstance_ShowsExposedAsSubtext()
    {
        (GetRow("ButtonInstance1.IsEnabled").DetailText ?? "").ShouldContain("Exposed as IsEnabled");
    }

    [Fact]
    public void FormsProperty_NotExposed_ShowsNoExposedSubtext()
    {
        _row.DefaultState.Variables.RemoveAll(item => item.ExposedAsName == "IsEnabled");

        (GetRow("ButtonInstance1.IsEnabled").DetailText ?? "").ShouldNotContain("Exposed");
    }

    [Fact]
    public void ExposedFormsProperty_OnInnerInstance_ShowsRenamedExposure()
    {
        _row.DefaultState.Variables.Single(item => item.ExposedAsName == "IsEnabled").ExposedAsName = "RowEnabled";

        (GetRow("ButtonInstance1.IsEnabled").DetailText ?? "").ShouldContain("Exposed as RowEnabled");
    }
}
