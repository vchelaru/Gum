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
    private readonly Func<bool> _isBmFontSupported;

    /// <summary>
    /// Initializes a new instance of <see cref="FontFileGeneratorSelector"/>.
    /// </summary>
    /// <param name="bmFontGenerator">The bmfont.exe-based generator (Windows-only).</param>
    /// <param name="kernSmithGenerator">The KernSmith-based generator (cross-platform).</param>
    /// <param name="getGeneratorType">
    /// A delegate that returns the current project's <see cref="FontGeneratorType"/>.
    /// Called on every <see cref="GenerateFont"/> invocation so it reflects the latest project state.
    /// </param>
    /// <param name="isBmFontSupported">
    /// Whether bmfont.exe can run here; defaults to <see cref="FontGeneratorResolver.IsBmFontSupported"/>.
    /// A <see cref="FontGeneratorType.BmFont"/> request is served by KernSmith when this is false.
    /// </param>
    public FontFileGeneratorSelector(
        IFontFileGenerator bmFontGenerator,
        IFontFileGenerator kernSmithGenerator,
        Func<FontGeneratorType> getGeneratorType,
        Func<bool>? isBmFontSupported = null)
    {
        _bmFontGenerator = bmFontGenerator;
        _kernSmithGenerator = kernSmithGenerator;
        _getGeneratorType = getGeneratorType;
        _isBmFontSupported = isBmFontSupported ?? (() => FontGeneratorResolver.IsBmFontSupported);
    }

    /// <inheritdoc/>
    public Task<GeneralResponse> GenerateFont(BmfcSave bmfcSave, string outputFntPath, bool createTask)
    {
        return CurrentGenerator.GenerateFont(bmfcSave, outputFntPath, createTask);
    }

    /// <inheritdoc/>
    public bool RequiresSizeEstimation => CurrentGenerator.RequiresSizeEstimation;

    private IFontFileGenerator CurrentGenerator =>
        FontGeneratorResolver.Resolve(_getGeneratorType(), _isBmFontSupported()) switch
        {
            FontGeneratorType.KernSmith => _kernSmithGenerator,
            _ => _bmFontGenerator
        };
}
