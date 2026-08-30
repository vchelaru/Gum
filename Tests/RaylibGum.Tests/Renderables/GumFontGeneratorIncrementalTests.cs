using System;
using System.IO;
using System.Linq;
using KernSmith;
using KernSmith.Gum;
using KernSmith.Output;
using KernSmith.Output.Model;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #4535 -- KernSmith 0.21.0's <see cref="BmFontIncrementalSession"/> (<see cref="BmFont.BeginIncremental"/>/
/// <see cref="BmFont.ResumeIncremental"/>) adds glyphs to a live atlas without moving existing ones. These tests
/// cover <see cref="GumFontGenerator.BeginIncremental"/>/<see cref="GumFontGenerator.ResumeIncremental"/>, the
/// thin wrappers that map a Gum <see cref="BmfcSave"/> to KernSmith's session entry points the same way
/// <see cref="GumFontGenerator.Generate"/> maps it for a one-shot atlas.
/// </summary>
public class GumFontGeneratorIncrementalTests
{
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    private static BmfcSave BuildBmfcSave(string ranges) => new BmfcSave
    {
        FontName = "Orbitron-Black",
        FontFile = FixtureFontPath,
        FontSize = 24,
        UseSmoothing = true,
        Ranges = ranges,
    };

    [Fact]
    public void BeginIncremental_ThenAddGlyphs_ReturnsThePlacedGlyph()
    {
        BmfcSave bmfcSave = BuildBmfcSave("65");

        using BmFontIncrementalSession session = GumFontGenerator.BeginIncremental(bmfcSave);
        GlyphAdditionResult result = session.AddGlyphs("A");

        result.Added.Select(glyph => glyph.Codepoint).ShouldContain((int)'A');
    }

    [Fact]
    public void BeginIncremental_WithNoFontFile_ThrowsNotSupportedException()
    {
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 18, Ranges = "65" };

        Should.Throw<NotSupportedException>(() => GumFontGenerator.BeginIncremental(bmfcSave));
    }

    [Fact]
    public void ResumeIncremental_AddsAGlyphNotInTheOriginalGeneration_WithoutMovingExistingGlyphs()
    {
        BmfcSave bmfcSave = BuildBmfcSave("65"); // just 'A'
        BmFontResult generated = GumFontGenerator.Generate(bmfcSave);
        CharEntry originalA = generated.Model.Characters.Single(character => character.Id == (int)'A');

        using BmFontIncrementalSession session = GumFontGenerator.ResumeIncremental(bmfcSave, generated.Model);
        GlyphAdditionResult result = session.AddGlyphs("B");

        result.Added.Select(glyph => glyph.Codepoint).ShouldContain((int)'B');
        session.CurrentModel.Characters.Single(character => character.Id == (int)'A').ShouldBe(originalA,
            "because resuming and adding a new glyph must not move a glyph already placed (stable packing)");
    }
}
