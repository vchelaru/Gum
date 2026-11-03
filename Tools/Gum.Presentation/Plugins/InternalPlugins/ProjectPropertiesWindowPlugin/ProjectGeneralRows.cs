using System;
using System.Collections.Generic;
using Gum.SettingsWindow;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// The rows of the Project > General page of the Settings window over the shared
/// <see cref="ProjectPropertiesViewModel"/>, built with <see cref="SettingsRows"/>.
/// </summary>
public sealed class ProjectGeneralRows : IRowsSettingsPage
{
    /// <summary>Where the custom bounds start when the toggle turns them on: the 1x1 white pixel the canvas draws without a texture.</summary>
    public static readonly (int Left, int Top, int Right, int Bottom) DefaultBounds = (0, 0, 1, 1);

    private readonly ProjectPropertiesViewModel _viewModel;
    private event Action? BoundsToggled;

    /// <summary>Creates the rows over <paramref name="viewModel"/>.</summary>
    public ProjectGeneralRows(ProjectPropertiesViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <inheritdoc/>
    public event Action? Reloaded
    {
        add { _viewModel.Reloaded += value; BoundsToggled += value; }
        remove { _viewModel.Reloaded -= value; BoundsToggled -= value; }
    }

    /// <summary>
    /// Whether the single pixel texture uses a custom rectangle of its file. The project saves four
    /// nullable bounds: all null is off, any set is on. Turning it on from off gives each bound its
    /// <see cref="DefaultBounds"/> value; a project with only some set already reads as on and stays as saved.
    /// Turning it off sets all four to null.
    /// </summary>
    public bool UseCustomBounds
    {
        get => _viewModel.SinglePixelTextureLeft != null || _viewModel.SinglePixelTextureTop != null
            || _viewModel.SinglePixelTextureRight != null || _viewModel.SinglePixelTextureBottom != null;
        set
        {
            if (value == UseCustomBounds)
            {
                return;
            }
            if (value)
            {
                _viewModel.SinglePixelTextureLeft ??= DefaultBounds.Left;
                _viewModel.SinglePixelTextureTop ??= DefaultBounds.Top;
                _viewModel.SinglePixelTextureRight ??= DefaultBounds.Right;
                _viewModel.SinglePixelTextureBottom ??= DefaultBounds.Bottom;
            }
            else
            {
                _viewModel.SinglePixelTextureLeft = null;
                _viewModel.SinglePixelTextureTop = null;
                _viewModel.SinglePixelTextureRight = null;
                _viewModel.SinglePixelTextureBottom = null;
            }
            // The four fields exist only while the toggle is on.
            BoundsToggled?.Invoke();
        }
    }

    /// <summary>The label and hint of every row, which the Settings window's search matches.</summary>
    public IEnumerable<string> SearchTerms() => SettingsRows.SearchTerms(BuildCategories());

    /// <inheritdoc/>
    public List<MemberCategory> BuildCategories()
    {
        ProjectPropertiesViewModel vm = _viewModel;

        List<InstanceMember> texture = new List<InstanceMember>
        {
            SettingsRows.Path(nameof(vm.SinglePixelTextureFile), "Texture file",
                "Image the canvas draws solid shapes from. Empty draws a plain white pixel.",
                () => vm.SinglePixelTextureFile, value => vm.SinglePixelTextureFile = value),
            SettingsRows.Toggle(nameof(UseCustomBounds), "Custom bounds",
                "Draw from a rectangle of the file instead of the whole image.",
                () => UseCustomBounds, value => UseCustomBounds = value, toggleText: "Use custom bounds"),
        };
        if (UseCustomBounds)
        {
            texture.Add(Bound(nameof(vm.SinglePixelTextureLeft), "Left", "Left edge of the rectangle, in pixels.",
                () => vm.SinglePixelTextureLeft, value => vm.SinglePixelTextureLeft = value));
            texture.Add(Bound(nameof(vm.SinglePixelTextureTop), "Top", "Top edge of the rectangle, in pixels.",
                () => vm.SinglePixelTextureTop, value => vm.SinglePixelTextureTop = value));
            texture.Add(Bound(nameof(vm.SinglePixelTextureRight), "Right", "Right edge of the rectangle, in pixels.",
                () => vm.SinglePixelTextureRight, value => vm.SinglePixelTextureRight = value));
            texture.Add(Bound(nameof(vm.SinglePixelTextureBottom), "Bottom", "Bottom edge of the rectangle, in pixels.",
                () => vm.SinglePixelTextureBottom, value => vm.SinglePixelTextureBottom = value));
        }

        List<MemberCategory> sections = new List<MemberCategory>
        {
            SettingsRows.Section("Rendering", new[]
            {
                SettingsRows.Choice(nameof(vm.TextureFilter), "Texture filter",
                    "How textures are scaled. Point keeps pixel art sharp; Linear smooths it.",
                    typeof(TextureFilter), new object[] { TextureFilter.Point, TextureFilter.Linear },
                    () => vm.TextureFilter, value => vm.TextureFilter = (TextureFilter)value!),
            }),
            SettingsRows.Section("Values and names", new[]
            {
                SettingsRows.Toggle(nameof(vm.RestrictToUnitValues), "Restrict to unit values",
                    "Moving or resizing on the canvas rounds values to whole numbers.",
                    () => vm.RestrictToUnitValues, value => vm.RestrictToUnitValues = value, toggleText: "Snap to whole numbers"),
                SettingsRows.Toggle(nameof(vm.RestrictFileNamesForAndroid), "Restrict file names",
                    "Rejects file names Android cannot use when you pick or type one.",
                    () => vm.RestrictFileNamesForAndroid, value => vm.RestrictFileNamesForAndroid = value, toggleText: "Android-safe names only"),
            }),
            SettingsRows.Section("Single pixel texture", texture),
        };
        SettingsRows.Finish(sections);
        return sections;
    }

    // A bound is a nullable int saved by the project; its row only exists while the bounds are on.
    private static InstanceMember Bound(string name, string label, string hint, Func<int?> get, Action<int?> set) =>
        SettingsRows.Number(name, label, hint, () => get() ?? 0, value => set(value));
}
