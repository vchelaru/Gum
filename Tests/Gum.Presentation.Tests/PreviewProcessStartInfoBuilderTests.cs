using System.Diagnostics;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class PreviewProcessStartInfoBuilderTests
{
    [Fact]
    public void Build_PassesExecutableAndArgumentsInOrder()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            executablePath: "/Gum/Preview/GumPreview",
            projectPath: "/MyGame/GumProject.gumx",
            elementName: "Screens/MainMenu",
            selectionFilePath: "/Temp/GumPreviewSelection_abc.txt",
            contentRootDirectory: "/MyGame/",
            parentProcessId: 4321);

        startInfo.FileName.ShouldBe("/Gum/Preview/GumPreview");
        startInfo.UseShellExecute.ShouldBeFalse();
        startInfo.ArgumentList.ShouldBe(new[]
        {
            "--project", "/MyGame/GumProject.gumx",
            "--element", "Screens/MainMenu",
            "--selection-file", "/Temp/GumPreviewSelection_abc.txt",
            "--content-root", "/MyGame/",
            "--parent-pid", "4321",
        });
    }

    [Fact]
    public void Build_UsesArgumentListSoPathsWithSpacesNeedNoManualQuoting()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            executablePath: "/Gum/Preview/GumPreview",
            projectPath: "/My Game Folder/GumProject.gumx",
            elementName: "MainMenu",
            selectionFilePath: "/Temp/sel.txt",
            contentRootDirectory: "/My Game Folder/",
            parentProcessId: 1);

        startInfo.ArgumentList[1].ShouldBe("/My Game Folder/GumProject.gumx");
    }

    [Fact]
    public void Build_PassesContentRootDirectory_SeparatelyFromProjectPath()
    {
        // issue #4748: when projectPath is a temporary converted copy of a .gumx project, content
        // (fonts, textures) still resolves from the original directory, not the copy's directory.
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            executablePath: "/Gum/Preview-Aot/GumPreview",
            projectPath: "/Temp/GumPreviewJson/abc123/GumProject.gumj",
            elementName: "MainMenu",
            selectionFilePath: "/Temp/sel.txt",
            contentRootDirectory: "/MyGame/",
            parentProcessId: 1);

        startInfo.ArgumentList[7].ShouldBe("/MyGame/");
    }

    [Fact]
    public void Build_WithLogPath_AddsLogArgument()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            "/Gum/Preview/GumPreview", "/p.gumx", "Main", "/sel.txt", "/MyGame/", 1, logPath: "/Temp/preview.log");

        startInfo.ArgumentList.ShouldContain("--log");
        startInfo.ArgumentList[startInfo.ArgumentList.IndexOf("--log") + 1].ShouldBe("/Temp/preview.log");
    }

    [Fact]
    public void Build_LogPathWithSpaces_PassesItAsOneArgument()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            "/Gum/Preview/GumPreview", "/p.gumx", "Main", "/sel.txt", "/MyGame/", 1, logPath: "/My Logs/preview log.txt");

        startInfo.ArgumentList[startInfo.ArgumentList.IndexOf("--log") + 1].ShouldBe("/My Logs/preview log.txt");
        startInfo.ArgumentList.Count.ShouldBe(12);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_WithoutLogPath_OmitsLogArgument(string? logPath)
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            "/Gum/Preview/GumPreview", "/p.gumx", "Main", "/sel.txt", "/MyGame/", 1, logPath: logPath);

        startInfo.ArgumentList.ShouldNotContain("--log");
    }
}
