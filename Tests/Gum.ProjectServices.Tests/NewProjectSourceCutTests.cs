using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A project made by File > New (#8354) is at a version that shows the NineSlice per-side source cut,
/// and its NineSlice standard carries the four sides, in both file formats and with or without Forms.
/// </summary>
public class NewProjectSourceCutTests
{
    [Theory]
    [InlineData("GumProject.gumx", false)]
    [InlineData("GumProject.gumx", true)]
    [InlineData("GumProject.gumj", false)]
    [InlineData("GumProject.gumj", true)]
    public void NewProject_HasTheSourceCutSides_AtAVersionThatShowsThem(string fileName, bool includeForms)
    {
        string folder = Path.Combine(Path.GetTempPath(), "gum-8354-" + Path.GetRandomFileName());
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, fileName);
            GumProjectTemplateWriter.Write(path, includeForms, themeDirectory: null);

            GumProjectSave project = GumProjectSave.Load(path)!;

            project.Version.ShouldBeGreaterThanOrEqualTo((int)GumProjectSave.GumxVersions.BbCodeTextEffects);
            StateSave nineSlice = project.StandardElements.Single(item => item.Name == "NineSlice").DefaultState;
            foreach (string side in new[] { "Left", "Top", "Right", "Bottom" })
            {
                nineSlice.Variables.ShouldContain(v => v.Name == "CustomFrameTextureCoordinate" + side, side);
            }
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
