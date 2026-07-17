using Gum.Commands;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class AlignmentViewModelTests
{
    private static AlignmentViewModel CreateViewModel()
    {
        CommonControlLogic commonControlLogic = new(
            Mock.Of<ISelectedState>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>());

        return new AlignmentViewModel(commonControlLogic, Mock.Of<ISelectedState>(), Mock.Of<IUndoManager>());
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
}
