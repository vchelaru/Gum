using System.Collections.Generic;
using System.Linq;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>Shared grid layout and teardown.</summary>
public abstract class GridScenarioBase : IBenchScenario
{
    private readonly List<GraphicalUiElement> _roots = new List<GraphicalUiElement>();

    public abstract string Id { get; }

    public abstract string Description { get; }

    public void Build(GumService gum, BenchConfig config, Texture2D spriteTexture)
    {
        for (int i = 0; i < config.SpriteCount; i++)
        {
            float cellX = (i % config.Columns) * config.CellSize;
            float cellY = (i / config.Columns) * config.CellSize;
            GraphicalUiElement element = CreateElement(config, spriteTexture, cellX, cellY);
            gum.Root.AddChild(element);
            _roots.Add(element);
        }
    }

    /// <summary>Creates one grid cell's element, occupying the cell at (cellX, cellY).</summary>
    protected abstract GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, float cellX, float cellY);

    protected static SpriteRuntime CreateSprite(BenchConfig config, Texture2D spriteTexture, float x, float y)
    {
        SpriteRuntime sprite = new SpriteRuntime();
        sprite.Texture = spriteTexture;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = config.SpriteSize;
        sprite.Height = config.SpriteSize;
        sprite.X = x;
        sprite.Y = y;
        return sprite;
    }

    public void Teardown(GumService gum)
    {
        foreach (GraphicalUiElement element in _roots.ToList())
        {
            element.RemoveFromRoot();
        }

        _roots.Clear();
    }
}

/// <summary>A. N plain sprites in a grid: no render targets, no shader.</summary>
public sealed class PlainSpritesScenario : GridScenarioBase
{
    public override string Id => "A";

    public override string Description => "N plain SpriteRuntime, no render targets, no shader";

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, float cellX, float cellY)
    {
        return CreateSprite(config, spriteTexture, cellX + BenchConfig.Margin, cellY + BenchConfig.Margin);
    }
}

/// <summary>B. Each sprite in its own IsRenderTarget container with the Outline shader as its render-target effect.</summary>
public sealed class RenderTargetOutlineScenario : GridScenarioBase
{
    public const string ShaderPath = "Outline.slang";

    public override string Id => "B";

    public override string Description => "each sprite in its own IsRenderTarget ContainerRuntime with Outline.slang";

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, float cellX, float cellY)
    {
        ContainerRuntime container = new ContainerRuntime();
        container.Width = config.CellSize;
        container.Height = config.CellSize;
        container.X = cellX;
        container.Y = cellY;
        container.IsRenderTarget = true;

        container.AddChild(CreateSprite(config, spriteTexture, BenchConfig.Margin, BenchConfig.Margin));

        // The same calls RenderTargetShaderScreen makes for the shipped outline.
        container.SetShaderParameter("Thickness", 2f);
        container.SetShaderParameter("OutlineRed", 255f);
        container.SetShaderParameter("OutlineGreen", 217f);
        container.SetShaderParameter("OutlineBlue", 26f);
        container.SourceShaderFile = ShaderPath;
        return container;
    }
}
