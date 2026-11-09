namespace KniSkiaGum.Tests;

/// <summary>
/// The host's public service exposes no SkiaSharp type (#7788). The cases live in
/// <see cref="SkiaHostTests.NoSkiaLeaksTestsBase"/>.
/// </summary>
public class NoSkiaLeaksTests : SkiaHostTests.NoSkiaLeaksTestsBase
{
    // BeforeDraw/AfterDraw hand out the SKCanvas for immediate-mode Skia drawing (used by the
    // KniSkiaGumImmediateMode and KniSkiaGumFontLoading samples). Whether they stay or get a Skia-free
    // replacement is the maintainer's call (#7788); remove these entries when they are replaced.
    protected override IReadOnlyCollection<string> AllowedLeaks => new[] { "BeforeDraw", "AfterDraw" };
}
