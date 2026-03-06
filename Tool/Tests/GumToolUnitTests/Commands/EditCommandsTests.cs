using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using ToolsUtilities;

namespace GumToolUnitTests.Commands;

public class EditCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly EditCommands _editCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IReferenceFinder> _referenceFinder;
    private readonly GumProjectSave _gumProject;

    public EditCommandsTests()
    {
        _mocker = new AutoMocker();

        _dialogService = _mocker.GetMock<IDialogService>();
        _fileCommands = _mocker.GetMock<IFileCommands>();
        _projectManager = _mocker.GetMock<IProjectManager>();
        _referenceFinder = _mocker.GetMock<IReferenceFinder>();

        _gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _gumProject;
        _projectManager.Setup(m => m.GumProjectSave).Returns(_gumProject);

        // Use the real NameVerifier to avoid mocking out parameters
        _mocker.Use<INameVerifier>(new NameVerifier());

        // Default: GetFullPathXmlFile returns a non-existent path so no file deletion occurs
        _fileCommands
            .Setup(f => f.GetFullPathXmlFile(It.IsAny<BehaviorSave>()))
            .Returns(new FilePath("C:/nonexistent/test.behx"));

        // Default: no element references to the behavior
        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(It.IsAny<BehaviorSave>(), It.IsAny<string>()))
            .Returns(new BehaviorReferences());

        _editCommands = _mocker.CreateInstance<EditCommands>();
    }

    [Fact]
    public void AskToRenameBehavior_UpdatesElementBehaviorReferences()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _gumProject.Behaviors.Add(behavior);
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = "MyBehavior" });

        var component = new ComponentSave { Name = "MyButton" };
        var behaviorRef = new ElementBehaviorReference { BehaviorName = "MyBehavior" };
        component.Behaviors.Add(behaviorRef);
        _gumProject.Components.Add(component);

        var refs = new BehaviorReferences();
        refs.ElementsWithBehaviorReference.Add((component, behaviorRef));
        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(behavior, "MyBehavior"))
            .Returns(refs);

        _dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("MyBehaviorRenamed");

        _editCommands.AskToRenameBehavior(behavior);

        behaviorRef.BehaviorName.ShouldBe("MyBehaviorRenamed");
    }

    [Fact]
    public void AskToRenameBehavior_UpdatesProjectBehaviorReference()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _gumProject.Behaviors.Add(behavior);
        var projectRef = new BehaviorReference { Name = "MyBehavior" };
        _gumProject.BehaviorReferences.Add(projectRef);

        _dialogService
            .Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("MyBehaviorRenamed");

        _editCommands.AskToRenameBehavior(behavior);

        projectRef.Name.ShouldBe("MyBehaviorRenamed");
        behavior.Name.ShouldBe("MyBehaviorRenamed");
    }
}
