using SkiaGum;
using SkiaSharp;
using System;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Draws individual Gum renderables (and strings) onto the canvas yourself, between
/// <see cref="Begin(SKMatrix?, Renderer.GumBatchDrawMode)"/> and <see cref="End"/>, instead of
/// drawing the whole scene. The Skia counterpart of MonoGame's GumBatch. Draws onto the canvas
/// of the host's last <c>Draw(SKCanvas)</c> (<see cref="SystemManagers.Canvas"/>) unless a canvas
/// is passed to Begin. It never clears the canvas, but a full-scene Draw does when
/// <see cref="Renderer.ClearsCanvas"/> is true (the Renderer's default; GumService.Initialize turns
/// it off), so draw with GumBatch after Draw in a frame; GumBatch drawing before such a Draw is wiped.
/// </summary>
public class GumBatch
{
    enum GumBatchState
    {
        NotRendering,
        BeginCalled
    }

    GumBatchState State;
    readonly SystemManagers systemManagers;
    readonly Text internalTextForRendering;

    public GumBatch() : this(SystemManagers.Default)
    {
    }

    internal GumBatch(SystemManagers managers)
    {
        if (managers == null)
        {
            throw new InvalidOperationException("SystemManagers is null - did you remember to initialize Gum?");
        }
        systemManagers = managers;
        internalTextForRendering = new Text(systemManagers);
    }

    /// <summary>
    /// Starts drawing on <see cref="SystemManagers.Canvas"/>. <paramref name="matrix"/>, when given,
    /// applies before the camera, the same composition MonoGame uses.
    /// <see cref="Renderer.GumBatchDrawMode.Deferred"/> is not supported on Skia yet and throws.
    /// </summary>
    public void Begin(SKMatrix? matrix = null, Renderer.GumBatchDrawMode mode = Renderer.GumBatchDrawMode.Immediate)
    {
        if (State == GumBatchState.BeginCalled)
        {
            throw new InvalidOperationException("Begin has already been called. You must call End before calling Begin again.");
        }

        systemManagers.Renderer.Begin(matrix, mode);
        State = GumBatchState.BeginCalled;
    }

    /// <summary>
    /// Starts drawing on <paramref name="canvas"/>, which also becomes
    /// <see cref="SystemManagers.Canvas"/> for later draws.
    /// </summary>
    public void Begin(SKCanvas canvas, SKMatrix? matrix = null, Renderer.GumBatchDrawMode mode = Renderer.GumBatchDrawMode.Immediate)
    {
        if (State == GumBatchState.BeginCalled)
        {
            throw new InvalidOperationException("Begin has already been called. You must call End before calling Begin again.");
        }

        systemManagers.Canvas = canvas;
        Begin(matrix, mode);
    }

    /// <summary>
    /// Draws <paramref name="text"/> with its top-left at <paramref name="position"/>, using the
    /// font family <paramref name="fontName"/> at <paramref name="fontSize"/> (the font properties
    /// Skia's Text uses).
    /// </summary>
    public void DrawString(string fontName, float fontSize, string text, SKPoint position, SKColor color)
    {
        if (State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling DrawString");
        }

        internalTextForRendering.FontName = fontName;
        internalTextForRendering.FontSize = fontSize;
        internalTextForRendering.Width = null;
        internalTextForRendering.RawText = text;
        internalTextForRendering.X = position.X;
        internalTextForRendering.Y = position.Y;
        internalTextForRendering.Color = color;
        Draw(internalTextForRendering);
    }

    public void Draw(IRenderableIpso renderable)
    {
        if (State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling Draw");
        }

        systemManagers.Renderer.Draw(renderable);
    }

    public void End()
    {
        if (State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling End");
        }
        State = GumBatchState.NotRendering;

        systemManagers.Renderer.End();
    }
}
