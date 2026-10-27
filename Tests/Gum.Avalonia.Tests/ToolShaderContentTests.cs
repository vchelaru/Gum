using System;
using System.IO;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head's csproj links the example shaders into Content/Shaders, and the Shader File drop-down
/// offers whatever is there. A shader missing from the output leaves the drop-down without it.
/// </summary>
public class ToolShaderContentTests
{
    [Theory]
    [InlineData("Grayscale.slang")]
    [InlineData("Blur.slang")]
    [InlineData("Sepia.slang")]
    public void ShippedShader_ShouldBeStagedWithTheHead(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Shaders", name);

        File.Exists(path).ShouldBeTrue(path);
        new FileInfo(path).Length.ShouldBeGreaterThan(0);
    }
}
