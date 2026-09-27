using System.Collections.Generic;
using System.Threading.Tasks;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Replaces the real font generator in the test container. The real one generates on a thread-pool
/// task that can outlive the test that started it and write to the Output panel after the headless
/// session is gone, which crashes the test host (#5097). This one completes immediately and writes
/// nothing; it records each requested .fnt path so a test can see that a font was asked for.
/// </summary>
public class NoOpFontFileGenerator : IFontFileGenerator
{
    public NoOpFontFileGenerator()
    {
        RequestedFntPaths = new List<string>();
    }

    /// <inheritdoc/>
    public bool RequiresSizeEstimation => false;

    /// <summary>The output path of every font asked for, in order. A test clears it before its gesture.</summary>
    public List<string> RequestedFntPaths { get; }

    /// <inheritdoc/>
    public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        lock (RequestedFntPaths)
        {
            RequestedFntPaths.Add(outputFntPath);
        }
        return Task.FromResult(GeneralResponse.SuccessfulResponse);
    }
}
