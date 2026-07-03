using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

// Covers the raylib .achx auto-load path in CustomSetPropertyOnRenderable
// (AssignSourceFileOnSprite / AssignSourceFileOnNineSlice). Setting SourceFile to
// an .achx must populate AnimationChains and advance to the first frame's texture.
// This path was previously commented out ("not yet supported") on raylib, so
// AnimationChains stayed null even though the programmatic chain API worked.
public class SourceFileAchxAutoLoadTests : BaseTestClass
{
    [Fact]
    public void SpriteRuntime_SourceFileSetToAchx_PopulatesAnimationChainsAndFirstFrameTexture()
    {
        WithTempAchx(achxPath =>
        {
            SpriteRuntime sut = new();

            sut.SourceFileName = achxPath;

            sut.AnimationChains.ShouldNotBeNull();
            sut.AnimationChains.Count.ShouldBe(1);
            sut.Texture.ShouldNotBeNull();
            sut.Texture!.Value.Width.ShouldBe(4);
        });
    }

    [Fact]
    public void NineSliceRuntime_SourceFileSetToAchx_PopulatesAnimationChains()
    {
        WithTempAchx(achxPath =>
        {
            NineSliceRuntime sut = new();

            sut.SourceFileName = achxPath;

            sut.AnimationChains.ShouldNotBeNull();
            sut.AnimationChains.Count.ShouldBe(1);
        });
    }

    private static void WithTempAchx(Action<string> action)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibAchxAutoLoad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            const string textureName = "achx_frame.png";
            Image image = Raylib.GenImageColor(4, 4, Raylib_cs.Color.Blue);
            try
            {
                Raylib.ExportImage(image, Path.Combine(tempRoot, textureName));
            }
            finally
            {
                Raylib.UnloadImage(image);
            }

            string achxPath = Path.Combine(tempRoot, "test.achx").Replace('\\', '/');
            AnimationChainListSave save = new();
            save.FileName = achxPath;
            save.FileRelativeTextures = true;
            save.AnimationChains = new List<AnimationChainSave>
            {
                new AnimationChainSave
                {
                    Name = "TestChain",
                    Frames = new List<AnimationFrameSave>
                    {
                        new AnimationFrameSave
                        {
                            TextureName = textureName,
                            FrameLength = 0.1f,
                            LeftCoordinate = 0f,
                            RightCoordinate = 1f,
                            TopCoordinate = 0f,
                            BottomCoordinate = 1f,
                        },
                    },
                },
            };
            FileManager.XmlSerialize(save, achxPath);

            LoaderManager.Self.CacheTextures = false;

            action(achxPath);
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
