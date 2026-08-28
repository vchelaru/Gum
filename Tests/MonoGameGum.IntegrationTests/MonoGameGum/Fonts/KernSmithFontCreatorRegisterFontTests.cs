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
/// <remarks>
/// <see cref="FileManager.RelativeDirectory"/> is also global mutable state, and RegisterFont's path
/// resolution now depends on it (#4527) - other tests in this assembly leave it wherever they last
/// set it (e.g. <c>GumService.Uninitialize</c> sets it to <c>"Content/"</c>, relative to the exe
/// directory), so ambient state left over from suite ordering is not safe to assume here. Each test
/// pins it explicitly instead, either via the constructor's <see cref="ContentDirectory"/> baseline
/// or by overriding it for a scenario that needs a different value.
/// </remarks>
public class KernSmithFontCreatorRegisterFontTests : IDisposable
{
    private const string FamilyName = "GumOrbitronFixture";
    private const int CodepointA = 65;

    private readonly Func<string, Stream>? _previousHook;
    private readonly string _previousRelativeDirectory;

    public KernSmithFontCreatorRegisterFontTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        _previousRelativeDirectory = FileManager.RelativeDirectory;
        FileManager.CustomGetStreamFromFile = null;
        FileManager.RelativeDirectory = ContentDirectory;
        KernSmithFontCreator.UnregisterFont(FamilyName);
    }

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        KernSmithFontCreator.UnregisterFont(FamilyName);
    }

    [Fact]
    public void RegisterFont_WhenTheFileIsOnlyServedByAStreamHook_RegistersTheFont()
    {
        const string bundledPath = "Fonts/BundledOnly.ttf";
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
    /// exclusively to the hook once one is installed. Also the #4527 regression case: the path is
    /// relative to FileManager.RelativeDirectory (ContentDirectory), not the title container root.
    /// </summary>
    [Fact]
    public void RegisterFont_WhenTheHookDoesNotHaveTheFontButDiskDoes_RegistersTheFont()
    {
        FileManager.CustomGetStreamFromFile = requestedPath =>
            throw new FileNotFoundException($"'{requestedPath}' is not in this bundle.", requestedPath);

        KernSmithFontCreator.RegisterFont(FamilyName, RelativeDirectoryRelativeFontPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    /// <summary>
    /// The title container is what knows where content lives on Android, iOS and consoles, so it
    /// has to stay reachable when FileManager resolves the path somewhere the font isn't. When the
    /// configured RelativeDirectory can't be translated to a title-container-relative path (it sits
    /// outside the exe directory entirely, as here), the fallback hands TitleContainer the path
    /// unchanged - the pre-#4527 exe-root-relative convention.
    /// </summary>
    [Fact]
    public void RegisterFont_WhenFileManagerResolvesElsewhere_FallsBackToTheTitleContainer()
    {
        FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(), "GumNoFontsHere") + Path.DirectorySeparatorChar;
        File.Exists(FileManager.MakeAbsolute(TitleContainerRelativeFontPath)).ShouldBeFalse();

        FileManager.CustomGetStreamFromFile = requestedPath =>
            throw new FileNotFoundException($"'{requestedPath}' is not in this bundle.", requestedPath);

        KernSmithFontCreator.RegisterFont(FamilyName, TitleContainerRelativeFontPath);

        GeneratedCodepoints().ShouldContain(CodepointA);
    }

    /// <summary>
    /// FileManager.RelativeDirectory pinned to the fixture's Content folder, matching how a real game
    /// configures it (e.g. via GumService initialization) - the base every test in this class resolves
    /// paths against unless it overrides RelativeDirectory itself.
    /// </summary>
    private static string ContentDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Content") + Path.DirectorySeparatorChar;

    /// <summary>Relative to FileManager.RelativeDirectory (i.e. ContentDirectory) - the #4527 convention.</summary>
    private const string RelativeDirectoryRelativeFontPath = "Fonts/Orbitron-Black.ttf";

    /// <summary>Relative to the title container root (the exe directory) - the pre-#4527 convention.</summary>
    private const string TitleContainerRelativeFontPath = "Content/Fonts/Orbitron-Black.ttf";

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
