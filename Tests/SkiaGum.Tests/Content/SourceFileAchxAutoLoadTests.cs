using Gum.Content.AnimationChain;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Tests.GueDeriving;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

// Setting a Sprite or NineSlice SourceFile to an .achx must load its animation chains, the same
// as the MonoGame/raylib dispatcher (issue #5226). Skia used to hand the .achx to
// LoadContent<SKBitmap>, leaving AnimationChains null.
public class SourceFileAchxAutoLoadTests
{
    public SourceFileAchxAutoLoadTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void SpriteRuntime_SourceFileSetToAchx_PopulatesAnimationChainsAndFirstFrameTexture()
    {
        WithTempAchx(achxPath =>
        {
            SpriteRuntime sut = new();

            sut.SourceFileName = achxPath;

            sut.AnimationChains.ShouldNotBeNull();
            sut.AnimationChains.Count.ShouldBe(1);
            sut.AnimationChains[0].Name.ShouldBe("TestChain");
            sut.Texture.ShouldNotBeNull();
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
            sut.Texture.ShouldNotBeNull();
        });
    }

    [Fact]
    public void SpriteRuntime_SourceFileSetToMissingAchx_ReportsErrorWhenConsumingSilently()
    {
        WithTempAchx(achxPath =>
        {
            string missingPath = achxPath.Replace("test.achx", "missing.achx");
            MissingFileBehavior savedBehavior = GraphicalUiElement.MissingFileBehavior;
            string? reportedError = null;
            Action<string> handler = message => reportedError = message;
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
            try
            {
                SpriteRuntime sut = new();

                sut.SourceFileName = missingPath;

                sut.AnimationChains.ShouldBeNull();
                reportedError.ShouldNotBeNull();
                reportedError.ShouldContain("missing.achx");
            }
            finally
            {
                CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
                GraphicalUiElement.MissingFileBehavior = savedBehavior;
            }
        });
    }

    private static void WithTempAchx(Action<string> action)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumSkiaAchxAutoLoad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        IContentLoader? originalLoader = LoaderManager.Self.ContentLoader;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;

        try
        {
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
                            TextureName = "frame.png",
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

            // The mock hands back a bitmap for any path, so frame textures resolve without a real
            // image file on disk.
            LoaderManager.Self.ContentLoader = new MockContentLoader();
            LoaderManager.Self.CacheTextures = false;

            action(achxPath);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
