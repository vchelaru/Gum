using System.IO;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class HeadOptionsTests
{
    [Fact]
    public void Parse_ReadsTheUserDataFolder_AsAFullPath()
    {
        HeadOptions options = HeadOptions.Parse(new[] { "project.gumx", "--user-data", "run-data", "--exit-after", "3" });

        options.UserDataFolder.ShouldBe(Path.GetFullPath("run-data"));
        options.ExitAfterSeconds.ShouldBe(3);
    }

    [Fact]
    public void Parse_WithoutTheFlag_LeavesTheUserDataFolderNull()
    {
        HeadOptions.Parse(new[] { "project.gumx" }).UserDataFolder.ShouldBeNull();
    }
}
