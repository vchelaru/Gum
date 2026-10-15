using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

public class VariableReferenceConflictFixerTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IVariableReferenceLogic> _variableReferenceLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly WeakReferenceMessenger _messenger;
    private readonly VariableReferenceConflictFixer _sut;
    private readonly GumProjectSave _project;
    private readonly HeadlessErrorChecker _checker;
    private bool _answer;
    private Action? _onAsked;
    private string? _shownMessage;
    private int _undoLocksOpen;
    private int _refreshRequests;

    public VariableReferenceConflictFixerTests()
    {
        _answer = true;
        _dialogService = new Mock<IDialogService>();
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns((string message, string? _, MessageDialogStyle? _) =>
            {
                _shownMessage = message;
                _onAsked?.Invoke();
                return _answer ? MessageDialogResult.Affirmative : MessageDialogResult.Negative;
            });
        _undoManager = new Mock<IUndoManager>();
        _undoManager.Setup(x => x.RequestLock(It.IsAny<ElementSave>())).Returns(() =>
        {
            _undoLocksOpen++;
            return new UndoLock(() => _undoLocksOpen--);
        });
        _pluginManager = new Mock<IPluginManager>();
        _variableReferenceLogic = new Mock<IVariableReferenceLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new WeakReferenceMessenger();
        _messenger.Register<RequestErrorRefreshMessage>(this, (_, _) => _refreshRequests++);

        _sut = new VariableReferenceConflictFixer(
            _dialogService.Object,
            _undoManager.Object,
            _pluginManager.Object,
            _variableReferenceLogic.Object,
            Mock.Of<IWireframeObjectManager>(),
            _guiCommands.Object,
            _fileCommands.Object,
            _messenger);

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;
        _checker = new HeadlessErrorChecker(new DefaultTypeResolver());
    }

    private ComponentSave CreateComponentWithStaleX(float staleValue, float referencedValue)
    {
        ComponentSave component = new ComponentSave { Name = "Stale", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "SourceX", Type = "float", Value = referencedValue, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = staleValue, SetsValue = true });
        VariableListSave<string> refs = new VariableListSave<string> { Name = "VariableReferences", Type = "string" };
        refs.Value.Add("X = SourceX");
        state.VariableLists.Add(refs);
        _project.Components.Add(component);
        return component;
    }

    private VariableReferenceConflict GetConflict(ElementSave element, string variableName)
    {
        return _checker.GetErrorsFor(element, _project)
            .Select(error => error.ReferenceConflict)
            .First(conflict => conflict != null && conflict.VariableName == variableName)!;
    }

    [Fact]
    public void Fix_ReplacesStaleValueAndKeepsReference_WhenConfirmed()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        VariableReferenceConflict conflict = GetConflict(component, "X");

        _sut.Fix(conflict);

        StateSave state = component.DefaultState;
        state.GetValue("X").ShouldBe(6f);
        state.VariableLists.Single(list => list.GetRootName() == "VariableReferences")
            .ValueAsIList.Cast<string>().ShouldBe(new[] { "X = SourceX" });
        _checker.GetErrorsFor(component, _project).ShouldNotContain(error => error.Code == "GUM0002");
    }

    [Fact]
    public void Fix_ShowsOldAndNewValue_BeforeChangingAnything()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        VariableReferenceConflict conflict = GetConflict(component, "X");
        object? valueWhenAsked = null;
        _onAsked = () => valueWhenAsked = component.DefaultState.GetValue("X");

        _sut.Fix(conflict);

        valueWhenAsked.ShouldBe(255f);
        _shownMessage.ShouldNotBeNull().ShouldContain("255");
        _shownMessage.ShouldContain("6");
        _shownMessage.ShouldContain("X");
    }

    [Fact]
    public void Fix_ChangesNothing_WhenDeclined()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        VariableReferenceConflict conflict = GetConflict(component, "X");
        _answer = false;

        _sut.Fix(conflict);

        component.DefaultState.GetValue("X").ShouldBe(255f);
        _undoManager.Verify(x => x.RequestLock(It.IsAny<ElementSave>()), Times.Never);
        _fileCommands.Verify(x => x.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never);
        _refreshRequests.ShouldBe(0);
    }

    [Fact]
    public void Fix_RecordsUndoSaveNotifyAndRefreshesErrors()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        VariableReferenceConflict conflict = GetConflict(component, "X");
        int locksOpenWhenNotified = -1;
        _pluginManager
            .Setup(x => x.VariableSet(component, null, "X", 255f, true))
            .Callback(() => locksOpenWhenNotified = _undoLocksOpen);

        _sut.Fix(conflict);

        _undoManager.Verify(x => x.RequestLock(component), Times.Once);
        locksOpenWhenNotified.ShouldBe(1, "so the plugins' reaction undoes together with the value");
        _pluginManager.Verify(x => x.VariableSet(component, null, "X", 255f, true), Times.Once);
        _variableReferenceLogic.Verify(x => x.ApplyReferencesToElement(component, true, true), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(component), Times.Once);
        _guiCommands.Verify(x => x.RefreshVariableValues(), Times.Once);
        _refreshRequests.ShouldBe(1);
        _undoLocksOpen.ShouldBe(0);
    }

    [Fact]
    public void Fix_LeavesOtherStaleValuesOnTheStateAlone()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        StateSave state = component.DefaultState;
        state.Variables.Add(new VariableSave { Name = "SourceY", Type = "float", Value = 9f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 77f, SetsValue = true });
        state.VariableLists.Single().ValueAsIList.Add("Y = SourceY");
        VariableReferenceConflict conflict = GetConflict(component, "X");

        _sut.Fix(conflict);

        state.GetValue("X").ShouldBe(6f);
        state.GetValue("Y").ShouldBe(77f);
        _checker.GetErrorsFor(component, _project)
            .Where(error => error.Code == "GUM0002")
            .Select(error => error.ReferenceConflict!.VariableName).ShouldBe(new[] { "Y" });
    }

    [Fact]
    public void Fix_ReplacesInstanceValue_WhenReferenceIsOnTheInstance()
    {
        ComponentSave component = new ComponentSave { Name = "InstanceStale", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        component.Instances.Add(new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = component });
        state.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 40f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Child.Width", Type = "float", Value = 5f, SetsValue = true });
        VariableListSave<string> refs = new VariableListSave<string> { Name = "Child.VariableReferences", Type = "string" };
        refs.Value.Add("Width = Width");
        state.VariableLists.Add(refs);
        _project.Components.Add(component);
        VariableReferenceConflict conflict = GetConflict(component, "Child.Width");

        _sut.Fix(conflict);

        state.GetValue("Child.Width").ShouldBe(40f);
        _pluginManager.Verify(x => x.VariableSet(component, component.GetInstance("Child"), "Width", 5f, true), Times.Once);
    }

    [Fact]
    public void Fix_ReplacesLocalValue_WhenReferenceIsOnInheritedCategorizedState()
    {
        ComponentSave label = new ComponentSave { Name = "LabelToFix", BaseType = "Container" };
        label.States.Add(new StateSave { Name = "Default", ParentContainer = label });
        StateSaveCategory category = new StateSaveCategory { Name = "TextCategory" };
        StateSave title = new StateSave { Name = "Title", ParentContainer = label };
        title.Variables.Add(new VariableSave { Name = "SourceFontSize", Type = "float", Value = 100f, SetsValue = true });
        VariableListSave<string> titleRefs = new VariableListSave<string> { Name = "VariableReferences", Type = "string" };
        titleRefs.Value.Add("FontSize = SourceFontSize");
        title.VariableLists.Add(titleRefs);
        category.States.Add(title);
        label.Categories.Add(category);
        _project.Components.Add(label);

        ComponentSave button = new ComponentSave { Name = "ButtonToFix", BaseType = "Container" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);
        button.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "LabelToFix", ParentContainer = button });
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState", Type = "TextCategory", Value = "Title", SetsValue = true
        });
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.FontSize", Type = "float", Value = 14f, SetsValue = true
        });
        _project.Components.Add(button);
        VariableReferenceConflict conflict = GetConflict(button, "TextInstance.FontSize");

        _sut.Fix(conflict);

        buttonDefault.GetValue("TextInstance.FontSize").ShouldBe(100f);
        title.VariableLists.Single().ValueAsIList.Cast<string>().ShouldBe(new[] { "FontSize = SourceFontSize" });
        _checker.GetErrorsFor(button, _project).ShouldNotContain(error => error.Code == "GUM0002");
    }

    [Fact]
    public void Fix_DoesNothing_WhenTheStateIsNoLongerPartOfTheElement()
    {
        ComponentSave component = CreateComponentWithStaleX(staleValue: 255f, referencedValue: 6f);
        VariableReferenceConflict conflict = GetConflict(component, "X");
        // An undo replaces the element's states with clones, orphaning the state the row captured.
        component.States.Clear();

        _sut.Fix(conflict);

        conflict.State.GetValue("X").ShouldBe(255f);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
    }
}
