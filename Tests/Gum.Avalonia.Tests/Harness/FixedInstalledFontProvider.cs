using Gum.Services.Fonts;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="IInstalledFontProvider"/>: a fixed list, so the Font drop-down
/// offers the same families on every machine and a test can name a family that is not installed.
/// </summary>
internal sealed class FixedInstalledFontProvider : IInstalledFontProvider
{
    /// <summary>The families every test sees as installed.</summary>
    public static readonly IReadOnlyList<string> Families = new[] { "Arial", "Verdana" };

    /// <inheritdoc/>
    public IReadOnlyList<string> GetInstalledFontFamilyNames() => Families;
}
