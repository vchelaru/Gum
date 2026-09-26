using Gum.DataTypes;
using Gum.ProjectServices.Screenshot;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ScreenshotRequest.ResolveSize"/>, the output size both <c>gumcli screenshot</c>
/// backends render at.
/// </summary>
public class ScreenshotRequestTests
{
    [Fact]
    public void ResolveSize_WithExplicitSize_UsesRequestSize()
    {
        ScreenshotRequest request = CreateRequest(width: 200, height: 150);
        GumProjectSave project = new GumProjectSave { DefaultCanvasWidth = 1366, DefaultCanvasHeight = 768 };

        (int width, int height) = request.ResolveSize(project);

        width.ShouldBe(200);
        height.ShouldBe(150);
    }

    [Fact]
    public void ResolveSize_WithNoSize_UsesProjectCanvasSize()
    {
        ScreenshotRequest request = CreateRequest(width: null, height: null);
        GumProjectSave project = new GumProjectSave { DefaultCanvasWidth = 1366, DefaultCanvasHeight = 768 };

        (int width, int height) = request.ResolveSize(project);

        width.ShouldBe(1366);
        height.ShouldBe(768);
    }

    [Fact]
    public void ResolveSize_WithNoSizeAndNoProjectCanvasSize_FallsBackTo800By600()
    {
        ScreenshotRequest request = CreateRequest(width: null, height: null);
        GumProjectSave project = new GumProjectSave { DefaultCanvasWidth = 0, DefaultCanvasHeight = 0 };

        (int width, int height) = request.ResolveSize(project);

        width.ShouldBe(800);
        height.ShouldBe(600);
    }

    private static ScreenshotRequest CreateRequest(int? width, int? height) => new ScreenshotRequest
    {
        ProjectPath = "Project.gumx",
        ElementName = "Screen",
        OutputPath = "Screen.png",
        Width = width,
        Height = height,
    };
}
