using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A project authored on Windows names fonts the machine may not have (Linux has no Arial). The
/// generator rasterizes the platform's substitute for such a font instead of producing no text at
/// all, and says so in the output.
/// </summary>
public class KernSmithFileGeneratorFallbackTests : IDisposable
{
    private readonly string _outputDirectory;

    public KernSmithFileGeneratorFallbackTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "GumKernSmithFallback_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        Directory.Delete(_outputDirectory, recursive: true);
    }

    [Fact]
    public async Task GenerateFont_UsesThePlatformSubstitute_WhenTheSystemFontIsNotInstalled()
    {
        RecordingCallbacks callbacks = new RecordingCallbacks();
        KernSmithFileGenerator generator = new KernSmithFileGenerator(callbacks);
        BmfcSave bmfcSave = new BmfcSave { FontName = "Gum Font That Is Not Installed 4c2b", FontSize = 18, Ranges = "65-70" };
        string outputFnt = Path.Combine(_outputDirectory, "Font18Missing.fnt");

        GeneralResponse response = await generator.GenerateFont(bmfcSave, outputFnt, createTask: false);

        response.Succeeded.ShouldBeTrue(response.Message);
        File.Exists(outputFnt).ShouldBeTrue();
        callbacks.Messages.ShouldContain(message => message.Contains("Gum Font That Is Not Installed 4c2b") && message.Contains("not installed"));
    }

    private sealed class RecordingCallbacks : IFontGenerationCallbacks
    {
        public List<string> Messages { get; } = new List<string>();

        public void OnOutput(string message)
        {
            Messages.Add(message);
        }
    }
}
