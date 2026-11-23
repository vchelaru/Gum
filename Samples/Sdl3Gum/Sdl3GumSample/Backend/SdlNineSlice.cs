using RenderingLibrary;
using RenderingLibrary.Graphics;
using SDL;
using static SDL.SDL3;

namespace Sdl3GumSample.Backend;

/// <summary>
/// The renderable behind a Gum NineSlice: the texture is cut into a 3x3 grid; corners keep their size,
/// edges stretch along one axis and the center stretches along both. Nine source-to-destination draws.
/// </summary>
public unsafe class SdlNineSlice : SdlSprite
{
    /// <inheritdoc/>
    public override void Draw(SDL_Renderer* renderer)
    {
        if (Texture == null)
        {
            return;
        }

        SDL_FRect source = GetSourceRect();

        // GumCommon's nine-slice math, shared by every runtime: where to cut the texture (a third of
        // each side by default) and how thick each band draws once the box is too small for the corners.
        NineSliceSourceCut cut = NineSliceSourceCut.Compute((int)source.w, (int)source.h,
            frameWidth: null, left: null, top: null, right: null, bottom: null);
        NineSliceBorders borders = NineSliceBorders.Compute(cut, 1, 1, 1, 1, Width, Height);

        float left = this.GetAbsoluteLeft();
        float top = this.GetAbsoluteTop();

        // The four column and row edges, in the texture and on screen.
        float[] sourceX = { source.x, source.x + borders.SourceLeft, source.x + source.w - borders.SourceRight, source.x + source.w };
        float[] sourceY = { source.y, source.y + borders.SourceTop, source.y + source.h - borders.SourceBottom, source.y + source.h };
        float[] destX = { left, left + borders.DestLeft, left + Width - borders.DestRight, left + Width };
        float[] destY = { top, top + borders.DestTop, top + Height - borders.DestBottom, top + Height };

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                SDL_FRect from = new SDL_FRect
                {
                    x = sourceX[column],
                    y = sourceY[row],
                    w = sourceX[column + 1] - sourceX[column],
                    h = sourceY[row + 1] - sourceY[row]
                };
                SDL_FRect to = new SDL_FRect
                {
                    x = destX[column],
                    y = destY[row],
                    w = destX[column + 1] - destX[column],
                    h = destY[row + 1] - destY[row]
                };
                if (to.w > 0 && to.h > 0)
                {
                    SDL_RenderTexture(renderer, Texture.Handle, &from, &to);
                }
            }
        }
    }
}
