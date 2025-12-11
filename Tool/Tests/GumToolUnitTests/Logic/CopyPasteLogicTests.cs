using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using SharpVectors.Dom;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace GumToolUnitTests.Logic;
public class CopyPasteLogicTests : BaseTestClass
{
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly WeakReferenceMessenger _messenger;

    StateSaveCategory selectedCategory = new();
    StateSave selectedStateSave = new();
    ComponentSave selectedComponent = new();

    private readonly AutoMocker mocker;
    public CopyPasteLogicTests()
    {
        mocker = new ();

        // Replace the mocked IMessenger with a real instance
        _messenger = new WeakReferenceMessenger();
        mocker.Use<IMessenger>(_messenger);

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
        pluginManager.Object.Plugins = new List<PluginBase>();

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

    [Fact]
    public void OnPaste_Instance_ShouldCreateOneUndo_ForMultiplePastedObjects()
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
    public void OnPaste_State_ShouldPaste_IfExposedVariableIsSet()
    {
        VariableSave variable = new ()
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
    public void OnPaste_State_ShouldPaste_IfCopiedStateHasExtraVariables_InEmptyCategory()
    {
        selectedCategory.States.Clear();

        selectedStateSave.SetValue("BadVariable", 5f, "float");
        _copyPasteLogic.OnCopy(CopyType.State);

        _copyPasteLogic.OnPaste(CopyType.State);

        selectedCategory.States.Count.ShouldBe(0,
            "because the paste should not be allowed since the pasted state " +
            "sets the BadVariable which doesn't exist on the component");
    }

    [Fact]
    public void OnPaste_Instance_ShouldSortVariables()
    {
        ScreenSave element = CreateDefaultScreen();

        SelectInstances(new List<InstanceSave> { element.Instances.First() });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _elementCommands.Verify(x => x.SortVariables(It.IsAny<ElementSave>()), Times.Once);
    }

    [Fact]
    public void OnPaste_Instance_ShouldPastSibling_IfNoSelectionMade()
    {
        ScreenSave screen = CreateDefaultScreen();

        var firstInstance = screen.Instances[0];
        SelectInstances(new List<InstanceSave>() { firstInstance });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(2);

        screen.Instances[0].ShouldBe(firstInstance);
    }

    [Fact]
    public void OnPaste_Instance_ShuldPasteAfterSelection()
    {
        /*
         * Parent
            * Child1
            * Child1Copy <--- pasted
            * Child 2
         */

        ScreenSave screen = CreateDefaultScreen();

        var firstInstance = screen.Instances[0];
        SelectInstances(new List<InstanceSave>() { firstInstance });

        // Adding another instance to ensure paste goes after selection
        InstanceSave secondInstance = new();
        screen.Instances.Add(secondInstance);
        secondInstance.ParentContainer = screen;
        secondInstance.Name = "Instance2";

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(3);

        screen.Instances[0].ShouldBe(firstInstance);
        // [1] is the pasted instance
        screen.Instances[2].ShouldBe(secondInstance);
    }

    [Fact]
    public void OnPaste_MultipleInstances_ShouldPasteAfterSelection()
    {
        /*
         * Parent
            * Instance1
            * Instance2
            * Instance3 <--- pasted
            * Instance4 <--- pasted
         */

        ScreenSave screen = CreateDefaultScreen();

        var firstInstance = screen.Instances[0];

        // Adding another instance to copy
        InstanceSave secondInstance = new();
        screen.Instances.Add(secondInstance);
        secondInstance.ParentContainer = screen;
        secondInstance.Name = "Instance2";

        SelectInstances(new List<InstanceSave> { firstInstance, secondInstance });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);
        screen.Instances[0].ShouldBe(firstInstance);
        screen.Instances[1].ShouldBe(secondInstance);
        screen.Instances[2].ShouldNotBe(firstInstance);
        screen.Instances[3].ShouldNotBe(secondInstance);
    }

    [Fact]
    public void OnPaste_MultipleInstancesWithDifferentParents_ShouldPasteInCorrectParents()
    {
        /*
         * Instance1
            * ChildOfFirst
            * ChildOfFirst1 <--- pasted
         * Instance2
            * ChildOfSecond
            * ChildOfSecond1 <--- pasted
         */
        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];


        // Adding another instance to copy
        InstanceSave secondInstance = new();
        screen.Instances.Add(secondInstance);
        secondInstance.ParentContainer = screen;
        secondInstance.Name = "Instance2";

        // adding a child to the first:
        InstanceSave childOfFirst = new();
        screen.Instances.Add(childOfFirst);
        childOfFirst.ParentContainer = screen;
        childOfFirst.Name = "ChildOfFirst";
        screen.DefaultState.SetValue("ChildOfFirst.Parent", "Instance1", "string");

        // adding a child to the second:
        InstanceSave childOfSecond = new();
        screen.Instances.Add(childOfSecond);
        childOfSecond.ParentContainer = screen;
        childOfSecond.Name = "ChildOfSecond";
        screen.DefaultState.SetValue("ChildOfSecond.Parent", "Instance2", "string");


        SelectInstances(new List<InstanceSave> { childOfFirst, childOfSecond });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        
        screen.Instances.Count.ShouldBe(6);
        // first two are the original instances
        screen.Instances[0].ShouldBe(screen.Instances[0]);
        screen.Instances[1].ShouldBe(screen.Instances[1]);

        var indexOfInstance1 = screen.Instances.IndexOf(instance1);
        var childOfFirstCopy = screen.Instances.Find(item => item.Name == "ChildOfFirst1");

        var childOfSecondCopy =
            screen.Instances.Find(item => item.Name == "ChildOfSecond1");

        screen.Instances.IndexOf(childOfFirstCopy).ShouldBeGreaterThan(
            screen.Instances.IndexOf(instance1));

        screen.Instances.IndexOf(childOfSecondCopy).ShouldBeGreaterThan(
            screen.Instances.IndexOf(secondInstance));

        screen.DefaultState.GetValue("ChildOfFirst1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("ChildOfSecond1.Parent").ShouldBe("Instance2");

    }

    [Fact]
    public void OnPaste_MultipleInstances_ShouldPasteDirectlyAfterCopy()
    {
        /*
         * Instance1
            * ChildA
            * ChildA1 <--- pasted
            * ChildB
         * Instance2
            * ChildC
            * ChildC1 <--- pasted
            * ChildD
         */



        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        // Adding another instance to copy
        InstanceSave secondInstance = new();
        screen.Instances.Add(secondInstance);
        secondInstance.ParentContainer = screen;
        secondInstance.Name = "Instance2";

        // adding a child to the first:
        InstanceSave childA = AddChild("ChildA", "Instance1");
        InstanceSave childB = AddChild("ChildB", "Instance1");
        InstanceSave childC = AddChild("ChildC", "Instance2");
        InstanceSave childD = AddChild("ChildD", "Instance2");

        SelectInstances(new List<InstanceSave> { childA, childC });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(8);


        var childA1 = screen.Instances.Find(item => item.Name == "ChildA1");
        var childC1 = screen.Instances.Find(item => item.Name == "ChildC1");

        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("ChildC1.Parent").ShouldBe("Instance2");

        IndexOf(childA).ShouldBeLessThan(IndexOf(childA1));
        IndexOf(childA1).ShouldBeLessThan(IndexOf(childB));

        IndexOf(childC).ShouldBeLessThan(IndexOf(childC1));
        IndexOf(childC1).ShouldBeLessThan(IndexOf(childD));

        int IndexOf(InstanceSave instance) => screen.Instances.IndexOf(instance);

        InstanceSave AddChild(string childName, string parentName)
        {
            InstanceSave child = new();
            screen.Instances.Add(child);
            child.ParentContainer = screen;
            child.Name = childName;
            screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");
            return child;
        }
    }

    [Fact]
    public void OnPaste_InstanceWithChild_ShouldCreateHierarchy()
    {
        /*
          
         * Instance1
            * Child
                * Grandchild
            * Child1 <--- pasted
                * Grandchild1 <--- pasted (automatic) 
         */


        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];


        // adding a child to the first:
        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave grandchild = AddChild("Grandchild", "Child");

        SelectInstances(new [] { child });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(5);

        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild1.Parent").ShouldBe("Child1");

        InstanceSave AddChild(string childName, string parentName)
        {
            InstanceSave child = new ();
            screen.Instances.Add(child);
            child.ParentContainer = screen;
            child.Name = childName;
            screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");
            return child;
        }
    }

