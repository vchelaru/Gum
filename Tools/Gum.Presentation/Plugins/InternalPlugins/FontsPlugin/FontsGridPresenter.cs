using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.ProjectServices.FontGeneration;
using Gum.Services;
using ToolsUtilities;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.FontsPlugin;

/// <summary>
/// Drives the Generation rows of the Fonts tab over a <see cref="FontsViewModel"/>: the font generator,
/// spacing and auto-size settings in one category with spaced names and option lists, and no rows for
/// the ranges table or the view model's bookkeeping members (the view draws those itself). Off Windows the
/// font generator row is read-only, since bmfont.exe cannot run there and KernSmith is used regardless.
/// </summary>
public sealed class FontsGridPresenter
{
    private const string GenerationCategory = "Generation";

    private static readonly string[] GenerationMembers =
    {
        nameof(FontsViewModel.FontGenerator),
        nameof(FontsViewModel.FontSpacingHorizontal),
        nameof(FontsViewModel.FontSpacingVertical),
        nameof(FontsViewModel.AutoSizeFontOutputs),
    };

    private readonly IDataUiGrid _grid;
    private readonly IOperatingSystemInfo _operatingSystem;
    private FontsViewModel? _viewModel;

    /// <summary>Creates a presenter over <paramref name="grid"/> for the current OS unless one is given.</summary>
    public FontsGridPresenter(IDataUiGrid grid, IOperatingSystemInfo? operatingSystem = null)
    {
        _grid = grid;
        _operatingSystem = operatingSystem ?? new OperatingSystemInfo();
    }

    /// <summary>Shows <paramref name="viewModel"/> (or clears the grid for null) and follows its reloads.</summary>
    public void Bind(FontsViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.Reloaded -= Rebuild;
        }
        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.Reloaded += Rebuild;
        }
        Rebuild();
    }

    /// <summary>Rebuilds the rows from the view model's current members.</summary>
    public void Rebuild()
    {
        // Clearing first makes the grid rebuild its rows even when the view model is the same object.
        _grid.Instance = null;
        if (_viewModel != null)
        {
            _grid.Instance = _viewModel;
            ApplyLayout(_grid.Model, _operatingSystem);
        }
    }

    /// <summary>Shapes the auto-populated rows of <paramref name="grid"/> as described on the class.</summary>
    public static void ApplyLayout(DataUiGridModel grid, IOperatingSystemInfo operatingSystem)
    {
        foreach (InstanceMember member in grid.Categories.SelectMany(category => category.Members).ToArray())
        {
            member.SupportsMakeDefault = false;
        }

        foreach (string name in GenerationMembers)
        {
            grid.MoveMemberToCategory(name, GenerationCategory);
        }

        foreach (MemberCategory category in grid.Categories)
        {
            foreach (InstanceMember extra in category.Members.Where(member => !GenerationMembers.Contains(member.Name)).ToArray())
            {
                category.Members.Remove(extra);
            }

            foreach (InstanceMember member in category.Members)
            {
                member.DisplayName = StringFunctions.InsertSpacesInCamelCaseString(member.DisplayName);
            }
        }

        InstanceMember? fontGenerator = grid.GetInstanceMember(nameof(FontsViewModel.FontGenerator));
        if (fontGenerator != null)
        {
            fontGenerator.CustomOptions = new List<object> { FontGeneratorType.BmFont, FontGeneratorType.KernSmith };
            if (FontGeneratorResolver.IsSubstituted(FontGeneratorType.BmFont, operatingSystem.IsWindows))
            {
                fontGenerator.IsReadOnly = true;
                fontGenerator.DetailText = $"KernSmith is used on {operatingSystem.DisplayName}";
            }
        }

        // Named after the spacing pass, which would otherwise split "Auto-Size" at its capital.
        InstanceMember? autoSize = grid.GetInstanceMember(nameof(FontsViewModel.AutoSizeFontOutputs));
        if (autoSize != null)
        {
            autoSize.DisplayName = "Auto-Size Font Outputs";
            autoSize.DetailText = "Fewer PNGs, but font generation can be much slower";
        }
    }
}
