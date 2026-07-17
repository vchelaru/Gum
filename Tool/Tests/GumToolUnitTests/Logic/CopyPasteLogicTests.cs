using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Messages;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
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
    private readonly WeakReferenceMessenger _messenger;

    StateSave selectedStateSave = new();
    ComponentSave selectedComponent = new();
    List<InstanceSave> _currentSelectedInstances = new();

    private readonly AutoMocker _mocker;
    public CopyPasteLogicTests()
    {
        _mocker = new ();

        // Replace the mocked IMessenger with a real instance
        _messenger = new WeakReferenceMessenger();
        _mocker.Use<IMessenger>(_messenger);

        // ICopyPasteProjectCommands is an interface (ADR-0005 Phase 3 narrow port), so AutoMocker's
        // default resolution would give CopyPasteLogic a no-op mock. Several CreateComponentFromInstance
        // tests rely on ProjectCommands' real PrepareNewComponentSave/AddComponent behavior (it seeds
        // the new component's Name/BaseType/DefaultState), so construct a real ProjectCommands (with its
        // own dependencies auto-mocked) and register it as the resolution target for the interface.
        var projectCommands = _mocker.CreateInstance<ProjectCommands>();
        _mocker.Use<ICopyPasteProjectCommands>(projectCommands);

        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();

        _selectedState = _mocker.GetMock<ISelectedState>();
        _elementCommands = _mocker.GetMock<IElementCommands>();

        // Mock the SelectedInstances setter so paste operations update selection state
        _selectedState
            .SetupSet(x => x.SelectedInstances = It.IsAny<IEnumerable<InstanceSave>>())
            .Callback((IEnumerable<InstanceSave> newInstances) =>
            {
                _currentSelectedInstances = newInstances?.ToList() ?? new List<InstanceSave>();
                _selectedState
                    .Setup(x => x.SelectedInstance)
                    .Returns(_currentSelectedInstances.FirstOrDefault());
                _messenger.Send(new SelectionChangedMessage());
            });

        selectedStateSave.Name = "CopiedState";
        selectedComponent.BaseType = "Sprite";
        selectedComponent.States.Add(new StateSave
        {
            Name = "Default",
            ParentContainer = selectedComponent
        });

        Mock<ICopyPastePluginNotifier> copyPastePluginNotifier = _mocker.GetMock<ICopyPastePluginNotifier>();
        copyPastePluginNotifier
            .Setup(x => x.InstanceAdd(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()))
            .Callback(() =>
            {

            });

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

        // The real ProjectCommands (see above) reads the project through IProjectState, so point it at
        // the same instance.
        _mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(gumProject);

        // CopyPasteLogic's own element-name-uniqueness check (PasteCopiedElement) reads the project
        // through ICopyPasteProjectProvider - a separate mock from IProjectState above - so point it at
        // the same instance too.
        _mocker.GetMock<ICopyPasteProjectProvider>().Setup(x => x.GumProjectSave).Returns(gumProject);

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

    #region Category

    [Fact]
    public void OnPaste_Category_ShouldAddCategoryToTargetElement()
    {
        ComponentSave sourceComponent = new() { Name = "Source" };
        sourceComponent.States.Add(new StateSave { Name = "Default", ParentContainer = sourceComponent });
        StateSaveCategory sourceCategory = new() { Name = "Colors" };
        sourceCategory.States.Add(new StateSave { Name = "Red" });
        sourceCategory.States.Add(new StateSave { Name = "Blue" });
        sourceComponent.Categories.Add(sourceCategory);

        ComponentSave targetComponent = new() { Name = "Target" };
        targetComponent.States.Add(new StateSave { Name = "Default", ParentContainer = targetComponent });

        _selectedState.Setup(x => x.SelectedElement).Returns(sourceComponent);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(sourceCategory);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _copyPasteLogic.OnCopy(CopyType.Category);

        _selectedState.Setup(x => x.SelectedElement).Returns(targetComponent);

        _copyPasteLogic.OnPaste(CopyType.Category);

        targetComponent.Categories.Count.ShouldBe(1);
        targetComponent.Categories[0].Name.ShouldBe("Colors");
        targetComponent.Categories[0].States.Count.ShouldBe(2);
        targetComponent.Categories[0].States.ShouldNotContain(sourceCategory.States[0],
            "because pasted states should be clones, not the same instances as the source");
    }

    [Fact]
    public void OnPaste_Category_ShouldUniquifyName_IfTargetHasCategoryWithSameName()
    {
        ComponentSave sourceComponent = new() { Name = "Source" };
        sourceComponent.States.Add(new StateSave { Name = "Default", ParentContainer = sourceComponent });
        StateSaveCategory sourceCategory = new() { Name = "Colors" };
        sourceCategory.States.Add(new StateSave { Name = "Red" });
        sourceComponent.Categories.Add(sourceCategory);

        ComponentSave targetComponent = new() { Name = "Target" };
        targetComponent.States.Add(new StateSave { Name = "Default", ParentContainer = targetComponent });
        targetComponent.Categories.Add(new StateSaveCategory { Name = "Colors" });

        _selectedState.Setup(x => x.SelectedElement).Returns(sourceComponent);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(sourceCategory);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _copyPasteLogic.OnCopy(CopyType.Category);

        _selectedState.Setup(x => x.SelectedElement).Returns(targetComponent);

        _copyPasteLogic.OnPaste(CopyType.Category);

        targetComponent.Categories.Count.ShouldBe(2);
        targetComponent.Categories.Select(item => item.Name).ShouldContain("Colors");
        targetComponent.Categories.Any(item => item.Name != "Colors").ShouldBeTrue(
            "because the pasted category's name should be uniquified");
    }

    [Fact]
    public void OnPaste_Category_ShouldShowToast_IfReferencedInstancesMissingOnTarget()
    {
        ComponentSave sourceComponent = new() { Name = "Source" };
        sourceComponent.States.Add(new StateSave { Name = "Default", ParentContainer = sourceComponent });
        StateSaveCategory sourceCategory = new() { Name = "Colors" };
        StateSave redState = new() { Name = "Red" };
        redState.SetValue("ColoredRectangleInstance.Red", 255f, "float");
        sourceCategory.States.Add(redState);
        sourceComponent.Categories.Add(sourceCategory);

        ComponentSave targetComponent = new() { Name = "Target" };
        targetComponent.States.Add(new StateSave { Name = "Default", ParentContainer = targetComponent });

        var dialogService = _mocker.GetMock<IDialogService>();

        _selectedState.Setup(x => x.SelectedElement).Returns(sourceComponent);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(sourceCategory);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _copyPasteLogic.OnCopy(CopyType.Category);

        _selectedState.Setup(x => x.SelectedElement).Returns(targetComponent);

        _copyPasteLogic.OnPaste(CopyType.Category);

        targetComponent.Categories.Count.ShouldBe(1,
            "because the paste should still succeed even if some referenced instances are missing");
        dialogService.Verify(
            x => x.ShowMessage(
                It.Is<string>(message => message.Contains("ColoredRectangleInstance")),
                It.IsAny<string?>(),
                It.IsAny<MessageDialogStyle?>()),
            Times.Once);
    }

    [Fact]
    public void OnPaste_Category_ShouldNotShowToast_IfAllReferencedInstancesPresentOnTarget()
    {
        ComponentSave sourceComponent = new() { Name = "Source" };
        sourceComponent.States.Add(new StateSave { Name = "Default", ParentContainer = sourceComponent });
        StateSaveCategory sourceCategory = new() { Name = "Colors" };
        StateSave redState = new() { Name = "Red" };
        redState.SetValue("ColoredRectangleInstance.Red", 255f, "float");
        sourceCategory.States.Add(redState);
        sourceComponent.Categories.Add(sourceCategory);

        ComponentSave targetComponent = new() { Name = "Target" };
        targetComponent.States.Add(new StateSave { Name = "Default", ParentContainer = targetComponent });
        targetComponent.Instances.Add(new InstanceSave
        {
            Name = "ColoredRectangleInstance",
            ParentContainer = targetComponent
        });

        var dialogService = _mocker.GetMock<IDialogService>();

        _selectedState.Setup(x => x.SelectedElement).Returns(sourceComponent);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(sourceCategory);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        _copyPasteLogic.OnCopy(CopyType.Category);

        _selectedState.Setup(x => x.SelectedElement).Returns(targetComponent);

        _copyPasteLogic.OnPaste(CopyType.Category);

        targetComponent.Categories.Count.ShouldBe(1);
        dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    #endregion

    [Fact]
    public void OnPaste_Instance_ShouldSortVariables()
    {
        ScreenSave element = CreateDefaultScreen();

        SelectInstances(element.Instances.First());

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _elementCommands.Verify(x => x.SortVariables(It.IsAny<ElementSave>()), Times.Once);
    }

    // Paste filters base-element default-state variables by the set of LHSes
    // owned by reachable VariableReferences — the same "reachable" walk the
    // GUM0002 error check uses. Inherited scalars NOT owned by a ref
    // (Text="Click Me") are still snapshotted (see
    // OnPaste_Instance_ShouldSnapshotBaseElementScalarVariables); inherited
    // scalars that ARE ref-owned (the UpgradeButton orphan FontSize=14 case)
    // are dropped so the new instance picks them up via inheritance / the ref
    // (see OnPaste_Instance_ShouldSkipBaseElementVariable_WhenReachableReferenceOwnsIt).
    // Directly-authored values on the source's own state pass through
    // untouched even when conflicted (see
    // OnPaste_Instance_ShouldCopySourceLevelVariable_EvenWhenReachableReferenceOwnsIt) —
    // GUM0002 handles the conflict display separately.

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
    public void OnPaste_Instance_ShouldCopyDirectlySetVariablesOnInstance()
    {
        // Sanity check: variables set directly on the source element's
        // selected state for the copied instance must be carried over to the
        // pasted instance. This guards against an over-broad version of the
        // base-state fix that would also strip legitimate local values.

        ComponentSave component = new() { Name = "Component", BaseType = "Container" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave instance = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = component };
        component.Instances.Add(instance);

        defaultState.SetValue("TextInstance.FontSize", 24, "int");

        ObjectFinder.Self.GumProjectSave!.Components.Add(component);

        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { component });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);

        SelectInstances(instance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        var pasted = component.Instances[1];
        defaultState.GetValue($"{pasted.Name}.FontSize").ShouldBe(24);
    }

    [Fact]
    public void OnPaste_Instance_ShouldCopyDirectlySetVariableReferencesOnInstance()
    {
        // Direct VariableReferences on the source instance must be carried
        // over. Only inherited base-element VariableReferences should be
        // skipped — locally-set ones are a real authored value.

        ComponentSave component = new() { Name = "Component", BaseType = "Container" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave instance = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = component };
        component.Instances.Add(instance);

        VariableListSave<string> directVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        directVarRefs.ValueAsIList.Add("Red = SomeOtherInstance.Red");
        defaultState.VariableLists.Add(directVarRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(component);

        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { component });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);

        SelectInstances(instance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        var pasted = component.Instances[1];
        var pastedList = defaultState.VariableLists
            .FirstOrDefault(v => v.Name == $"{pasted.Name}.VariableReferences");
        pastedList.ShouldNotBeNull(
            "because VariableReferences set directly on the source element should be carried " +
            "over to the pasted instance.");
        pastedList!.ValueAsIList.Count.ShouldBe(1);
        pastedList.ValueAsIList[0].ShouldBe("Red = SomeOtherInstance.Red");
    }

    [Fact]
    public void OnPaste_Instance_ShouldSnapshotBaseElementScalarVariables()
    {
        // Regression: an instance that gets its visible value via inheritance
        // (e.g. UpgradeButton's TextInstance inherits Text="Click Me" from
        // Button) must paste as a copy that ALSO shows that value. The new
        // instance is DefinedByBase=false (a fresh BaseType instance), so it
        // does not pick up the base's instance-variables via inheritance —
        // they must be snapshotted onto the copy at paste time.

        ComponentSave baseComponent = new() { Name = "BaseComponent", BaseType = "Container" };
        StateSave baseDefaultState = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefaultState);

        InstanceSave textInBase = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Add(textInBase);

        baseDefaultState.SetValue("TextInstance.Text", "Click Me", "string");

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponent", BaseType = "BaseComponent" };
        StateSave derivedDefaultState = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefaultState);

        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "Text",
            DefinedByBase = true,
            ParentContainer = derivedComponent
        };
        derivedComponent.Instances.Add(textInDerived);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        _selectedState.Setup(x => x.SelectedElement).Returns(derivedComponent);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { derivedComponent });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(derivedDefaultState);

        SelectInstances(textInDerived);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        var pasted = derivedComponent.Instances[1];
        derivedDefaultState.GetValue($"{pasted.Name}.Text")
            .ShouldBe("Click Me",
                "because the source instance inherited Text='Click Me' from the base; " +
                "the pasted instance is DefinedByBase=false and so will not re-inherit, " +
                "meaning the value must be snapshotted at paste time to preserve appearance.");
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
    public void OnPaste_Instance_ParentOnlySelected_ShouldSelectOnlyNewParent()
    {
        /*
         * Container  (selected at copy time)
         *    Child
         * Container1 <--- pasted, should be selected
         *    Child1  <--- pasted, should NOT be selected
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave container = screen.Instances[0];
        container.Name = "Container";
        AddChild("Child", "Container", screen);

        SelectInstances(container);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        _currentSelectedInstances.Count.ShouldBe(1,
            "because only the parent was selected at copy time, so only the new parent should be selected");
        _currentSelectedInstances[0].Name.ShouldBe("Container1");
    }

    [Fact]
    public void OnPaste_Instance_ParentAndChildSelected_ShouldSelectBothNewCopies()
    {
        /*
         * Inverse guard against over-narrowing: when BOTH the parent and its child were
         * selected at copy time, BOTH new copies should be selected after paste.
         *
         * Container  (selected at copy time)
         *    Child   (selected at copy time)
         * Container1 <--- pasted, should be selected
         *    Child1  <--- pasted, should ALSO be selected
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave container = screen.Instances[0];
        container.Name = "Container";
        InstanceSave child = AddChild("Child", "Container", screen);

        SelectInstances(container, child);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        _currentSelectedInstances.Count.ShouldBe(2,
            "because both the parent and child were selected at copy time, so both new copies should be selected");
        _currentSelectedInstances.Select(item => item.Name).OrderBy(name => name)
            .ShouldBe(new[] { "Child1", "Container1" });
    }

    [Fact]
    public void OnPaste_Instance_ChildSelected_ShouldSelectOnlyNewChild()
    {
        /*
         * Container
         *    Child  (selected at copy time)
         *       Grandchild
         * Pasted as siblings under Container:
         *    Child1       <--- pasted, should be selected
         *       Grandchild1 <--- pasted, should NOT be selected
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave container = screen.Instances[0];
        container.Name = "Container";
        InstanceSave child = AddChild("Child", "Container", screen);
        AddChild("Grandchild", "Child", screen);

        SelectInstances(child);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(5);

        _currentSelectedInstances.Count.ShouldBe(1,
            "because only the child was selected at copy time, so only the new child should be " +
            "selected, not the grandchild that was dragged along recursively");
        _currentSelectedInstances[0].Name.ShouldBe("Child1");
    }

    [Fact]
    public void OnPaste_MultipleInstances_SubsetSelected_ShouldSelectOnlyNewCopiesOfSelected()
    {
        /*
         * ParentA (selected at copy time)
         *    ChildA
         * ParentB (selected at copy time)
         *    ChildB
         * After paste, four new instances exist; only the two new parents should be selected.
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave parentA = screen.Instances[0];
        parentA.Name = "ParentA";
        AddChild("ChildA", "ParentA", screen);

        InstanceSave parentB = AddChild("ParentB", "", screen);
        AddChild("ChildB", "ParentB", screen);

        SelectInstances(parentA, parentB);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(8);

        _currentSelectedInstances.Count.ShouldBe(2,
            "because only the two parents were selected at copy time, not their children");
        _currentSelectedInstances.Select(item => item.Name).OrderBy(name => name)
            .ShouldBe(new[] { "ParentA1", "ParentB1" });
    }

    [Fact]
    public void OnPaste_InstanceWithDottedSubInstanceParent_InDifferentScreen_ShouldPreserveHierarchy()
    {
        /*
         * Screen1:
         *   ComboBoxInstance
         *   ListBoxItemInstance (Parent = "ComboBoxInstance.InnerPanelInstance")
         *
         * Copy ComboBoxInstance (which recursively includes ListBoxItemInstance because its
         * Parent starts with "ComboBoxInstance.") and paste into Screen2.
         *
         * Expected Screen2:
         *   Instance1 (from CreateDefaultScreen)
         *   ComboBoxInstance
         *   ListBoxItemInstance (Parent = "ComboBoxInstance.InnerPanelInstance")
         */

        ScreenSave screen1 = CreateDefaultScreen();
        screen1.Name = "Screen1";
        var comboBoxInstance = screen1.Instances[0];
        comboBoxInstance.Name = "ComboBoxInstance";

        AddChild("ListBoxItemInstance", "ComboBoxInstance.InnerPanelInstance", screen1);

        ScreenSave screen2 = CreateDefaultScreen();
        screen2.Name = "Screen2";

        SelectInstances(comboBoxInstance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        SelectElement(screen2);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen2.Instances.Count.ShouldBe(3, "Instance1 from CreateDefaultScreen plus the 2 pasted instances");

        screen2.DefaultState.GetValue("ListBoxItemInstance.Parent")
            .ShouldBe("ComboBoxInstance.InnerPanelInstance",
                "The ListBoxItem's dotted parent path should be preserved in the target screen");
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

    #region Cut + Paste Tests

    [Fact]
    public void OnCut_ThenPaste_InstanceWithChild_ShouldPreserveHierarchy()
    {
        /*
         * Instance1
         *   Child
         *     Grandchild
         *
         * Cut Child (which includes Grandchild), paste without changing selection.
         * Result should be:
         * Instance1
         *   Child <--- pasted
         *     Grandchild <--- pasted
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave grandchild = AddChild("Grandchild", "Child");

        SetupDeleteLogicMock();

        SelectInstances(child);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        // After cut, the instances should be removed from the screen
        screen.Instances.Count.ShouldBe(1, "Only Instance1 should remain after cut");

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(3);

        screen.DefaultState.GetValue("Child.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild.Parent").ShouldBe("Child");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPaste_InstanceWithChild_IntoNewParent_ShouldPreserveInternalHierarchy()
    {
        /*
         * Instance1
         *   ChildA
         *     GrandchildA
         * TargetContainer
         *
         * Cut ChildA (which includes GrandchildA), select TargetContainer, paste.
         * Result should be:
         * Instance1
         * TargetContainer
         *   ChildA <--- pasted, re-parented to TargetContainer
         *     GrandchildA <--- pasted, internal hierarchy preserved
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave childA = AddChild("ChildA", "Instance1");
        InstanceSave grandchildA = AddChild("GrandchildA", "ChildA");

        InstanceSave targetContainer = AddChild("TargetContainer", "");

        SetupDeleteLogicMock();

        SelectInstances(childA);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(2, "Only Instance1 and TargetContainer should remain after cut");

        SelectInstances(targetContainer);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);

        screen.DefaultState.GetValue("ChildA.Parent").ShouldBe("TargetContainer");
        screen.DefaultState.GetValue("GrandchildA.Parent").ShouldBe("ChildA");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPasteMultipleTimes_NoSelectionChange_ShouldPreserveHierarchy()
    {
        /*
         * Instance1
         *   Child
         *     Grandchild
         *
         * Cut Child (includes Grandchild), paste twice without changing selection.
         * After second paste:
         * Instance1
         *   Child
         *     Grandchild
         *   Child1
         *     Grandchild1
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave grandchild = AddChild("Grandchild", "Child");

        SetupDeleteLogicMock();

        SelectInstances(child);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(1, "Only Instance1 should remain after cut");

        // First paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(3);
        screen.DefaultState.GetValue("Child.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild.Parent").ShouldBe("Child");

        // Second paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(5);
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild1.Parent").ShouldBe("Child1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPasteMultipleTimes_IntoNewParent_ShouldPreserveInternalHierarchy()
    {
        /*
         * Instance1
         *   ChildA
         *     GrandchildA
         * TargetContainer
         *
         * Cut ChildA (includes GrandchildA), select TargetContainer, paste twice.
         * After second paste:
         * Instance1
         * TargetContainer
         *   ChildA
         *     GrandchildA
         *   ChildA1
         *     GrandchildA1
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave childA = AddChild("ChildA", "Instance1");
        InstanceSave grandchildA = AddChild("GrandchildA", "ChildA");
        InstanceSave targetContainer = AddChild("TargetContainer", "");

        SetupDeleteLogicMock();

        SelectInstances(childA);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(2, "Only Instance1 and TargetContainer should remain after cut");

        SelectInstances(targetContainer);

        // First paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);
        screen.DefaultState.GetValue("ChildA.Parent").ShouldBe("TargetContainer");
        screen.DefaultState.GetValue("GrandchildA.Parent").ShouldBe("ChildA");

        // Second paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(6);
        screen.DefaultState.GetValue("ChildA1.Parent").ShouldBe("TargetContainer");
        screen.DefaultState.GetValue("GrandchildA1.Parent").ShouldBe("ChildA1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPaste_ThenChangeSelection_ThenPaste_ShouldRespectNewSelection()
    {
        /*
         * Instance1
         *   Child
         *     Grandchild
         * TargetContainer
         *
         * Cut Child (includes Grandchild), paste without selection change (restores under Instance1),
         * then select TargetContainer, paste again.
         * Final result:
         * Instance1
         *   Child
         *     Grandchild
         * TargetContainer
         *   Child1
         *     Grandchild1
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave grandchild = AddChild("Grandchild", "Child");
        InstanceSave targetContainer = AddChild("TargetContainer", "");

        SetupDeleteLogicMock();

        SelectInstances(child);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        // First paste (no selection change) - restores under Instance1
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);
        screen.DefaultState.GetValue("Child.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild.Parent").ShouldBe("Child");

        // Change selection to TargetContainer
        SelectInstances(targetContainer);

        // Second paste - should go under TargetContainer
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(6);
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("TargetContainer");
        screen.DefaultState.GetValue("Grandchild1.Parent").ShouldBe("Child1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPasteMultipleTimes_DeepHierarchy_ShouldPreserveAllLevels()
    {
        /*
         * Instance1
         *   Child
         *     Grandchild
         *       GreatGrandchild
         *
         * Cut Child (includes all descendants), paste twice.
         * After second paste:
         * Instance1
         *   Child
         *     Grandchild
         *       GreatGrandchild
         *   Child1
         *     Grandchild1
         *       GreatGrandchild1
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");
        InstanceSave grandchild = AddChild("Grandchild", "Child");
        InstanceSave greatGrandchild = AddChild("GreatGrandchild", "Grandchild");

        SetupDeleteLogicMock();

        SelectInstances(child);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(1, "Only Instance1 should remain after cut");

        // First paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4);
        screen.DefaultState.GetValue("Child.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild.Parent").ShouldBe("Child");
        screen.DefaultState.GetValue("GreatGrandchild.Parent").ShouldBe("Grandchild");

        // Second paste
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(7);
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("Instance1");
        screen.DefaultState.GetValue("Grandchild1.Parent").ShouldBe("Child1");
        screen.DefaultState.GetValue("GreatGrandchild1.Parent").ShouldBe("Grandchild1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_SingleInstance_ThenPasteMultipleTimes_ShouldWorkCorrectly()
    {
        /*
         * Instance1
         *   Child
         *
         * Cut Child (no sub-hierarchy), paste three times.
         * Result:
         * Instance1
         *   Child
         *   Child1
         *   Child2
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave instance1 = screen.Instances[0];

        InstanceSave child = AddChild("Child", "Instance1");

        SetupDeleteLogicMock();

        SelectInstances(child);

        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(1);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        screen.Instances.Count.ShouldBe(2);
        screen.DefaultState.GetValue("Child.Parent").ShouldBe("Instance1");

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        screen.Instances.Count.ShouldBe(3);
        screen.DefaultState.GetValue("Child1.Parent").ShouldBe("Instance1");

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        screen.Instances.Count.ShouldBe(4);
        screen.DefaultState.GetValue("Child2.Parent").ShouldBe("Instance1");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPasteTwice_IntoSameContainer_ShouldNotCreateSelfReference()
    {
        /*
         * Cut ContainerInstance (with children) from cont2, select cont2, paste twice.
         * The second paste should NOT create any self-referencing parent.
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave cont2 = screen.Instances[0];
        cont2.Name = "cont2";

        InstanceSave containerInstance = AddChild("ContainerInstance", "cont2");
        InstanceSave coloredRect = AddChild("ColoredRectangleInstance", "ContainerInstance");
        InstanceSave coloredRect1 = AddChild("ColoredRectangleInstance1", "ContainerInstance");

        SetupDeleteLogicMock();

        SelectInstances(containerInstance);
        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(1, "Only cont2 should remain after cut");

        SelectInstances(cont2);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(4, "cont2 + ContainerInstance + 2 children");
        screen.DefaultState.GetValue("ContainerInstance.Parent").ShouldBe("cont2");

        // Paste again into cont2 (re-select cont2)
        SelectInstances(cont2);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(7, "cont2 + 2 sets of ContainerInstance + children");

        // Verify no self-referencing parents exist
        foreach (var variable in screen.DefaultState.Variables)
        {
            if (variable.GetRootName() == "Parent" && variable.Value is string parentValue)
            {
                var instanceName = variable.SourceObject;
                parentValue.ShouldNotBe(instanceName,
                    $"Instance '{instanceName}' has self-referencing parent: {instanceName}.Parent = {parentValue}");
            }
        }

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    [Fact]
    public void OnCut_ThenPaste_ThenPasteIntoFirstPasteResult_ShouldNotCreateSelfReference()
    {
        /*
         * Reproduces the exact user bug scenario (issue #2240 edge case):
         * 1. Cut ContainerInstance (with children) from cont2
         * 2. Select cont2, paste → creates ContainerInstance under cont2
         * 3. Select the just-pasted ContainerInstance
         * 4. Paste again → should create ContainerInstance3 under ContainerInstance
         *    NOT ContainerInstance3.Parent = ContainerInstance3 (self-reference!)
         */

        ScreenSave screen = CreateDefaultScreen();
        InstanceSave cont2 = screen.Instances[0];
        cont2.Name = "cont2";

        InstanceSave containerInstance = AddChild("ContainerInstance", "cont2");
        InstanceSave coloredRect = AddChild("ColoredRectangleInstance", "ContainerInstance");

        SetupDeleteLogicMock();

        // Cut ContainerInstance (with child)
        SelectInstances(containerInstance);
        _copyPasteLogic.OnCut(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(1, "Only cont2 should remain after cut");

        // First paste: select cont2, paste
        SelectInstances(cont2);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        screen.Instances.Count.ShouldBe(3, "cont2 + ContainerInstance + ColoredRectangleInstance");
        screen.DefaultState.GetValue("ContainerInstance.Parent").ShouldBe("cont2");
        screen.DefaultState.GetValue("ColoredRectangleInstance.Parent").ShouldBe("ContainerInstance");

        // Second paste: select the just-pasted ContainerInstance and paste INTO it
        var pastedContainerInstance = screen.Instances.First(i => i.Name == "ContainerInstance");
        SelectInstances(pastedContainerInstance);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        // Should have created new copies under ContainerInstance
        screen.Instances.Count.ShouldBe(5, "cont2 + ContainerInstance + ColoredRectangleInstance + 2 new copies");

        // Verify no self-referencing parents exist
        foreach (var variable in screen.DefaultState.Variables)
        {
            if (variable.GetRootName() == "Parent" && variable.Value is string parentValue)
            {
                var instanceName = variable.SourceObject;
                parentValue.ShouldNotBe(instanceName,
                    $"Instance '{instanceName}' has self-referencing parent: {instanceName}.Parent = {parentValue}");
            }
        }

        // The new ContainerInstance copy should be parented to ContainerInstance (the selected target), not to itself
        var newContainerCopy = screen.Instances.FirstOrDefault(i =>
            i.Name != "cont2" && i.Name != "ContainerInstance" && i.Name.StartsWith("ContainerInstance"));
        newContainerCopy.ShouldNotBeNull("Should have a renamed copy of ContainerInstance");
        screen.DefaultState.GetValue($"{newContainerCopy.Name}.Parent").ShouldBe("ContainerInstance",
            "The second paste's container should be parented to the selected ContainerInstance, not itself");

        InstanceSave AddChild(string childName, string parentName) => this.AddChild(childName, parentName, screen);
    }

    #endregion

    #region CreateComponentFromInstance

    [Fact]
    public void CreateComponentFromInstance_AddsComponentToProject()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        ObjectFinder.Self.GumProjectSave!.Components.ShouldContain(component);
    }

    [Fact]
    public void CreateComponentFromInstance_CopiesChildVariables()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.DefaultState.GetVariableSave("ChildText.Text")?.Value.ShouldBe("Hello");
    }

    [Fact]
    public void CreateComponentFromInstance_CopiesChildrenAsInstances()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.Instances.Select(item => item.Name).ShouldBe(new[] { "ChildText", "GrandChild" }, ignoreOrder: true);
    }

    [Fact]
    public void CreateComponentFromInstance_CopiesIntrinsicInstanceVariablesToRoot()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.DefaultState.GetVariableSave("Width")?.Value.ShouldBe(200f);
        component.DefaultState.GetVariableSave("Height")?.Value.ShouldBe(80f);
    }

    [Fact]
    public void CreateComponentFromInstance_CopiesNonReferenceVariableLists()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);
        VariableListSave<float> childList = new VariableListSave<float> { Name = "ChildText.Points", Type = "float" };
        childList.ValueAsIList.Add(1f);
        childList.ValueAsIList.Add(2f);
        screen.DefaultState.VariableLists.Add(childList);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // Only VariableReferences lists are dropped; ordinary list-typed variables must survive.
        component.DefaultState.VariableLists.ShouldContain(item => item.Name == "ChildText.Points");
    }

    [Fact]
    public void CreateComponentFromInstance_DoesNotCopyChildVariableReferences()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);
        VariableListSave<string> childReferences = new VariableListSave<string> { Name = "ChildText.VariableReferences", Type = "string" };
        childReferences.ValueAsIList.Add("FontSize = MyButton.Height");
        screen.DefaultState.VariableLists.Add(childReferences);
        // A variable reference materializes its resolved value as a hard scalar in the same state;
        // that scalar is what preserves the value, so the reference row itself can be dropped.
        screen.DefaultState.SetValue("ChildText.FontSize", 28, "int");

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // The reference (which pointed at the now-promoted instance) is dropped...
        component.DefaultState.VariableLists.ShouldNotContain(item => item.Name == "ChildText.VariableReferences");
        // ...but the materialized value is preserved.
        component.DefaultState.GetVariableSave("ChildText.FontSize")?.Value.ShouldBe(28);
    }

    [Fact]
    public void CreateComponentFromInstance_DoesNotCopyPositionalInstanceVariablesToRoot()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // The promoted instance sat at X=50, Y=120 on its parent. A component root must not adopt
        // a parent-relative position, so those values must not transfer to the component root.
        object? rootX = component.DefaultState.GetVariableSave("X")?.Value;
        object? rootY = component.DefaultState.GetVariableSave("Y")?.Value;
        rootX.ShouldNotBe(50f);
        rootY.ShouldNotBe(120f);
    }

    [Fact]
    public void CreateComponentFromInstance_DoesNotCopyPromotedInstanceAsChild()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.Instances.ShouldNotContain(item => item.Name == "MyButton");
    }

    [Fact]
    public void CreateComponentFromInstance_DoesNotCopyPromotedInstanceVariableReferencesToRoot()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);
        VariableListSave<string> buttonReferences = new VariableListSave<string> { Name = "MyButton.VariableReferences", Type = "string" };
        buttonReferences.ValueAsIList.Add("Width = SomeOtherInstance.Width");
        screen.DefaultState.VariableLists.Add(buttonReferences);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // The promoted instance's reference row must not become a root-level VariableReferences list;
        // its materialized scalar (Width=200, asserted elsewhere) carries the value.
        component.DefaultState.VariableLists.ShouldNotContain(item => item.GetRootName() == "VariableReferences");
    }

    [Fact]
    public void CreateComponentFromInstance_DoesNotCopyUnrelatedSiblingInstance()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.Instances.ShouldNotContain(item => item.Name == "UnrelatedSibling");
        component.DefaultState.GetVariableSave("UnrelatedSibling.Text").ShouldBeNull();
    }

    [Fact]
    public void CreateComponentFromInstance_DropsParentOfDirectChild()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // ChildText was parented to MyButton on the screen. Now that MyButton IS the component
        // root, the direct child attaches to the root, so its Parent variable must be dropped.
        component.DefaultState.GetVariableSave("ChildText.Parent").ShouldBeNull();
    }

    [Fact]
    public void CreateComponentFromInstance_PreservesNestedChildParent()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        // GrandChild was parented to ChildText (another child), so that relationship is preserved.
        component.DefaultState.GetVariableSave("GrandChild.Parent")?.Value.ShouldBe("ChildText");
    }

    [Fact]
    public void CreateComponentFromInstance_SetsBaseTypeToInstanceBaseType()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.BaseType.ShouldBe("Container");
    }

    [Fact]
    public void CreateComponentFromInstance_SetsComponentName()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        ComponentSave component = _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        component.Name.ShouldBe("ButtonComponent");
    }

    [Fact]
    public void CreateComponentFromInstance_WithoutReplace_LeavesSourceElementUnchanged()
    {
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);
        int instanceCountBefore = screen.Instances.Count;

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: false);

        screen.Instances.Count.ShouldBe(instanceCountBefore);
        screen.Instances.ShouldContain(myButton);
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_AddsInstanceOfNewComponent()
    {
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        InstanceSave? replacement = screen.Instances.FirstOrDefault(item => item.Name == "MyButton");
        replacement.ShouldNotBeNull();
        replacement!.BaseType.ShouldBe("ButtonComponent");
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_DoesNotKeepIntrinsicVariablesOnReplacement()
    {
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        // Width is intrinsic and now lives on the component root; the replacement instance should
        // inherit it rather than carry a redundant override.
        screen.DefaultState.GetVariableSave("MyButton.Width").ShouldBeNull();
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_PreservesPositionOnReplacement()
    {
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        screen.DefaultState.GetVariableSave("MyButton.X")?.Value.ShouldBe(50f);
        screen.DefaultState.GetVariableSave("MyButton.Y")?.Value.ShouldBe(120f);
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_RefreshesWireframe()
    {
        SetupDeleteLogicMock();
        Mock<IWireframeObjectManager> wireframeObjectManager = _mocker.GetMock<IWireframeObjectManager>();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        // Without a wireframe rebuild the replacement instance renders blank until the user
        // reselects the screen, so the replace must refresh the wireframe itself.
        wireframeObjectManager.Verify(x => x.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_HoldsUndoLockWhileSelectingNewComponent()
    {
        // The replace records its undo against the SOURCE element, whose baseline snapshot is
        // captured (by UndoPlugin's RecordState) when the user selects the instance. AddComponent
        // selects the new component; UndoPlugin records state on element selection, and RecordState
        // is a no-op only while an undo lock is held. So the lock MUST already be held when
        // AddComponent changes the selection - otherwise the source-element baseline is overwritten
        // with a component snapshot and the resulting undo is corrupt.
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        bool lockRequested = false;
        bool lockHeldWhenComponentSelected = false;
        _mocker.GetMock<IUndoManager>()
            .Setup(x => x.RequestLock())
            .Callback(() => lockRequested = true);
        _selectedState
            .SetupSet(x => x.SelectedComponent = It.IsAny<ComponentSave>())
            .Callback<ComponentSave>(_ => lockHeldWhenComponentSelected = lockRequested);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        lockHeldWhenComponentSelected.ShouldBeTrue(
            "the undo lock must be held before AddComponent selects the new component, otherwise " +
            "UndoPlugin overwrites the source element's undo baseline with a component snapshot");
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_RemovesOriginalChildren()
    {
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        screen.Instances.ShouldNotContain(item => item.Name == "ChildText");
        screen.Instances.ShouldNotContain(item => item.Name == "GrandChild");
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_SelectsReplacementInstance()
    {
        SetupDeleteLogicMock();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        // AddComponent selects the new component and RemoveInstance clears the instance selection,
        // so the replace must explicitly re-select the new instance of the component.
        _selectedState.VerifySet(x => x.SelectedInstance = It.Is<InstanceSave>(
            instance => instance != null && instance.Name == "MyButton" && instance.BaseType == "ButtonComponent"),
            Times.Once);
    }

    [Fact]
    public void CreateComponentFromInstance_WithReplace_RequestsSingleUndoLock()
    {
        SetupDeleteLogicMock();
        Mock<IUndoManager> undoManager = _mocker.GetMock<IUndoManager>();
        ScreenSave screen = CreateScreenWithButton(out InstanceSave myButton, out _, out _, out _);

        _copyPasteLogic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        undoManager.Verify(x => x.RequestLock(), Times.Once);
    }

    /// <summary>
    /// Builds a screen containing MyButton (a Container at X=50,Y=120,Width=200,Height=80) with a
    /// direct child ChildText (Text, "Hello"), a nested GrandChild parented to ChildText, and an
    /// UnrelatedSibling that is not part of the MyButton subtree.
    /// </summary>
    private ScreenSave CreateScreenWithButton(out InstanceSave myButton, out InstanceSave childText,
        out InstanceSave grandChild, out InstanceSave unrelatedSibling)
    {
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        myButton = new InstanceSave { Name = "MyButton", BaseType = "Container", ParentContainer = screen };
        childText = new InstanceSave { Name = "ChildText", BaseType = "Text", ParentContainer = screen };
        grandChild = new InstanceSave { Name = "GrandChild", BaseType = "Container", ParentContainer = screen };
        unrelatedSibling = new InstanceSave { Name = "UnrelatedSibling", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(myButton);
        screen.Instances.Add(childText);
        screen.Instances.Add(grandChild);
        screen.Instances.Add(unrelatedSibling);

        defaultState.SetValue("ChildText.Parent", "MyButton", "string");
        defaultState.SetValue("GrandChild.Parent", "ChildText", "string");

        defaultState.SetValue("MyButton.X", 50f, "float");
        defaultState.SetValue("MyButton.Y", 120f, "float");
        defaultState.SetValue("MyButton.Width", 200f, "float");
        defaultState.SetValue("MyButton.Height", 80f, "float");

        defaultState.SetValue("ChildText.Text", "Hello", "string");
        defaultState.SetValue("UnrelatedSibling.Text", "Unrelated", "string");

        return screen;
    }

    #endregion

    #region Utilities

    private void SetupDeleteLogicMock()
    {
        var deleteLogic = _mocker.GetMock<IDeleteLogic>();
        deleteLogic
            .Setup(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()))
            .Callback((InstanceSave instanceToRemove, ElementSave element) =>
            {
                element.Instances.Remove(instanceToRemove);

                // Simulate RemoveReferencesToInstance
                foreach (var state in element.AllStates)
                {
                    state.Variables.RemoveAll(item => item.SourceObject == instanceToRemove.Name);
                    state.VariableLists.RemoveAll(item => item.SourceObject == instanceToRemove.Name);

                    state.Variables.RemoveAll(item =>
                        item.GetRootName() == "Parent" && item.Value is string valueAsString &&
                        (valueAsString == instanceToRemove.Name || valueAsString.StartsWith(instanceToRemove.Name + ".")));
                }
            });
    }


    [Fact]
    public void OnPaste_Instance_ShouldSkipBaseElementVariable_WhenReachableReferenceOwnsIt()
    {
        // UpgradeButton repro: Button.Default has the orphan TextInstance.FontSize=14.
        // TextInstance.BaseType = Label; Label.TextCategory.Title has a VariableReferences
        // row that assigns FontSize. DerivedButton sets TextInstance.TextCategoryState=Title
        // on its own Default. When pasting TextInstance, the base capture's FontSize=14
        // must be SKIPPED because FontSize is owned by a reachable reference (via the
        // active categorized state on the instance's BaseType chain).
        ComponentSave label = new() { Name = "LabelForCopy", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave titleState = new() { Name = "Title", ParentContainer = label };
        titleState.Variables.Add(new VariableSave
        {
            Name = "SourceFontSize",
            Type = "int",
            Value = 28,
            SetsValue = true
        });
        VariableListSave<string> titleRefs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        titleRefs.ValueAsIList.Add("FontSize = SourceFontSize");
        titleState.VariableLists.Add(titleRefs);
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave baseButton = new() { Name = "BaseButtonForCopy", BaseType = "Container" };
        StateSave baseDefault = new() { Name = "Default", ParentContainer = baseButton };
        baseButton.States.Add(baseDefault);
        baseButton.Instances.Add(new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "LabelForCopy",
            ParentContainer = baseButton
        });
        // The orphan: FontSize=14 explicitly set at the base level.
        baseDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.FontSize",
            Type = "int",
            Value = 14,
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Components.Add(baseButton);

        ComponentSave derivedButton = new() { Name = "DerivedButtonForCopy", BaseType = "BaseButtonForCopy" };
        StateSave derivedDefault = new() { Name = "Default", ParentContainer = derivedButton };
        derivedButton.States.Add(derivedDefault);
        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "LabelForCopy",
            DefinedByBase = true,
            ParentContainer = derivedButton
        };
        derivedButton.Instances.Add(textInDerived);
        // Active categorized state on the instance — makes Label.Title's references reachable.
        derivedDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedButton);

        _selectedState.Setup(x => x.SelectedElement).Returns(derivedButton);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { derivedButton });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(derivedDefault);
        SelectInstances(textInDerived);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        derivedButton.Instances.Count.ShouldBe(2);
        InstanceSave newInstance = derivedButton.Instances.First(i => i != textInDerived);

        VariableSave? newFontSize = derivedDefault.Variables
            .FirstOrDefault(v => v.Name == $"{newInstance.Name}.FontSize");
        newFontSize.ShouldBeNull(
            "because FontSize is owned by Label.Title's VariableReferences (reachable via the active " +
            "TextCategoryState=Title), and the base orphan in BaseButton.Default should be dropped " +
            "rather than snapshotted onto the new instance.");
    }

    [Fact]
    public void OnPaste_Instance_ShouldCopySourceLevelVariable_EvenWhenReachableReferenceOwnsIt()
    {
        // Regression guard for the other side: if the source's OWN state has an
        // explicit variable that's also owned by a reachable ref, the explicit
        // value must still come through. (GUM0002 flags the conflict at runtime
        // — but copy/paste must preserve author intent at the source level.)
        ComponentSave label = new() { Name = "LabelForCopySource", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave titleState = new() { Name = "Title", ParentContainer = label };
        titleState.Variables.Add(new VariableSave
        {
            Name = "SourceFontSize",
            Type = "int",
            Value = 28,
            SetsValue = true
        });
        VariableListSave<string> titleRefs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        titleRefs.ValueAsIList.Add("FontSize = SourceFontSize");
        titleState.VariableLists.Add(titleRefs);
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave container = new() { Name = "ContainerForCopySource", BaseType = "Container" };
        StateSave containerDefault = new() { Name = "Default", ParentContainer = container };
        container.States.Add(containerDefault);
        InstanceSave textInstance = new()
        {
            Name = "TextInstance",
            BaseType = "LabelForCopySource",
            ParentContainer = container
        };
        container.Instances.Add(textInstance);
        // Both the categorized state assignment AND the explicit FontSize override authored
        // directly on the source's OWN state. Copy/paste must preserve the explicit value.
        containerDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });
        containerDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.FontSize",
            Type = "int",
            Value = 14,
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Components.Add(container);

        _selectedState.Setup(x => x.SelectedElement).Returns(container);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { container });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(containerDefault);
        SelectInstances(textInstance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave newInstance = container.Instances.First(i => i != textInstance);

        VariableSave? newFontSize = containerDefault.Variables
            .FirstOrDefault(v => v.Name == $"{newInstance.Name}.FontSize");
        newFontSize.ShouldNotBeNull(
            "because the explicit FontSize=14 was authored on the source's own state — " +
            "the ref-owned filter should only drop base-element captures, not directly-authored values.");
        newFontSize!.Value.ShouldBe(14);
    }

    [Fact]
    public void OnPaste_Instance_ShouldSkipBaseElementVariableReferencesRow()
    {
        // UpgradeButton repro continued: Button has TextInstance with an authored
        // VariableReferences row pointing at Strong styles. UpgradeButton derives
        // and sets TextInstance.TextCategoryState="Title" which should drive refs
        // via Label.Title.VariableReferences instead. Today, paste snapshots the
        // base's Strong ref row onto the new instance as TextInstance1.VariableReferences;
        // the snapshotted local row then SHADOWS the categorized-state walk because
        // GetVariableListRecursive returns the local explicit row first. Result:
        // pasted instance shows Strong, not Title. Fix: drop VariableReferences rows
        // from base captures so the categorized walk wins for the new instance too.
        ComponentSave label = new() { Name = "LabelForRefRow", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);
        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave titleState = new() { Name = "Title", ParentContainer = label };
        VariableListSave<string> titleRefs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        titleRefs.ValueAsIList.Add("Font = TitleFontMarker");
        titleState.VariableLists.Add(titleRefs);
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);
        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave baseButton = new() { Name = "BaseButtonForRefRow", BaseType = "Container" };
        StateSave baseDefault = new() { Name = "Default", ParentContainer = baseButton };
        baseButton.States.Add(baseDefault);
        baseButton.Instances.Add(new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "LabelForRefRow",
            ParentContainer = baseButton
        });
        // The base's authored ref row — should NOT be snapshotted onto the pasted instance.
        VariableListSave<string> baseRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        baseRefs.ValueAsIList.Add("Font = StrongFontMarker");
        baseDefault.VariableLists.Add(baseRefs);
        ObjectFinder.Self.GumProjectSave.Components.Add(baseButton);

        ComponentSave derivedButton = new() { Name = "DerivedButtonForRefRow", BaseType = "BaseButtonForRefRow" };
        StateSave derivedDefault = new() { Name = "Default", ParentContainer = derivedButton };
        derivedButton.States.Add(derivedDefault);
        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "LabelForRefRow",
            DefinedByBase = true,
            ParentContainer = derivedButton
        };
        derivedButton.Instances.Add(textInDerived);
        derivedDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });
        ObjectFinder.Self.GumProjectSave.Components.Add(derivedButton);

        _selectedState.Setup(x => x.SelectedElement).Returns(derivedButton);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { derivedButton });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(derivedDefault);
        SelectInstances(textInDerived);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave newInstance = derivedButton.Instances.First(i => i != textInDerived);

        VariableListSave? newRefRow = derivedDefault.VariableLists
            .FirstOrDefault(v => v.Name == $"{newInstance.Name}.VariableReferences");
        newRefRow.ShouldBeNull(
            "because the base element's inherited VariableReferences row should not be snapshotted " +
            "onto the new instance — otherwise it shadows the categorized state's reference walk.");
    }

    [Fact]
    public void OnPaste_Instance_ShouldSkipBaseElementVariable_WhenCategorizedStateDirectlySetsIt()
    {
        // Broader version of the ref-owned scalar filter: the matched categorized
        // state on the instance's BaseType can also set a scalar DIRECTLY (no
        // intermediate VariableReferences row). The base orphan still needs to be
        // dropped — otherwise the snapshotted base scalar shadows the categorized
        // state's value, exactly the same shape as the ref case.
        ComponentSave label = new() { Name = "LabelDirectSet", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);
        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave titleState = new() { Name = "Title", ParentContainer = label };
        // Direct scalar set, NO VariableReferences row.
        titleState.Variables.Add(new VariableSave
        {
            Name = "FontSize",
            Type = "int",
            Value = 28,
            SetsValue = true
        });
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);
        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave baseButton = new() { Name = "BaseButtonDirectSet", BaseType = "Container" };
        StateSave baseDefault = new() { Name = "Default", ParentContainer = baseButton };
        baseButton.States.Add(baseDefault);
        baseButton.Instances.Add(new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "LabelDirectSet",
            ParentContainer = baseButton
        });
        // The orphan: FontSize=14 on the base, conflicting with Title's direct FontSize=28.
        baseDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.FontSize",
            Type = "int",
            Value = 14,
            SetsValue = true
        });
        ObjectFinder.Self.GumProjectSave.Components.Add(baseButton);

        ComponentSave derivedButton = new() { Name = "DerivedButtonDirectSet", BaseType = "BaseButtonDirectSet" };
        StateSave derivedDefault = new() { Name = "Default", ParentContainer = derivedButton };
        derivedButton.States.Add(derivedDefault);
        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "LabelDirectSet",
            DefinedByBase = true,
            ParentContainer = derivedButton
        };
        derivedButton.Instances.Add(textInDerived);
        derivedDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });
        ObjectFinder.Self.GumProjectSave.Components.Add(derivedButton);

        _selectedState.Setup(x => x.SelectedElement).Returns(derivedButton);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { derivedButton });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(derivedDefault);
        SelectInstances(textInDerived);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave newInstance = derivedButton.Instances.First(i => i != textInDerived);

        VariableSave? newFontSize = derivedDefault.Variables
            .FirstOrDefault(v => v.Name == $"{newInstance.Name}.FontSize");
        newFontSize.ShouldBeNull(
            "because Label.Title directly sets FontSize=28; the base orphan should be dropped " +
            "the same way it is when the categorized state assigns via a VariableReferences row.");
    }

    [Fact]
    public void OnPaste_Instance_ShouldCopySourceLevelVariableReferencesRow()
    {
        // Regression guard: a VariableReferences row authored DIRECTLY on the source's
        // own state must still come through. Only base-element captures are filtered.
        ComponentSave component = new() { Name = "ComponentWithLocalRefs", BaseType = "Container" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave instance = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = component };
        component.Instances.Add(instance);

        VariableListSave<string> localRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        localRefs.ValueAsIList.Add("Red = SomeOtherInstance.Red");
        defaultState.VariableLists.Add(localRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(component);

        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedElements).Returns(new List<ElementSave> { component });
        _selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);
        SelectInstances(instance);

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave newInstance = component.Instances.First(i => i != instance);

        VariableListSave? newRefRow = defaultState.VariableLists
            .FirstOrDefault(v => v.Name == $"{newInstance.Name}.VariableReferences");
        newRefRow.ShouldNotBeNull(
            "because the VariableReferences row was authored on the source's own state.");
        newRefRow!.ValueAsIList.Count.ShouldBe(1);
        newRefRow.ValueAsIList[0].ShouldBe("Red = SomeOtherInstance.Red");
    }

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
        _currentSelectedInstances = new List<InstanceSave>(instances);

        _selectedState
            .Setup(x => x.SelectedInstance)
            .Returns(instances.First());
        _selectedState
            .Setup(x => x.SelectedInstances)
            .Returns(() => _currentSelectedInstances);

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
