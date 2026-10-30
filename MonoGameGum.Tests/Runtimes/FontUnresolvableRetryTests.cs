#pragma warning disable CS0618
using Gum.GueDeriving;
using MonoGameGum.Tests.Hot;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// A font that did not exist when a Text asked for it is asked for again once its file exists.
public class FontUnresolvableRetryTests : BaseTestClass
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "FontRetry_" + Path.GetRandomFileName());
    private readonly bool _cacheTextures = LoaderManager.Self.CacheTextures;

    public FontUnresolvableRetryTests()
    {
        Directory.CreateDirectory(_folder);
        FileManager.RelativeDirectory = _folder + "/";
        LoaderManager.Self.CacheTextures = true;
        LoaderManager.Self.ContentLoader = new SizedTextureLoader();
    }

    public override void Dispose()
    {
        LoaderManager.Self.CacheTextures = _cacheTextures;
        try { Directory.Delete(_folder, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    [Fact]
    public void Font_AskedForWhileMissing_IsFoundByTheNextTextOnceTheFileExists()
    {
        string family = "Retry" + Guid.NewGuid().ToString("N").Substring(0, 8);

        TextRuntime first = new TextRuntime { Font = family, FontSize = 24 };
        PreviewAccuracy2World.WriteFont(_folder, "Font24" + family, advance: 12, lineHeight: 28);
        TextRuntime second = new TextRuntime { Font = family, FontSize = 24 };

        second.BitmapFont.ShouldNotBeNull();
        second.BitmapFont!.LineHeightInPixels.ShouldBe(28);
        first.BitmapFont?.LineHeightInPixels.ShouldNotBe(28);
    }

    [Fact]
    public void Font_AskedForWhileMissing_StaysUnresolvedWhileTheFileIsStillMissing()
    {
        string family = "Retry" + Guid.NewGuid().ToString("N").Substring(0, 8);

        new TextRuntime { Font = family, FontSize = 24 };
        TextRuntime second = new TextRuntime { Font = family, FontSize = 24 };

        second.BitmapFont?.LineHeightInPixels.ShouldNotBe(28);
    }
}
