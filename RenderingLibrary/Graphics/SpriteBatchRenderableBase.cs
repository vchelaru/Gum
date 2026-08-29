
using System;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using System.Collections.ObjectModel;
using ToolsUtilitiesStandard.Helpers;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Gum.Graphics.Animation;
using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics;


public abstract class SpriteBatchRenderableBase : IRenderable
{
    public BlendState BlendState
    {
        get;
        set;
    }

    bool IRenderable.Wrap => false;

    void IRenderable.PreRender() { }

    // BatchKey identifies the command stream (SpriteBatch vs Apos.Shapes), not a specific resource
    // like texture — BatchOrchestrator reads this on every renderable regardless of draw order, so
    // a coarser key here keeps that flush machinery cheap. Per-texture grouping instead goes
    // through BatchSortKey below, which only BatchKeyGroupedOrderer reads.
    public string BatchKey => "SpriteBatch";

    // Subclasses that carry a texture (Sprite, Text, NineSlice) override this with the Texture2D
    // reference they're about to draw with, so BatchKeyGroupedOrderer can group same-texture draws
    // into contiguous runs. Default null means "no finer grouping than BatchKey."
    public virtual object? BatchSortKey => null;

    public void StartBatch(ISystemManagers systemManagers)
    {
        var asSystemManagers = (SystemManagers)systemManagers;
        var spriteRenderer = asSystemManagers.Renderer.SpriteRenderer;
        spriteRenderer.Begin(createNewParameters:false);
        spriteRenderer.ForceSetRenderStatesToCurrent();
    }
    public void EndBatch(ISystemManagers systemManagers)
    {
        var asSystemManagers = (SystemManagers)systemManagers;
        asSystemManagers.Renderer.SpriteRenderer.End();
    }

    public abstract void Render(ISystemManagers managers);
}
