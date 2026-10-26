using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// A frame texture that cannot be loaded from an .achx behaves one way on every runtime (#6890), through
/// the real Skia loader. With <see cref="MissingFileBehavior.ConsumeSilently"/> the frame stays with no
/// texture and the rest of the file loads. With <see cref="MissingFileBehavior.ThrowException"/> a
/// FileNotFoundException naming the texture is thrown.
/// </summary>
public class AchxFrameTextureLoadFailureTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumSkiaAchxFrameFailure_" + Guid.NewGuid().ToString("N")) + "/";

    public AchxFrameTextureLoadFailureTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        LoaderManager.Self.ContentLoader = new ContentLoader();
        LoaderManager.Self.CacheTextures = false;
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string, string> FailingFrames => new()
    {
        { "missing.png", "missing" },
        { "photo.jpg", "undecodable" },
        { "broken.png", "corrupt" },
    };

    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void SpriteSourceFile_FrameTextureFailsToLoad_ConsumeSilentlyKeepsFrameWithoutTexture(string textureName, string kind)
    {
        WriteAchxWithFrame(textureName, kind);

        AnimationChainList? chains = null;
        WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains.Count.ShouldBe(1);
        chains[0][0].Texture.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void NineSliceSourceFile_FrameTextureFailsToLoad_ConsumeSilentlyKeepsFrameWithoutTexture(string textureName, string kind)
    {
        WriteAchxWithFrame(textureName, kind);

        AnimationChainList? chains = null;
        WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            NineSliceRuntime nineSlice = new() { SourceFileName = _directory + "a.achx" };
            chains = nineSlice.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains.Count.ShouldBe(1);
        chains[0][0].Texture.ShouldBeNull();
    }

    // The undecodable and corrupt images are left out: Skia caches a null bitmap for them, which
    // AnimationFrame does not check, so they ignore ThrowException (#6889).
    [Fact]
    public void SpriteSourceFile_FrameTextureIsMissing_ThrowExceptionThrowsFileNotFoundNamingTexture()
    {
        WriteAchxWithFrame("missing.png", "missing");

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, "missing.png").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("photo.jpg", "undecodable")]
    [InlineData("broken.png", "corrupt")]
    public void SpriteSourceFile_FrameTextureCannotBeDecoded_ThrowExceptionDoesNotThrow(string textureName, string kind)
    {
        WriteAchxWithFrame(textureName, kind);

        AnimationChainList? chains = null;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains[0][0].Texture.ShouldBeNull();
    }

    private static Exception? FindFileNotFound(Exception? exception, string textureName)
    {
        while (exception != null)
        {
            if (exception is FileNotFoundException && exception.Message.Contains(textureName))
            {
                return exception;
            }
            exception = exception.InnerException;
        }
        return null;
    }

    private static void WithMissingFileBehavior(MissingFileBehavior behavior, Action action)
    {
        MissingFileBehavior saved = GraphicalUiElement.MissingFileBehavior;
        GraphicalUiElement.MissingFileBehavior = behavior;
        try
        {
            action();
        }
        finally
        {
            GraphicalUiElement.MissingFileBehavior = saved;
        }
    }

    private void WriteAchxWithFrame(string textureName, string kind)
    {
        Directory.CreateDirectory(_directory);
        if (kind == "undecodable" || kind == "corrupt")
        {
            File.WriteAllBytes(_directory + textureName, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }
        File.WriteAllText(_directory + "a.achx",
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<AnimationChainArraySave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <FileRelativeTextures>true</FileRelativeTextures>\n" +
            "  <TimeMeasurementUnit>Second</TimeMeasurementUnit>\n" +
            "  <CoordinateType>UV</CoordinateType>\n" +
            $"  <AnimationChain><Name>Idle</Name><Frame><TextureName>{textureName}</TextureName><FrameLength>0.1</FrameLength>" +
            "<LeftCoordinate>0</LeftCoordinate><RightCoordinate>1</RightCoordinate><TopCoordinate>0</TopCoordinate><BottomCoordinate>1</BottomCoordinate></Frame></AnimationChain>\n" +
            "</AnimationChainArraySave>\n");
    }
}
