using Gum;
using Shouldly;
using ToolsUtilities;

namespace UnityGum.Tests;

public class StreamingAssetsLocationTests
{
    [Fact]
    public void Folder_GetGumPath_CombinesWithStreamingAssetsFolder()
    {
        StreamingAssetsLocation location = new StreamingAssetsLocation("/Game/Data/StreamingAssets");

        location.IsUrl.ShouldBeFalse();
        location.GetGumPath("GumProject/GumProject.gumx")
            .ShouldBe(Path.Combine("/Game/Data/StreamingAssets", "GumProject/GumProject.gumx"));
    }

    [Fact]
    public void Folder_GetUrl_ReturnsNull()
    {
        StreamingAssetsLocation location = new StreamingAssetsLocation("/Game/Data/StreamingAssets");

        location.GetUrl("/Game/Data/StreamingAssets/GumProject/GumProject.gumx").ShouldBeNull();
    }

    [Fact]
    public void Url_GetGumPath_IsRootedUnderVirtualRoot()
    {
        StreamingAssetsLocation location = new StreamingAssetsLocation("jar:file:///data/app/com.game/base.apk!/assets");

        location.IsUrl.ShouldBeTrue();
        location.GetGumPath("GumProject\\GumProject.gumx").ShouldBe("/StreamingAssets/GumProject/GumProject.gumx");
    }

    [Fact]
    public void Url_GetUrl_MapsVirtualPathBackToStreamingAssetsUrl()
    {
        StreamingAssetsLocation location = new StreamingAssetsLocation("jar:file:///data/app/com.game/base.apk!/assets/");

        location.GetUrl("/StreamingAssets/GumProject/Components\\Button.gucx")
            .ShouldBe("jar:file:///data/app/com.game/base.apk!/assets/GumProject/Components/Button.gucx");
        location.GetUrl("/Other/GumProject.gumx").ShouldBeNull();
        location.GetUrl("/StreamingAssetsX/GumProject.gumx").ShouldBeNull();
    }

    [Fact]
    public void Url_FileManagerRead_ReachesHookAsMappableVirtualPath()
    {
        StreamingAssetsLocation location = new StreamingAssetsLocation("jar:file:///data/app/com.game/base.apk!/assets");
        string projectPath = location.GetGumPath("GumProject/GumProject.gumx");
        string previousDirectory = FileManager.RelativeDirectory;
        Func<string, Stream>? previousHook = FileManager.CustomGetStreamFromFile;
        List<string?> urls = new List<string?>();
        FileManager.CustomGetStreamFromFile = path =>
        {
            urls.Add(location.GetUrl(path));
            return new MemoryStream();
        };

        try
        {
            FileManager.RelativeDirectory = FileManager.GetDirectory(projectPath);
            using Stream stream = FileManager.GetStreamForFile("Components/../Screens/Main.gusx");
        }
        finally
        {
            FileManager.RelativeDirectory = previousDirectory;
            FileManager.CustomGetStreamFromFile = previousHook;
        }

        urls.ShouldBe(new string?[] { "jar:file:///data/app/com.game/base.apk!/assets/GumProject/Screens/Main.gusx" });
    }
}
