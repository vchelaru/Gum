using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

/// <summary>
/// A frame texture that cannot be loaded from an .achx behaves one way on every runtime (#6890), through
/// the real raylib loader. With <see cref="MissingFileBehavior.ConsumeSilently"/> the frame stays with no
/// texture and the rest of the file loads. With <see cref="MissingFileBehavior.ThrowException"/> a
/// FileNotFoundException naming the texture is thrown. A missing file reaches AnimationFrame as an
/// IOException and an undecodable image as a FileNotFoundException.
/// </summary>
public class AchxFrameTextureLoadFailureTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumRaylibAchxFrameFailure_" + Guid.NewGuid().ToString("N")) + "/";
    private readonly bool _savedCacheTextures = LoaderManager.Self.CacheTextures;

    public AchxFrameTextureLoadFailureTests()
    {
        LoaderManager.Self.CacheTextures = false;
        FileManager.CustomGetStreamFromFile = null;
    }

    public override void Dispose()
    {
        LoaderManager.Self.CacheTextures = _savedCacheTextures;
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
    public void SpriteSourceFile_FrameTextureFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingTexture(string textureName, string kind)
    {
        WriteAchxWithFrame(textureName, kind);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, textureName).ShouldNotBeNull();
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
