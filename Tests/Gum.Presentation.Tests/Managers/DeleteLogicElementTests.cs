using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class DeleteLogicElementTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DeleteLogic _deleteLogic;
    private readonly GumProjectSave _project;

    public DeleteLogicElementTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IDeleteProjectProvider>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);
        _mocker.Use<IElementRemovalLogic>(new ElementRemovalLogic());

        _deleteLogic = _mocker.CreateInstance<DeleteLogic>();
    }

    [Fact]
    public void RemoveElement_Component_RemovesItNotifiesAndSavesTheProject()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        _project.Components.Add(button);
        _project.ComponentReferences.Add(new ElementReference { Name = "Button", ElementType = ElementType.Component });

        _deleteLogic.RemoveElement(button);

        _project.Components.ShouldBeEmpty();
        _project.ComponentReferences.ShouldBeEmpty();
        _mocker.GetMock<IDeletePluginNotifier>().Verify(x => x.ElementDelete(button), Times.Once);
        _mocker.GetMock<IFileCommands>().Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
    }
}
