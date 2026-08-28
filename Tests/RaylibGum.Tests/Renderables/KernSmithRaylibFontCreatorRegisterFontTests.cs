using System;
using System.IO;
using System.Linq;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #4523 — raylib only had the <c>byte[]</c> registration overload, so a game shipping a
/// .ttf had to read the file itself. The path overload reads through
/// <see cref="FileManager.GetStreamForFile"/>, so a font inside a <c>.gumpkg</c> registers like one
/// on disk. Both the hook and KernSmith's font registry are global mutable state, so each test
/// restores them.
/// </summary>
public class KernSmithRaylibFontCreatorRegisterFontTests : IDisposable
{
    private const string FamilyName = "GumOrbitronFixture";
    private const int CodepointA = 65;

    private readonly Func<string, Stream>? _previousHook;

    public KernSmithRaylibFontCreatorRegisterFontTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        FileManager.CustomGetStreamFromFile = null;
        KernSmithRaylibFontCreator.UnregisterFont(FamilyName);
    }

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        KernSmithRaylibFontCreator.UnregisterFont(FamilyName);
    }

    [Fact]
    public void RegisterFont_WhenTheFileIsOnlyServedByAStreamHook_RegistersTheFont()
    {
        const string bundledPath = "Content/Fonts/BundledOnly.ttf";
        byte[] fontBytes = File.ReadAllBytes(FixtureFontPath);
        File.Exists(FileManager.MakeAbsolute(bundledPath)).ShouldBeFalse();

        FileManager.CustomGetStreamFromFile = requestedPath =>
            requestedPath.Replace('\\', '/').EndsWith(bundledPath, StringComparison.OrdinalIgnoreCase)
                ? new MemoryStream(fontBytes)
                : throw new FileNotFoundException($"'{requestedPath}' is not in this bundle.", requestedPath);

        KernSmithRaylibFontCreator.RegisterFont(FamilyName, bundledPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    [Fact]
    public void RegisterFont_WhenTheFileIsOnDisk_RegistersTheFont()
    {
        KernSmithRaylibFontCreator.RegisterFont(FamilyName, FixtureFontPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    /// <summary>
    /// Absolute, because other tests in this assembly leave <see cref="FileManager.RelativeDirectory"/>
    /// pointing at the content folder and a relative path would resolve under it twice.
    /// </summary>
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    /// <summary>
    /// Rasterizes the registered family by name. KernSmith resolves a registered font ahead of any
    /// system-installed one, so reaching the glyph proves the registration took.
    /// </summary>
    private static int[] GeneratedCodepoints()
    {
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = FamilyName,
            FontSize = 24,
            UseSmoothing = true,
            Ranges = CodepointA.ToString(),
        };

        KernSmith.Output.BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        return result.Model.Characters.Select(character => character.Id).ToArray();
    }
}
