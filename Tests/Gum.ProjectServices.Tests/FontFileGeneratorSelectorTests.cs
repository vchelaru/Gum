using System;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

public class FontFileGeneratorSelectorTests
{
    [Fact]
    public async Task GenerateFont_ShouldDelegateToBmFont_WhenGeneratorTypeIsBmFont()
    {
        RecordingFontFileGenerator bmFont = new RecordingFontFileGenerator();
        RecordingFontFileGenerator kernSmith = new RecordingFontFileGenerator();
        FontFileGeneratorSelector selector = new FontFileGeneratorSelector(
            bmFont, kernSmith, () => FontGeneratorType.BmFont);

        BmfcSave bmfcSave = new BmfcSave();
        await selector.GenerateFont(bmfcSave, "/tmp/test.fnt", createTask: false);

        bmFont.WasCalled.ShouldBeTrue();
        kernSmith.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateFont_ShouldDelegateToKernSmith_WhenGeneratorTypeIsKernSmith()
    {
        RecordingFontFileGenerator bmFont = new RecordingFontFileGenerator();
        RecordingFontFileGenerator kernSmith = new RecordingFontFileGenerator();
        FontFileGeneratorSelector selector = new FontFileGeneratorSelector(
            bmFont, kernSmith, () => FontGeneratorType.KernSmith);

        BmfcSave bmfcSave = new BmfcSave();
        await selector.GenerateFont(bmfcSave, "/tmp/test.fnt", createTask: false);

        kernSmith.WasCalled.ShouldBeTrue();
        bmFont.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateFont_ShouldReevaluateGeneratorType_OnEachCall()
    {
        RecordingFontFileGenerator bmFont = new RecordingFontFileGenerator();
        RecordingFontFileGenerator kernSmith = new RecordingFontFileGenerator();
        FontGeneratorType currentType = FontGeneratorType.BmFont;
        FontFileGeneratorSelector selector = new FontFileGeneratorSelector(
            bmFont, kernSmith, () => currentType);

        BmfcSave bmfcSave = new BmfcSave();

        await selector.GenerateFont(bmfcSave, "/tmp/test.fnt", createTask: false);
        bmFont.WasCalled.ShouldBeTrue();

        // Switch generator type mid-session
        bmFont.Reset();
        currentType = FontGeneratorType.KernSmith;

        await selector.GenerateFont(bmfcSave, "/tmp/test.fnt", createTask: false);
        kernSmith.WasCalled.ShouldBeTrue();
        bmFont.WasCalled.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    /// <summary>
    /// A recording <see cref="IFontFileGenerator"/> that tracks whether it was called.
    /// </summary>
    private sealed class RecordingFontFileGenerator : IFontFileGenerator
    {
        public bool WasCalled { get; private set; }

        public void Reset()
        {
            WasCalled = false;
        }

        public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
        {
            WasCalled = true;
            return Task.FromResult(GeneralResponse.SuccessfulResponse);
        }
    }
}
