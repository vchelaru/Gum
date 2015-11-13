using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public interface IRenderable
    {
        BlendState BlendState { get; }

        bool Wrap { get; }

        void Render(SpriteRenderer spriteRenderer, SystemManagers managers);

        float Z { get; set; }
    }
}
