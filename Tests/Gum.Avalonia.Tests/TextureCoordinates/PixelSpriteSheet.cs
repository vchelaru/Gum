namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// A real pixel-art sprite sheet (transparent background, 176x112) with the frames a sprite-sheet
/// plugin would report for it, so screenshots show sprite pixels apart from region outlines.
/// </summary>
internal static class PixelSpriteSheet
{
    public const int Width = 176;
    public const int Height = 112;

    public static IReadOnlyList<(string Name, int X, int Y, int Width, int Height)> Frames { get; } = new[]
    {
        ("Dot", 20, 20, 8, 8),
        ("Puffs", 33, 16, 38, 16),
        ("Smoke", 73, 16, 12, 16),
        ("Sparkle", 91, 17, 5, 15),
        ("Alert", 112, 18, 16, 14),
        ("Crate_Break", 16, 48, 32, 16),
        ("Crate_Red", 64, 48, 16, 16),
        ("Glass", 96, 48, 16, 16),
        ("Spikes", 129, 53, 15, 11),
        ("Block_Break", 16, 80, 31, 16),
        ("Block_Blue", 64, 80, 16, 16),
        ("Coin_1", 96, 80, 16, 16),
        ("Coin_2", 115, 80, 10, 16),
        ("Coin_3", 134, 80, 4, 16),
        ("Coin_4", 147, 80, 10, 16),
    };

    public static void Write(string path) => File.WriteAllBytes(path, Convert.FromBase64String(Png));

    private const string Png = "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";
}
