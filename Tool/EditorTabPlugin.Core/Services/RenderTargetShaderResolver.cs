using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using ShadowDusk.Compiler;
using ShadowDusk.Core;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Resolves a render-target Container's <c>SourceShaderFile</c> (a <c>.fx</c> path) into a KNI/XNA
/// <see cref="Effect"/> for the tool's WYSIWYG preview, compiling the shader at runtime with
/// ShadowDusk (no content pipeline). Each head registers it through
/// <see cref="EditorTabPluginBase.CreateRenderTargetShaderResolver"/> for the bytecode its device
/// accepts: DXBC for the WPF head's DirectX 11 backend, GLSL for the Avalonia head's SDL2/GL one.
/// Gum core ships no shader loader; this is the tool-side equivalent of the <c>FontService</c>
/// wiring.
/// </summary>
public static class RenderTargetShaderResolver
{
    /// <summary>A resolver that compiles for <paramref name="target"/>, for the head to register.</summary>
    public static Func<string, object?> For(PlatformTarget target) => path => Resolve(path, target);

    /// <summary>
    /// Compiles the <c>.fx</c> at <paramref name="absolutePath"/> (already made absolute by the
    /// runtime against <c>FileManager.RelativeDirectory</c>) into an <see cref="Effect"/> for
    /// <paramref name="target"/>. On any failure this <b>throws with a descriptive message</b>
    /// rather than silently returning null, so the reason is visible: the caller
    /// (<c>CustomSetPropertyOnRenderable.AssignSourceShaderFileOnContainer</c>) includes the thrown
    /// message in the error it reports to the tool's Output window, and honors
    /// <c>GraphicalUiElement.MissingFileBehavior</c> (the tool uses <c>ConsumeSilently</c>, so the
    /// container just renders unshaded after the message is logged). Failure modes surfaced this
    /// way: GraphicsDevice not ready, file missing, ShadowDusk compile errors, and KNI rejecting the
    /// compiled bytecode (or a native compiler binary failing to load).
    /// </summary>
    public static object? Resolve(string absolutePath, PlatformTarget target)
    {
        // The GraphicsDevice only exists once the wireframe is initialized; the resolver is invoked
        // during rendering, so it is normally present.
        GraphicsDevice? graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice;
        if (graphicsDevice == null)
        {
            throw new InvalidOperationException(
                "Cannot compile the render-target shader: the GraphicsDevice is not available yet.");
        }

        // File.Exists is false for null/empty too; the runtime only calls the resolver with a
        // non-empty absolute path, so this really reports a genuinely missing file.
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Render-target shader file not found: " + absolutePath, absolutePath);
        }

        // If KNI rejects the compiled bytecode (or a native compiler binary fails to load), the
        // resulting exception propagates and is surfaced by the caller just like a compile error.
        return new Effect(graphicsDevice, Compile(File.ReadAllText(absolutePath), target, absolutePath));
    }

    /// <summary>
    /// Compiles <paramref name="source"/> for <paramref name="target"/> and returns the bytecode,
    /// or throws naming <paramref name="describedPath"/> and every compiler error.
    /// </summary>
    public static byte[] Compile(string source, PlatformTarget target, string describedPath)
    {
        EffectCompiler compiler = new EffectCompiler();
        Result<CompiledShader, ShaderError[]> result = compiler.Compile(source, new CompilerOptions { Target = target });
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not compile the render-target shader '" + describedPath + "':\n" +
                string.Join("\n", result.Error.Select(error => error.Message)));
        }
        return result.Value.Data;
    }
}
