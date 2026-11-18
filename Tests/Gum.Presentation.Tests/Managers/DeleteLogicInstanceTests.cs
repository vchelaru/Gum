using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.Editing;
using Gum.Services;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class DeleteLogicInstanceTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DeleteLogic _deleteLogic;
    private readonly GumProjectSave _project;

    public DeleteLogicInstanceTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IReferenceFinderProjectProvider>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);
        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());
        _mocker.Use<IVariableOverrideRemover>(new VariableOverrideRemover());
        _mocker.Use<IInstanceRemovalLogic>(_mocker.CreateInstance<InstanceRemovalLogic>());
        _mocker.Use<IElementRemovalLogic>(new ElementRemovalLogic());
        _mocker.Use<IInstanceOverrideRemover>(_mocker.CreateInstance<InstanceOverrideRemover>());

        _deleteLogic = _mocker.CreateInstance<DeleteLogic>();
    }

    [Fact]
    public void RemoveInstance_ExposedVariableSetElsewhere_RemovesValueSavesNotifiesAndRecordsForUndo()
    {
        ComponentSave hud = new ComponentSave { Name = "Hud" };
        hud.States.Add(new StateSave { Name = "Default", ParentContainer = hud });
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        hud.Instances.Add(counter);
        hud.GetDefaultStateOrThrow().Variables.Add(new VariableSave { Name = "Counter1.Text", Type = "string", ExposedAsName = "Counter1Text" });
        _project.Components.Add(hud);
        ScreenSave game = new ScreenSave { Name = "Game" };
        game.States.Add(new StateSave { Name = "Default", ParentContainer = game });
        InstanceSave hudInstance = new InstanceSave { Name = "HudInstance", BaseType = "Hud", ParentContainer = game };
        game.Instances.Add(hudInstance);
        VariableSave gameValue = new VariableSave { Name = "HudInstance.Counter1Text", Type = "string", Value = "5" };
        game.GetDefaultStateOrThrow().Variables.Add(gameValue);
        _project.Screens.Add(game);

        _deleteLogic.RemoveInstance(counter, hud);

        hud.Instances.ShouldBeEmpty();
        hud.GetDefaultStateOrThrow().Variables.ShouldBeEmpty();
        game.GetDefaultStateOrThrow().Variables.ShouldBeEmpty();
        _mocker.GetMock<IFileCommands>().Verify(x => x.TryAutoSaveElement(game), Times.Once);
        _mocker.GetMock<IPluginManager>().Verify(x => x.VariableSet(game, hudInstance, "Counter1Text", null, It.IsAny<bool>()), Times.Once);
        _mocker.GetMock<IUndoManager>().Verify(x => x.RecordCrossElementVariableChanges(
            It.Is<IEnumerable<CrossElementVariableChange>>(changes => changes.Single().Before!.Name == gameValue.Name)), Times.Once);
    }
}
