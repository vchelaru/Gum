using System.IO;
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
    /// runtime against <c>FileManager.RelativeDirectory</c>) into an <see cref="Effect"/>, or
    /// returns <c>null</c> on any failure so resolution degrades per
    /// <c>GraphicalUiElement.MissingFileBehavior</c> (the tool uses <c>ConsumeSilently</c>),
    /// mirroring how a missing texture renders unshaded rather than crashing the editor.
    /// </summary>
    public static object? Resolve(string absolutePath)
    {
        // The GraphicsDevice only exists once the wireframe is initialized; the resolver is invoked
        // during rendering, so it is normally present. Guard anyway — a transient null degrades to
        // unshaded rather than throwing.
        GraphicsDevice? graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice;
        // File.Exists is false for null/empty too, so it covers a missing path as well as a missing
        // file. The runtime only invokes the resolver with a non-empty absolute path, so this is
        // really guarding the GraphicsDevice-not-ready and file-missing cases.
        if (graphicsDevice == null || !File.Exists(absolutePath))
        {
            return null;
        }

        try
        {
            EffectCompiler compiler = new EffectCompiler();
            // The tool renders through KNI's DirectX 11 backend (nkast.Kni.Platform.WinForms.DX11),
            // so the shader must be compiled to DXBC for the device to accept it — not the OpenGL
            // target a DesktopGL game would use.
            var result = compiler.Compile(
                File.ReadAllText(absolutePath),
                new CompilerOptions { Target = PlatformTarget.DirectX });

            return result.IsSuccess ? new Effect(graphicsDevice, result.Value.Data) : null;
        }
        catch
        {
            // A throw here is typically a missing/unloadable native compiler asset rather than a
            // shader-syntax error; treat it as a failed resolve so the container renders unshaded.
            return null;
        }
    }
}
