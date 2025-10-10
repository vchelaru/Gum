using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics;

public interface ITextureCoordinate
{
    /// <summary>
    /// The rectangle defining the source on the texture.  If null, the entire texture is used. Values are in pixels.
    /// </summary>
    Rectangle? SourceRectangle { get; set; }

    /// <summary>
    /// Whether to wrap when the SourceRectangle is larger than the texture. 
    /// </summary>
    bool Wrap { get; set; }

    /// <summary>
    /// The width of the texture in pixels. Null if no texture is assigned.
    /// </summary>
    float? TextureWidth { get; }

    /// <summary>
    /// The height of the texture in pixels. Null if no texture is assigned.
    /// </summary>
    float? TextureHeight { get; }

    /// <summary>
    /// Whether to flip the sprite horizontally.
    /// </summary>
    bool FlipHorizontal { get; set; }
}

