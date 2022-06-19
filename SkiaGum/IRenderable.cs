using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

public interface IRenderable
{
    //BlendState BlendState { get; }

    bool Wrap { get; }

    void Render(SKCanvas canvas);

    /// <summary>
    /// Perform logic which needs to occur before a SpriteBatch has been started
    /// </summary>
    void PreRender();
}