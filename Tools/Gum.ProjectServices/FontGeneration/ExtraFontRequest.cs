using Gum.DataTypes;
using System.IO;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// A font the game needs at runtime that no element in the project uses (BBCode runs, oversampled
/// sizes), asked for by name so <c>gumcli fonts</c> bakes it along with the fonts the project uses.
/// </summary>
/// <param name="FontName">A system font family, or a .ttf/.otf path relative to the project folder.</param>
/// <param name="FontSize">The font size, as a Text's FontSize.</param>
/// <param name="IsBold">Whether to bake the bold style.</param>
/// <param name="IsItalic">Whether to bake the italic style.</param>
/// <param name="OutlineThickness">The outline thickness in pixels; zero is no outline.</param>
public sealed record ExtraFontRequest(
    string FontName,
    int FontSize,
    bool IsBold = false,
    bool IsItalic = false,
    int OutlineThickness = 0)
{
    /// <summary>
    /// Builds the <see cref="BmfcSave"/> a Text with these values would produce in <paramref name="project"/>.
    /// </summary>
    public BmfcSave ToBmfcSave(GumProjectSave project)
    {
        // Mirrors the end of FontReferenceCollector.BuildBmfcSave (GumCommon, shared with FlatRedBall,
        // so it is not widened for this): the same fields a Text with these values would carry.
        BmfcSave bmfcSave = new BmfcSave
        {
            FontSize = FontSize,
            OutlineThickness = OutlineThickness,
            IsBold = IsBold,
            IsItalic = IsItalic,
            SpacingHorizontal = project.FontSpacingHorizontal,
            SpacingVertical = project.FontSpacingVertical,
            IncludePathHashInCacheName = project.UsesPathHashedFontCacheNames
        };

        if (BmfcSave.IsFontFilePath(FontName))
        {
            bmfcSave.FontFile = FontName;
            bmfcSave.FontName = Path.GetFileNameWithoutExtension(FontName);
        }
        else
        {
            bmfcSave.FontName = FontName;
        }

        bmfcSave.Ranges = project.GetFontRanges(FontName, out bool rangesAreFontSpecific) ?? BmfcSave.GetEffectiveDefaultRanges();
        bmfcSave.RangesAreFontSpecific = rangesAreFontSpecific;

        return bmfcSave;
    }
}
