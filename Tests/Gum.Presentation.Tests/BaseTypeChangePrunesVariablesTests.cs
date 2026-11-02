using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Tests.Shared;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Changing the base type of an element or instance removes the variables, variable lists and
/// <c>VariableReferences</c> lines the new type cannot have (#7191), in the default state and every category
/// state, and undo restores all of it as one step. Runs the real <see cref="UndoManager"/>,
/// <see cref="BaseTypeVariablePruner"/>, <see cref="RenameLogic"/> and <see cref="InstanceOverrideRemover"/>.
/// <see cref="LeftoverVariablesProject"/> supplies the Text-to-Container data.
/// </summary>
public class BaseTypeChangePrunesVariablesTests : BaseTestClass
{
    private readonly GumProjectSave _project;
    private readonly UndoManager _undoManager;
    private readonly InheritanceLogic _inheritanceLogic;
    private readonly Mock<ISelectedState> _selectedState;

    public BaseTypeChangePrunesVariablesTests()
    {
        AutoMocker mocker = new AutoMocker();

        _project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        LeftoverVariablesProject.AddComponents(_project, typeChanged: false);
        ObjectFinder.Self.GumProjectSave = _project;

        mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        mocker.GetMock<IRenameProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        mocker.GetMock<IPluginManager>().Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>())).Returns(new List<Attribute>());

        _selectedState = mocker.GetMock<ISelectedState>();
        _selectedState.SetupProperty(x => x.SelectedStateSave);
        _selectedState.SetupProperty(x => x.SelectedStateCategorySave);

        _undoManager = mocker.CreateInstance<UndoManager>();
        mocker.Use<IUndoManager>(_undoManager);
        mocker.Use(new Lazy<IUndoManager>(() => _undoManager));
        mocker.Use<IReferenceFinder>(mocker.CreateInstance<ReferenceFinder>());
        mocker.Use<IRenameLogic>(mocker.CreateInstance<RenameLogic>());
        mocker.Use<IInstanceOverrideRemover>(mocker.CreateInstance<InstanceOverrideRemover>());
        mocker.Use<IVariableDefinedByTypeLogic>(mocker.CreateInstance<VariableSaveLogic>());
        mocker.Use<IBaseTypeVariablePruner>(mocker.CreateInstance<BaseTypeVariablePruner>());

        _inheritanceLogic = mocker.CreateInstance<InheritanceLogic>();
    }

    private ComponentSave Component(string name) => _project.Components.Single(c => c.Name == name);

    private void Select(ComponentSave component)
    {
        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(component);
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _selectedState.Object.SelectedStateSave = component.DefaultState;
        _undoManager.RecordState();
    }

    /// <summary>What the tool does when the user picks a new base type for the element, under the edit's undo lock.</summary>
    private void ChangeElementBaseType(ComponentSave component, string newBaseType)
    {
        Select(component);
        using (_undoManager.RequestLock())
        {
            component.BaseType = newBaseType;
            _inheritanceLogic.HandleElementBaseType(component);
        }
    }

    private void ChangeInstanceBaseType(ComponentSave container, string instanceName, string newBaseType)
    {
        Select(container);
        InstanceSave instance = container.GetInstance(instanceName)!;
        using (_undoManager.RequestLock())
        {
            instance.BaseType = newBaseType;
            _inheritanceLogic.HandleInstanceVariableBaseTypeSet(container, instance);
        }
    }

    private static VariableSave? Variable(ElementSave element, string stateName, string variableName) =>
        element.AllStates.Single(s => s.Name == stateName).GetVariableSave(variableName);

    private static VariableListSave<string> AddReferences(StateSave state, string listName, params string[] lines)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = listName, Type = "string" };
        list.Value.AddRange(lines);
        state.VariableLists.Add(list);
        return list;
    }

    private static List<string>? Lines(StateSave state, string listName) =>
        (state.GetVariableListSave(listName) as VariableListSave<string>)?.Value.ToList();

    [Fact]
    public void ChangingElementBaseType_RemovesVariablesTheNewTypeLacks_InDefaultAndCategoryStates()
    {
        ComponentSave label = Component("Label");

        ChangeElementBaseType(label, "Container");

        Variable(label, "Default", "Text").ShouldBeNull();
        Variable(label, "Default", "FontSize").ShouldBeNull();
        Variable(label, "Big", "Text").ShouldBeNull();
    }

    [Fact]
    public void ChangingElementBaseType_KeepsVariablesTheNewTypeShares_AndCustomVariables()
    {
        ComponentSave label = Component("Label");
        label.DefaultState.Variables.Add(new VariableSave { Name = "Speed", Type = "float", Value = 3f, SetsValue = true, IsCustomVariable = true });

        ChangeElementBaseType(label, "Container");

        Variable(label, "Default", "Width")!.Value.ShouldBe(77f);
        Variable(label, "Default", "Speed")!.Value.ShouldBe(3f);
    }

    [Fact]
    public void ChangingElementBaseType_RemovesVariableReferencesLinesWhoseLeftSideTheNewTypeLacks()
    {
        ComponentSave reader = Component("Reader");
        VariableListSave<string> references = (VariableListSave<string>)reader.DefaultState.GetVariableListSave("VariableReferences")!;
        references.Value.Add("Width = Components/Label.Width");
        references.Value.Add("// Text = Components/Label.Text");

        ChangeElementBaseType(reader, "Container");

        Lines(reader.DefaultState, "VariableReferences")
            .ShouldBe(new[] { "Width = Components/Label.Width", "// Text = Components/Label.Text" });
    }

    [Fact]
    public void ChangingElementBaseType_RemovesVariableReferencesListWhenEveryLineWentAway()
    {
        ComponentSave reader = Component("Reader");

        ChangeElementBaseType(reader, "Container");

        reader.DefaultState.GetVariableListSave("VariableReferences").ShouldBeNull();
    }

    [Fact]
    public void ChangingInstanceBaseType_RemovesItsVariablesTheNewTypeLacks_InDefaultAndCategoryStates()
    {
        ComponentSave card = Component("Card");

        ChangeInstanceBaseType(card, "Caption", "Container");

        Variable(card, "Default", "Caption.Text").ShouldBeNull();
        Variable(card, "Default", "Caption.FontSize").ShouldBeNull();
        Variable(card, "Alt", "Caption.Text").ShouldBeNull();
        Variable(card, "Default", "Caption.Width")!.Value.ShouldBe(55f);
    }

    [Fact]
    public void ChangingInstanceBaseType_RemovesTheInstanceVariableReferencesLinesTheNewTypeLacks()
    {
        ComponentSave card = Component("Card");
        AddReferences(card.DefaultState, "Caption.VariableReferences", "Text = Components/Label.Text", "Width = Components/Label.Width");

        ChangeInstanceBaseType(card, "Caption", "Container");

        Lines(card.DefaultState, "Caption.VariableReferences").ShouldBe(new[] { "Width = Components/Label.Width" });
    }

    [Fact]
    public void ChangingInstanceBaseType_KeepsTheVariablesTheTwoTypesShare()
    {
        ComponentSave pic = new ComponentSave { Name = "Pic", BaseType = "Container" };
        pic.States.Add(new StateSave { Name = "Default", ParentContainer = pic });
        _project.Components.Add(pic);
        _project.ComponentReferences.Add(new ElementReference { Name = "Pic", ElementType = ElementType.Component });
        pic.Instances.Add(new InstanceSave { Name = "Img", BaseType = "NineSlice", ParentContainer = pic });
        pic.DefaultState.Variables.Add(new VariableSave { Name = "Img.SourceFile", Type = "string", Value = "a.png", SetsValue = true });
        pic.DefaultState.Variables.Add(new VariableSave { Name = "Img.TextureTop", Type = "int", Value = 4, SetsValue = true });
        pic.DefaultState.Variables.Add(new VariableSave { Name = "Img.CustomFrameTextureCoordinateWidth", Type = "float?", Value = 9f, SetsValue = true });

        ChangeInstanceBaseType(pic, "Img", "Sprite");

        Variable(pic, "Default", "Img.SourceFile")!.Value.ShouldBe("a.png");
        Variable(pic, "Default", "Img.TextureTop")!.Value.ShouldBe(4);
        Variable(pic, "Default", "Img.CustomFrameTextureCoordinateWidth").ShouldBeNull();
    }

    [Fact]
    public void ChangingInstanceBaseType_ExposedVariableTheNewTypeLacks_GoesAwayLikeAnUnexposedOneAndClearsItsConsumers()
    {
        ComponentSave card = Component("Card");
        ComponentSave outer = Component("Outer");

        ChangeInstanceBaseType(card, "Caption", "Container");

        card.AllStates.SelectMany(state => state.Variables).ShouldNotContain(variable => variable.ExposedAsName == "CaptionText");
        Variable(outer, "Default", "Card1.CaptionText").ShouldBeNull();
    }

    [Fact]
    public void ChangingInstanceBaseType_LeavesAVariableReferencesLineThatReadsAPrunedExposedVariable()
    {
        // As with an exposed variable the user removed: the reading line stays, and the existing GUM0009 warning names it.
        ComponentSave reader = Component("Reader");
        ComponentSave card = Component("Card");

        ChangeInstanceBaseType(card, "Caption", "Container");

        Lines(reader.DefaultState, "VariableReferences").ShouldBe(new[] { LeftoverVariablesProject.ReferenceLine });
    }

    [Fact]
    public void Undo_OfElementBaseTypeChange_RestoresEveryPrunedValueInOneStep_AndRedoPrunesAgain()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ComponentSave label = Component("Label");
        ChangeElementBaseType(label, "Container");
        List<string> after = LeftoverVariablesProject.Snapshot(_project);
        after.ShouldNotBe(before);

        _undoManager.PerformUndo();

        label.BaseType.ShouldBe("Text");
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);

        _undoManager.PerformRedo();

        label.BaseType.ShouldBe("Container");
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(after);
    }

    [Fact]
    public void Undo_OfInstanceBaseTypeChange_RestoresThePrunedValuesLinesAndConsumersInOneStep_AndRedoPrunesAgain()
    {
        ComponentSave card = Component("Card");
        AddReferences(card.DefaultState, "Caption.VariableReferences", "Text = Components/Label.Text", "Width = Components/Label.Width");
        List<string> before = LeftoverVariablesProject.Snapshot(_project);
        ChangeInstanceBaseType(card, "Caption", "Container");
        List<string> after = LeftoverVariablesProject.Snapshot(_project);
        after.ShouldNotBe(before);

        _undoManager.PerformUndo();

        card.GetInstance("Caption")!.BaseType.ShouldBe("Text");
        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);

        _undoManager.PerformRedo();

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(after);
    }

    [Fact]
    public void SwitchingBackWithoutUndo_DoesNotBringThePrunedValuesBack()
    {
        ComponentSave label = Component("Label");
        ComponentSave card = Component("Card");

        ChangeElementBaseType(label, "Container");
        ChangeElementBaseType(label, "Text");
        ChangeInstanceBaseType(card, "Caption", "Container");
        ChangeInstanceBaseType(card, "Caption", "Text");

        Variable(label, "Default", "Text").ShouldBeNull();
        Variable(label, "Default", "FontSize").ShouldBeNull();
        Variable(label, "Big", "Text").ShouldBeNull();
        Variable(card, "Default", "Caption.Text").ShouldBeNull();
        Variable(card, "Alt", "Caption.Text").ShouldBeNull();
    }

    [Fact]
    public void ChangingBaseTypeToOneThatStillHasTheVariables_RemovesNothing()
    {
        List<string> before = LeftoverVariablesProject.Snapshot(_project);

        ChangeElementBaseType(Component("Label"), "Text");
        ChangeInstanceBaseType(Component("Card"), "Caption", "Text");

        LeftoverVariablesProject.Snapshot(_project).ShouldBe(before);
    }

    // Deliberate (#7191): a base type change prunes only the edited element. Inheritors keep their own values as hidden
    // leftovers and no check reports them. These pin that so nobody "fixes" it into a cascade.
    private ComponentSave AddInheritorOfLabel()
    {
        ComponentSave fancy = new ComponentSave { Name = "FancyLabel", BaseType = "Label" };
        fancy.States.Add(new StateSave { Name = "Default", ParentContainer = fancy });
        fancy.DefaultState!.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 99, SetsValue = true });
        fancy.DefaultState!.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 11f, SetsValue = true });
        StateSaveCategory fancyLooks = new StateSaveCategory { Name = "Fancy" };
        StateSave huge = new StateSave { Name = "Huge", ParentContainer = fancy };
        huge.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 120, SetsValue = true });
        fancyLooks.States.Add(huge);
        fancy.Categories.Add(fancyLooks);
        _project.Components.Add(fancy);
        _project.ComponentReferences.Add(new ElementReference { Name = "FancyLabel", ElementType = ElementType.Component });
        return fancy;
    }

    [Fact]
    public void ChangingElementBaseType_DoesNotPruneComponentsThatInheritFromIt()
    {
        ComponentSave label = Component("Label");
        ComponentSave fancy = AddInheritorOfLabel();

        ChangeElementBaseType(label, "Container");

        Variable(label, "Default", "FontSize").ShouldBeNull();
        Variable(fancy, "Default", "FontSize")!.Value.ShouldBe(99);
        Variable(fancy, "Huge", "FontSize")!.Value.ShouldBe(120);
        Variable(fancy, "Default", "Width")!.Value.ShouldBe(11f);
    }

    [Fact]
    public void UndoAndRedo_OfElementBaseTypeChange_LeaveInheritorsUntouched()
    {
        ComponentSave label = Component("Label");
        ComponentSave fancy = AddInheritorOfLabel();
        ChangeElementBaseType(label, "Container");

        _undoManager.PerformUndo();
        Variable(fancy, "Default", "FontSize")!.Value.ShouldBe(99);
        Variable(fancy, "Huge", "FontSize")!.Value.ShouldBe(120);

        _undoManager.PerformRedo();
        Variable(fancy, "Default", "FontSize")!.Value.ShouldBe(99);
        Variable(fancy, "Huge", "FontSize")!.Value.ShouldBe(120);
    }

    [Fact]
    public void ChangingElementBaseType_AddsNoErrorOrWarningForInheritors()
    {
        ComponentSave label = Component("Label");
        ComponentSave fancy = AddInheritorOfLabel();
        HeadlessErrorChecker checker = new HeadlessErrorChecker(new DefaultTypeResolver()) { ReportsLeftoverInstanceVariables = true };
        List<string> before = checker.GetErrorsFor(fancy, _project).Select(e => e.Code + e.Message).ToList();

        ChangeElementBaseType(label, "Container");

        checker.GetErrorsFor(fancy, _project).Select(e => e.Code + e.Message).ToList().ShouldBe(before);
    }
}
