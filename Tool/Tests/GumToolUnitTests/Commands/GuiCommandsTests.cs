using Gum.Commands;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System;

namespace GumToolUnitTests.Commands;

/// <summary>
/// Characterization (pinning) test for <see cref="GuiCommands.ShowSpinner"/>, which used to
/// directly <c>new</c> the concrete WPF <c>Gum.Controls.Spinner</c>. Now pins that it delegates
/// to the injected <see cref="ISpinnerFactory"/> seam instead (#3956).
/// </summary>
public class GuiCommandsTests
{
    private static GuiCommands CreateSut(ISpinnerFactory spinnerFactory, IDispatcher? dispatcher = null)
    {
        return new GuiCommands(
            new Lazy<ISelectedState>(() => new Mock<ISelectedState>().Object),
            dispatcher ?? new Mock<IDispatcher>().Object,
            new Mock<IOutputManager>().Object,
            new Lazy<PropertyGridManager>(() => throw new InvalidOperationException("Not needed by this test.")),
            new Mock<IPluginManager>().Object,
            spinnerFactory);
    }

    /// <summary>
    /// Its only caller (ToolFontGenerationCallbacks.SpinnerHandle.Dispose) runs on whichever
    /// thread a background Task continuation happens to resume on after font generation, not
    /// necessarily the UI thread - calling Application.Current.MainWindow/Activate() inline
    /// crashed with a Dispatcher cross-thread-access exception (#4253).
    /// </summary>
    [Fact]
    public void ActivateMainWindow_PostsWorkToDispatcher_InsteadOfRunningInline()
    {
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        GuiCommands sut = CreateSut(new Mock<ISpinnerFactory>().Object, dispatcher.Object);

        sut.ActivateMainWindow();

        dispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void ShowSpinner_ReturnsSpinnerFromInjectedFactory()
    {
        Mock<ISpinner> spinner = new Mock<ISpinner>();
        Mock<ISpinnerFactory> spinnerFactory = new Mock<ISpinnerFactory>();
        spinnerFactory.Setup(f => f.Create()).Returns(spinner.Object);

        GuiCommands sut = CreateSut(spinnerFactory.Object);

        ISpinner result = sut.ShowSpinner();

        result.ShouldBe(spinner.Object);
        spinnerFactory.Verify(f => f.Create(), Times.Once);
    }
}
