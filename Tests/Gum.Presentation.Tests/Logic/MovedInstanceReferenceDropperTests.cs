using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// Create Component moving Label (a child of Box) out of Button: each kind of reference that stops
/// applying is dropped, reported, and restored by one undo of the action the drop ran under.
/// </summary>
public class MovedInstanceReferenceDropperTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly GumProjectSave _project;
    private readonly UndoManager _undoManager;
    private readonly MovedInstanceReferenceDropper _dropper;
    private readonly ComponentSave _button;
    private readonly InstanceSave _box;
    private readonly InstanceSave _label;

    public MovedInstanceReferenceDropperTests()
    {
        _mocker = new AutoMocker();
        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;
        _mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());

        _undoManager = _mocker.CreateInstance<UndoManager>();
        _mocker.Use<IUndoManager>(_undoManager);
        _dropper = _mocker.CreateInstance<MovedInstanceReferenceDropper>();

        _button = AddComponent("Button", "Container");
        _box = AddInstance(_button, "Box", "Container");
        _label = AddInstance(_button, "Label", "Text");
        AddInstance(_button, "Caption", "Text");
        _button.DefaultState.SetValue("Label.Parent", "Box", "string");
    }

    [Fact]
    public void Drop_IgnoresWhatTheMoveCarries()
    {
        _button.DefaultState.SetValue("Label.Text", "Hi", "string");
        _button.DefaultState.VariableLists.Add(References("Label.VariableReferences", "Y = Label.X"));
        _button.DefaultState.VariableLists.Add(References("Box.VariableReferences", "Width = Label.Width"));

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldBeEmpty();
        _button.DefaultState.VariableLists.Count.ShouldBe(2);
    }

    [Fact]
    public void Drop_OutsideLineInTheElement_DropsTheLineKeepingItsValue_AndUndoRestoresIt()
    {
        _button.DefaultState.VariableLists.Add(References("Caption.VariableReferences", "X = Label.X", "Y = 4"));
        _button.DefaultState.SetValue("Caption.X", 5f, "float");

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldHaveSingleItem().ShouldBe(new MovedInstanceReference("Label",
            "Button (Default): Caption.VariableReferences line \"X = Label.X\"", "Caption.X = 5"));
        Lines(_button.DefaultState, "Caption.VariableReferences").ShouldBe(new[] { "Y = 4" });
        _button.DefaultState.GetValue("Caption.X").ShouldBe(5f);

        _undoManager.PerformUndo();

        Lines(_button.DefaultState, "Caption.VariableReferences").ShouldBe(new[] { "X = Label.X", "Y = 4" });
    }

    [Fact]
    public void Drop_LinesInDerivedAndOtherElements_RemovesTheirLists_AndUndoRestoresThem()
    {
        ComponentSave bigButton = AddComponent("BigButton", "Button");
        bigButton.DefaultState.VariableLists.Add(References("Caption.VariableReferences", "Y = Label.Y"));
        bigButton.DefaultState.SetValue("Caption.Y", 2f, "float");
        ScreenSave menu = AddScreen("Menu");
        AddInstance(menu, "OkButton", "Button");
        menu.DefaultState.VariableLists.Add(References("VariableReferences", "X = Components/Button.Label.X"));
        menu.DefaultState.SetValue("X", 7f, "float");

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldBe(new[]
        {
            new MovedInstanceReference("Label", "BigButton (Default): Caption.VariableReferences line \"Y = Label.Y\"", "Caption.Y = 2"),
            new MovedInstanceReference("Label", "Menu (Default): VariableReferences line \"X = Components/Button.Label.X\"", "X = 7"),
        }, ignoreOrder: true);
        bigButton.DefaultState.VariableLists.ShouldBeEmpty();
        menu.DefaultState.VariableLists.ShouldBeEmpty();
        menu.DefaultState.GetValue("X").ShouldBe(7f);

        _undoManager.PerformUndo();

        Lines(bigButton.DefaultState, "Caption.VariableReferences").ShouldBe(new[] { "Y = Label.Y" });
        Lines(menu.DefaultState, "VariableReferences").ShouldBe(new[] { "X = Components/Button.Label.X" });
    }

    [Fact]
    public void Drop_MovedInstanceValuesInOtherStates_AndValuesNamingIt_AndUndoRestoresThem()
    {
        StateSave hover = AddCategoryState(_button, "Look", "Hover");
        hover.SetValue("Label.Red", 10, "int");
        hover.SetValue("Caption.Parent", "Label", "string");
        hover.VariableLists.Add(References("Label.VariableReferences", "Blue = Caption.Blue"));
        _button.DefaultState.SetValue("DefaultChildContainer", "Label", "string");

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.Select(item => item.Description).ShouldBe(new[]
        {
            "Button (Hover): Label.Red = 10",
            "Button (Hover): Caption.Parent = Label",
            "Button (Hover): Label.VariableReferences",
            "Button: DefaultChildContainer = Label",
        }, ignoreOrder: true);
        hover.Variables.ShouldBeEmpty();
        hover.VariableLists.ShouldBeEmpty();
        _button.DefaultState.GetValue("DefaultChildContainer").ShouldBeNull();

        _undoManager.PerformUndo();

        StateSave restoredHover = _button.Categories.Single().States.Single();
        restoredHover.GetValue("Label.Red").ShouldBe(10);
        restoredHover.GetValue("Caption.Parent").ShouldBe("Label");
        Lines(restoredHover, "Label.VariableReferences").ShouldBe(new[] { "Blue = Caption.Blue" });
        _button.DefaultState.GetValue("DefaultChildContainer").ShouldBe("Label");
    }

    [Fact]
    public void Drop_DerivedElementOverridesAndParentsThroughAnInstance_AndUndoRestoresThem()
    {
        ComponentSave bigButton = AddComponent("BigButton", "Button");
        bigButton.DefaultState.SetValue("Label.FontSize", 30, "int");
        bigButton.DefaultState.VariableLists.Add(References("Label.VariableReferences", "Red = Caption.Red"));
        ScreenSave menu = AddScreen("Menu");
        AddInstance(menu, "OkButton", "Button");
        AddInstance(menu, "Icon", "Sprite");
        menu.DefaultState.SetValue("Icon.Parent", "OkButton.Label", "string");

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.Select(item => item.Description).ShouldBe(new[]
        {
            "BigButton (Default): Label.FontSize = 30",
            "BigButton (Default): Label.VariableReferences",
            "Menu: Icon.Parent = OkButton.Label",
        }, ignoreOrder: true);
        bigButton.DefaultState.Variables.ShouldBeEmpty();
        bigButton.DefaultState.VariableLists.ShouldBeEmpty();
        menu.DefaultState.GetValue("Icon.Parent").ShouldBeNull();

        _undoManager.PerformUndo();

        bigButton.DefaultState.GetValue("Label.FontSize").ShouldBe(30);
        Lines(bigButton.DefaultState, "Label.VariableReferences").ShouldBe(new[] { "Red = Caption.Red" });
        menu.DefaultState.GetValue("Icon.Parent").ShouldBe("OkButton.Label");
    }

    [Fact]
    public void Drop_ExposedVariable_DropsWhatInstancesSetOnIt_AndUndoRestoresIt()
    {
        _button.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Label.Text", Type = "string", Value = "Hi", SetsValue = true, ExposedAsName = "LabelText"
        });
        ScreenSave menu = AddScreen("Menu");
        AddInstance(menu, "OkButton", "Button");
        menu.DefaultState.SetValue("OkButton.LabelText", "Ok", "string");
        ComponentSave fancyButton = AddComponent("FancyButton", "Button");
        AddInstance(fancyButton, "Icon", "Text");
        fancyButton.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Icon.Text", Type = "string", Value = "*", SetsValue = true, ExposedAsName = "LabelText"
        });

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.Select(item => item.Description).ShouldBe(new[]
        {
            "Button: exposed variable LabelText (Label.Text)",
            "Menu (Default): OkButton.LabelText = Ok",
        }, ignoreOrder: true);
        menu.DefaultState.GetValue("OkButton.LabelText").ShouldBeNull();
        fancyButton.DefaultState.GetValue("Icon.Text").ShouldBe("*");

        _undoManager.PerformUndo();

        menu.DefaultState.GetValue("OkButton.LabelText").ShouldBe("Ok");
    }

    [Fact]
    public void Drop_SubAnimationsOfAMovedInstance_RemovesTheirKeyframes()
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationSave show = new AnimationSave { Name = "Show" };
        show.Animations.Add(new AnimationReferenceSave { Name = "Label.FadeIn" });
        animations.Animations.Add(show);
        Mock<IAnimationUndoProvider> animationProvider = _mocker.GetMock<IAnimationUndoProvider>();
        animationProvider.Setup(x => x.GetCurrentAnimations(_button)).Returns(animations);

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldHaveSingleItem().Description.ShouldBe("Button: animation Show plays Label.FadeIn");
        animationProvider.Verify(x => x.RemoveKeyframesPlaying(_button, "Label"), Times.Once);
    }

    [Fact]
    public void Drop_ReportsLinesOwnedByMovedInstancesThatReadOutside()
    {
        _button.DefaultState.VariableLists.Add(References("Label.VariableReferences", "X = Caption.X", "Y = Label.X"));
        _button.DefaultState.SetValue("Label.X", 3f, "float");
        _button.DefaultState.VariableLists.Add(References("Box.VariableReferences", "Width = Caption.Width"));
        _button.DefaultState.SetValue("Box.Width", 40f, "float");

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldBe(new[]
        {
            new MovedInstanceReference("Label", "Button (Default): Label.VariableReferences line \"X = Caption.X\"", "Label.X = 3"),
            new MovedInstanceReference("Box", "Button (Default): Box.VariableReferences line \"Width = Caption.Width\"", "Box.Width = 40"),
        }, ignoreOrder: true);
    }

    [Fact]
    public void Drop_PromotedInstanceLineInAnotherStateNamingAMovedInstance_DropsTheLine()
    {
        StateSave hover = AddCategoryState(_button, "Look", "Hover");
        hover.VariableLists.Add(References("Box.VariableReferences", "Width = Label.Width", "Height = Caption.Height"));

        MovedInstanceReference[] result = DropInsideUndoLock();

        result.ShouldHaveSingleItem().Description.ShouldBe("Button (Hover): Box.VariableReferences line \"Width = Label.Width\"");
        Lines(hover, "Box.VariableReferences").ShouldBe(new[] { "Height = Caption.Height" });
    }

    private MovedInstanceReference[] DropInsideUndoLock()
    {
        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElement).Returns(_button);
        selectedState.Setup(x => x.SelectedStateContainer).Returns(_button);
        selectedState.Setup(x => x.SelectedComponent).Returns(_button);
        selectedState.Setup(x => x.SelectedStateSave).Returns(_button.DefaultState);
        _undoManager.RecordState();

        using UndoLock undoLock = _undoManager.RequestLock();
        MovedInstanceReference[] result = _dropper.DropReferencesBrokenByMove(_button, _box, new[] { _label });
        // The move itself, which the undo action is recorded for.
        _button.Instances.Remove(_label);
        return result;
    }

    private static List<string> Lines(StateSave state, string listName) =>
        state.VariableLists.Single(item => item.Name == listName).ValueAsIList.Cast<string>().ToList();

    private static VariableListSave<string> References(string name, params string[] lines) =>
        new VariableListSave<string> { Name = name, Type = "string", Value = lines.ToList() };

    private ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        _project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);
        _project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
        return screen;
    }

    private static InstanceSave AddInstance(ElementSave element, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element };
        element.Instances.Add(instance);
        return instance;
    }

    private static StateSave AddCategoryState(ElementSave element, string categoryName, string stateName)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        StateSave state = new StateSave { Name = stateName, ParentContainer = element };
        category.States.Add(state);
        element.Categories.Add(category);
        return state;
    }
}
