using Gum.Commands;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Moq;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class ScreenshotServiceTests : BaseTestClass
{
    // ScreenshotService now takes IWireframeCommands + IGuiCommands via its constructor (drained
    // from Locator). These pin that, until a screenshot is actually requested, the render hooks are
    // no-ops that never touch the injected dependencies. Because that no-op path never dereferences
    // the SelectionManager either, passing null for it here is safe (and avoids standing up its
    // ~14-argument graph just to verify a guard).
    private readonly Mock<IWireframeCommands> _wireframeCommands = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly ScreenshotService _screenshotService;

    public ScreenshotServiceTests()
    {
        _screenshotService = new ScreenshotService(
            selectionManager: null!,
            _wireframeCommands.Object,
            _guiCommands.Object);
    }

    [Fact]
    public void HandleAfterRender_does_nothing_when_no_screenshot_is_requested()
    {
        _screenshotService.HandleAfterRender();

        _wireframeCommands.VerifyNoOtherCalls();
        _guiCommands.VerifyNoOtherCalls();
    }

    [Fact]
    public void HandleBeforeRender_does_nothing_when_no_screenshot_is_requested()
    {
        _screenshotService.HandleBeforeRender();

        _wireframeCommands.VerifyNoOtherCalls();
    }
}
