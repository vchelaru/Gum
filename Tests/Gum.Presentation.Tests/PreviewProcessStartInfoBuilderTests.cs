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
}
