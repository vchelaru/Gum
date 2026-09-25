using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class DeleteLogicStateTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DeleteLogic _deleteLogic;
    private readonly GumProjectSave _project;

    public DeleteLogicStateTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IReferenceFinderProjectProvider>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);
        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());

        _deleteLogic = _mocker.CreateInstance<DeleteLogic>();
    }

    [Fact]
    public void RemoveState_CategorizedStateUsedByInstance_RemovesReferencingVariableAndSavesContainer()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSaveCategory visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        StateSave shownState = new StateSave { Name = "Shown", ParentContainer = button };
        StateSave hiddenState = new StateSave { Name = "Hidden", ParentContainer = button };
        visibilityCategory.States.Add(shownState);
        visibilityCategory.States.Add(hiddenState);
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "shownButton", BaseType = "Button", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "hiddenButton", BaseType = "Button", ParentContainer = screen });
        VariableSave shownVariable = new VariableSave { Name = "shownButton.VisibilityState", Value = "Shown" };
        VariableSave hiddenVariable = new VariableSave { Name = "hiddenButton.VisibilityState", Value = "Hidden" };
        screenDefault.Variables.Add(shownVariable);
        screenDefault.Variables.Add(hiddenVariable);
        _project.Screens.Add(screen);

        _deleteLogic.RemoveState(shownState, button);

        screenDefault.Variables.ShouldNotContain(shownVariable);
        screenDefault.Variables.ShouldContain(hiddenVariable);
        _mocker.GetMock<IFileCommands>().Verify(x => x.TryAutoSaveElement(screen), Times.Once);
    }
}
