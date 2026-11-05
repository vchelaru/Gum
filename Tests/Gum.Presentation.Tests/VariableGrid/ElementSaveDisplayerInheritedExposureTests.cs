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
/// An exposure declared on a base component labels the inherited instance's rows "Exposed as ..." on a
/// derived component too, and a derived exposure of the same variable wins (#7650).
/// </summary>
public class ElementSaveDisplayerInheritedExposureTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly ComponentSave _base;
    private readonly ComponentSave _derived;
    private readonly InstanceSave _inner;

    public ElementSaveDisplayerInheritedExposureTests()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        project.Behaviors.Add(behavior);

        ComponentSave button = new ComponentSave { Name = "Controls/Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        project.Components.Add(button);

        _base = new ComponentSave { Name = "BaseRow", BaseType = "Container" };
        StateSave baseDefault = new StateSave { Name = "Default", ParentContainer = _base };
        _base.States.Add(baseDefault);
        _inner = new InstanceSave { Name = "Inner", BaseType = "Controls/Button", ParentContainer = _base };
        _base.Instances.Add(_inner);
        baseDefault.Variables.Add(new VariableSave
        {
            Name = "Inner.Width", Type = "float", SetsValue = false, ExposedAsName = "InnerWidth",
        });
        baseDefault.Variables.Add(new VariableSave
        {
            Name = "Inner.IsEnabled", Type = "bool", SetsValue = false, ExposedAsName = "InnerEnabled",
        });
        project.Components.Add(_base);

        _derived = new ComponentSave { Name = "DerivedRow", BaseType = "BaseRow" };
        _derived.States.Add(new StateSave { Name = "Default", ParentContainer = _derived });
        project.Components.Add(_derived);

        ObjectFinder.Self.GumProjectSave = project;

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(_derived.DefaultState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_derived);
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

    private VariableGridEntry GetRow(ElementSave owner, string name) =>
        _displayer.GetCategories(owner, _inner, owner.DefaultState!, null)
            .SelectMany(category => category.Members)
            .Single(member => member.Name == name);

    [Fact]
    public void BaseExposedPlainVariable_OnDerivedComponent_ShowsExposedAsSubtext()
    {
        (GetRow(_derived, "Inner.Width").DetailText ?? "").ShouldContain("Exposed as InnerWidth");
    }

    [Fact]
    public void BaseExposedFormsProperty_OnDerivedComponent_ShowsExposedAsSubtext()
    {
        (GetRow(_derived, "Inner.IsEnabled").DetailText ?? "").ShouldContain("Exposed as InnerEnabled");
    }

    [Fact]
    public void DerivedExposure_WinsOverBaseExposure()
    {
        _derived.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "Inner.Width", Type = "float", SetsValue = false, ExposedAsName = "DerivedWidth",
        });

        string detail = GetRow(_derived, "Inner.Width").DetailText ?? "";

        detail.ShouldContain("Exposed as DerivedWidth");
        detail.ShouldNotContain("InnerWidth");
    }

    [Fact]
    public void BaseExposedPlainVariable_OnBaseComponent_StillShowsExposedAsSubtext()
    {
        (GetRow(_base, "Inner.Width").DetailText ?? "").ShouldContain("Exposed as InnerWidth");
    }

    [Fact]
    public void ExposedAsLookup_FollowsBaseTypeChain_DerivedWins()
    {
        ExposedVariableLookup.GetExposure(_derived, _inner, "Width")!.ExposedAsName.ShouldBe("InnerWidth");

        _derived.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "Inner.Width", Type = "float", SetsValue = false, ExposedAsName = "DerivedWidth",
        });

        ExposedVariableLookup.GetExposure(_derived, _inner, "Width")!.ExposedAsName.ShouldBe("DerivedWidth");
    }
}
