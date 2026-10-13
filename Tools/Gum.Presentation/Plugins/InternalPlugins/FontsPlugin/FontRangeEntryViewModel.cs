using Gum.Mvvm;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Plugins.FontsPlugin;

/// <summary>
/// One row of the Fonts tab's ranges table: the characters baked for one font, or for every font
/// without a row of its own when <see cref="IsDefault"/>.
/// </summary>
public class FontRangeEntryViewModel : ViewModel
{
    /// <summary>Creates a row; the default row has no font and cannot be removed.</summary>
    public FontRangeEntryViewModel(bool isDefault)
    {
        IsDefault = isDefault;
        FontFamily = "";
        Ranges = BmfcSave.DefaultRanges;
        FamilyError = "";
        WarningText = "";
    }

    /// <summary>True for the row that applies to every font with no row of its own.</summary>
    public bool IsDefault { get; }

    /// <summary>A system font's family name, or a font file's project-relative path. Empty on the default row.</summary>
    public string FontFamily
    {
        get => Get<string>() ?? "";
        set => Set(value);
    }

    /// <summary>Comma-separated codepoints or start-end pairs, such as 32-126,160-255.</summary>
    public string Ranges
    {
        get => Get<string>() ?? "";
        set => Set(value);
    }

    /// <summary>
    /// True when the ranges cannot be typed in: the default row while a font character file supplies
    /// them. Set by <see cref="FontsViewModel"/>.
    /// </summary>
    public bool IsRangesReadOnly
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>Why the font cannot be saved (none chosen, or another row already names it); empty when fine. Set by <see cref="FontsViewModel"/>.</summary>
    public string FamilyError
    {
        get => Get<string>() ?? "";
        set => Set(value);
    }

    /// <summary>Why the row does nothing but is still saved (no Text uses the font); empty when fine. Set by <see cref="FontsViewModel"/>.</summary>
    public string WarningText
    {
        get => Get<string>() ?? "";
        set => Set(value);
    }

    /// <summary>The problem to show in red, or empty.</summary>
    [DependsOn(nameof(FamilyError))]
    [DependsOn(nameof(Ranges))]
    public string ErrorText
    {
        get
        {
            if (FamilyError != "")
            {
                return FamilyError;
            }

            return BmfcSave.GetIfIsValidRange(Ranges)
                ? ""
                : $"\"{Ranges}\" is not a valid range. Use start-end pairs or single codepoints, separated by commas with no spaces, such as 32-126,160-255.";
        }
    }

    /// <summary>True when <see cref="ErrorText"/> has something to show.</summary>
    [DependsOn(nameof(ErrorText))]
    public bool HasError => ErrorText != "";

    /// <summary>True when <see cref="WarningText"/> has something to show and there is no error to show instead.</summary>
    [DependsOn(nameof(WarningText))]
    [DependsOn(nameof(ErrorText))]
    public bool HasWarning => WarningText != "" && !HasError;
}
