using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class AlignmentViewModelTests
{
    private static AlignmentViewModel CreateViewModel(IStateEditingIndicatorService? stateEditingIndicatorService = null)
    {
        CommonControlLogic commonControlLogic = new(
            Mock.Of<ISelectedState>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>());

        return new AlignmentViewModel(commonControlLogic, Mock.Of<ISelectedState>(), Mock.Of<IUndoManager>(),
            stateEditingIndicatorService ?? Mock.Of<IStateEditingIndicatorService>(), Mock.Of<IOutputManager>());
    }

    private static AlignmentViewModel CreateViewModel(ISelectedState selectedState, IGuiCommands guiCommands, IOutputManager outputManager)
    {
        CommonControlLogic commonControlLogic = new(
            selectedState,
            Mock.Of<IWireframeCommands>(),
            guiCommands,
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>());

        return new AlignmentViewModel(commonControlLogic, selectedState, Mock.Of<IUndoManager>(),
            Mock.Of<IStateEditingIndicatorService>(), outputManager);
    }

    private static Mock<ISelectedState> SelectInstances(StateSave state, params InstanceSave[] instances)
    {
        Mock<ISelectedState> selectedState = new();
        selectedState.Setup(s => s.SelectedStateSave).Returns(state);
        selectedState.Setup(s => s.SelectedInstances).Returns(instances);
        selectedState.Setup(s => s.SelectedInstance).Returns(instances.FirstOrDefault());
        return selectedState;
    }

    [Fact]
    public void SizeToChildren_ChangesOnlyTheUnlockedInstances_AndNamesTheLockedOnesInOutput()
    {
        StateSave state = new();
        InstanceSave locked = new() { Name = "Panel", Locked = true };
        InstanceSave unlocked = new() { Name = "Label" };
        Mock<IOutputManager> output = new();
        AlignmentViewModel viewModel = CreateViewModel(SelectInstances(state, locked, unlocked).Object, Mock.Of<IGuiCommands>(), output.Object);

        viewModel.SizeToChildren_Click();

        state.GetValue("Label.WidthUnits").ShouldBe(DimensionUnitType.RelativeToChildren);
        state.GetValue("Panel.WidthUnits").ShouldBeNull();
        state.GetValue("Panel.Width").ShouldBeNull();
        output.Verify(o => o.AddOutput(It.Is<string>(line => line.Contains("Panel") && !line.Contains("Label"))), Times.Once);
    }

    [Fact]
    public void SizeToChildren_WithOnlyALockedInstanceSelected_ChangesNothing()
    {
        StateSave state = new();
        InstanceSave locked = new() { Name = "Panel", Locked = true };
        Mock<IGuiCommands> guiCommands = new();
        Mock<IOutputManager> output = new();
        AlignmentViewModel viewModel = CreateViewModel(SelectInstances(state, locked).Object, guiCommands.Object, output.Object);

        viewModel.SizeToChildren_Click();

        state.Variables.ShouldBeEmpty();
        guiCommands.Verify(g => g.RefreshVariables(It.IsAny<bool>()), Times.Never);
        output.Verify(o => o.AddOutput(It.Is<string>(line => line.Contains("Panel"))), Times.Once);
    }

    [Fact]
    public void IsMarginTextVisible_IsFalse_WhenDockMarginIsZero()
    {
        AlignmentViewModel viewModel = CreateViewModel();

        viewModel.IsMarginTextVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsMarginTextVisible_IsTrue_WhenDockMarginIsNonZero()
    {
        AlignmentViewModel viewModel = CreateViewModel();

        viewModel.DockMarginText = "5";

        viewModel.IsMarginTextVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsMarginTextVisible_IsFalse_WhenDockMarginIsResetToZero()
    {
        AlignmentViewModel viewModel = CreateViewModel();
        viewModel.DockMarginText = "5";

        viewModel.DockMarginText = "0";

        viewModel.IsMarginTextVisible.ShouldBeFalse();
    }

    [Fact]
    public void NormalizeNegativeZero_ConvertsNegativeZeroToPositiveZero()
    {
        float negativeZero = -0f * 2f;

        negativeZero.ShouldBe(0f);
        float.IsNegative(negativeZero).ShouldBeTrue();

        float normalized = AlignmentViewModel.NormalizeNegativeZero(negativeZero);

        float.IsNegative(normalized).ShouldBeFalse();
    }

    [Fact]
    public void NormalizeNegativeZero_LeavesPositiveZeroAlone()
    {
        float normalized = AlignmentViewModel.NormalizeNegativeZero(0f);

        float.IsNegative(normalized).ShouldBeFalse();
        normalized.ShouldBe(0f);
    }

    [Fact]
    public void NormalizeNegativeZero_LeavesNonZeroValuesUnchanged()
    {
        AlignmentViewModel.NormalizeNegativeZero(-5f).ShouldBe(-5f);
        AlignmentViewModel.NormalizeNegativeZero(5f).ShouldBe(5f);
        AlignmentViewModel.NormalizeNegativeZero(-0.001f).ShouldBe(-0.001f);
    }

    [Fact]
    public void RefreshStateLabel_CopiesInfoFromStateEditingIndicatorService()
    {
        var stateEditingIndicatorService = new Mock<IStateEditingIndicatorService>();
        stateEditingIndicatorService
            .Setup(s => s.GetInfo())
            .Returns(new StateEditingIndicatorInfo(true, "Editing state MyState", System.Drawing.Color.Yellow));
        AlignmentViewModel viewModel = CreateViewModel(stateEditingIndicatorService.Object);

        viewModel.RefreshStateLabel();

        viewModel.HasStateInformation.ShouldBeTrue();
        viewModel.StateInformation.ShouldBe("Editing state MyState");
        viewModel.StateBackground.ShouldBe(System.Drawing.Color.Yellow);
    }
}
