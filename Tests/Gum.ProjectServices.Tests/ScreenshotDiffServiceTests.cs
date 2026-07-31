using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Screenshot;
using Moq;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ScreenshotDiffService"/>, which renders every Screen and Component in a
/// project through two <see cref="IScreenshotService"/> backends and diffs each pair (#4174) — the
/// orchestration behind <c>gumcli diff-screenshots</c>. Backends are mocked so this suite exercises
/// enumeration/tolerance/aggregation logic without a real MonoGame or raylib render.
/// </summary>
public class ScreenshotDiffServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ScreenshotDiffServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumScreenshotDiffTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Diff_BothBackendsRenderIdenticalPixels_ReportsAllElementsMatching()
    {
        string projectPath = CreateProjectWithScreenAndComponent();

        Mock<IScreenshotService> backendA = MockBackendWritingSolidColor(SKColors.Red);
        Mock<IScreenshotService> backendB = MockBackendWritingSolidColor(SKColors.Red);

        ScreenshotDiffService service = new ScreenshotDiffService();
        ScreenshotDiffResult result = service.Diff(new ScreenshotDiffRequest
        {
            ProjectPath = projectPath,
            BackendA = backendA.Object,
            BackendB = backendB.Object,
            OutputDirectory = _tempDirectory,
        });

        result.HasMismatch.ShouldBeFalse();
        result.ElementDiffs.Count.ShouldBe(2);
        result.ElementDiffs.ShouldAllBe(d => d.Matches);
    }

    [Fact]
    public void Diff_OneElementRendersDifferentColor_ReportsThatElementAsMismatch()
    {
        string projectPath = CreateProjectWithScreenAndComponent();

        Mock<IScreenshotService> backendA = MockBackendWritingSolidColor(SKColors.Red);
        Mock<IScreenshotService> backendB = new Mock<IScreenshotService>();
        backendB.Setup(s => s.TakeScreenshot(It.IsAny<ScreenshotRequest>()))
            .Returns((ScreenshotRequest request) =>
            {
                SKColor color = request.ElementName == "MismatchedScreen" ? SKColors.Blue : SKColors.Red;
                WriteSolidPng(request.OutputPath, color);
                return ScreenshotResult.Succeeded(request.OutputPath);
            });

        ScreenshotDiffService service = new ScreenshotDiffService();
        ScreenshotDiffResult result = service.Diff(new ScreenshotDiffRequest
        {
            ProjectPath = projectPath,
            BackendA = backendA.Object,
            BackendB = backendB.Object,
            OutputDirectory = _tempDirectory,
        });

        result.HasMismatch.ShouldBeTrue();
        ElementScreenshotDiff mismatched = result.ElementDiffs.Single(d => d.ElementName == "MismatchedScreen");
        mismatched.Matches.ShouldBeFalse();
        ElementScreenshotDiff matched = result.ElementDiffs.Single(d => d.ElementName != "MismatchedScreen");
        matched.Matches.ShouldBeTrue();
    }

    [Fact]
    public void Diff_BackendReturnsFailure_ReportsElementAsMismatchWithErrorMessage()
    {
        string projectPath = CreateProjectWithScreenAndComponent();

        Mock<IScreenshotService> backendA = MockBackendWritingSolidColor(SKColors.Red);
        Mock<IScreenshotService> backendB = new Mock<IScreenshotService>();
        backendB.Setup(s => s.TakeScreenshot(It.IsAny<ScreenshotRequest>()))
            .Returns(ScreenshotResult.Failed("simulated render failure"));

        ScreenshotDiffService service = new ScreenshotDiffService();
        ScreenshotDiffResult result = service.Diff(new ScreenshotDiffRequest
        {
            ProjectPath = projectPath,
            BackendA = backendA.Object,
            BackendB = backendB.Object,
            OutputDirectory = _tempDirectory,
        });

        result.HasMismatch.ShouldBeTrue();
        result.ElementDiffs.ShouldAllBe(d => !d.Matches && d.ErrorMessage == "simulated render failure");
    }

    [Fact]
    public void Diff_ProjectFileDoesNotExist_ThrowsInvalidOperationException()
    {
        ScreenshotDiffService service = new ScreenshotDiffService();

        Should.Throw<InvalidOperationException>(() => service.Diff(new ScreenshotDiffRequest
        {
            ProjectPath = Path.Combine(_tempDirectory, "DoesNotExist.gumx"),
            BackendA = Mock.Of<IScreenshotService>(),
            BackendB = Mock.Of<IScreenshotService>(),
            OutputDirectory = _tempDirectory,
        }));
    }

    private static Mock<IScreenshotService> MockBackendWritingSolidColor(SKColor color)
    {
        Mock<IScreenshotService> backend = new Mock<IScreenshotService>();
        backend.Setup(s => s.TakeScreenshot(It.IsAny<ScreenshotRequest>()))
            .Returns((ScreenshotRequest request) =>
            {
                WriteSolidPng(request.OutputPath, color);
                return ScreenshotResult.Succeeded(request.OutputPath);
            });
        return backend;
    }

    private static void WriteSolidPng(string path, SKColor color)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        using SKBitmap bitmap = new SKBitmap(4, 4);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, quality: 100);
        using FileStream stream = File.Create(path);
        encoded.SaveTo(stream);
    }

    private string CreateProjectWithScreenAndComponent()
    {
        string projectPath = Path.Combine(_tempDirectory, "Project.gumx");

        ProjectCreator creator = new ProjectCreator();
        GumProjectSave project = creator.Create(projectPath);

        ScreenSave screen = new ScreenSave { Name = "MismatchedScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });

        ComponentSave component = new ComponentSave { Name = "MatchingComponent" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });

        project.Save(projectPath, saveElements: true);

        return projectPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
