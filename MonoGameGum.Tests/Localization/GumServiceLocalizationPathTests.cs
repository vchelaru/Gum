using Gum;
using Shouldly;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Localization;

public class GumServiceLocalizationPathTests
{
    // A project saved on Windows stores "Localization\Strings.resx". The resolved path goes straight
    // to File.Exists and Directory.GetFiles, so both separators must come out as the native one:
    // a backslash is a file-name character on macOS/Linux.
    [Theory]
    [InlineData("Localization\\Strings.resx")]
    [InlineData("Localization/Strings.resx")]
    public void ResolveLocalizationFilePaths_ShouldUseNativeSeparators(string relativePath)
    {
        string projectDirectory = "/game/Content/GumProject/";

        var resolved = GumService.ResolveLocalizationFilePaths(projectDirectory, new[] { relativePath });

        resolved.ShouldBe(new[]
        {
            "/game/Content/GumProject/Localization/Strings.resx".Replace('/', Path.DirectorySeparatorChar)
        });
    }

    [Fact]
    public void ResolveLocalizationFilePaths_ShouldSkipEmptyEntries()
    {
        var resolved = GumService.ResolveLocalizationFilePaths("/game/", new[] { "", null, "Strings.csv" });

        resolved.ShouldBe(new[] { "/game/Strings.csv".Replace('/', Path.DirectorySeparatorChar) });
    }
}
