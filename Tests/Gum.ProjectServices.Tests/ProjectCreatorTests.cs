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

    [Fact]
    public void Create_ShouldWriteStandardElementFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string standardsDir = Path.Combine(_tempDirectory, "Standards");
        string[] expectedElements = { "Circle", "ColoredRectangle", "Component", "Container",
            "NineSlice", "Polygon", "Rectangle", "Sprite", "Text" };

        foreach (string name in expectedElements)
        {
            File.Exists(Path.Combine(standardsDir, $"{name}.gutx")).ShouldBeTrue($"{name}.gutx should exist");
        }
    }

    [Fact]
    public void Create_ShouldWriteExampleSpriteFramePng()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        File.Exists(Path.Combine(_tempDirectory, "ExampleSpriteFrame.png")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldIncludeStandardElementReferences()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        GumProjectSave project = _sut.Create(filePath);

        project.StandardElementReferences.Count.ShouldBe(9);
        project.StandardElementReferences.ShouldContain(r => r.Name == "Container");
        project.StandardElementReferences.ShouldContain(r => r.Name == "NineSlice");
        project.StandardElementReferences.ShouldContain(r => r.Name == "Sprite");
        project.StandardElementReferences.ShouldContain(r => r.Name == "Text");
    }

    [Fact]
    public void Create_ShouldProduceProjectThatLoadsStandardElements()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        ProjectLoader loader = new ProjectLoader();
        ProjectLoadResult result = loader.Load(filePath);

        result.Success.ShouldBeTrue();
        result.Project!.StandardElements.Count.ShouldBe(9);
        result.Project.StandardElements.ShouldContain(e => e.Name == "Container");
        result.Project.StandardElements.ShouldContain(e => e.Name == "NineSlice");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
