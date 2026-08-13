using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V3.Fonts;

/// <summary>
/// Issue #4304 — the legacy bmfont.exe backend writes FontSize into a Windows GDI LOGFONT, which
/// only accepts whole pixel heights, so a fractional <see cref="BmfcSave.FontSize"/> must round
/// before it reaches the .bmfc template.
/// </summary>
public class BmfcSaveGdiFontSizeTests
{
    [Fact]
    public void GetGdiRoundedFontSize_RoundsDownForFractionBelowHalf()
    {
        BmfcSave.GetGdiRoundedFontSize(18.3f).ShouldBe(18);
    }

    [Fact]
    public void GetGdiRoundedFontSize_RoundsUpForFractionAboveHalf()
    {
        BmfcSave.GetGdiRoundedFontSize(18.7f).ShouldBe(19);
    }

    [Fact]
    public void GetGdiRoundedFontSize_RoundsHalfAwayFromZero()
    {
        BmfcSave.GetGdiRoundedFontSize(18.5f).ShouldBe(19);
    }

    [Fact]
    public void GetGdiRoundedFontSize_LeavesWholeNumberUnchanged()
    {
        BmfcSave.GetGdiRoundedFontSize(18f).ShouldBe(18);
    }
}
