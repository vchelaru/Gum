using Gum.DataTypes;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class GumProjectSaveTests
{
    [Fact]
    public void Save_ShouldNotRecreateStandardElementFile_WhenSourceFileIsMissing()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumProjectSaveTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            // Repro #3369: the user deleted Arc.gutx on disk and declined the "Recreate?" prompt.
            // The loader keeps an in-memory stub flagged IsSourceFileMissing. A subsequent project
            // save (e.g. the load-time re-save) must NOT silently rewrite that file — but a present
            // standard must still save normally.
            GumProjectSave project = new GumProjectSave();
            project.StandardElements.Add(new StandardElementSave { Name = "Arc", IsSourceFileMissing = true });
            project.StandardElements.Add(new StandardElementSave { Name = "Container" });

            project.Save(gumxPath, saveElements: true);

            File.Exists(Path.Combine(tempDir, "Standards", "Arc.gutx")).ShouldBeFalse();
            File.Exists(Path.Combine(tempDir, "Standards", "Container.gutx")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Save_ShouldNotRecreateScreenOrComponentFile_WhenSourceFileIsMissing()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumProjectSaveTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            // The same hazard applies to missing screens and components, not just standards: the
            // load-time re-save loops over all three element types in GumProjectSave.Save.
            GumProjectSave project = new GumProjectSave();
            project.Screens.Add(new ScreenSave { Name = "GhostScreen", IsSourceFileMissing = true });
            project.Screens.Add(new ScreenSave { Name = "RealScreen" });
            project.Components.Add(new ComponentSave { Name = "GhostComponent", IsSourceFileMissing = true });
            project.Components.Add(new ComponentSave { Name = "RealComponent" });

            project.Save(gumxPath, saveElements: true);

            File.Exists(Path.Combine(tempDir, "Screens", "GhostScreen.gusx")).ShouldBeFalse();
            File.Exists(Path.Combine(tempDir, "Screens", "RealScreen.gusx")).ShouldBeTrue();
            File.Exists(Path.Combine(tempDir, "Components", "GhostComponent.gucx")).ShouldBeFalse();
            File.Exists(Path.Combine(tempDir, "Components", "RealComponent.gucx")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
