using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Tests.Shared;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Changing the base type of an element or instance never removes or converts the variables it had
/// (default state, category states, exposed variables, VariableReferences). The tool keeps them on purpose, so
/// switching back after an accidental change restores every value. The Variables tab hides the ones the
/// current type does not use, except exposed ones, which belong to the container's interface.
/// A base type handler that prunes or converts variables, or a grid that lists leftovers, turns these red.
/// </summary>
public class LeftoverVariablesBaseTypeChangeTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly GumProjectSave _project;
    private readonly InheritanceLogic _inheritanceLogic;
    private readonly SetVariableLogic _setVariableLogic;
    private readonly ElementSaveDisplayer _displayer;

    public LeftoverVariablesBaseTypeChangeTests()
    {
        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        LeftoverVariablesProject.AddComponents(_project, typeChanged: false);
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.GetMock<IProjectState>().Setup(x => x.ProjectDirectory).Returns("/TestProject/");
        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());
        _mocker.Use(StandardElementsManager.Self);
        var typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);
        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());

        _inheritanceLogic = new InheritanceLogic(
            new Mock<IFileCommands>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object);
        // The real check: the mock answers false, which makes the tool revert an instance's base type.
        _mocker.Use<ICircularReferenceManager>(new CircularReferenceManager(ObjectFinder.Self));
        _setVariableLogic = _mocker.CreateInstance<SetVariableLogic>();
        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    private ComponentSave Component(string name) => _project.Components.Single(c => c.Name == name);

    /// <summary>Does what the tool does when the user picks a new base type for the element.</summary>
    private void ChangeElementBaseType(ComponentSave component, string newBaseType)
    {
        string? oldValue = component.BaseType;
        component.BaseType = newBaseType;
        _setVariableLogic.ReactToPropertyValueChanged("BaseType", oldValue, component, instance: null,
            component.DefaultState, refresh: false, recordUndo: false, trySave: false);
        _inheritanceLogic.HandleElementBaseType(component);
    }

    /// <summary>Does what the tool does when the user picks a new base type for the instance.</summary>
    private void ChangeInstanceBaseType(ComponentSave container, InstanceSave instance, string newBaseType)
    {
        string oldValue = instance.BaseType;
        instance.BaseType = newBaseType;
        _setVariableLogic.ReactToPropertyValueChanged("BaseType", oldValue, container, instance,
            container.DefaultState, refresh: false, recordUndo: false, trySave: false);
        _inheritanceLogic.HandleInstanceVariableBaseTypeSet(container, instance);
    }

    private List<string> GridRowNames(ElementSave element, InstanceSave? instance, StateSave state)
    {
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(element);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(instance);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(state);
        return _displayer.GetCategories(element, instance, state, null)
            .SelectMany(category => category.Members)
            .Select(member => member.Name)
            .ToList();
    }

    [Fact]
    public void ChangingElementBaseType_KeepsEveryVariable_AndSwitchingBackRestoresThem()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ComponentSave label = Component("Label");
        before.ShouldContain("Label/Default|Text|string|Greetings|sets=True|exposed=");
        before.ShouldContain("Label/Big|Text|string|BigHello|sets=True|exposed=");

        ChangeElementBaseType(label, "Container");
        ChangeElementBaseType(Component("Reader"), "Container");

        label.BaseType.ShouldBe("Container");
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);
        label.DefaultState!.GetVariableSave("Text")!.Value.ShouldBe(LeftoverVariablesProject.LabelText);
        label.DefaultState.GetVariableSave("FontSize")!.Value.ShouldBe(33);
        label.Categories.Single().States.Single().GetVariableSave("Text")!.Value.ShouldBe(LeftoverVariablesProject.LabelBigText);

        ChangeElementBaseType(label, "Text");
        ChangeElementBaseType(Component("Reader"), "Text");

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);
    }

    [Fact]
    public void ChangingInstanceBaseType_KeepsEveryVariable_ExposedVariablesAndTheirConsumers_AndReferences()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ComponentSave card = Component("Card");
        InstanceSave caption = card.Instances.Single(i => i.Name == "Caption");

        ChangeInstanceBaseType(card, caption, "Container");

        caption.BaseType.ShouldBe("Container");
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);
        card.DefaultState!.GetVariableSave("Caption.Text")!.Value.ShouldBe(LeftoverVariablesProject.CaptionTextValue);
        card.DefaultState.GetVariableSave("Caption.FontSize")!.Value.ShouldBe(44);
        card.Categories.Single().States.Single().GetVariableSave("Caption.Text")!.Value.ShouldBe(LeftoverVariablesProject.CaptionAltText);
        card.DefaultState.GetVariableSave("Caption.Text")!.ExposedAsName.ShouldBe("CaptionText");
        Component("Outer").DefaultState!.GetVariableSave("Card1.CaptionText")!.Value.ShouldBe(LeftoverVariablesProject.OuterCaptionText);
        ((VariableListSave<string>)Component("Reader").DefaultState!.GetVariableListSave("VariableReferences")!).Value
            .ShouldBe(new[] { LeftoverVariablesProject.ReferenceLine });

        ChangeInstanceBaseType(card, caption, "Text");

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);
    }

    [Fact]
    public void VariablesTab_HidesLeftoversOfTheCurrentType_ButKeepsExposedOnes_AndShowsThemAgainWhenSwitchedBack()
    {
        ComponentSave label = Component("Label");
        ComponentSave card = Component("Card");
        InstanceSave caption = card.Instances.Single(i => i.Name == "Caption");
        InstanceSave card1 = Component("Outer").Instances.Single();
        GridRowNames(label, null, label.DefaultState!).ShouldContain("Text");
        GridRowNames(card, caption, card.DefaultState!).ShouldContain("Caption.FontSize");

        ChangeElementBaseType(label, "Container");
        ChangeInstanceBaseType(card, caption, "Container");

        List<string> labelRows = GridRowNames(label, null, label.DefaultState!);
        labelRows.ShouldContain("Width");
        labelRows.ShouldNotContain("Text");
        labelRows.ShouldNotContain("FontSize");
        List<string> captionRows = GridRowNames(card, caption, card.DefaultState!);
        captionRows.ShouldContain("Caption.Width");
        captionRows.ShouldNotContain("Caption.Text");
        captionRows.ShouldNotContain("Caption.FontSize");
        GridRowNames(card, null, card.DefaultState!).ShouldContain("CaptionText");
        GridRowNames(Component("Outer"), card1, Component("Outer").DefaultState!).ShouldContain("Card1.CaptionText");

        ChangeElementBaseType(label, "Text");
        ChangeInstanceBaseType(card, caption, "Text");

        GridRowNames(label, null, label.DefaultState!).ShouldContain("Text");
        GridRowNames(label, null, label.DefaultState!).ShouldContain("FontSize");
        GridRowNames(card, caption, card.DefaultState!).ShouldContain("Caption.Text");
    }
}
