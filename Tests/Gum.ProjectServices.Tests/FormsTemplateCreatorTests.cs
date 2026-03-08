using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FormsTemplateCreatorTests : IDisposable
{
    private readonly FormsTemplateCreator _sut;
    private readonly string _tempDirectory;

    public FormsTemplateCreatorTests()
    {
        _sut = new FormsTemplateCreator();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFormsTemplateCreatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Create_ShouldCreateBehaviorFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string behaviorsDir = Path.Combine(_tempDirectory, "Behaviors");
        File.Exists(Path.Combine(behaviorsDir, "ButtonBehavior.behx")).ShouldBeTrue();
        File.Exists(Path.Combine(behaviorsDir, "ListBoxBehavior.behx")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateControlComponents()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string controlsDir = Path.Combine(_tempDirectory, "Components", "Controls");
        File.Exists(Path.Combine(controlsDir, "ButtonStandard.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(controlsDir, "CheckBox.gucx")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateGumxFile()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        File.Exists(filePath).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateStandardFiles()
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
    public void Create_ShouldCreateUISpriteSheet()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        File.Exists(Path.Combine(_tempDirectory, "UISpriteSheet.png")).ShouldBeTrue();
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
    public void Create_ShouldRenameGumxToMatchProjectName()
    {
        string filePath = Path.Combine(_tempDirectory, "MyCustomProject.gumx");

        _sut.Create(filePath);

        File.Exists(filePath).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "GumProject.gumx")).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
