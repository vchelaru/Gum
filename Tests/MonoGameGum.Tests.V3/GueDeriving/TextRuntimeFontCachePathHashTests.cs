using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3.GueDeriving;

/// <summary>
/// #6007: a font file's cache name carries a hash of its path once the project is at
/// <see cref="GumProjectSave.GumxVersions.FontCachePathHash"/>. The runtime looks the .fnt up by
/// <see cref="TextRuntime.GetFontCacheFileName"/> and bakes under <see cref="BmfcSave.FontCacheFileName"/>,
/// so the two must agree even though the baked <see cref="BmfcSave.FontFile"/> is an absolute path.
/// </summary>
public class TextRuntimeFontCachePathHashTests
{
    [Fact]
    public void BakedFontCacheFileName_MatchesTheLookedUpName_ForAProjectAtTheHashedVersion()
    {
        BmfcSave baked = BakeFontFile("Fonts/Icons.ttf", GumProjectSave.NativeVersion, out string lookedUp);

        baked.FontCacheFileName.ShouldBe(lookedUp);
        lookedUp.Replace('\\', '/').ShouldStartWith("FontCache/Font24Icons_ttf_");
    }

    [Fact]
    public void BakedFontCacheFileName_MatchesTheLegacyName_ForAProjectBelowTheHashedVersion()
    {
        BmfcSave baked = BakeFontFile(
            "Fonts/Icons.ttf", (int)GumProjectSave.GumxVersions.ColorOperationExpansion, out string lookedUp);

        baked.FontCacheFileName.ShouldBe(lookedUp);
        lookedUp.Replace('\\', '/').ShouldBe("FontCache/Font24Icons_ttf.fnt");
    }

    [Fact]
    public void LookedUpName_DiffersBetweenSameNamedFilesInDifferentFolders_ForAProjectAtTheHashedVersion()
    {
        BakeFontFile("Fonts/Icons.ttf", GumProjectSave.NativeVersion, out string fonts);
        BakeFontFile("Other/Icons.ttf", GumProjectSave.NativeVersion, out string other);

        fonts.ShouldNotBe(other);
    }

    [Fact]
    public void LookedUpName_IsTheLegacyNameWithNoProjectLoaded()
    {
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        try
        {
            ObjectFinder.Self.GumProjectSave = null;
            TextRuntime text = new TextRuntime { FontSize = 24 };

            text.GetFontCacheFileName("Fonts/Icons.ttf").Replace('\\', '/').ShouldBe("FontCache/Font24Icons_ttf.fnt");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    private static BmfcSave BakeFontFile(string fontFile, int projectVersion, out string lookedUpName)
    {
        CapturingInMemoryFontCreator creator = new CapturingInMemoryFontCreator();
        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

        try
        {
            ObjectFinder.Self.GumProjectSave = new GumProjectSave { Version = projectVersion };

            TextRuntime text = new TextRuntime();
            text.FontSize = 24;
            text.Font = fontFile;

            creator.LastBmfcSave.ShouldNotBeNull();
            lookedUpName = text.GetFontCacheFileName(fontFile);
            return creator.LastBmfcSave!;
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    private sealed class CapturingInMemoryFontCreator : IInMemoryFontCreator
    {
        public BmfcSave? LastBmfcSave { get; private set; }

        public RenderingLibrary.Graphics.BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            LastBmfcSave = bmfcSave;
            return null;
        }
    }
}
