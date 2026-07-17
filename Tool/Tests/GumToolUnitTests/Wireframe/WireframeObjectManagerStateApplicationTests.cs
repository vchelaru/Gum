using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Guards against the default state being applied twice per wireframe rebuild. The GUE returned by
/// CreateGraphicalUiElement has already had its default state applied (ToGraphicalUiElement calls
/// SetInitialState), so RefreshAll's post-creation step must not re-apply it. Re-applying it pushed
/// every default-state variable to the renderable twice — e.g. a missing Sprite SourceFile reported
/// its error twice per rebuild (issue #3212).
/// </summary>
public class WireframeObjectManagerStateApplicationTests : BaseTestClass
{
    private sealed class CountingGue : GraphicalUiElement
    {
        public int ApplyStateCount { get; private set; }

        // Only the call count matters here; skip base application to keep the bare GUE robust.
        public override void ApplyState(StateSave state) => ApplyStateCount++;
    }

    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly WireframeObjectManager _wireframeObjectManager;
    private readonly ScreenSave _screen;
    private readonly StateSave _defaultState;

    public WireframeObjectManagerStateApplicationTests()
    {
        _screen = new ScreenSave { Name = "TestScreen" };
        _defaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(_defaultState);

        GumProjectSave project = new();
        project.Screens.Add(_screen);
        ObjectFinder.Self.GumProjectSave = project;

        _wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            _selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectState>());
    }

    [Fact]
    public void ApplySelectedStateAfterCreation_AppliesSelectedNonDefaultStateOnce()
    {
        StateSave nonDefaultState = new() { Name = "Highlighted", ParentContainer = _screen };
        _selectedState.Setup(x => x.SelectedStateSave).Returns(nonDefaultState);
        CountingGue gue = new();

        _wireframeObjectManager.ApplySelectedStateAfterCreation(gue, _screen);

        // Only the selected non-default state is applied; the default is not re-applied.
        gue.ApplyStateCount.ShouldBe(1);
    }

    [Fact]
    public void ApplySelectedStateAfterCreation_DoesNotReapplyDefaultState()
    {
        _selectedState.Setup(x => x.SelectedStateSave).Returns(_defaultState);
        CountingGue gue = new();

        _wireframeObjectManager.ApplySelectedStateAfterCreation(gue, _screen);

        gue.ApplyStateCount.ShouldBe(0);
    }
}
