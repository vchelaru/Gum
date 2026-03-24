using System;
using System.Threading.Tasks;
using Gum.DataTypes;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Delegates font generation to the correct <see cref="IFontFileGenerator"/> implementation
/// based on the current project's <see cref="FontGeneratorType"/> setting.
/// </summary>
/// <remarks>
/// This class exists because the Gum tool registers DI services before a project is loaded,
/// so the generator choice must be deferred until generation time.
/// </remarks>
public class FontFileGeneratorSelector : IFontFileGenerator
{
    private readonly IFontFileGenerator _bmFontGenerator;
    private readonly IFontFileGenerator _kernSmithGenerator;
    private readonly Func<FontGeneratorType> _getGeneratorType;

    /// <summary>
    /// Initializes a new instance of <see cref="FontFileGeneratorSelector"/>.
    /// </summary>
    /// <param name="bmFontGenerator">The bmfont.exe-based generator (Windows-only).</param>
    /// <param name="kernSmithGenerator">The KernSmith-based generator (cross-platform).</param>
    /// <param name="getGeneratorType">
    /// A delegate that returns the current project's <see cref="FontGeneratorType"/>.
    /// Called on every <see cref="GenerateFont"/> invocation so it reflects the latest project state.
    /// </param>
    public FontFileGeneratorSelector(
        IFontFileGenerator bmFontGenerator,
        IFontFileGenerator kernSmithGenerator,
        Func<FontGeneratorType> getGeneratorType)
    {
        _bmFontGenerator = bmFontGenerator;
        _kernSmithGenerator = kernSmithGenerator;
        _getGeneratorType = getGeneratorType;
    }

    /// <inheritdoc/>
    public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        IFontFileGenerator generator = _getGeneratorType() switch
        {
            FontGeneratorType.KernSmith => _kernSmithGenerator,
            _ => _bmFontGenerator
        };

        return generator.GenerateFont(bmfcSave, outputFntPath, createTask);
    }
}
