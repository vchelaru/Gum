using Gum.DataTypes;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ProjectCreatorTests : IDisposable
{
    private readonly ProjectCreator _sut;
    private readonly string _tempDirectory;

    public ProjectCreatorTests()
    {
        _sut = new ProjectCreator();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumProjectCreatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Create_ShouldCreateGumxFile()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        GumProjectSave project = _sut.Create(filePath);

        File.Exists(filePath).ShouldBeTrue();
        project.ShouldNotBeNull();
    }

    [Fact]
    public void Create_ShouldCreateStandardSubfolders()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        Directory.Exists(Path.Combine(_tempDirectory, "Screens")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Components")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Standards")).ShouldBeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "Behaviors")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldProduceLoadableProject()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        ProjectLoader loader = new ProjectLoader();
        ProjectLoadResult result = loader.Load(filePath);

        result.Success.ShouldBeTrue();
        result.Project.ShouldNotBeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
