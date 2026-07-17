using Gum.Managers;
using Gum.Settings;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace GumToolUnitTests.ToolStates;

/// <summary>
/// Pins ProjectState's flattened GeneralSettingsFile properties (ADR-0005 Phase 3): IProjectState
/// used to expose the whole WinForms-entangled GeneralSettingsFile, which blocked any consumer of
/// IProjectState from moving into the headless Gum.Presentation assembly. Narrowed to the two
/// fields real usages actually read.
/// </summary>
public class ProjectStateTests
{
    [Fact]
    public void EffectiveUseStandardsPalette_ReturnsValueFromProjectManagerGeneralSettingsFile()
    {
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(x => x.GeneralSettingsFile).Returns(new GeneralSettingsFile { UseStandardsPalette = false });
        ProjectState projectState = new(projectManager.Object);

        projectState.EffectiveUseStandardsPalette.ShouldBeFalse();
    }

    [Fact]
    public void OutlineColor_ReturnsValuesFromProjectManagerGeneralSettingsFile()
    {
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(x => x.GeneralSettingsFile).Returns(new GeneralSettingsFile
        {
            OutlineColorR = 10,
            OutlineColorG = 20,
            OutlineColorB = 30
        });
        ProjectState projectState = new(projectManager.Object);

        projectState.OutlineColorR.ShouldBe((byte)10);
        projectState.OutlineColorG.ShouldBe((byte)20);
        projectState.OutlineColorB.ShouldBe((byte)30);
    }
}
