
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

    public string BatchKey => "SpriteBatch";

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
