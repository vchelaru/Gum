using System.IO;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The one place the New Project dialog turns a path into what it shows.</summary>
public class NewProjectDisplayPathTests
{
    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    [Theory]
    [InlineData("a/b/c")]
    [InlineData("a\\b\\c")]
    [InlineData("a/b\\c")]
    public void Normalize_UsesOnlyTheOsSeparator(string path)
    {
        NewProjectDisplayPath.Normalize(path).ShouldBe("a" + Sep + "b" + Sep + "c");
    }

    [Fact]
    public void Normalize_LeavesAPathWithoutSeparatorsAlone()
    {
        NewProjectDisplayPath.Normalize("MyGame.gumj").ShouldBe("MyGame.gumj");
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a/b/")]
    [InlineData("a\\b\\")]
    public void AsFolder_EndsWithExactlyOneSeparator(string path)
    {
        NewProjectDisplayPath.AsFolder(path).ShouldBe("a" + Sep + "b" + Sep);
    }
}
