using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
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

    public ShapeBatch ShapeBatch
    {
        get
        {
            return _sb;
        }
    }

    public bool IsInitialized { get; private set; }

    public static ShapeRenderer Self
    {
        get
        {
            _self ??= new ShapeRenderer();
            return _self;
        }
    }

    public void Initialize()
    {
        var gumService = GumService.Default;
        if(gumService.IsInitialized == false)
        {
            throw new InvalidOperationException("GumService must be initialized before ShapeRenderer.");
        }

        Initialize(gumService.Game.GraphicsDevice, gumService.Game.Content);
    }

    public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        if(IsInitialized)
        {
            throw new InvalidOperationException("ShapeRenderer is already initialized");
        }
        IsInitialized = true;
        _sb = new ShapeBatch(graphicsDevice, contentManager);
    }
}
