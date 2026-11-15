using Gum.DataTypes;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Project-level editor settings of a new project come from one place (<see cref="NewProjectDefaults"/>), never from
/// whichever template file the project was copied from (#7461).
/// </summary>
public class NewProjectSettingsTests : NewProjectServiceTestBase
{
    public static IEnumerable<object?[]> Templates()
    {
        yield return new object?[] { "empty", null };
        yield return new object?[] { "forms", null };
        string themes = Path.Combine(GumSourceLocator.Find(AppContext.BaseDirectory)!, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");
        foreach (string theme in ThemeNames(themes))
        {
            yield return new object?[] { "forms", theme };
        }
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public async Task NewProject_ShouldStartWithTheCentralDefaults_ForEveryTemplate(string template, string? theme)
    {
        foreach (bool xml in new[] { true, false })
        {
            NewProjectResult result = await Run(Request(null, name: "G" + xml, template: template, theme: theme, xml: xml));
            result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);

            ProjectLoadResult load = new ProjectLoader().Load(result.Files.GumProjectPath!);
            load.Success.ShouldBeTrue(load.ErrorMessage);
            GumProjectSave project = load.Project!;
            GumProjectSave expected = new GumProjectSave();

            project.ShowOutlines.ShouldBeTrue();
            project.ShowOutlines.ShouldBe(expected.ShowOutlines);
            project.ShowCanvasOutline.ShouldBe(expected.ShowCanvasOutline);
            project.ShowRuler.ShouldBe(expected.ShowRuler);
            project.ShowCheckerBackground.ShouldBe(expected.ShowCheckerBackground);
            project.SnapToGrid.ShouldBe(expected.SnapToGrid);
            project.GridSize.ShouldBe(expected.GridSize);
            project.TextureFilter.ShouldBe(expected.TextureFilter);
            project.RestrictToUnitValues.ShouldBe(expected.RestrictToUnitValues);
        }
    }
}
