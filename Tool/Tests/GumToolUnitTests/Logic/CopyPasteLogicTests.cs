using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Logic;
public class CopyPasteLogicTests : BaseTestClass
{
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;

    StateSaveCategory selectedCategory = new();
    StateSave selectedStateSave = new();
    ComponentSave selectedComponent = new();

    private readonly AutoMocker mocker;
    public CopyPasteLogicTests()
    {
        mocker = new ();

        _copyPasteLogic = mocker.CreateInstance<CopyPasteLogic>();

        _selectedState = mocker.GetMock<ISelectedState>();
        _elementCommands = mocker.GetMock<IElementCommands>();


        selectedStateSave.Name = "CopiedState";
        selectedComponent.BaseType = "Sprite";
        selectedComponent.States.Add(new StateSave
        {
            Name = "Default",
            ParentContainer = selectedComponent
        });

        Mock<PluginManager> pluginManager = mocker.GetMock<PluginManager>();
        pluginManager
            .Setup(x => x.InstanceAdd(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()))
            .Callback(() =>
            {

            });

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();

        selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(selectedCategory);

        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(selectedStateSave);

        selectedState
            .Setup(x => x.SelectedElement)
            .Returns(selectedComponent);

        selectedState
            .Setup(x => x.SelectedElements)
            .Returns(new List<ElementSave>() { selectedComponent });

        var gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = gumProject;

        StandardElementSave spriteElement = new StandardElementSave();
        spriteElement.Name = "Sprite";

        StateSave spriteDefaultState = new()
        {
            Name = "Default",
            ParentContainer = spriteElement
        };

        spriteElement.States.Add(spriteDefaultState);
        gumProject.StandardElements.Add(spriteElement);
    }

    [Fact(Skip = "Need WireframeObjectManager to take dependencies in constructor")]
    public void OnPaste_ShouldCreateOneUndo_ForMultiplePastedObjects()
    {
        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave>
            {
                new InstanceSave
                {
                    Name = "Instance1"
                },
                new InstanceSave
                {
                    Name = "Instance2"
                }
            });

        Mock<IUndoManager> undoManager = mocker.GetMock<IUndoManager>();

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        undoManager
            .Verify(x => x.RequestLock(), Times.Once);
    }

    [Fact]
    public void OnPaste_State_ShouldAddStateToCategory()
    {
        StateSaveCategory category = new StateSaveCategory();
        StateSave selectedStateSave = new StateSave();
        ComponentSave component = new ComponentSave();

        var selectedState = mocker.GetMock<ISelectedState>();

        selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(category);

        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(selectedStateSave);

        selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);

        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        category.States.Count.ShouldBe(1);
    }

    [Fact]
    public void OnPaste_State_ShouldSelectNewState()
    {
        StateSaveCategory category = new ();
        StateSave selectedStateSave = new ();
        selectedStateSave.Name = "CopiedState";
        ComponentSave component = new ();

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();

        selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(category);

        selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(selectedStateSave);

        selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);

        StateSave existingState = new();
        existingState.Name = "ExistingState";
        existingState.SetValue("X", 10f, "float");
        category.States.Add(existingState);

        _copyPasteLogic.OnCopy(CopyType.State);

        var wasCalled = false;
        selectedState
            .SetupSet(x => x.SelectedStateSave = It.IsAny<StateSave>())
            .Callback((StateSave newState) =>
            {
                newState.Name.ShouldBe("CopiedState");
                wasCalled = true;
            });
        _copyPasteLogic.OnPaste(CopyType.State);


        wasCalled.ShouldBeTrue();
        // We cannot check if variables have propagated on new state because
        // the plugin handles that. See unit tests for MainStatePlugin

    }

    [Fact]
    public void OnPaste_State_ShouldNotPaste_IfCopiedStateHasExtraVariables()
    {
        selectedStateSave.SetValue("Y", 5f, "float");
        StateSave existingState = new();
        existingState.Name = "ExistingState";
        existingState.SetValue("X", 10f, "float");
        selectedCategory.States.Add(existingState);


        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        selectedCategory.States.Count.ShouldBe(1, 
            "because the paste should not be allowed since the pasted state " +
            "sets the Y value which is not already set in other states in the " +
            "target category.");
    }

    [Fact]
    public void OnPaste_State_ShouldNotPaste_IfCopiedStateHasUnsupportedVariables()
    {
        selectedStateSave.SetValue("BadVariable", 5f, "float");
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        selectedCategory.States.Count.ShouldBe(0,
            "because the paste should not be allowed since the pasted state " +
            "sets the BadVariable which doesn't exist on the component");

    }

    [Fact]
    public void OnPaste_ShouldPaste_IfExposedVariableIsSet()
    {
        var variable = new VariableSave
        {
            Name = "Instance.X",
            ExposedAsName = "InstanceX",
            Type = "float"
        };
        selectedComponent.DefaultState.Variables.Add(variable);

        selectedCategory.States.Clear();

        selectedStateSave.SetValue("InstanceX", 5f, "float");
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        selectedCategory.States.Count.ShouldBe(1);
    }

    [Fact]
    public void OnPaste_ShouldPaste_IfCopiedStateHasExtraVariables_InEmptyCategory()
    {
        selectedCategory.States.Clear();

        selectedStateSave.SetValue("BadVariable", 5f, "float");
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        selectedCategory.States.Count.ShouldBe(0,
            "because the paste should not be allowed since the pasted state " +
            "sets the BadVariable which doesn't exist on the component");
    }

    [Fact(Skip = "Need WireframeObjectManager to inject its ISelectedState")]
    public void OnPaste_ShouldSortVariables()
    {
        var element = new ScreenSave();
        element.States.Add(new Gum.DataTypes.Variables.StateSave());

        var instance = new InstanceSave();
        element.Instances.Add(instance);
        instance.ParentContainer = element;
        instance.Name = "Instance1";

        _selectedState
            .Setup(x => x.SelectedInstance).Returns(instance);
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave> { instance });

        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        _selectedState.Setup(x => x.SelectedStateSave).Returns(element.DefaultState);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _elementCommands.Verify(x => x.SortVariables(It.IsAny<ElementSave>()), Times.Once);
    }
}
