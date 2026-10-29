using System.Text;
using Shouldly;
using ToolsUtilities;
using WpfDataUi.Controls;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>
/// FilePickingLogic.RemoveDotDotSlash must normalize like FileManager.RemoveDotDotSlash (#6948). DataUi.Core
/// cannot reference ToolsUtilities (GumCommon compiles the same sources, so a consumer of both sees every type
/// twice), so these tests tie the two together. The only difference is that FilePickingLogic always writes "/".
/// </summary>
public class FilePickingLogicPathTests : BaseTestClass
{
    [Theory]
    [InlineData("/a/../b.png", "/b.png")]
    [InlineData("/../b.png", "/b.png")]
    [InlineData("C:/../b.png", "C:/b.png")]
    [InlineData("c/dir/.", "c/dir/")]
    [InlineData("c/dir/sub/..", "c/dir/")]
    [InlineData("a/.//b.png", "a/b.png")]
    [InlineData("a//b.png", "a/b.png")]
    [InlineData("c/a/../b.png", "c/b.png")]
    [InlineData("a/../../b.png", "../b.png")]
    [InlineData("c\\dir\\sub\\..\\x.png", "c/dir/x.png")]
    [InlineData("/home/user/../x.png", "/home/x.png")]
    [InlineData("./c/a/../b.png", "./c/b.png")]
    [InlineData("c/dir/.hidden/sheet.png", "c/dir/.hidden/sheet.png")]
    [InlineData("", "")]
    public void RemoveDotDotSlash_CollapsesDotSegmentsAndKeepsTheLeadingSeparator(string input, string expected)
    {
        FilePickingLogic.RemoveDotDotSlash(input).ShouldBe(expected);
    }

    [Fact]
    public void RemoveDotDotSlash_LeavesTheSchemeSeparatorOfAUrl()
    {
        FilePickingLogic.RemoveDotDotSlash("https://example.com/a/../b.png").ShouldBe("https://example.com/a/../b.png");
    }

    [Fact]
    public void RemoveDotDotSlash_MatchesFileManager_ForRandomPaths()
    {
        string[] prefixes = { "", "", "/", "./", "C:/", "c:/", "C:", "//", "https://h/" };
        string[] segmentPool = { "a", "bb", "c.png", ".hidden", "a..b", "..", "..", "..", ".", "" };
        Random random = new(6948);
        char separator = Path.DirectorySeparatorChar;

        for (int caseIndex = 0; caseIndex < 3000; caseIndex++)
        {
            StringBuilder builder = new(prefixes[random.Next(prefixes.Length)]);
            int segmentCount = random.Next(1, 7);
            for (int i = 0; i < segmentCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(random.Next(2) == 0 ? '/' : '\\');
                }
                builder.Append(segmentPool[random.Next(segmentPool.Length)]);
            }
            string input = builder.ToString();

            string expected = FileManager.RemoveDotDotSlash(input).Replace(separator, '/');
            FilePickingLogic.RemoveDotDotSlash(input).ShouldBe(expected, $"input: {input}");
        }
    }
}
