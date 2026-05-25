using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
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
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibAnimChainTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            // Drop a real PNG in tempRoot so the loader has something to find.
            string textureFileName = "anim_frame_test.png";
            string textureFullPath = Path.Combine(tempRoot, textureFileName);
            Image image = Raylib.GenImageColor(4, 4, Raylib_cs.Color.Blue);
            try
            {
                Raylib.ExportImage(image, textureFullPath);
            }
            finally
            {
                Raylib.UnloadImage(image);
            }

            // Construct an in-memory AnimationChainListSave whose FileName points
            // into tempRoot — ToAnimationChainList uses GetDirectory(FileName) to
            // set FileManager.RelativeDirectory before loading per-frame textures.
            AnimationChainListSave save = new AnimationChainListSave();
            save.FileName = Path.Combine(tempRoot, "test.achx").Replace('\\', '/');
            save.FileRelativeTextures = true;
            save.AnimationChains = new List<AnimationChainSave>();

            AnimationChainSave chainSave = new AnimationChainSave();
            chainSave.Name = "TestChain";
            chainSave.Frames = new List<AnimationFrameSave>
            {
                new AnimationFrameSave
                {
                    TextureName = textureFileName,
                    FrameLength = 0.1f,
                    LeftCoordinate = 0f,
                    RightCoordinate = 1f,
                    TopCoordinate = 0f,
                    BottomCoordinate = 1f,
                },
            };
            save.AnimationChains.Add(chainSave);

            // CacheTextures disabled so any stale cached null from a prior test
            // run can't mask the load.
            LoaderManager.Self.CacheTextures = false;

            AnimationChainList list = save.ToAnimationChainList();

            list.Count.ShouldBe(1);
            list[0].Count.ShouldBe(1);
            AnimationFrame frame = list[0][0];
            frame.Texture.ShouldNotBeNull();
            frame.Texture!.Value.Width.ShouldBe(4);
            frame.Texture!.Value.Height.ShouldBe(4);
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