    [Fact]
    public void OnPaste_MultipleInstances_WithNested_ShouldParentCorrectly()
    {
        /*
        
        Instance1
            CopiedChild
                CopiedGrandchild
            CopiedChild1 <--- pasted
                CopiedGrandchild1 <--- pasted, child of copied object
            ChildA
                CopiedGrandchildA
                CopiedGrandchildA1 <-- pasted
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave copiedChild = AddChild("CopiedChild", "Instance1");
        InstanceSave copiedGrandchild = AddChild("CopiedGrandchild", "CopiedChild");

        InstanceSave childA = AddChild("ChildA", "Instance1");
        InstanceSave copiedGrandchildA = AddChild("CopiedGrandchildA", "ChildA");

        SelectInstances(new[] { copiedChild, copiedGrandchildA });


        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(8);

        InstanceSave copiedGrandchildA1 = screen.Instances.Find(item => item.Name == "CopiedGrandchildA1")!;
        InstanceSave copiedChild1 = screen.Instances.Find(item => item.Name == "CopiedChild1")!;
        InstanceSave copiedGrandchild1 = screen.Instances.Find(item => item.Name == "CopiedGrandchild1")!;

        IndexOf(copiedChild).ShouldBeLessThan(IndexOf(copiedChild1));
        IndexOf(copiedChild1).ShouldBeLessThan(IndexOf(childA));
        IndexOf(copiedGrandchildA).ShouldBeLessThan(IndexOf(copiedGrandchildA1));

        screen.DefaultState.GetValue("CopiedGrandchildA1.Parent").ShouldBe("ChildA");
        screen.DefaultState.GetValue("CopiedChild1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("CopiedGrandchild1.Parent").ShouldBe("CopiedChild1");



        int IndexOf(InstanceSave instance) => screen.Instances.IndexOf(instance);

        InstanceSave AddChild(string childName, string parentName)
        {
            InstanceSave child = new();
            screen.Instances.Add(child);
            child.ParentContainer = screen;
            child.Name = childName;
            screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");
            return child;
        }
    }

    [Fact]
    public void OnPaste_Instance_AfterSelection_ShouldAddToSelection()
    {
        /*
         
        Instance1
            Child
            SelectedChild
                Child1 <--- pasted
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave selectedChild = AddChild("SelectedChild", "Instance1");

        SelectInstances(new[] { child });

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(new[] { selectedChild });

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        var pastedInstance = screen.Instances.Find(item => item.Name == "Child1")!;
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("SelectedChild");

        InstanceSave AddChild(string childName, string parentName)
        {
            InstanceSave child = new();
            screen.Instances.Add(child);
            child.ParentContainer = screen;
            child.Name = childName;
            screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");
            return child;
        }
    }

    #region Utilities

    private ScreenSave CreateDefaultScreen()
    {
        ScreenSave element = new ();
        var defaultState = new Gum.DataTypes.Variables.StateSave();
        defaultState.ParentContainer = element;
        element.States.Add(defaultState);

        InstanceSave instance = new();
        element.Instances.Add(instance);
        instance.ParentContainer = element;
        instance.Name = "Instance1";

        return element;
    }

    private void SelectInstances(IEnumerable<InstanceSave> instances)
    {
        _selectedState
            .Setup(x => x.SelectedInstance)
            .Returns(instances.First());
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(instances);

        var firstInstance = instances.First();
        var parentElement = firstInstance.ParentContainer!;
        _selectedState.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { parentElement });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(parentElement.DefaultState);


        _messenger.Send(new SelectionChangedMessage { /* properties */ });
    }

    #endregion
}
