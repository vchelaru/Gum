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
/// Changing the base type of an element or instance removes the variables its new type cannot have (#7191;
/// before that the tool kept them, in case the change was accidental, and these tests pinned it). Undo restores
/// them (<see cref="BaseTypeChangePrunesVariablesTests"/>); switching back by hand does not. The Variables tab
/// lists the rows of the current type, and an exposed variable the new type cannot have no longer appears.
/// Projects that already hold leftovers from an earlier Gum are pinned by the Gum.Cli tests, which load them
/// and check that nothing new is reported.
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
        VariableSaveLogic variableSaveLogic = _mocker.CreateInstance<VariableSaveLogic>();
        _mocker.Use<IVariableSaveLogic>(variableSaveLogic);
        _mocker.Use<IVariableDefinedByTypeLogic>(variableSaveLogic);

        _mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.GetMock<IRenameProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());
        _mocker.Use<IRenameLogic>(_mocker.CreateInstance<RenameLogic>());
        _mocker.Use<IInstanceOverrideRemover>(_mocker.CreateInstance<InstanceOverrideRemover>());
        _inheritanceLogic = new InheritanceLogic(
            new Mock<IFileCommands>().Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            _mocker.CreateInstance<BaseTypeVariablePruner>());
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

    private static List<string> Without(List<string> snapshot, params string[] linePrefixes) =>
        snapshot.Where(line => !linePrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal))).ToList();

    [Fact]
    public void ChangingElementBaseType_PrunesVariablesTheNewTypeLacks_AndSwitchingBackDoesNotRestoreThem()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ComponentSave label = Component("Label");
        before.ShouldContain("Label/Default|Text|string|Greetings|sets=True|exposed=");
        before.ShouldContain("Label/Big|Text|string|BigHello|sets=True|exposed=");

        ChangeElementBaseType(label, "Container");
        ChangeElementBaseType(Component("Reader"), "Container");

        label.BaseType.ShouldBe("Container");
        List<string> pruned = Without(before, "Label/Default|Text|", "Label/Default|FontSize|", "Label/Big|Text|",
            "Reader/Default|Text|", "Reader/Default|VariableReferences|");
        pruned.Count.ShouldBeLessThan(before.Count);
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(pruned);
        label.DefaultState!.GetVariableSave("Width")!.Value.ShouldBe(77f);

        ChangeElementBaseType(label, "Text");
        ChangeElementBaseType(Component("Reader"), "Text");

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(pruned);
    }

    [Fact]
    public void ChangingInstanceBaseType_PrunesVariablesTheNewTypeLacks_ExposureAndConsumers_AndSwitchingBackDoesNotRestoreThem()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ComponentSave card = Component("Card");
        InstanceSave caption = card.Instances.Single(i => i.Name == "Caption");

        ChangeInstanceBaseType(card, caption, "Container");

        caption.BaseType.ShouldBe("Container");
        List<string> pruned = Without(before, "Card/Default|Caption.Text|", "Card/Default|Caption.FontSize|",
            "Card/Alt|Caption.Text|", "Outer/Default|Card1.CaptionText|");
        pruned.Count.ShouldBeLessThan(before.Count);
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(pruned);
        card.DefaultState!.GetVariableSave("Caption.Width")!.Value.ShouldBe(55f);
        // The line that reads the removed exposed variable is untouched; the existing GUM0009 warning reports it.
        ((VariableListSave<string>)Component("Reader").DefaultState!.GetVariableListSave("VariableReferences")!).Value
            .ShouldBe(new[] { LeftoverVariablesProject.ReferenceLine });

        ChangeInstanceBaseType(card, caption, "Text");

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(pruned);
    }

    [Fact]
    public void VariablesTab_ListsTheRowsOfTheCurrentType_AndNoLongerTheExposedVariableThePruneRemoved()
    {
        ComponentSave label = Component("Label");
        ComponentSave card = Component("Card");
        InstanceSave caption = card.Instances.Single(i => i.Name == "Caption");
        InstanceSave card1 = Component("Outer").Instances.Single();
        GridRowNames(label, null, label.DefaultState!).ShouldContain("Text");
        GridRowNames(card, caption, card.DefaultState!).ShouldContain("Caption.FontSize");
        GridRowNames(card, null, card.DefaultState!).ShouldContain("CaptionText");

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
        GridRowNames(card, null, card.DefaultState!).ShouldNotContain("CaptionText");
        GridRowNames(Component("Outer"), card1, Component("Outer").DefaultState!).ShouldNotContain("Card1.CaptionText");

        ChangeElementBaseType(label, "Text");
        ChangeInstanceBaseType(card, caption, "Text");

        GridRowNames(label, null, label.DefaultState!).ShouldContain("Text");
        GridRowNames(label, null, label.DefaultState!).ShouldContain("FontSize");
        GridRowNames(card, caption, card.DefaultState!).ShouldContain("Caption.Text");
        label.DefaultState!.GetVariableSave("Text").ShouldBeNull();
    }
}
