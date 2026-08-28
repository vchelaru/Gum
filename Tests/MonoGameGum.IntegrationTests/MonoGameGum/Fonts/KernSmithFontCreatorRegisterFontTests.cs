using System;
using System.IO;
using System.Linq;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Issue #4523 — a <c>.ttf</c> handed to <see cref="KernSmithFontCreator.RegisterFont(string, string, string?, int)"/>
/// can live behind <see cref="FileManager.CustomGetStreamFromFile"/> rather than on disk (a
/// <c>.gumpkg</c> bundle, a host's asset zip), so registration must read through that hook before
/// falling back to the title container. Both the hook and KernSmith's font registry are global
/// mutable state, so each test restores them.
/// </summary>
public class KernSmithFontCreatorRegisterFontTests : IDisposable
{
    private const string FamilyName = "GumOrbitronFixture";
    private const int CodepointA = 65;

    private readonly Func<string, Stream>? _previousHook;

    public KernSmithFontCreatorRegisterFontTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        FileManager.CustomGetStreamFromFile = null;
        KernSmithFontCreator.UnregisterFont(FamilyName);
    }

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        KernSmithFontCreator.UnregisterFont(FamilyName);
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

        KernSmithFontCreator.RegisterFont(FamilyName, bundledPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    /// <summary>
    /// A host hook that doesn't carry this font must not hide a copy on disk: FileManager routes
    /// exclusively to the hook once one is installed.
    /// </summary>
    [Fact]
    public void RegisterFont_WhenTheHookDoesNotHaveTheFontButDiskDoes_RegistersTheFont()
    {
        FileManager.CustomGetStreamFromFile = requestedPath =>
            throw new FileNotFoundException($"'{requestedPath}' is not in this bundle.", requestedPath);

        KernSmithFontCreator.RegisterFont(FamilyName, RelativeFixtureFontPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    private const string RelativeFixtureFontPath = "Content/Fonts/Orbitron-Black.ttf";

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
