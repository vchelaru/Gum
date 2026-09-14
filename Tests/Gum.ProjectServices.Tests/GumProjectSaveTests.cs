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

    [SkippableFact]
    public void Load_ShouldResolveFullFileNameToOnDiskCasing_WhenRequestedPathCasingDiffers()
    {
        // Repro #4687: on a case-insensitive filesystem (Windows/macOS), a project can be requested
        // via a path whose casing no longer matches disk (e.g. a stale recent-projects entry recorded
        // before the file was externally renamed to its canonical casing). Load() must resolve
        // FullFileName to the real on-disk name - a later Save() writes to FullFileName verbatim, so
        // a stale casing here silently renames the file back and breaks case-sensitive consumers like
        // `gumcli pack`.
        string tempDir = Path.Combine(Path.GetTempPath(), "GumProjectSaveTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string onDiskPath = Path.Combine(tempDir, "GumProject.gumx");
        try
        {
            new GumProjectSave().Save(onDiskPath, saveElements: false);

            string requestedPath = Path.Combine(tempDir, "gumproject.gumx");
            // On a case-sensitive filesystem (Linux) the differently-cased path is simply a different,
            // absent file, so the drift this guards against cannot happen there.
            Skip.IfNot(File.Exists(requestedPath), "case-sensitive filesystem");
            GumProjectSave loaded = GumProjectSave.Load(requestedPath);

            Path.GetFileName(loaded.FullFileName).ShouldBe("GumProject.gumx");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
