using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// Issue #4853 — a Text's <see cref="Text.Font"/> setter never re-wrapped/re-measured the already-set
/// <see cref="Text.RawText"/>. Text.Font's getter substitutes raylib's native
/// <see cref="Raylib.GetFontDefault"/> (a small placeholder, ~10px) whenever the real font hasn't
/// resolved yet; RawText assignment is not suspend-aware (unlike font resolution, which defers under
/// <c>GraphicalUiElement.IsAllLayoutSuspended</c>), so it can measure against that placeholder first,
/// caching wrap/pre-render dimensions in <see cref="Text.WrappedTextWidth"/>/mPreRenderWidth. When the
/// real, larger font resolves afterward, nothing invalidated that stale measurement -- Text.Font's
/// setter only updated the field and line height, unlike the MonoGame/KNI/FNA Text's BitmapFont setter
/// (RenderingLibrary/Graphics/Text.cs), which already calls AssignBitmapFontAndRefresh on every real
/// reassignment. This surfaced as a Tooltip's nine-slice background staying undersized (measured
/// against the placeholder-font width) until a second Show() forced an unrelated extra layout pass that
/// happened to re-trigger the wrap.
///
/// <c>Width</c> is explicitly nulled before each measurement below, mirroring exactly what
/// GraphicalUiElement's RelativeToChildren measurement path does (GumRuntime/GraphicalUiElement.cs,
/// the IText branch) before reading WrappedTextWidth -- a bare Text otherwise defaults Width to a
/// small wrap box, which would wrap both fonts down near that same box width and mask the bug.
/// </summary>
public class TextFontReassignmentRefreshesWrapTests : BaseTestClass
{
    [Fact]
    public void FontReassignedAfterRawTextSet_RefreshesWrappedTextWidthToTheNewFontsMetrics()
    {
        var text = new Text(SystemManagers.Default);

        // Measures against whatever Font currently resolves to -- the constructor already assigned
        // raylib's native default (Text.DefaultFont is unset in tests), so this pins the placeholder
        // measurement exactly like the real bug's first, too-early wrap.
        text.RawText = "The quick brown fox jumps over the lazy dog";
        text.Width = null;
        float widthAgainstPlaceholderFont = text.WrappedTextWidth;

        string fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");
        Font realFont = Raylib.LoadFontEx(fontPath, 48, null, 0);
        try
        {
            realFont.BaseSize.ShouldBe(48, "sanity check: LoadFontEx must have actually loaded the font");

            text.Font = realFont;
            text.Width = null;
            float widthAgainstRealFont = text.WrappedTextWidth;

            // 48px real font vs. raylib's ~10px native placeholder: the fixed measurement must be
            // substantially wider, not just "some other stale number". Before the fix this stayed
            // exactly equal to widthAgainstPlaceholderFont -- Text.Font's setter never re-wrapped.
            widthAgainstRealFont.ShouldBeGreaterThan(widthAgainstPlaceholderFont * 2,
                "assigning a real, much larger font should invalidate the wrap measurement taken against the small placeholder font");
        }
        finally
        {
            Raylib.UnloadFont(realFont);
        }
    }
}
