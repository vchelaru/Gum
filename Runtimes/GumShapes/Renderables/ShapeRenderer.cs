using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class ShapeRenderer
{
    // Bump this whenever the shipped apos-shapes.xnb files in
    // buildTransitive/MonoGame/Content/{DesktopGL,WindowsDX}/ are regenerated
    // against a new Apos.Shapes version. The XnbBuilderMonoGame* projects must
    // be rebuilt and their output XNBs copied in alongside this constant change.
    const string CompiledAgainstAposShapesVersion = "0.6.8";

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
        ValidateAposShapesVersion();
        IsInitialized = true;
        _sb = new ShapeBatch(graphicsDevice, contentManager);
    }

    [Conditional("DEBUG")]
    static void ValidateAposShapesVersion()
    {
        var assembly = typeof(ShapeBatch).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "<unknown>";

        // InformationalVersion may include a +commit suffix; strip it for comparison.
        var plusIndex = informational.IndexOf('+');
        var resolved = plusIndex >= 0 ? informational.Substring(0, plusIndex) : informational;

        if (!resolved.StartsWith(CompiledAgainstAposShapesVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Gum.Shapes was built against Apos.Shapes {CompiledAgainstAposShapesVersion}, " +
                $"but the resolved Apos.Shapes assembly reports version '{resolved}'. " +
                "The shipped apos-shapes.xnb may be incompatible with this Apos.Shapes runtime. " +
                "To fix: rebuild the XnbBuilderMonoGameDesktopGL and XnbBuilderMonoGameWindowsDX " +
                "projects against the new Apos.Shapes version, copy the produced XNBs into " +
                "Runtimes/GumShapes/buildTransitive/MonoGame/Content/{DesktopGL,WindowsDX}/, " +
                "and update CompiledAgainstAposShapesVersion in ShapeRenderer.cs.");
        }
    }
}
