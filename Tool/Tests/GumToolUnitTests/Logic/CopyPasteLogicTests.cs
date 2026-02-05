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
using System.Windows.Forms;
using System.Windows.Markup;

namespace GumToolUnitTests.Logic;
public class CopyPasteLogicTests : BaseTestClass
{
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly WeakReferenceMessenger _messenger;

    StateSave selectedStateSave = new();
    ComponentSave selectedComponent = new();


    private readonly AutoMocker _mocker;
    public CopyPasteLogicTests()
    {
        _mocker = new ();

        // Replace the mocked IMessenger with a real instance
        _messenger = new WeakReferenceMessenger();
        _mocker.Use<IMessenger>(_messenger);

        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();

        _selectedState = _mocker.GetMock<ISelectedState>();
        _elementCommands = _mocker.GetMock<IElementCommands>();


        selectedStateSave.Name = "CopiedState";
        selectedComponent.BaseType = "Sprite";
        selectedComponent.States.Add(new StateSave
        {
            Name = "Default",
            ParentContainer = selectedComponent
        });

        Mock<PluginManager> pluginManager = _mocker.GetMock<PluginManager>();
        pluginManager
            .Setup(x => x.InstanceAdd(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()))
            .Callback(() =>
            {

            });
        pluginManager.Object.Plugins = new List<PluginBase>();

        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();

        selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns((StateSaveCategory?)null);

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

        StandardElementsManager.Self.Initialize();

        spriteElement.States.Add(spriteDefaultState);
        gumProject.StandardElements.Add(spriteElement);
    }

    [Fact]
    public void OnCopy_MultipleInstances_ShouldStoreOnlyOneState()
    {
        ScreenSave screen = CreateDefaultScreen();

        AddChild("Child2", "");

        SelectInstances(screen.Instances[0], screen.Instances[1]);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.CopiedData.CopiedStates.Count.ShouldBe(1, "Because both instances are in the same screen");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_Instance_ShouldCreateOneUndo_ForMultiplePastedObjects()
    {
        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();

        ComponentSave component = new();
        component.States.Add(new StateSave());

        selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave>
            {
                new InstanceSave
                {
                    Name = "Instance1",
                    ParentContainer = component
                },
                new InstanceSave
                {
                    Name = "Instance2",
                    ParentContainer = component
                }
            });

        Mock<IUndoManager> undoManager = _mocker.GetMock<IUndoManager>();

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

        var selectedState = _mocker.GetMock<ISelectedState>();

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

        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();

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
        StateSaveCategory selectedCategory = new();

        _selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(selectedCategory);

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
        StateSaveCategory selectedCategory = new();

        _selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(selectedCategory);

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
        StateSaveCategory selectedCategory = new();

        _selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(selectedCategory);

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
        StateSaveCategory selectedCategory = new();

        _selectedState
            .Setup(x => x.SelectedStateCategorySave)
            .Returns(selectedCategory);

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

        SelectInstances(element.Instances.First());

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _elementCommands.Verify(x => x.SortVariables(It.IsAny<ElementSave>()), Times.Once);
    }

