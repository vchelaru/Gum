using System.Threading.Tasks;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Replaces the real font generator in the test container. The real one generates on a thread-pool
/// task that can outlive the test that started it and write to the Output panel after the headless
/// session is gone, which crashes the test host (#5097). This one completes immediately and writes nothing.
/// </summary>
public class NoOpFontFileGenerator : IFontFileGenerator
{
    /// <inheritdoc/>
    public bool RequiresSizeEstimation => false;

    private readonly List<string> _requestedFntPaths = new List<string>();

    /// <summary>The .fnt paths the tool has asked for so far, so a test can see what would have been generated.</summary>
    public List<string> RequestedFntPaths()
    {
        lock (_requestedFntPaths)
        {
            return _requestedFntPaths.ToList();
        }
    }

    /// <inheritdoc/>
    public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        lock (_requestedFntPaths)
        {
            _requestedFntPaths.Add(outputFntPath);
        }
        return Task.FromResult(GeneralResponse.SuccessfulResponse);
    }
}
