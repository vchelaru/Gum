using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A baked font records the ranges it was requested with, so a later change to the project's ranges
/// (made outside the tool, where nothing clears the font cache) re-bakes it (#6008).
/// </summary>
public class HeadlessFontGenerationRangeChangeTests : BaseTestClass
{
    private readonly string _projectDirectory =
        Path.Combine(Path.GetTempPath(), "GumFontRangeTest_" + Guid.NewGuid()) + Path.DirectorySeparatorChar;

    public HeadlessFontGenerationRangeChangeTests()
    {
        StandardElementSave textStandard = Project.StandardElements.First(e => e.Name == "Text");
        textStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Arial" });
        textStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 18 });

        ObjectFinder.Self.GumProjectSave = Project;
        Directory.CreateDirectory(_projectDirectory);
    }

    [Fact]
    public async Task CreateAllMissingFontFiles_RebakesAFont_OnlyWhenTheRangesItWasBakedWithChange()
    {
        try
        {
            WritingFontFileGenerator generator = new();
            HeadlessFontGenerationService service = new(generator);

            Project.FontRanges = "32-126";
            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(1);

            // Nothing changed: the stamped font is up to date.
            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(0);

            // The same codepoints spelled differently are not a change.
            Project.FontRanges = "32-100,101-126";
            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(0);

            // A different range re-bakes it with the new range.
            Project.FontRanges = "65-70";
            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(1);
            generator.RangesBaked.ShouldBe(new[] { "32-126", "65-70" });

            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(0);
        }
        finally
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAllMissingFontFiles_LeavesAFontWithNoStampAlone()
    {
        // A .fnt Gum did not stamp (an older bake, or one bmfont.exe wrote) has no recorded ranges,
        // so it counts as current instead of every existing font re-baking after an upgrade.
        try
        {
            BmfcSave arial = new BmfcSave { FontName = "Arial", FontSize = 18 };
            string cachedPath = Path.Combine(_projectDirectory, arial.FontCacheFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
            File.WriteAllText(cachedPath, WritingFontFileGenerator.MinimalFnt);

            Project.FontRanges = "65-70";
            WritingFontFileGenerator generator = new();
            HeadlessFontGenerationService service = new(generator);

            (await service.CreateAllMissingFontFiles(Project, _projectDirectory)).ShouldBe(0);
            generator.RangesBaked.ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    /// <summary>Writes a minimal text .fnt like a real generator would and records the ranges it was asked for.</summary>
    private sealed class WritingFontFileGenerator : IFontFileGenerator
    {
        public const string MinimalFnt =
            "info face=\"Arial\" size=18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=21 base=17 scaleW=64 scaleH=64 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            "chars count=0\n";

        public bool RequiresSizeEstimation => false;
        public List<string> RangesBaked { get; } = new();

        public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
        {
            RangesBaked.Add(bmfcSave.Ranges);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFntPath)!);
            File.WriteAllText(outputFntPath, MinimalFnt);
            return Task.FromResult(GeneralResponse.SuccessfulResponse);
        }
    }
}
