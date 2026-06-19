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
/// ShadowDusk (no content pipeline). Registered into
/// <see cref="Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver"/> so the
/// editor renders shaded containers the same way a game runtime does once it registers its own
/// resolver. Gum core ships no shader loader; this is the tool-side equivalent of the
/// <c>FontService</c> wiring.
/// </summary>
internal static class RenderTargetShaderResolver
{
    /// <summary>
    /// Compiles the <c>.fx</c> at <paramref name="absolutePath"/> (already made absolute by the
    /// runtime against <c>FileManager.RelativeDirectory</c>) into an <see cref="Effect"/>. On any
    /// failure this <b>throws with a descriptive message</b> rather than silently returning null, so
    /// the reason is visible: the caller
    /// (<c>CustomSetPropertyOnRenderable.AssignSourceShaderFileOnContainer</c>) includes the thrown
    /// message in the error it reports to the tool's Output window, and honors
    /// <c>GraphicalUiElement.MissingFileBehavior</c> (the tool uses <c>ConsumeSilently</c>, so the
    /// container just renders unshaded after the message is logged). Failure modes surfaced this
    /// way: GraphicsDevice not ready, file missing, ShadowDusk compile errors, and KNI rejecting the
    /// compiled bytecode (or a native compiler binary failing to load).
    /// </summary>
    public static object? Resolve(string absolutePath)
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

        EffectCompiler compiler = new EffectCompiler();
        // The tool renders through KNI's DirectX 11 backend (nkast.Kni.Platform.WinForms.DX11), so
        // the shader must be compiled to DXBC for the device to accept it — not the OpenGL target a
        // DesktopGL game would use.
        Result<CompiledShader, ShaderError[]> result = compiler.Compile(
            File.ReadAllText(absolutePath),
            new CompilerOptions { Target = PlatformTarget.DirectX });

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                "ShadowDusk could not compile the render-target shader '" + absolutePath + "':\n" +
                string.Join("\n", result.Error.Select(error => error.Message)));
        }

        // If KNI rejects the compiled bytecode (or a native compiler binary fails to load), the
        // resulting exception propagates and is surfaced by the caller just like a compile error.
        return new Effect(graphicsDevice, result.Value.Data);
    }
}
