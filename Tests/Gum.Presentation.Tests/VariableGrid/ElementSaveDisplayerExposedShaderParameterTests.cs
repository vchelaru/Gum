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
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// A shader parameter a component exposes (#7010 slice 6) reaches the users of that component as an ordinary
/// exposed variable, and the component's own tab does not show the saved placeholder as a stray extra row.
/// </summary>
public class ElementSaveDisplayerExposedShaderParameterTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly ComponentSave _component;
    private readonly ScreenSave _screen;
    private readonly InstanceSave _user;

    public ElementSaveDisplayerExposedShaderParameterTests()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        _component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(componentDefault);
        _component.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = _component });
        componentDefault.Variables.Add(new VariableSave
        {
            Name = "Holder.Outline_Thickness",
            Type = "float",
            ShaderParameter = "Thickness",
            SetsValue = false,
            ExposedAsName = "OutlineThickness",
        });
        project.Components.Add(_component);

        _screen = new ScreenSave { Name = "TestScreen" };
        _screen.States.Add(new StateSave { Name = "Default", ParentContainer = _screen });
        _user = new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = _screen };
        _screen.Instances.Add(_user);
        project.Screens.Add(_screen);

        ObjectFinder.Self.GumProjectSave = project;

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(_screen.DefaultState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_screen);
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

    [Fact]
    public void TheUserOfTheComponent_SeesTheExposedParameterAsAFloatRow_AndSetsItThroughTheExposedName()
    {
        List<VariableGridEntry> members = _displayer.GetCategories(_screen, _user, _screen.DefaultState, null)
            .SelectMany(category => category.Members)
            .ToList();

        VariableGridEntry row = members.Single(member => member.Name == "Comp1.OutlineThickness");
        row.GetValueType(_user).ShouldBe(typeof(float));

        row.SetValue(_user, 5f, VariablePropertyCommitType.Full);

        _screen.DefaultState.GetValueRecursive("Comp1.OutlineThickness").ShouldBe(5f);
    }

    [Fact]
    public void TheComponentItself_ShowsTheExposedPlaceholderOnlyOnce()
    {
        List<VariableGridEntry> members = _displayer.GetCategories(_component, null, _component.DefaultState, null)
            .SelectMany(category => category.Members)
            .ToList();

        members.Count(member => member.Name.Contains("Outline")).ShouldBeLessThanOrEqualTo(1);
    }
}
