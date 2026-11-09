namespace BrutalGum.Tests;

/// <summary>
/// The host's public service exposes no SkiaSharp type (#7788). The cases live in
/// <see cref="SkiaHostTests.NoSkiaLeaksTestsBase"/>.
/// </summary>
public class NoSkiaLeaksTests : SkiaHostTests.NoSkiaLeaksTestsBase
{
    // ClearColor is an SKColor; a separate change replaces it with a Skia-free type. Delete this entry then.
    protected override IReadOnlyCollection<string> AllowedLeaks => new[] { "ClearColor" };
}
