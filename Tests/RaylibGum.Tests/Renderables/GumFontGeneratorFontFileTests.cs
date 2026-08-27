using System;
using System.IO;
using System.Linq;
using System.Text;
using Gum.Bundle;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #4515 — a <c>.ttf</c> referenced by a Gum project can live inside a <c>.gumpkg</c> rather
/// than on disk, and a bundle serves its entries only through <see cref="FileManager.CustomGetStreamFromFile"/>.
/// <see cref="GumFontGenerator"/> must read <see cref="BmfcSave.FontFile"/> through that hook so
/// bundle-backed (and host-hook-backed) fonts rasterize instead of silently falling back.
/// The hook is global mutable state, so each test stashes the previous value and restores it.
/// </summary>
public class GumFontGeneratorFontFileTests : IDisposable
{
    private readonly Func<string, Stream>? _previousHook;
    private readonly string _tempDirectory;

    public GumFontGeneratorFontFileTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        FileManager.CustomGetStreamFromFile = null;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFontGeneratorFontFileTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void Generate_WhenFontFileIsOnlyServedByAGumpkgBundle_RasterizesTheFont()
    {
        const string fontEntryPath = "Fonts/Orbitron-Black.ttf";
        const int codepointA = 65;
        byte[] fontBytes = ReadFixtureFontBytes();

        string gumpkgPath = Path.Combine(_tempDirectory, "Project.gumpkg");
        using (FileStream output = File.Create(gumpkgPath))
        {
            GumBundleWriter.Write(output, new (string, byte[])[]
            {
                ("Project.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
                (fontEntryPath, fontBytes),
            });
        }

        GumBundleLoader.Resolve(gumpkgPath).UsedBundle.ShouldBeTrue();

        // The absolute path CustomSetPropertyOnRenderable.ResolveFontFilePath builds for a
        // project-relative Font value: the loaded project's directory plus the authored path.
        BmfcSave bmfcSave = BuildBmfcSave(Path.Combine(_tempDirectory, "Fonts", "Orbitron-Black.ttf"), codepointA);

        KernSmith.Output.BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        result.Model.Characters.Select(character => character.Id).ShouldContain(codepointA);
    }

    /// <summary>
    /// A host hook that doesn't carry this font must not hide a copy on disk: FileManager routes
    /// exclusively to the hook once one is installed.
    /// </summary>
    [Fact]
    public void Generate_WhenTheHookDoesNotHaveTheFontButDiskDoes_RasterizesTheFont()
    {
        const int codepointA = 65;
        FileManager.CustomGetStreamFromFile = requestedPath =>
            throw new FileNotFoundException($"'{requestedPath}' is not in this bundle.", requestedPath);

        BmfcSave bmfcSave = BuildBmfcSave(FixtureFontPath, codepointA);

        KernSmith.Output.BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        result.Model.Characters.Select(character => character.Id).ShouldContain(codepointA);
    }

    [Fact]
    public void Generate_WhenFontFileIsOnDiskAndNoHookIsInstalled_RasterizesTheFont()
    {
        const int codepointA = 65;

        BmfcSave bmfcSave = BuildBmfcSave(FixtureFontPath, codepointA);

        KernSmith.Output.BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        result.Model.Characters.Select(character => character.Id).ShouldContain(codepointA);
    }

    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    private static byte[] ReadFixtureFontBytes() => File.ReadAllBytes(FixtureFontPath);

    private static BmfcSave BuildBmfcSave(string fontFile, int codepoint) => new BmfcSave
    {
        FontName = "Orbitron-Black",
        FontFile = fontFile,
        FontSize = 24,
        UseSmoothing = true,
        Ranges = codepoint.ToString(),
    };
}
