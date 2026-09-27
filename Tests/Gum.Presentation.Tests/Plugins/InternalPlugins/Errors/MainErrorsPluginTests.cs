using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Errors;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Errors;

/// <summary>
/// The Errors tab re-checks the selected element whenever a variable is set. The check walks the
/// element's file references and hits the disk, so it must only run for a committed value - not on
/// every tick of a drag (issue #4946).
/// </summary>
public class MainErrorsPluginTests : BaseTestClass
{
    private readonly Mock<IErrorChecker> _errorChecker = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly MainErrorsPlugin _plugin;
    private readonly List<Action> _posted = new();
    private AllErrorsViewModel _viewModel = null!;

    public MainErrorsPluginTests()
    {
        _project = new GumProjectSave();
        _screen = new ScreenSave { Name = "MyScreen" };
        _project.Screens.Add(_screen);
        _projectState.Setup(p => p.GumProjectSave).Returns(_project);
        _errorChecker.Setup(c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);

        Mock<ITabManager> tabManager = new();
        tabManager.Setup(t => t.AddControl(It.IsAny<object>(), "Errors", It.IsAny<TabLocation>()))
            .Callback<object, string, TabLocation>((content, _, _) => _viewModel = (AllErrorsViewModel)content)
            .Returns(Mock.Of<IPluginTab>());
        _errorChecker.Setup(c => c.GetProjectErrors(It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);
        Mock<IDispatcher> dispatcher = new();
        dispatcher.Setup(d => d.Post(It.IsAny<Action>())).Callback<Action>(_posted.Add);

        _plugin = new MainErrorsPlugin(
            _errorChecker.Object,
            _messenger,
            _selectedState.Object,
            Mock.Of<IClipboardService>(),
            Mock.Of<IFileSystemRevealService>(),
            _projectState.Object,
            dispatcher.Object)
        {
            TabManager = tabManager.Object,
        };
        _plugin.StartUp();
    }

    [Fact]
    public void VariableSet_IntermediateCommit_DoesNotCheckForErrors()
    {
        _plugin.CallVariableSet(_screen, null, "Red", 0, isFullCommit: false);

        _errorChecker.Verify(
            c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()),
            Times.Never);
    }

    /// <summary>
    /// The tree's "!" indicator doesn't check on these notifications itself; it shows the result of
    /// this check (issue #4950).
    /// </summary>
    [Theory]
    [InlineData("VariableSet")]
    [InlineData("InstanceAdd")]
    [InlineData("InstanceDelete")]
    [InlineData("ElementReloaded")]
    [InlineData("VariableRemovedFromCategory")]
    [InlineData("BehaviorReferencesChanged")]
    [InlineData("RequestErrorRefreshMessage")]
    public void ANotificationTheTreeIndicatorReliesOn_ChecksTheElementOnce(string notification)
    {
        _selectedState.Setup(s => s.SelectedElement).Returns(_screen);
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Sprite", ParentContainer = _screen };

        switch (notification)
        {
            case "VariableSet":
                _plugin.CallVariableSet(_screen, null, "Red", 0, isFullCommit: true);
                break;
            case "InstanceAdd":
                _plugin.CallInstanceAdd(_screen, instance);
                break;
            case "InstanceDelete":
                _plugin.CallInstanceDelete(_screen, instance);
                break;
            case "ElementReloaded":
                _plugin.CallElementReloaded(_screen);
                break;
            case "VariableRemovedFromCategory":
                _plugin.CallVariableRemovedFromCategory("Red", new StateSaveCategory { Name = "Category" });
                break;
            case "BehaviorReferencesChanged":
                _plugin.CallBehaviorReferencesChanged(_screen);
                break;
            case "RequestErrorRefreshMessage":
                _messenger.Send(new RequestErrorRefreshMessage());
                break;
        }

        _errorChecker.Verify(c => c.GetErrorsFor(_screen, _project), Times.Once);
    }

    /// <summary>
    /// Project-level rows (#5262) come from a pass over the whole project: it runs once per burst of
    /// load, save and file-change notifications, and a selection change reuses its result.
    /// </summary>
    [Fact]
    public void ProjectErrors_RunOncePerBurst_AndStayListedAcrossSelectionChanges()
    {
        ErrorViewModel projectError = new ErrorViewModel { Code = "GUM0008", Message = "Strings.csv" };
        _errorChecker.Setup(c => c.GetProjectErrors(_project)).Returns(new[] { projectError });

        _plugin.CallProjectLoad(_project);
        _plugin.CallProjectSave(_project);
        _plugin.CallReactToFileChanged(new ToolsUtilities.FilePath("C:/Game/strings.csv"));
        _posted.ShouldHaveSingleItem().Invoke();
        _plugin.CallElementSelected(_screen);
        _plugin.CallElementSelected(null);

        _errorChecker.Verify(c => c.GetProjectErrors(_project), Times.Once);
        _viewModel.Errors.ShouldBe(new[] { projectError });

        _plugin.CallReactToFileChanged(new ToolsUtilities.FilePath("C:/Game/strings.csv"));
        _posted.Count.ShouldBe(2, "a notification after the pass ran queues another");
    }

    [Fact]
    public void ProjectLoad_DropsThePreviousProjectsRows_BeforeItsPassRuns()
    {
        _errorChecker.Setup(c => c.GetProjectErrors(_project))
            .Returns(new[] { new ErrorViewModel { Code = "GUM0008", Message = "old project" } });
        _plugin.CallProjectLoad(_project);
        _posted.Single().Invoke();

        _plugin.CallProjectLoad(_project);
        _plugin.CallElementSelected(_screen);

        _viewModel.Errors.ShouldBeEmpty();
    }
}
