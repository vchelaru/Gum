using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ProjectServices;
using Gum.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers <see cref="PreviewGumxProjectionService"/> (issue #4748): projecting a .gumx project to a
/// temporary JSON copy so the Native AOT preview build can serve it.
/// </summary>
public class PreviewGumxProjectionServiceTests
{
    [Fact]
    public void Project_ReturnsConvertedPathAndTheProjectsOwnDirectoryAsContentRoot()
    {
        // Forward slashes only, matching this class's own path convention - avoids a Windows-style
        // "C:\..." literal, which is meaningless on the Linux/macOS CI legs this suite also runs on.
        string gumxPath = "/MyGame/GumProject.gumx";
        GumProjectSave project = new GumProjectSave { FullFileName = gumxPath };
        FakeConvertService convertService = new FakeConvertService(resultProjectFilePath: "/Temp/SomeHash/GumProject.gumj");
        PreviewGumxProjectionService service = new PreviewGumxProjectionService(convertService, new PathCaseSensitivity());

        PreviewGumxProjection projection = service.Project(project);

        projection.ProjectFilePath.ShouldBe("/Temp/SomeHash/GumProject.gumj");
        projection.ContentRootDirectory.ShouldBe("/MyGame/");
        convertService.LastProject.ShouldBeSameAs(project);
    }

    [Fact]
    public void Project_CalledTwiceForTheSameProjectPath_UsesTheSameOutputDirectoryBothTimes()
    {
        // The same project gets re-projected after every save while a preview stays open (see
        // PreviewLauncher.RefreshIfRunning) - GumPreview's own hot-reload watcher only picks up the
        // change if both writes land in the directory it is already watching.
        GumProjectSave project = new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" };
        FakeConvertService convertService = new FakeConvertService(resultProjectFilePath: "unused");
        PreviewGumxProjectionService service = new PreviewGumxProjectionService(convertService, new PathCaseSensitivity());

        service.Project(project);
        string firstOutputDirectory = convertService.LastOutputDirectory!;
        service.Project(project);
        string secondOutputDirectory = convertService.LastOutputDirectory!;

        firstOutputDirectory.ShouldBe(secondOutputDirectory);
    }

    // Where the file system keeps Proj.gumx and proj.gumx apart, they are two projects, and each
    // preview must watch its own copy. Returns early where no case-sensitive directory can be made.
    [Fact]
    public void Project_ForTwoProjectsWhoseNamesDifferOnlyByCase_UsesSeparateOutputDirectories()
    {
        string? folder = CaseSensitiveTempDirectory.TryCreate();
        if (folder == null)
        {
            return;
        }
        try
        {
            string upperPath = System.IO.Path.Combine(folder, "Proj.gumx");
            string lowerPath = System.IO.Path.Combine(folder, "proj.gumx");
            System.IO.File.WriteAllText(upperPath, string.Empty);
            System.IO.File.WriteAllText(lowerPath, string.Empty);
            FakeConvertService convertService = new FakeConvertService(resultProjectFilePath: "unused");
            PreviewGumxProjectionService service = new PreviewGumxProjectionService(convertService, new PathCaseSensitivity());

            service.Project(new GumProjectSave { FullFileName = upperPath });
            string upperOutputDirectory = convertService.LastOutputDirectory!;
            service.Project(new GumProjectSave { FullFileName = lowerPath });
            string lowerOutputDirectory = convertService.LastOutputDirectory!;

            lowerOutputDirectory.ShouldNotBe(upperOutputDirectory);
        }
        finally
        {
            System.IO.Directory.Delete(folder, recursive: true);
        }
    }

    private class FakeConvertService : IConvertProjectToJsonService
    {
        private readonly string _resultProjectFilePath;

        public FakeConvertService(string resultProjectFilePath)
        {
            _resultProjectFilePath = resultProjectFilePath;
        }

        public GumProjectSave? LastProject { get; private set; }
        public string? LastOutputDirectory { get; private set; }

        public ConvertProjectToJsonResult ConvertToJson(GumProjectSave project) =>
            throw new System.NotSupportedException("PreviewGumxProjectionService always calls the output-directory overload.");

        public ConvertProjectToJsonResult ConvertToJson(GumProjectSave project, string outputDirectory)
        {
            LastProject = project;
            LastOutputDirectory = outputDirectory;
            return new ConvertProjectToJsonResult { ProjectFilePath = _resultProjectFilePath };
        }
    }
}
