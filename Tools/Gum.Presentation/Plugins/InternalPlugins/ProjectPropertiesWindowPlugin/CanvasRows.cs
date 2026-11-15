using System;
using System.Collections.Generic;
using Gum.SettingsWindow;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// The rows of the Project > Canvas page of the Settings window over the shared
/// <see cref="ProjectPropertiesViewModel"/>, built with <see cref="SettingsRows"/>. Each row reads and
/// writes the same view model property the page always did, so the saved data is unchanged.
/// </summary>
public sealed class CanvasRows : IRowsSettingsPage
{
    private readonly ProjectPropertiesViewModel _viewModel;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public CanvasRows(ProjectPropertiesViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <inheritdoc/>
    public event Action? Reloaded
    {
        add => _viewModel.Reloaded += value;
        remove => _viewModel.Reloaded -= value;
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        ProjectPropertiesViewModel vm = _viewModel;

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Canvas size", new[]
            {
                SettingsRows.Number(nameof(vm.CanvasWidth), "Canvas width",
                    "Width of the canvas that top-level elements are laid out against, in pixels.",
                    () => vm.CanvasWidth, value => vm.CanvasWidth = value),
                SettingsRows.Number(nameof(vm.CanvasHeight), "Canvas height",
                    "Height of the canvas that top-level elements are laid out against, in pixels.",
                    () => vm.CanvasHeight, value => vm.CanvasHeight = value),
            }),
            SettingsRows.Section("Guides", new[]
            {
                SettingsRows.Toggle(nameof(vm.ShowOutlines), "Show outlines",
                    "Draws an outline around containers and other invisible instances.",
                    () => vm.ShowOutlines, value => vm.ShowOutlines = value, toggleText: "Outline containers"),
                SettingsRows.Toggle(nameof(vm.ShowCanvasOutline), "Show canvas outline",
                    "Draws the border of the canvas.",
                    () => vm.ShowCanvasOutline, value => vm.ShowCanvasOutline = value, toggleText: "Outline the canvas"),
                SettingsRows.Toggle(nameof(vm.ShowCheckerBackground), "Show checker background",
                    "Draws a checkerboard behind the canvas so transparent areas show.",
                    () => vm.ShowCheckerBackground, value => vm.ShowCheckerBackground = value, toggleText: "Show checkerboard"),
            }),
        };
        SettingsRows.Finish(sections);
        return sections;
    }
}
