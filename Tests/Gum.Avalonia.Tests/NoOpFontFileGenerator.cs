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

    /// <inheritdoc/>
    public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask) =>
        Task.FromResult(GeneralResponse.SuccessfulResponse);
}
