using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A frame texture that cannot be loaded from an .achx (missing, an image no decoder reads, a corrupt
/// PNG) behaves one way on every runtime (#6890), through the real MonoGame loader and a real
/// GraphicsDevice. With <see cref="MissingFileBehavior.ConsumeSilently"/> the frame stays with no
/// texture, the rest of the file loads and PropertyAssignmentError names the texture. With
/// <see cref="MissingFileBehavior.ThrowException"/> loading throws a FileNotFoundException naming the
/// first texture that failed.
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

    public static TheoryData<string> FailingFrames => new() { "missing.png", "photo.jpg", "broken.png" };

    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void SpriteSourceFile_FrameTextureFailsToLoad_ConsumeSilentlyKeepsFrameWithoutTextureAndRaisesError(string textureName)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(null, textureName);

        AnimationChainList? chains = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains.Count.ShouldBe(1);
        chains[0][0].Texture.ShouldBeNull();
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain(textureName);
    }

    // A NineSlice treats a frame without a texture like a Sprite does: it draws nothing for that frame
    // and playback goes on, instead of throwing from Update (#6897).
    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void NineSliceSourceFile_FrameTextureFailsToLoad_ConsumeSilentlyPlaysBlankFrameWithoutThrowing(string textureName)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(CreatePng(game), "ok.png", textureName);

        NineSliceRuntime? nineSlice = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            nineSlice = new NineSliceRuntime { SourceFileName = _directory + "a.achx" };
            nineSlice.CurrentChainName = "Idle";
            nineSlice.Animate = true;
            nineSlice.AnimateSelf(0.15);
            nineSlice.AnimateSelf(0.15);
        });

        nineSlice!.AnimationChains![0][1].Texture.ShouldBeNull();
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain(textureName);
    }

    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void SpriteSourceFile_FrameTextureFailsToLoad_ThrowExceptionThrowsFileNotFoundNamingTexture(string textureName)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(null, textureName);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, textureName).ShouldNotBeNull();
    }

    // The error surfaces when the file loads, not later in Update.
    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void NineSliceSourceFile_FrameTextureFailsToLoad_ThrowExceptionThrowsAtLoadNamingTexture(string textureName)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(CreatePng(game), "ok.png", textureName);

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new NineSliceRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, textureName).ShouldNotBeNull();
    }

    // The user flow: a file with a good, a missing and a corrupt frame fails on the first bad one.
    [Fact]
    public void SpriteSourceFile_GoodMissingAndBrokenFrames_ThrowExceptionFailsOnTheFirstBadFrame()
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(CreatePng(game), "ok.png", "missing.png", "broken.png");

        Exception thrown = null!;
        WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            thrown = Should.Throw<Exception>(() => new SpriteRuntime { SourceFileName = _directory + "a.achx" });
        });

        FindFileNotFound(thrown, "missing.png").ShouldNotBeNull();
        FindFileNotFound(thrown, "broken.png").ShouldBeNull();
    }

    [Fact]
    public void SpriteSourceFile_FrameTextureIsValid_LoadsTextureAndRaisesNoError()
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(CreatePng(game), "ok.png");

        AnimationChainList? chains = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ThrowException, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains[0][0].Texture.ShouldNotBeNull();
        errors.ShouldBeEmpty();
    }

    // Frames of one sprite sheet share the texture; the load raises one error for it, not one per frame (#6899).
    [Theory]
    [MemberData(nameof(FailingFrames))]
    public void SpriteSourceFile_ManyFramesShareOneFailingTexture_ConsumeSilentlyRaisesOneError(string textureName)
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(CreatePng(game), "ok.png", textureName, textureName, textureName, "ok.png", textureName);

        AnimationChainList? chains = null;
        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
            chains = sprite.AnimationChains;
        });

        chains.ShouldNotBeNull();
        chains[0].Count.ShouldBe(6);
        chains[0][0].Texture.ShouldNotBeNull();
        chains[0][1].Texture.ShouldBeNull();
        chains[0][4].Texture.ShouldNotBeNull();
        chains[0][5].Texture.ShouldBeNull();
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain(textureName);
    }

    [Fact]
    public void SpriteSourceFile_FramesShareTwoFailingTextures_ConsumeSilentlyRaisesOneErrorForEach()
    {
        using Game game = new();
        game.RunOneFrame();
        WriteAchx(null, "missing.png", "broken.png", "missing.png", "broken.png", "missing.png");

        List<string> errors = WithMissingFileBehavior(MissingFileBehavior.ConsumeSilently, () =>
        {
            SpriteRuntime sprite = new() { SourceFileName = _directory + "a.achx" };
        });

        errors.Count.ShouldBe(2);
        errors.Count(error => error.Contains("missing.png")).ShouldBe(1);
        errors.Count(error => error.Contains("broken.png")).ShouldBe(1);
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

    private static List<string> WithMissingFileBehavior(MissingFileBehavior behavior, Action action)
    {
        MissingFileBehavior saved = GraphicalUiElement.MissingFileBehavior;
        GraphicalUiElement.MissingFileBehavior = behavior;
        List<string> errors = new();
        Action<string> handler = errors.Add;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
        try
        {
            action();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            GraphicalUiElement.MissingFileBehavior = saved;
        }
        return errors;
    }

    // ok.png is a valid image, missing.png has no file, anything else holds bytes no decoder reads.
    private void WriteAchx(byte[]? validPng, params string[] textureNames)
    {
        Directory.CreateDirectory(_directory);
        string frames = "";
        foreach (string textureName in textureNames)
        {
            if (textureName == "ok.png")
            {
                File.WriteAllBytes(_directory + textureName, validPng!);
            }
            else if (textureName != "missing.png")
            {
                File.WriteAllBytes(_directory + textureName, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            }
            frames += $"<Frame><TextureName>{textureName}</TextureName><FrameLength>0.1</FrameLength>" +
                "<LeftCoordinate>0</LeftCoordinate><RightCoordinate>1</RightCoordinate><TopCoordinate>0</TopCoordinate><BottomCoordinate>1</BottomCoordinate></Frame>";
        }
        File.WriteAllText(_directory + "a.achx",
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<AnimationChainArraySave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <FileRelativeTextures>true</FileRelativeTextures>\n" +
            "  <TimeMeasurementUnit>Second</TimeMeasurementUnit>\n" +
            "  <CoordinateType>UV</CoordinateType>\n" +
            $"  <AnimationChain><Name>Idle</Name>{frames}</AnimationChain>\n" +
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
