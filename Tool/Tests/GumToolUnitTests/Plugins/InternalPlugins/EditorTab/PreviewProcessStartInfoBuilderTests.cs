using System.Diagnostics;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class PreviewProcessStartInfoBuilderTests
{
    [Fact]
    public void Build_PassesExecutableAndArgumentsInOrder()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            executablePath: @"C:\Gum\Preview\GumPreview.exe",
            gumxPath: @"C:\MyGame\GumProject.gumx",
            elementName: "Screens/MainMenu",
            selectionFilePath: @"C:\Temp\GumPreviewSelection_abc.txt");

        startInfo.FileName.ShouldBe(@"C:\Gum\Preview\GumPreview.exe");
        startInfo.UseShellExecute.ShouldBeFalse();
        startInfo.ArgumentList.ShouldBe(new[]
        {
            "--project", @"C:\MyGame\GumProject.gumx",
            "--element", "Screens/MainMenu",
            "--selection-file", @"C:\Temp\GumPreviewSelection_abc.txt",
        });
    }

    [Fact]
    public void Build_UsesArgumentListSoPathsWithSpacesNeedNoManualQuoting()
    {
        ProcessStartInfo startInfo = PreviewProcessStartInfoBuilder.Build(
            executablePath: @"C:\Gum\Preview\GumPreview.exe",
            gumxPath: @"C:\My Game Folder\GumProject.gumx",
            elementName: "MainMenu",
            selectionFilePath: @"C:\Temp\sel.txt");

        startInfo.ArgumentList[1].ShouldBe(@"C:\My Game Folder\GumProject.gumx");
    }
}
