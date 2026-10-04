using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #5679 — an optional parameter replaces the old method signature in the compiled DLL, so a
// game built against an earlier Gum.Shapes and not rebuilt throws MissingMethodException when it
// reaches the call. These pin the pre-#5672 public signatures so they stay callable.
public class ShapePublicSignatureTests
{
    [Fact]
    public void RenderableShapeBase_KeepsParameterlessGetEffectiveXnaBlendState()
    {
        MethodInfo? method = typeof(RenderableShapeBase).GetMethod(
            nameof(RenderableShapeBase.GetEffectiveXnaBlendState),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: System.Type.EmptyTypes,
            modifiers: null);

        method.ShouldNotBeNull();
    }

    [Fact]
    public void ShapeRenderer_KeepsFourParameterBeginBatch()
    {
        MethodInfo? method = typeof(ShapeRenderer).GetMethod(
            nameof(ShapeRenderer.BeginBatch),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(Matrix?), typeof(RasterizerState), typeof(RenderableShapeBase), typeof(RenderStateChangeStatistics) },
            modifiers: null);

        method.ShouldNotBeNull();
    }
}
