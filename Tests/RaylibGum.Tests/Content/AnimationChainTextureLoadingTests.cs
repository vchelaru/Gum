using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

public class AnimationChainTextureLoadingTests : BaseTestClass
{
    // Regression for the missing raylib branch in
    // RenderingLibrary/Graphics/Animation/AnimationFrame.cs (ToAnimationFrame).
    // ToAnimationFrame had per-frame texture-load branches for MONOGAME/KNI/XNA4
    // and SOKOL but no RAYLIB branch, so loadTexture was a no-op on raylib and
    // every frame.Texture stayed null — Sprite.Render then early-returned on
    // null Texture and the .achx animation row rendered nothing.
    [Fact]
    public void ToAnimationChainList_OnRaylib_LoadsPerFrameTextureRelativeToAchxFolder()
    {
        WithTempTextures((tempRoot, fileNames) =>
        {
            AnimationChainListSave save = BuildSave(tempRoot, "test.achx", new[]
            {
                (fileNames[0], 0.1f),
            });

            AnimationChainList list = save.ToAnimationChainList();

            list.Count.ShouldBe(1);
            list[0].Count.ShouldBe(1);
            AnimationFrame frame = list[0][0];
            frame.Texture.ShouldNotBeNull();
            frame.Texture!.Value.Width.ShouldBe(4);
            frame.Texture!.Value.Height.ShouldBe(4);
        }, count: 1);
    }

    // Diagnostic: even with frame textures correctly loaded by the test above,
    // the raylib SpriteScreen's animation row appears to never advance frames.
    // This walks the actual runtime path — wire the chain onto a SpriteRuntime,
    // set Animate=true, call AnimateSelf on a parent container with a delta
    // large enough to push past frame[0]'s 0.1s — and asserts the frame index
    // moved. If this passes, the bug is in the per-frame Update wiring in the
    // sample (not the SpriteRuntime/AnimationLogic chain).
    [Fact]
    public void SpriteRuntime_AnimateSelfWithPositiveDelta_ShouldAdvanceFrameIndex()
    {
        WithTempTextures((tempRoot, fileNames) =>
        {
            AnimationChainListSave save = BuildSave(tempRoot, "anim.achx", new[]
            {
                (fileNames[0], 0.1f),
                (fileNames[1], 0.1f),
            });
            AnimationChainList list = save.ToAnimationChainList();

            SpriteRuntime spriteRuntime = new();
            spriteRuntime.AnimationChains = list;
            spriteRuntime.CurrentChainName = "TestChain";
            spriteRuntime.Animate = true;

            spriteRuntime.AnimationChainFrameIndex.ShouldBe(0);

            // Tick through GraphicalUiElement.AnimateSelf — the same path
            // GumService.Update walks every frame. If IAnimatable is duplicated
            // across GumCommon and the backend assembly, the `as IAnimatable`
            // cast in AnimateSelf returns null and this assertion fails.
            spriteRuntime.AnimateSelf(0.15);

            spriteRuntime.AnimationChainFrameIndex.ShouldBe(1);
        }, count: 2);
    }

    private static AnimationChainListSave BuildSave(string tempRoot, string achxName, (string TextureName, float FrameLength)[] frames)
    {
        AnimationChainListSave save = new AnimationChainListSave();
        save.FileName = Path.Combine(tempRoot, achxName).Replace('\\', '/');
        save.FileRelativeTextures = true;
        save.AnimationChains = new List<AnimationChainSave>();

        AnimationChainSave chainSave = new AnimationChainSave();
        chainSave.Name = "TestChain";
        chainSave.Frames = new List<AnimationFrameSave>();
        foreach (var (name, length) in frames)
        {
            chainSave.Frames.Add(new AnimationFrameSave
            {
                TextureName = name,
                FrameLength = length,
                LeftCoordinate = 0f,
                RightCoordinate = 1f,
                TopCoordinate = 0f,
                BottomCoordinate = 1f,
            });
        }
        save.AnimationChains.Add(chainSave);
        return save;
    }

    private static void WithTempTextures(Action<string, string[]> action, int count)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibAnimChainTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            string[] fileNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                fileNames[i] = $"anim_frame_test_{i}.png";
                Image image = Raylib.GenImageColor(4, 4, Raylib_cs.Color.Blue);
                try
                {
                    Raylib.ExportImage(image, Path.Combine(tempRoot, fileNames[i]));
                }
                finally
                {
                    Raylib.UnloadImage(image);
                }
            }

            LoaderManager.Self.CacheTextures = false;

            action(tempRoot, fileNames);
        }
        finally
        {
            FileManager.RelativeDirectory = savedRelativeDirectory;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
