using System.IO;
using EditorTabPlugin_XNA.Services;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Core;

namespace GumPreview;

/// <summary>
/// Resolves a container's <c>SourceShaderFile</c> (a <c>.fx</c> or <c>.slang</c> path) into an
/// <see cref="Effect"/> for the Preview window by compiling it at runtime with ShadowDusk for
/// OpenGL. Gum core ships no shader loader, so without this the Preview renders a shaded container
/// unshaded while the editor canvas shows the effect. Failures throw with the reason, which
/// <c>CustomSetPropertyOnRenderable</c> reports.
/// </summary>
internal static class PreviewShaderResolver
{
    public static void Register(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => Resolve(graphicsDevice, path);
    }

    private static Effect Resolve(GraphicsDevice graphicsDevice, string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Render-target shader file not found: " + absolutePath, absolutePath);
        }

        // Compiled next to its path, which is what a relative #include in the shader resolves against.
        byte[] bytecode = ShaderSourceCompiler.Compile(ShaderSourceCompiler.ReadFxSource(absolutePath), PlatformTarget.OpenGL, absolutePath);
        return new Effect(graphicsDevice, bytecode);
    }
}
