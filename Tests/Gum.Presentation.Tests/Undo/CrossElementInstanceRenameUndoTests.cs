using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Undo;

/// <summary>
/// Undo/redo of an instance rename that rewrites variable reference lines and Parent values on OTHER
/// elements (#5248). Runs the real <see cref="RenameLogic"/>, <see cref="ReferenceFinder"/> and
/// <see cref="UndoManager"/>, so the lock the rename takes, the cross-element rewrite and the undo
/// replay are exercised together.
/// </summary>
public class CrossElementInstanceRenameUndoTests : BaseTestClass
{
    private readonly GumProjectSave _project;
    private readonly UndoManager _undoManager;
    private readonly RenameLogic _renameLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IFileCommands> _fileCommands;

    public CrossElementInstanceRenameUndoTests()
    {
        AutoMocker mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        mocker.GetMock<IRenameProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);

        _selectedState = mocker.GetMock<ISelectedState>();
        _selectedState.SetupProperty(x => x.SelectedStateSave);
        _selectedState.SetupProperty(x => x.SelectedStateCategorySave);

        _fileCommands = mocker.GetMock<IFileCommands>();

        string? whyNotValid;
        mocker.GetMock<INameVerifier>()
            .Setup(x => x.IsInstanceNameValid(It.IsAny<string>(), It.IsAny<InstanceSave?>(), It.IsAny<IInstanceContainer?>(), out whyNotValid))
            .Returns(true);

        _undoManager = mocker.CreateInstance<UndoManager>();
        mocker.Use<IUndoManager>(_undoManager);
        mocker.Use(new Lazy<IUndoManager>(() => _undoManager));
        mocker.Use<IReferenceFinder>(mocker.CreateInstance<ReferenceFinder>());

        _renameLogic = mocker.CreateInstance<RenameLogic>();
    }

    private ComponentSave AddComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        _project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static InstanceSave AddInstance(ElementSave element, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element };
        element.Instances.Add(instance);
        return instance;
    }

    private static VariableListSave<string> AddReferences(StateSave state, string listName, params string[] lines)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = listName, Type = "string" };
        list.Value.AddRange(lines);
        state.VariableLists.Add(list);
        return list;
    }

    private static List<string> Lines(ElementSave element, string listName) =>
        element.DefaultState.GetVariableListSave(listName).ValueAsIList.Cast<string>().ToList();

    /// <summary>Selects Button and captures the undo baseline as UndoPlugin would, then renames
    /// Button's Background instance to Bg as the F2 and Variables tab paths do.</summary>
    private void RenameBackgroundToBg(ComponentSave button)
    {
        _selectedState.Setup(x => x.SelectedElement).Returns(button);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(button);
        _selectedState.Setup(x => x.SelectedComponent).Returns(button);
        _selectedState.Object.SelectedStateSave = button.DefaultState;
        _undoManager.RecordState();

        InstanceSave background = button.GetInstance("Background")!;
        background.Name = "Bg";
        _renameLogic.HandleRename(button, background, "Background", NameChangeAction.Rename, askAboutRename: false);
    }

    [Fact]
    public void HandleRename_Undo_ShouldRestoreReferenceLinesAndParentsInOtherElements()
    {
        ComponentSave button = AddComponent("Button");
        AddInstance(button, "Background", "NineSlice");
        ComponentSave panel = AddComponent("Panel");
        AddInstance(panel, "MyButton", "Button");
        AddInstance(panel, "Icon", "Sprite");
        AddReferences(panel.DefaultState, "VariableReferences", "Height = Components/Button.Background.Width");
        panel.DefaultState.Variables.Add(new VariableSave { Name = "Icon.Parent", Type = "string", Value = "MyButton.Background", SetsValue = true });

        RenameBackgroundToBg(button);
        Lines(panel, "VariableReferences").ShouldBe(new[] { "Height = Components/Button.Bg.Width" });
        panel.DefaultState.GetValue("Icon.Parent").ShouldBe("MyButton.Bg");

        _undoManager.PerformUndo();

        button.Instances.Select(item => item.Name).ShouldBe(new[] { "Background" });
        Lines(panel, "VariableReferences").ShouldBe(new[] { "Height = Components/Button.Background.Width" });
        panel.DefaultState.GetValue("Icon.Parent").ShouldBe("MyButton.Background");
        _fileCommands.Verify(x => x.TryAutoSaveElement(panel), Times.AtLeast(2));

        _undoManager.PerformRedo();

        Lines(panel, "VariableReferences").ShouldBe(new[] { "Height = Components/Button.Bg.Width" });
        panel.DefaultState.GetValue("Icon.Parent").ShouldBe("MyButton.Bg");
    }

    [Fact]
    public void HandleRename_Undo_ShouldRestoreInstanceReferenceLinesInDerivedElement()
    {
        ComponentSave button = AddComponent("Button");
        AddInstance(button, "Background", "NineSlice");
        ComponentSave bigButton = AddComponent("BigButton");
        bigButton.BaseType = "Button";
        AddInstance(bigButton, "Label", "Text");
        AddReferences(bigButton.DefaultState, "Label.VariableReferences", "Width=Background.Width", "Height=Background.Height");

        RenameBackgroundToBg(button);
        Lines(bigButton, "Label.VariableReferences").ShouldBe(new[] { "Width=Bg.Width", "Height=Bg.Height" });

        _undoManager.PerformUndo();

        Lines(bigButton, "Label.VariableReferences").ShouldBe(new[] { "Width=Background.Width", "Height=Background.Height" });
    }

    [Fact]
    public void HandleRename_Undo_ShouldNotOverwriteLinesEditedSinceTheRename()
    {
        ComponentSave button = AddComponent("Button");
        AddInstance(button, "Background", "NineSlice");
        ComponentSave panel = AddComponent("Panel");
        VariableListSave<string> references = AddReferences(panel.DefaultState, "VariableReferences", "Height = Components/Button.Background.Width");

        RenameBackgroundToBg(button);
        references.Value[0] = "Height = 40";

        _undoManager.PerformUndo();

        Lines(panel, "VariableReferences").ShouldBe(new[] { "Height = 40" });
    }
}
