using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class ShapeRenderer
{
    static ShapeRenderer _self = default!;
    ShapeBatch _sb = default!;

    public ShapeBatch ShapeBatch => _sb;

    public static ShapeRenderer Self
    {
        get
        {
            _self ??= new ShapeRenderer();
            return _self;
        }
    }

    public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _sb = new ShapeBatch(graphicsDevice, contentManager);
    }
}
