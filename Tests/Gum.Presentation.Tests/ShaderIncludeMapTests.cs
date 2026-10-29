using Gum.Managers;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

public class ShaderIncludeMapTests
{
    private static FilePath Path(string name) => new FilePath("C:/Project/Shaders/" + name);

    [Fact]
    public void GetShadersIncluding_ListsEveryShaderThatReadTheFile()
    {
        ShaderIncludeMap map = new ShaderIncludeMap();
        map.SetIncludes(Path("Blur.fx"), new[] { Path("Common.hlsli") });
        map.SetIncludes(Path("Glow.fx"), new[] { Path("Common.hlsli"), Path("Glow.hlsli") });

        map.GetShadersIncluding(Path("Common.hlsli")).ShouldBe(new[] { Path("Blur.fx"), Path("Glow.fx") }, ignoreOrder: true);
        map.GetShadersIncluding(Path("Glow.hlsli")).ShouldBe(new[] { Path("Glow.fx") });
        map.IsIncluded(Path("Common.hlsli")).ShouldBeTrue();
        map.IsIncluded(Path("Other.hlsli")).ShouldBeFalse();
    }

    [Fact]
    public void SetIncludes_ReplacesWhatTheShaderReadBefore()
    {
        ShaderIncludeMap map = new ShaderIncludeMap();
        map.SetIncludes(Path("Blur.fx"), new[] { Path("Old.hlsli") });

        map.SetIncludes(Path("Blur.fx"), new[] { Path("New.hlsli") });

        map.IsIncluded(Path("Old.hlsli")).ShouldBeFalse();
        map.GetShadersIncluding(Path("New.hlsli")).ShouldBe(new[] { Path("Blur.fx") });
    }

    [Fact]
    public void SetIncludes_WithNoFiles_ForgetsTheShader()
    {
        ShaderIncludeMap map = new ShaderIncludeMap();
        map.SetIncludes(Path("Blur.fx"), new[] { Path("Common.hlsli") });

        map.SetIncludes(Path("Blur.fx"), new FilePath[0]);

        map.IsIncluded(Path("Common.hlsli")).ShouldBeFalse();
    }

    [Fact]
    public void Lookup_IgnoresTheSpellingOfThePath()
    {
        ShaderIncludeMap map = new ShaderIncludeMap();
        map.SetIncludes(Path("Blur.fx"), new[] { new FilePath("C:\\Project\\Shaders\\Common.hlsli") });

        map.IsIncluded(new FilePath("C:/Project/Shaders/../Shaders/Common.hlsli")).ShouldBeTrue();
    }
}
