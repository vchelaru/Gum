using Gum.Plugins.InternalPlugins.HideShowTools;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainHideShowToolsPlugin into the headless Gum.Presentation
/// assembly (ADR-0005 Phase 3) so this business logic is unit testable. MainPanelViewModel itself
/// is WPF-typed, so the dependency is narrowed to <see cref="IToolsVisibility"/>.
/// </summary>
public class HideShowToolsLogicTests
{
    private readonly Mock<IToolsVisibility> _toolsVisibility = new();
    private readonly HideShowToolsLogic _logic;

    public HideShowToolsLogicTests()
    {
        _logic = new HideShowToolsLogic(_toolsVisibility.Object);
    }

    [Fact]
    public void ToggleToolsVisibility_WhenHidden_ShowsAndEnsuresMinimumWidth()
    {
        _toolsVisibility.SetupProperty(x => x.IsToolsVisible, false);

        bool result = _logic.ToggleToolsVisibility();

        result.ShouldBeTrue();
        _toolsVisibility.Object.IsToolsVisible.ShouldBeTrue();
        _toolsVisibility.Verify(x => x.EnsureMinimumWidth(), Times.Once);
    }

    [Fact]
    public void ToggleToolsVisibility_WhenVisible_HidesAndDoesNotEnsureMinimumWidth()
    {
        _toolsVisibility.SetupProperty(x => x.IsToolsVisible, true);

        bool result = _logic.ToggleToolsVisibility();

        result.ShouldBeFalse();
        _toolsVisibility.Object.IsToolsVisible.ShouldBeFalse();
        _toolsVisibility.Verify(x => x.EnsureMinimumWidth(), Times.Never);
    }
}
