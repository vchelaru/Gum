using System.Collections.Generic;
using System.Linq;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>Shared grid layout and teardown.</summary>
public abstract class GridScenarioBase : IBenchScenario
{
    private readonly List<GraphicalUiElement> _roots = new List<GraphicalUiElement>();

    public abstract string Id { get; }

    public abstract string Description { get; }

    public virtual void Build(GumService gum, BenchConfig config, Texture2D spriteTexture)
    {
        AddGrid(gum, config, spriteTexture);
    }

    protected void AddGrid(GumService gum, BenchConfig config, Texture2D spriteTexture)
    {
        for (int i = 0; i < config.SpriteCount; i++)
        {
            float cellX = (i % config.Columns) * config.CellSize;
            float cellY = (i / config.Columns) * config.CellSize;
            GraphicalUiElement element = CreateElement(config, spriteTexture, i, cellX, cellY);
            gum.Root.AddChild(element);
            _roots.Add(element);
        }
    }

    /// <summary>Creates the element for grid cell <paramref name="index"/>, occupying the cell at (cellX, cellY).</summary>
    protected abstract GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, int index, float cellX, float cellY);

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

    /// <summary>A cell-sized render-target container holding the sprite, with the Outline shader when asked.</summary>
    protected static ContainerRuntime CreateContainer(BenchConfig config, Texture2D spriteTexture, float x, float y, bool outline)
    {
        ContainerRuntime container = new ContainerRuntime();
        container.Width = config.CellSize;
        container.Height = config.CellSize;
        container.X = x;
        container.Y = y;
        container.IsRenderTarget = true;

        container.AddChild(CreateSprite(config, spriteTexture, BenchConfig.Margin, BenchConfig.Margin));

        if (outline)
        {
            // The same calls RenderTargetShaderScreen makes for the shipped outline.
            container.SetShaderParameter("Thickness", 2f);
            container.SetShaderParameter("OutlineRed", 255f);
            container.SetShaderParameter("OutlineGreen", 217f);
            container.SetShaderParameter("OutlineBlue", 26f);
            container.SourceShaderFile = "Outline.slang";
        }

        return container;
    }

    public virtual void Teardown(GumService gum)
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

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, int index, float cellX, float cellY)
    {
        return CreateSprite(config, spriteTexture, cellX + BenchConfig.Margin, cellY + BenchConfig.Margin);
    }
}

/// <summary>B. Each sprite in its own IsRenderTarget container with the Outline shader as its render-target effect.</summary>
public sealed class RenderTargetOutlineScenario : GridScenarioBase
{
    public override string Id => "B";

    public override string Description => "each sprite in its own IsRenderTarget ContainerRuntime with Outline.slang";

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, int index, float cellX, float cellY)
    {
        return CreateContainer(config, spriteTexture, cellX, cellY, outline: true);
    }
}

/// <summary>B0. Like B but with no shader: splits the render-target round trip from the effect cycle.</summary>
public sealed class RenderTargetNoShaderScenario : GridScenarioBase
{
    public override string Id => "B0";

    public override string Description => "each sprite in its own IsRenderTarget ContainerRuntime, no shader";

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, int index, float cellX, float cellY)
    {
        return CreateContainer(config, spriteTexture, cellX, cellY, outline: false);
    }
}

/// <summary>
/// C0. Emulates a cached render-target container, the ceiling for that feature: N separate RenderTarget2D
/// instances (one per sprite, identical content, none shared) are baked once in Build with the sprite and the
/// Outline shader, then every measured frame draws N plain SpriteRuntime through Gum with Texture set to its own
/// target. No bake and no effect in the measured frames.
/// </summary>
public sealed class CachedTargetsScenario : GridScenarioBase
{
    private readonly List<RenderTarget2D> _targets = new List<RenderTarget2D>();

    public override string Id => "C0";

    public override string Description => "N plain SpriteRuntime, each Texture = its own pre-baked (sprite + Outline) RenderTarget2D";

    public override void Build(GumService gum, BenchConfig config, Texture2D spriteTexture)
    {
        GraphicsDevice device = gum.Renderer.GraphicsDevice
            ?? throw new System.InvalidOperationException("GumService is not initialized.");

        // One outlined container, drawn alone into each target in turn.
        ContainerRuntime source = CreateContainer(config, spriteTexture, 0, 0, outline: true);
        gum.Root.AddChild(source);

        for (int i = 0; i < config.SpriteCount; i++)
        {
            RenderTarget2D target = new RenderTarget2D(device, config.CellSize, config.CellSize, false,
                SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _targets.Add(target);

            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            gum.Draw();
        }

        device.SetRenderTarget(null);
        source.RemoveFromRoot();

        // Wait for the GPU so setup time covers the bakes.
        Color[] pixel = new Color[1];
        _targets[_targets.Count - 1].GetData(0, new Rectangle(0, 0, 1, 1), pixel, 0, 1);

        AddGrid(gum, config, spriteTexture);
    }

    protected override GraphicalUiElement CreateElement(BenchConfig config, Texture2D spriteTexture, int index, float cellX, float cellY)
    {
        SpriteRuntime sprite = CreateSprite(config, _targets[index], cellX, cellY);
        sprite.Width = config.CellSize;
        sprite.Height = config.CellSize;
        return sprite;
    }

    public override void Teardown(GumService gum)
    {
        base.Teardown(gum);
        foreach (RenderTarget2D target in _targets)
        {
            target.Dispose();
        }

        _targets.Clear();
    }
}
