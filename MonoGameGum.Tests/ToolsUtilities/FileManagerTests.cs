using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.ToolsUtilities;
public class FileManagerTests
{
    [Fact]
    public void FromFileText_ShouldLoad_WhenPathHasDotDotSlash()
    {
        System.IO.Directory.CreateDirectory("DirectoryA1");
        System.IO.Directory.CreateDirectory("DirectoryB1");

        System.IO.File.WriteAllText("DirectoryA1/test.txt", "Test content A");

        var text = FileManager.FromFileText("DirectoryB1/../DirectoryA1/test.txt");

        text.ShouldBe("Test content A");
    }

    [Fact]
    public void FromFileText_ShouldLoad_WhenPathHasBackSlashes()
    {

        System.IO.Directory.CreateDirectory("DirectoryA2");

        System.IO.File.WriteAllText("DirectoryA2/test.txt", "Test content A");

        var text = FileManager.FromFileText("DirectoryA2\\test.txt");

        text.ShouldBe("Test content A");
    }
}
