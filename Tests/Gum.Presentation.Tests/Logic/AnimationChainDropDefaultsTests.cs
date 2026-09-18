using System;
using System.IO;
using Gum.Logic;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

// Issue #4824: dropping a .achx/.achj onto a Sprite to set its SourceFile should also enable
// Animate and select the first chain, instead of leaving the sprite showing a static first frame.
// This pins the pure "which chain name should be auto-selected" decision, decoupled from the
// plugin's DI/dialog plumbing.
public class AnimationChainDropDefaultsTests : BaseTestClass
{
    [Fact]
    public void GetFirstChainNameOrNull_AchxWithChains_ReturnsFirstChainName()
    {
        WithTempFile(".achx", """
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Walk</Name>
          </AnimationChain>
          <AnimationChain>
            <Name>Run</Name>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            string? result = AnimationChainDropDefaults.GetFirstChainNameOrNull(path);

            result.ShouldBe("Walk");
        });
    }

    [Fact]
    public void GetFirstChainNameOrNull_AchjWithChains_ReturnsFirstChainName()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Idle", "frames": [] },
            { "name": "Walk", "frames": [] }
          ]
        }
        """, path =>
        {
            string? result = AnimationChainDropDefaults.GetFirstChainNameOrNull(path);

            result.ShouldBe("Idle");
        });
    }

    [Fact]
    public void GetFirstChainNameOrNull_AchxWithNoChains_ReturnsNull()
    {
        WithTempFile(".achx", """
        <AnimationChainArraySave>
        </AnimationChainArraySave>
        """, path =>
        {
            string? result = AnimationChainDropDefaults.GetFirstChainNameOrNull(path);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public void GetFirstChainNameOrNull_NonAnimationChainExtension_ReturnsNull()
    {
        WithTempFile(".png", "not really a png", path =>
        {
            string? result = AnimationChainDropDefaults.GetFirstChainNameOrNull(path);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public void GetFirstChainNameOrNull_FileDoesNotExist_ReturnsNull()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".achx");

        string? result = AnimationChainDropDefaults.GetFirstChainNameOrNull(missingPath);

        result.ShouldBeNull();
    }

    private static void WithTempFile(string extension, string content, Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(path, content);
        try
        {
            action(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
