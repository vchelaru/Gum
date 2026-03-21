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
}
