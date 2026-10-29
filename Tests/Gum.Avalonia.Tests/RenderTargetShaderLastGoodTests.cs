using EditorTabPlugin_XNA.Services;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// A shader edited on disk into one that does not compile keeps showing the last version that did
/// (#7017), and says why.
/// </summary>
public class RenderTargetShaderLastGoodTests
{
    private static string UniquePath() => "/shaders/" + Guid.NewGuid().ToString("N") + ".slang";

    private static byte[] Compiles(params byte[] bytes) => bytes;

    private static byte[] DoesNotCompile() => throw new InvalidOperationException("syntax error on line 3");

    [Fact]
    public void ABrokenEdit_KeepsTheLastGoodVersion_AndReportsTheError()
    {
        string path = UniquePath();
        List<string> reported = new List<string>();
        RenderTargetShaderResolver.CreateKeepingLastGood(path, () => Compiles(1, 2, 3), bytes => bytes.Length, reported.Add);

        int length = RenderTargetShaderResolver.CreateKeepingLastGood(path, DoesNotCompile, bytes => bytes.Length, reported.Add);

        length.ShouldBe(3);
        reported.Count.ShouldBe(1);
        reported[0].ShouldContain("syntax error on line 3");
        reported[0].ShouldContain("last version");
    }

    [Fact]
    public void AFixedEditAfterABrokenOne_UsesTheNewVersion()
    {
        string path = UniquePath();
        RenderTargetShaderResolver.CreateKeepingLastGood(path, () => Compiles(1), bytes => bytes.Length, _ => { });
        RenderTargetShaderResolver.CreateKeepingLastGood(path, DoesNotCompile, bytes => bytes.Length, _ => { });

        int length = RenderTargetShaderResolver.CreateKeepingLastGood(path, () => Compiles(1, 2, 3, 4), bytes => bytes.Length, _ => { });

        length.ShouldBe(4);
    }

    [Fact]
    public void ABrokenShaderThatNeverCompiled_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            RenderTargetShaderResolver.CreateKeepingLastGood(UniquePath(), DoesNotCompile, bytes => bytes.Length, _ => { }))
            .Message.ShouldContain("syntax error");
    }

    [Fact]
    public void WithNoWayToReport_ABrokenEditThrowsAsItAlwaysDid()
    {
        string path = UniquePath();
        RenderTargetShaderResolver.CreateKeepingLastGood(path, () => Compiles(1, 2), bytes => bytes.Length, null);

        Should.Throw<InvalidOperationException>(() =>
            RenderTargetShaderResolver.CreateKeepingLastGood(path, DoesNotCompile, bytes => bytes.Length, null));
    }

    [Fact]
    public void BytecodeTheDeviceRejects_FallsBackLikeACompileError()
    {
        string path = UniquePath();
        RenderTargetShaderResolver.CreateKeepingLastGood(path, () => Compiles(9, 9), bytes => bytes.Length, _ => { });
        List<string> reported = new List<string>();

        int length = RenderTargetShaderResolver.CreateKeepingLastGood(
            path,
            () => Compiles(1),
            bytes => bytes.Length == 1 ? throw new InvalidOperationException("device rejected it") : bytes.Length,
            reported.Add);

        length.ShouldBe(2);
        reported.ShouldHaveSingleItem().ShouldContain("device rejected it");
    }

    [Fact]
    public void EachFileKeepsItsOwnLastGoodVersion()
    {
        string first = UniquePath();
        string second = UniquePath();
        RenderTargetShaderResolver.CreateKeepingLastGood(first, () => Compiles(1), bytes => bytes.Length, _ => { });
        RenderTargetShaderResolver.CreateKeepingLastGood(second, () => Compiles(1, 2, 3), bytes => bytes.Length, _ => { });

        RenderTargetShaderResolver.CreateKeepingLastGood(first, DoesNotCompile, bytes => bytes.Length, _ => { })
            .ShouldBe(1);
        RenderTargetShaderResolver.CreateKeepingLastGood(second, DoesNotCompile, bytes => bytes.Length, _ => { })
            .ShouldBe(3);
    }
}