    [Fact]
    public void OnPaste_Instance_ShouldPastaSibling_IfNoSelectionMade()
    {
        ScreenSave screen = CreateDefaultScreen();

        var firstInstance = screen.Instances[0];
        SelectInstances(firstInstance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(2);

        screen.Instances[0].ShouldBe(firstInstance);
    }

    [Fact] 
    public void OnPaste_Instance_ShouldPasteAtEnd_IfInElementWithSameNamedInstance()
    {
        /*
         * Screen1
            * Instance1
            * ExistingInstance
            * Instance2 <--- pasted
         * Screen2
            * Instance1 <--- copied
         */

        ScreenSave screen1 = CreateDefaultScreen();
        screen1.Name = "Screen1";
        screen1.Instances[0].Name = "Instance1";
        InstanceSave existingInstance = AddChild("ExistingInstance", null, screen1);

        ScreenSave screen2 = CreateDefaultScreen();
        screen2.Name = "Screen2";

        SelectInstances(screen2.Instances[0]);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectElement(screen1);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen1.Instances[2].Name.ShouldBe("Instance2");
        
    }

    [Fact(Skip ="This requires either a full SelectedState, or lots of mocking behavior which is a pain")]
    public void OnPaste_Instance_MultipleTimes_ShouldPasteSiblingAtEnd()
    {
        ScreenSave screen = CreateDefaultScreen();
        var firstInstance = screen.Instances[0];

        SelectInstances(firstInstance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        var firstPasted = screen.Instances[1];

        // simulate the _selectedState getting this selection

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(3);
        screen.Instances[0].ShouldBe(firstInstance);
        screen.Instances[1].ShouldBe(firstPasted);

    }

    [Fact]
    public void OnPaste_Instance_ShouldPasteAfterSelection()
    {
        /*
         * Parent
            * Child1
            * Child1Copy <--- pasted
            * Child 2
         */

        ScreenSave screen = CreateDefaultScreen();

        var firstInstance = screen.Instances[0];
        SelectInstances(firstInstance);

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

        SelectInstances(firstInstance, secondInstance);

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


        SelectInstances(childOfFirst, childOfSecond);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        
        screen.Instances.Count.ShouldBe(6);
        // first two are the original instances
        screen.Instances[0].ShouldBe(screen.Instances[0]);
        screen.Instances[1].ShouldBe(screen.Instances[1]);

        var indexOfInstance1 = screen.Instances.IndexOf(instance1);
        var childOfFirstCopy = screen.Instances.Find(item => item.Name == "ChildOfFirst1")!;

        var childOfSecondCopy =
            screen.Instances.Find(item => item.Name == "ChildOfSecond1")!;

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

        SelectInstances(childA, childC);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(8);


        var childA1 = screen.Instances.Find(item => item.Name == "ChildA1")!;
        var childC1 = screen.Instances.Find(item => item.Name == "ChildC1")!;

        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("ChildC1.Parent").ShouldBe("Instance2");

        IndexOf(childA).ShouldBeLessThan(IndexOf(childA1));
        IndexOf(childA1).ShouldBeLessThan(IndexOf(childB));

        IndexOf(childC).ShouldBeLessThan(IndexOf(childC1));
        IndexOf(childC1).ShouldBeLessThan(IndexOf(childD));

        int IndexOf(InstanceSave instance) => screen.Instances.IndexOf(instance);

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
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

        SelectInstances(child);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(5);

        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild1.Parent").ShouldBe("Child1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
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

        SelectInstances(copiedChild, copiedGrandchildA);


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

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
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

        SelectInstances(child);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(selectedChild);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        var pastedInstance = screen.Instances.Find(item => item.Name == "Child1")!;
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("SelectedChild");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_Instance_AfterSelection_ShouldAddToEndOfChildren()
    {
        /*
         SelectedParent
            ChildA  (copied)
            ChildB
            ChildA1 <-- pasted 
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave selectedParent = screen.Instances[0];
        selectedParent.Name = "SelectedParent";

        InstanceSave childA = AddChild("ChildA", "SelectedParent");
        InstanceSave childB = AddChild("ChildB", "SelectedParent");

        SelectInstances(childA);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        SelectInstances(selectedParent);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);


        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("SelectedParent");

        var childA1 = screen.Instances.Find(item => item.Name == "ChildA1")!;
        IndexOf(childA1).ShouldBeGreaterThan(IndexOf(childB));

        int IndexOf(InstanceSave instance) => screen.Instances.IndexOf(instance);

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_MultipleInstances_InDifferentInstances_ShouldPasteInCorrectParents()
    {
        /*
        ParentA
            ChildA (copied)
        ParentB
            ChildB (copied)
        SelectedParent
            ChildA1 <--- pasted
            ChildB1 <--- pasted
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave parentA = screen.Instances[0];
        parentA.Name = "ParentA";

        InstanceSave childA = AddChild("ChildA", "ParentA");

        InstanceSave parentB = AddChild("ParentB", "");
        InstanceSave childB = AddChild("ChildB", "Parent2");

        InstanceSave selectedParent = AddChild("SelectedParent", "");

        SelectInstances(childA, childB);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(selectedParent);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("SelectedParent");
        screen.DefaultState.GetValue("ChildB1.Parent").ShouldBe("SelectedParent");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_InstanceWitHierarchy_InDiffInstance_ShouldPasteWithHierarchy()
    {
        /*
         ParentA
            ChildACopied
                GrandchildACopied
            ChildB
                ChildACopied1 <--- pasted
                    GrandchildACopied1 <--- pasted
        */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave parentA = screen.Instances[0];
        parentA.Name = "ParentA";

        InstanceSave childACopied = AddChild("ChildACopied", "ParentA");
        InstanceSave grandchildACopied = AddChild("GrandchildACopied", "ChildACopied");

        InstanceSave childB = AddChild("ChildB", "ParentA");

        SelectInstances(childACopied, grandchildACopied);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(childB);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(6);

        screen.DefaultState.GetValue("ChildACopied1.Parent").ShouldBe("ChildB");
        screen.DefaultState.GetValue("GrandchildACopied1.Parent").ShouldBe("ChildACopied1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_Instance_InDifferentElementOnInstance_ShouldPasteAsChild()
    {
        /*
        ScreenA
            ChildA (copied)
        ScreenB
            ChildB
                ChildA1 <--- pasted
        */

        ScreenSave screenA = CreateDefaultScreen();
        screenA.Name = "ScreenA";
        InstanceSave childA = screenA.Instances[0];
        childA.Name = "ChildA";

        ScreenSave screenB = CreateDefaultScreen();
        screenB.Name = "ScreenB";
        InstanceSave childB = screenB.Instances[0];
        childB.Name = "ChildB";

        SelectInstances(childA);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(childB);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screenA.Instances.Count.ShouldBe(1);
        screenB.Instances.Count.ShouldBe(2);

        screenB.DefaultState.GetValue("ChildA.Parent").ShouldBe("ChildB");

    }

    [Fact]
    public void OnPaste_InstanceMultipleTimes_NewTarget_ShouldPasteSiblings()
    {
        /*
         (Screen)
            ChildA (copied)
            ChildB 
                ChildA1 <--- pasted first 
                ChildA2 <--- pasted again
         */

        ScreenSave screen = CreateDefaultScreen();

        InstanceSave ChildA = screen.Instances[0];
        ChildA.Name = "ChildA";

        InstanceSave ChildB = AddChild("ChildB", "Parent");

        SelectInstances(ChildA);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(ChildB);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("ChildB");
        screen.DefaultState.GetValue("ChildA2.Parent").ShouldBe("ChildB");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact(Skip = "For this to work, need to have a full ISelectedState")]
    public void OnPaste_MultipleInstancesInSeparateParents_ShouldPasteSiblings()
    {
        /*
         (Screen)
            ParentA
                ChildA (copied)
                ChildA1 (pasted)
                ChildA2 (pasted again)
            ParentB
                ChildB (copied
                ChildB1 (pasted)
                ChildB2 (pasted again)
 */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave parentA = screen.Instances[0];
        parentA.Name = "ParentA";

        InstanceSave childA = AddChild("ChildA", "ParentA");

        InstanceSave parentB = AddChild("ParentB", "");
        InstanceSave childB = AddChild("ChildB", "");

        SelectInstances(childA, childB);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(8);

        InstanceSave childA1 = screen.Instances.Find(item => item.Name == "ChildA1")!;
        InstanceSave childA2 = screen.Instances.Find(item => item.Name == "ChildA2")!;

        InstanceSave childB1 = screen.Instances.Find(item => item.Name == "ChildB1")!;
        InstanceSave childB2 = screen.Instances.Find(item => item.Name == "ChildB2")!;

        IndexOf(childA).ShouldBeLessThan(IndexOf(childA1));
        IndexOf(childA1).ShouldBeLessThan(IndexOf(childA2));

        IndexOf(childB).ShouldBeLessThan(IndexOf(childB1));
        IndexOf(childB1).ShouldBeLessThan(IndexOf(childB2));


        int IndexOf(InstanceSave instance) => screen.Instances.IndexOf(instance);
        
        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_Instance_OnSameElement_ShouldPasteOnDefaultChild()
    {
        ComponentSave component = new();
        ObjectFinder.Self.GumProjectSave!.Components.Add(component);
        component.Name = "ComponentWithDefaultContainer";
        StateSave defaultState = new();
        defaultState.ParentContainer = component;
        component.States.Add(defaultState);

        InstanceSave defaultContainer = new();
        component.Instances.Add(defaultContainer);
        defaultContainer.ParentContainer = component;
        defaultContainer.Name = "DefaultContainer";
        component.DefaultState.SetValue("DefaultChildContainer", "DefaultContainer");

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];
        InstanceSave parentInstance = AddChild("ParentInstance", "");
        parentInstance.BaseType = "ComponentWithDefaultContainer";

        SelectInstances(instance1);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(parentInstance);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.DefaultState.GetValue("Instance2.Parent").ShouldBe("ParentInstance.DefaultContainer");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnPaste_Instance_OnParentWithSameName_ShouldNotCreateCircularDependency()
    {
        ScreenSave screen1 = CreateDefaultScreen();
        screen1.Name = "Screen1";
        InstanceSave instance1InScreen1 = screen1.Instances[0];

        ScreenSave screen2 = CreateDefaultScreen();
        screen2.Name = "Screen2";
        InstanceSave instance1InScreen2 = screen2.Instances[0];

        SelectInstances(instance1InScreen2);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectInstances(instance1InScreen1);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen1.Instances.Count.ShouldBe(2);

        screen1.Instances[1].Name.ShouldBe("Instance2");

        var instance2ParentVariable = screen1.DefaultState.GetVariableSave("Instance2.Parent");
        instance2ParentVariable.ShouldNotBeNull();
        ((string)instance2ParentVariable.Value!).ShouldBe("Instance1");

    }

    #region Utilities

    private ScreenSave CreateDefaultScreen()
    {
        ScreenSave element = new ();
        element.Name = "DefaultScreen";
        ObjectFinder.Self.GumProjectSave!.Screens.Add(element);
        StateSave defaultState = new ();
        defaultState.ParentContainer = element;
        element.States.Add(defaultState);

        InstanceSave instance = new();
        element.Instances.Add(instance);
        instance.ParentContainer = element;
        instance.Name = "Instance1";

        return element;
    }

    private void SelectElement(ElementSave elementSave)
    {
        _selectedState
            .Setup(x => x.SelectedInstance)
            .Returns((InstanceSave?)null);
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(new List<InstanceSave>());

        _selectedState.Setup(x => x.SelectedElement).Returns(elementSave);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { elementSave });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(elementSave.DefaultState);


        _messenger.Send(new SelectionChangedMessage { /* properties */ });
    }

    private void SelectInstances(params InstanceSave[] instances)
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

    InstanceSave AddChild(string childName, string? parentName, ScreenSave screen)
    {
        InstanceSave child = new();
        screen.Instances.Add(child);
        child.ParentContainer = screen;
        child.Name = childName;
        if (!string.IsNullOrEmpty(parentName))
        {
            screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");
        }
        return child;
    }

    #endregion
}
