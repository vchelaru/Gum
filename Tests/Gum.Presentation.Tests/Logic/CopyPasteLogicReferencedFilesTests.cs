using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// A pasted category or state can carry a variable that names a file, so plugins are told about it the
/// way they are for an added one: the File Watch plugin forgets which files the project references (#7039).
/// </summary>
public class CopyPasteLogicReferencedFilesTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new AutoMocker();
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly ComponentSave _component;
    private readonly StateSaveCategory _category;
    private readonly StateSave _state;

    public CopyPasteLogicReferencedFilesTests()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        _component = new ComponentSave { Name = "Card" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(defaultState);
        _component.Instances.Add(new InstanceSave { Name = "Item", BaseType = "Sprite", ParentContainer = _component });
        defaultState.Variables.Add(new VariableSave
        {
            Name = "Item.SourceFile", Type = "string", Value = "Textures/Default.png", IsFile = true, SetsValue = true,
        });

        _category = new StateSaveCategory { Name = "Mood" };
        _state = new StateSave { Name = "Happy", ParentContainer = _component };
        _state.Variables.Add(new VariableSave
        {
            Name = "Item.SourceFile", Type = "string", Value = "Textures/Happy.png", IsFile = true, SetsValue = true,
        });
        _category.States.Add(_state);
        _component.Categories.Add(_category);

        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElement).Returns(_component);
        selectedState.Setup(x => x.SelectedStateCategorySave).Returns(_category);
        selectedState.Setup(x => x.SelectedStateSave).Returns(_state);
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());

        _mocker.Use(new Lazy<IElementTreeRoots>(() => _mocker.GetMock<IElementTreeRoots>().Object));
        _mocker.Use<ICircularReferenceManager>(new CircularReferenceManager(ObjectFinder.Self));
        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();
    }

    [Fact]
    public void PastingACategory_TellsPluginsACategoryWasAdded_AfterItIsInTheElement()
    {
        bool wasInElementWhenTold = false;
        _mocker.GetMock<ICopyPastePluginNotifier>()
            .Setup(x => x.CategoryAdd(It.IsAny<StateSaveCategory>()))
            .Callback<StateSaveCategory>(category => wasInElementWhenTold = _component.Categories.Contains(category));
        _copyPasteLogic.OnCopy(CopyType.Category);

        _copyPasteLogic.OnPaste(CopyType.Category);

        _component.Categories.Count.ShouldBe(2);
        _mocker.GetMock<ICopyPastePluginNotifier>().Verify(
            x => x.CategoryAdd(It.Is<StateSaveCategory>(category => category != _category)), Times.Once);
        wasInElementWhenTold.ShouldBeTrue();
    }

    [Fact]
    public void PastingAState_TellsPluginsAStateWasAdded_AfterItIsInTheCategory()
    {
        bool wasInCategoryWhenTold = false;
        _mocker.GetMock<ICopyPastePluginNotifier>()
            .Setup(x => x.StateAdd(It.IsAny<StateSave>()))
            .Callback<StateSave>(state => wasInCategoryWhenTold = _category.States.Contains(state));
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        _category.States.Count.ShouldBe(2);
        _mocker.GetMock<ICopyPastePluginNotifier>().Verify(
            x => x.StateAdd(It.Is<StateSave>(state => state != _state)), Times.Once);
        wasInCategoryWhenTold.ShouldBeTrue();
    }

    [Fact]
    public void APasteTheValidationRefuses_DoesNotTellPluginsAStateWasAdded()
    {
        // The category's states set no variable the copied state sets, so the paste is refused.
        StateSave copied = new StateSave { Name = "Other", ParentContainer = _component };
        copied.Variables.Add(new VariableSave { Name = "Item.Width", Type = "float", Value = 5f, SetsValue = true });
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(copied);
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        _mocker.GetMock<ICopyPastePluginNotifier>().Verify(x => x.StateAdd(It.IsAny<StateSave>()), Times.Never);
    }
}
