using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.SvgExportPlugin;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainSvgExportPlugin into the headless Gum.Presentation assembly
/// (ADR-0005 Phase 3) so this decision logic is unit testable. The actual gumcli invocation
/// (SvgExportCommand) is a separate, plugin-side class and is not exercised here.
/// </summary>
public class SvgExportMenuLogicTests
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly SvgExportMenuLogic _logic;

    public SvgExportMenuLogicTests()
    {
        _logic = new SvgExportMenuLogic(_selectedState.Object, _projectState.Object, _guiCommands.Object);
    }

    [Fact]
    public void GetIsExportable_ScreenSelected_ReturnsTrue()
    {
        ScreenSave screen = new() { Name = "MyScreen" };
        _selectedState.Setup(x => x.SelectedElement).Returns(screen);

        bool result = _logic.GetIsExportable(out ElementSave? element);

        result.ShouldBeTrue();
        element.ShouldBe(screen);
    }

    [Fact]
    public void GetIsExportable_NothingSelected_ReturnsFalse()
    {
        _selectedState.Setup(x => x.SelectedElement).Returns((ElementSave?)null);

        bool result = _logic.GetIsExportable(out ElementSave? element);

        result.ShouldBeFalse();
        element.ShouldBeNull();
    }

    [Fact]
    public void TryPrepareExport_ComponentSelectedAndProjectLoaded_ReturnsTrue()
    {
        ComponentSave component = new() { Name = "MyComponent" };
        GumProjectSave project = new();
        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _projectState.Setup(x => x.GumProjectSave).Returns(project);

        bool result = _logic.TryPrepareExport(out ElementSave? element, out GumProjectSave? projectSave);

        result.ShouldBeTrue();
        element.ShouldBe(component);
        projectSave.ShouldBe(project);
        _guiCommands.Verify(x => x.PrintOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TryPrepareExport_NoElementSelected_ReturnsFalseAndPrintsNothing()
    {
        _selectedState.Setup(x => x.SelectedElement).Returns((ElementSave?)null);

        bool result = _logic.TryPrepareExport(out _, out _);

        result.ShouldBeFalse();
        _guiCommands.Verify(x => x.PrintOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TryPrepareExport_NoProjectLoaded_ReturnsFalseAndPrintsMessage()
    {
        ComponentSave component = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _projectState.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);

        bool result = _logic.TryPrepareExport(out _, out GumProjectSave? projectSave);

        result.ShouldBeFalse();
        projectSave.ShouldBeNull();
        _guiCommands.Verify(x => x.PrintOutput("No project is loaded."), Times.Once);
    }
}
