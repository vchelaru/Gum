using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

// .achj counterpart of SourceFileAchxAutoLoadTests: covers the raylib .achj auto-load path in
// CustomSetPropertyOnRenderable (AssignSourceFileOnSprite / AssignSourceFileOnNineSlice, gated by
// IsAnimationChainFile — issue #4476). Setting SourceFile to an .achj must populate
// AnimationChains and advance to the first frame's texture, same as .achx does today.
public class SourceFileAchjAutoLoadTests : BaseTestClass
{
    [Fact]
    public void SpriteRuntime_SourceFileSetToAchj_PopulatesAnimationChainsAndFirstFrameTexture()
    {
        WithTempAchj(achjPath =>
        {
            SpriteRuntime sut = new();

            sut.SourceFileName = achjPath;

            sut.AnimationChains.ShouldNotBeNull();
            sut.AnimationChains.Count.ShouldBe(1);
            sut.Texture.ShouldNotBeNull();
            sut.Texture!.Value.Width.ShouldBe(4);
        });
    }

    [Fact]
    public void NineSliceRuntime_SourceFileSetToAchj_PopulatesAnimationChains()
    {
        WithTempAchj(achjPath =>
        {
            NineSliceRuntime sut = new();

            sut.SourceFileName = achjPath;

            sut.AnimationChains.ShouldNotBeNull();
            sut.AnimationChains.Count.ShouldBe(1);
        });
    }

    private static void WithTempAchj(Action<string> action)
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRaylibAchjAutoLoad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        string savedRelativeDirectory = FileManager.RelativeDirectory;
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;

        try
        {
            const string textureName = "achj_frame.png";
            Image image = Raylib.GenImageColor(4, 4, Raylib_cs.Color.Blue);
            try
            {
                Raylib.ExportImage(image, Path.Combine(tempRoot, textureName));
            }
            finally
            {
                Raylib.UnloadImage(image);
            }

            string achjPath = Path.Combine(tempRoot, "test.achj").Replace('\\', '/');
            File.WriteAllText(achjPath, $$"""
            {
              "fileRelativeTextures": true,
              "animationChains": [
                {
                  "name": "TestChain",
                  "frames": [
                    {
                      "textureName": "{{textureName}}",
                      "frameLength": 0.1,
                      "leftCoordinate": 0.0,
                      "rightCoordinate": 1.0,
                      "topCoordinate": 0.0,
                      "bottomCoordinate": 1.0
                    }
                  ]
                }
              ]
            }
            """);

            LoaderManager.Self.CacheTextures = false;

            action(achjPath);
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
