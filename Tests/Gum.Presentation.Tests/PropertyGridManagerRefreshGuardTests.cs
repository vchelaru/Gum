using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.PropertyGridHelpers;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// RefreshEntireGrid ignores calls while one is already running. A refresh that throws must not leave
/// that guard set, or the Variables tab never refreshes again for the rest of the session.
/// </summary>
public class PropertyGridManagerRefreshGuardTests : BaseTestClass
{
    [Fact]
    public void RefreshEntireGrid_ShouldRunAgain_WhenThePreviousRefreshThrew()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupSequence(x => x.SelectedElement)
            .Throws(new InvalidOperationException("selection failed"))
            .Returns(screen);
        PropertyGridManager sut = new PropertyGridManager(
            selectedState.Object,
            Mock.Of<IExposeVariableService>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IDialogService>(),
            new LocalizationService(),
            Mock.Of<ITabManager>(),
            Mock.Of<IWireframeObjectManager>(),
            new TypeManager(),
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectState>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<ICompositeMemberRegistry>(),
            Mock.Of<INameVerifier>(),
            Mock.Of<IDeleteVariableService>(),
            Mock.Of<IEditVariableService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IVariableSaveLogic>(),
            Mock.Of<IClipboardService>(),
            Mock.Of<IStateEditingIndicatorService>(),
            Mock.Of<IVariableGridHead>());
        Should.Throw<InvalidOperationException>(() => sut.RefreshEntireGrid());

        // The second refresh may fail further in (the view was never created), which is not what is
        // under test: it only has to get past the guard and read the selection again.
        Record.Exception(() => sut.RefreshEntireGrid());

        selectedState.Verify(x => x.SelectedElement, Times.AtLeast(2));
    }
}
