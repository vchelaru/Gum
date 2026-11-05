using Gum;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>One sprite-count / sprite-size combination.</summary>
public readonly record struct BenchConfig(int SpriteCount, int SpriteSize)
{
    /// <summary>Transparent margin around each sprite, which the outline draws into.</summary>
    public const int Margin = 4;

    public int CellSize => SpriteSize + (2 * Margin);

    public int Columns => (int)System.Math.Ceiling(System.Math.Sqrt(SpriteCount));

    /// <summary>Side of the square scene render target that holds the whole grid.</summary>
    public int SceneSize => Columns * CellSize;

    public override string ToString() => $"{SpriteCount}x{SpriteSize}";
}

/// <summary>
/// A scene to measure. Add a new scenario by implementing this and adding one line to
/// <see cref="ScenarioRegistry"/>.
/// </summary>
public interface IBenchScenario
{
    string Id { get; }

    string Description { get; }

    /// <summary>Adds the scene to <c>gum.Root</c>. Called once per config, before warm-up.</summary>
    void Build(GumService gum, BenchConfig config, Texture2D spriteTexture);

    /// <summary>Called before every frame's Gum draw (warm-up and measured), outside the timed region.</summary>
    void BeforeDraw(int frameIndex)
    {
    }

    /// <summary>Removes everything <see cref="Build"/> added.</summary>
    void Teardown(GumService gum);
}
