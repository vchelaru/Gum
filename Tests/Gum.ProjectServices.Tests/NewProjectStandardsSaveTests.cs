using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A brand new project seeds every built-in standard in memory
/// (<see cref="StandardElementsManager.PopulateProjectWithDefaultStandards"/>) before the first
/// save. The first save is expected to persist all of them to their own .gutx file -- exactly
/// what <see cref="GumProjectSave.Save(string, bool)"/> does when called with
/// <c>saveElements: true</c>, which is what the real new-project flow does on its first save.
/// </summary>
public class NewProjectStandardsSaveTests
{
    [Fact]
    public void Save_WritesAGutxFileForEverySeededStandardType()
    {
        StandardElementsManager.Self.Initialize();
        StandardElementsManager.Self.RegisterExtendedDefaultStates();

        GumProjectSave project = new GumProjectSave
        {
            Version = GumProjectSave.NativeVersion
        };
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        string tempDir = Path.Combine(Path.GetTempPath(), "GumNewProjectStandardsSaveTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Project.gumx");

        try
        {
            project.Save(gumxPath, saveElements: true);

            List<string> expectedNames = project.StandardElements.Select(s => s.Name).ToList();
            expectedNames.ShouldNotBeEmpty();

            List<string> missing = new List<string>();
            foreach (string name in expectedNames)
            {
                string expectedPath = Path.Combine(tempDir, "Standards", name + "." + GumProjectSave.StandardExtension);
                if (!File.Exists(expectedPath))
                {
                    missing.Add(name);
                }
            }

            missing.ShouldBeEmpty($"Standards missing a .gutx file after the first save: {string.Join(", ", missing)}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
