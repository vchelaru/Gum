using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A frame texture that cannot be loaded from an .achx (missing, an image no decoder reads, a corrupt
/// PNG) behaves one way on every runtime (#6890), through the real MonoGame loader and a real
/// GraphicsDevice. With <see cref="MissingFileBehavior.ConsumeSilently"/> the frame stays with no
/// texture and the rest of the file loads. With <see cref="MissingFileBehavior.ThrowException"/> a
/// FileNotFoundException naming the texture is thrown.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class AchxFrameTextureLoadFailureTests : BaseTestClass
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GumAchxFrameFailure_" + Guid.NewGuid().ToString("N")) + "/";

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
        using Game game = new();
        game.RunOneFrame();
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

    // A NineSlice cannot show a frame with no texture, so on MonoGame it throws whatever ConsumeSilently says.
    // That is a separate gap (#6897); this pins that a missing, undecodable and corrupt texture fail alike.
    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void NineSliceSourceFile_FrameTextureFailsToLoad_AllFailuresThrowTheSameError(string textureName, string kind)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchxWithFrame(textureName, kind);

        InvalidOperationException thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            thrown = Should.Throw<InvalidOperationException>(() => new NineSliceRuntime { SourceFileName = _directory + "a.achx" });
        });

        thrown.Message.ShouldContain("null texture");
    }

    // The missing texture is left out: the desktop loader returns null for a missing file, which
    // AnimationFrame does not check, so a missing frame texture ignores ThrowException (#6896).
    [Theory]
    [InlineData("photo.jpg", "undecodable")]
    [InlineData("broken.png", "corrupt")]
    public void SpriteSourceFile_FrameTextureFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingTexture(string textureName, string kind)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchxWithFrame(textureName, kind);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, textureName).ShouldNotBeNull();
    }

    [Fact]
    public void SpriteSourceFile_FrameTextureIsValid_LoadsTexture()
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchxWithFrame("ok.png", "valid", validPngBytes: CreatePng(game));

        AnimationChainList? chains = null;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains[0][0].Texture.ShouldNotBeNull();
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

    private void WriteAchxWithFrame(string textureName, string kind, byte[]? validPngBytes = null)
    {
        Directory.CreateDirectory(_directory);
        if (kind == "undecodable" || kind == "corrupt")
        {
            File.WriteAllBytes(_directory + textureName, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }
        else if (validPngBytes != null)
        {
            File.WriteAllBytes(_directory + textureName, validPngBytes);
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

    private static byte[] CreatePng(Game game)
    {
        using Microsoft.Xna.Framework.Graphics.Texture2D source = new(game.GraphicsDevice, 2, 2, false, Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color);
        source.SetData(new Microsoft.Xna.Framework.Color[] { Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.White });
        using MemoryStream stream = new();
        source.SaveAsPng(stream, 2, 2);
        return stream.ToArray();
    }

    private sealed class Game : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(GraphicsDevice);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
