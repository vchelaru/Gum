using System;
using System.IO;
using System.Threading.Tasks;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #4001 — a dropshadow font now consists of a primary .fnt plus a sibling "-shadow.fnt"
/// (the shadow silhouette, sharing the same PNG). The regeneration check must treat the font as
/// missing when the sibling is absent, so a primary left over without its shadow sibling
/// regenerates instead of silently rendering with no shadow.
/// </summary>
public class HeadlessFontGenerationShadowSiblingTests : IDisposable
{
    private readonly string _projectDirectory;

    public HeadlessFontGenerationShadowSiblingTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumShadowRegen_" + Guid.NewGuid().ToString("N"))
            + Path.DirectorySeparatorChar;
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void RegeneratesWhenShadowSiblingMissing()
    {
        BmfcSave bmfcSave = ShadowBmfcSave();
        WritePrimaryFntOnly(bmfcSave);

        RecordingFontFileGenerator generator = new RecordingFontFileGenerator();
        new HeadlessFontGenerationService(generator)
            .CreateFontIfNecessary(bmfcSave, _projectDirectory, autoSizeFontOutputs: false);

        generator.CallCount.ShouldBe(1);
    }

    [Fact]
    public void SkipsRegenerationWhenBothPrimaryAndShadowSiblingExist()
    {
        BmfcSave bmfcSave = ShadowBmfcSave();
        WritePrimaryFntOnly(bmfcSave);
        WriteShadowSibling(bmfcSave);

        RecordingFontFileGenerator generator = new RecordingFontFileGenerator();
        new HeadlessFontGenerationService(generator)
            .CreateFontIfNecessary(bmfcSave, _projectDirectory, autoSizeFontOutputs: false);

        generator.CallCount.ShouldBe(0);
    }

    [Fact]
    public void NonDropshadowFont_WithPrimaryPresent_DoesNotRegenerate()
    {
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 24 };
        WritePrimaryFntOnly(bmfcSave);

        RecordingFontFileGenerator generator = new RecordingFontFileGenerator();
        new HeadlessFontGenerationService(generator)
            .CreateFontIfNecessary(bmfcSave, _projectDirectory, autoSizeFontOutputs: false);

        generator.CallCount.ShouldBe(0);
    }

    private static BmfcSave ShadowBmfcSave() => new BmfcSave
    {
        FontName = "Arial",
        FontSize = 24,
        HasDropshadow = true,
        DropshadowBlur = 2f,
    };

    private string PrimaryPath(BmfcSave bmfcSave) => Path.Combine(_projectDirectory, bmfcSave.FontCacheFileName);

    private void WritePrimaryFntOnly(BmfcSave bmfcSave)
    {
        string primary = PrimaryPath(bmfcSave);
        Directory.CreateDirectory(Path.GetDirectoryName(primary)!);
        File.WriteAllText(primary, "");
    }

    private void WriteShadowSibling(BmfcSave bmfcSave)
    {
        string primary = PrimaryPath(bmfcSave);
        string sibling = primary.Substring(0, primary.Length - ".fnt".Length) + "-shadow.fnt";
        File.WriteAllText(sibling, "");
    }

    private sealed class RecordingFontFileGenerator : IFontFileGenerator
    {
        public bool RequiresSizeEstimation { get; init; }
        public bool UsesExternalProcess { get; init; }
        public int CallCount { get; private set; }

        public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
        {
            CallCount++;
            return Task.FromResult(GeneralResponse.SuccessfulResponse);
        }
    }
}
