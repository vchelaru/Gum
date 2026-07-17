using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace GumToolUnitTests.ToolStates;

/// <summary>
/// Pins ProjectState's flattened GeneralSettingsFile properties (ADR-0005 Phase 3): IProjectState
/// used to expose the whole WinForms-entangled GeneralSettingsFile, which blocked any consumer of
/// IProjectState from moving into the headless Gum.Presentation assembly. Narrowed to the two
/// fields real usages actually read. IProjectManager was narrowed the same way (#3754) so
/// ProjectState's own dependency stays headless too.
/// </summary>
public class ProjectStateTests
{
    [Fact]
    public void EffectiveUseStandardsPalette_ReturnsValueFromProjectManager()
    {
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(x => x.EffectiveUseStandardsPalette).Returns(false);
        ProjectState projectState = new(projectManager.Object);

        projectState.EffectiveUseStandardsPalette.ShouldBeFalse();
    }

    [Fact]
    public void OutlineColor_ReturnsValuesFromProjectManager()
    {
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(x => x.OutlineColorR).Returns((byte)10);
        projectManager.Setup(x => x.OutlineColorG).Returns((byte)20);
        projectManager.Setup(x => x.OutlineColorB).Returns((byte)30);
        ProjectState projectState = new(projectManager.Object);

        projectState.OutlineColorR.ShouldBe((byte)10);
        projectState.OutlineColorG.ShouldBe((byte)20);
        projectState.OutlineColorB.ShouldBe((byte)30);
    }
}
