using System.Threading.Tasks;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Strategy interface for generating a single bitmap font file (.fnt + .png) from a BmfcSave description.
/// </summary>
public interface IFontFileGenerator
{
    /// <summary>
    /// Generates a bitmap font file at the specified output path.
    /// </summary>
    /// <param name="bmfcSave">Font description (name, size, style, ranges, etc.).</param>
    /// <param name="outputFntPath">Absolute path for the output .fnt file.</param>
    /// <param name="createTask">When true, runs asynchronously; when false, blocks until complete.</param>
    Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask);

    /// <summary>
    /// Whether this generator needs <see cref="BmfcSave.OutputWidth"/>/<see cref="BmfcSave.OutputHeight"/>
    /// pre-populated by the caller before generation. Generators that can size their own atlas
    /// (e.g. KernSmith's autofit) should return false so the caller skips that work entirely.
    /// </summary>
    bool RequiresSizeEstimation { get; }

    /// <summary>
    /// Whether this generator shells out to a separate top-level-window process (e.g. bmfont.exe)
    /// to produce a font. In-process generators (e.g. KernSmith) should return false. Callers use
    /// this to know when the external process may have stolen window focus.
    /// </summary>
    bool UsesExternalProcess { get; }
}
