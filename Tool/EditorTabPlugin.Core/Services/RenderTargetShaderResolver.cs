using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Gum.Managers;
using ShadowDusk.Core;
using ToolsUtilities;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Resolves a render-target Container's <c>SourceShaderFile</c> (a <c>.fx</c> or <c>.slang</c> path)
/// into a KNI/XNA <see cref="Effect"/> for the tool's WYSIWYG preview, compiling the shader at
/// runtime with ShadowDusk (no content pipeline). Each head registers it through
/// <see cref="EditorTabPluginBase.CreateRenderTargetShaderResolver"/> for the bytecode its device
/// accepts: DXBC for the WPF head's DirectX 11 backend, GLSL for the Avalonia head's SDL2/GL one.
/// Gum core ships no shader loader; this is the tool-side equivalent of the <c>FontService</c>
/// wiring.
/// </summary>
public static class RenderTargetShaderResolver
{
    // The bytecode of the last version of each shader file that compiled, so an edit that breaks the
    // shader does not blank the container. Bytecode rather than an Effect: the canvas disposes the
    // effects it cached every time it reloads, and a disposed Effect cannot be handed out again.
    private static readonly Dictionary<string, byte[]> LastGoodBytecode = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A resolver that compiles for <paramref name="target"/>, for the head to register. With a
    /// <paramref name="reportError"/>, a shader that stops compiling keeps its last good effect and the
    /// error goes to the callback instead of being thrown. With <paramref name="includes"/>, the files each
    /// shader <c>#include</c>s are recorded there, so an edit of one reloads the shader.
    /// </summary>
    public static Func<string, object?> For(PlatformTarget target, Action<string>? reportError = null, IShaderIncludeMap? includes = null) =>
        path => Resolve(path, target, reportError, includes);

    /// <summary>
    /// Compiles the <c>.fx</c>/<c>.slang</c> file at <paramref name="absolutePath"/> (already made
    /// absolute by the runtime against <c>FileManager.RelativeDirectory</c>) into an
    /// <see cref="Effect"/> for <paramref name="target"/>. On any failure this <b>throws with a
    /// descriptive message</b> rather than silently returning null, so the reason is visible: the caller
    /// (<c>CustomSetPropertyOnRenderable.AssignSourceShaderFileOnContainer</c>) includes the thrown
    /// message in the error it reports to the tool's Output window, and honors
    /// <c>GraphicalUiElement.MissingFileBehavior</c> (the tool uses <c>ConsumeSilently</c>, so the
    /// container just renders unshaded after the message is logged). Failure modes surfaced this
    /// way: GraphicsDevice not ready, file missing, ShadowDusk conversion/compile errors, and KNI
    /// rejecting the compiled bytecode (or a native compiler binary failing to load).
    /// </summary>
    public static object? Resolve(string absolutePath, PlatformTarget target, Action<string>? reportError = null, IShaderIncludeMap? includes = null)
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
        return CreateKeepingLastGood(
            absolutePath,
            () => CompileFile(absolutePath, target, includes),
            bytecode => new Effect(graphicsDevice, bytecode),
            reportError);
    }

    /// <summary>
    /// Compiles with <paramref name="compile"/> and builds the result with <paramref name="create"/>.
    /// When either fails and <paramref name="reportError"/> is given, the last bytecode that built for
    /// <paramref name="absolutePath"/> is built again and the failure is reported, so the container keeps
    /// the last shader that worked. With no earlier good version, or no callback, the failure is thrown.
    /// </summary>
    public static T CreateKeepingLastGood<T>(string absolutePath, Func<byte[]> compile, Func<byte[], T> create, Action<string>? reportError)
    {
        byte[] bytecode;
        T created;
        try
        {
            bytecode = compile();
            created = create(bytecode);
        }
        catch (Exception exception) when (reportError != null && TryGetLastGood(absolutePath, out byte[]? lastGood))
        {
            reportError(exception.Message + "\nKeeping the last version of the shader that compiled.");
            return create(lastGood!);
        }

        lock (LastGoodBytecode)
        {
            LastGoodBytecode[absolutePath] = bytecode;
        }
        return created;
    }

    private static bool TryGetLastGood(string absolutePath, out byte[]? bytecode)
    {
        lock (LastGoodBytecode)
        {
            return LastGoodBytecode.TryGetValue(absolutePath, out bytecode);
        }
    }

    /// <summary>
    /// Compiles <paramref name="source"/> for <paramref name="target"/> and returns the bytecode,
    /// or throws naming <paramref name="describedPath"/> and every compiler error. <paramref name="describedPath"/>
    /// is also what a relative <c>#include</c> resolves against, and each file included is passed to
    /// <paramref name="onInclude"/>.
    /// </summary>
    public static byte[] Compile(string source, PlatformTarget target, string describedPath, Action<string>? onInclude = null) =>
        ShaderSourceCompiler.Compile(source, target, describedPath, onInclude);

    /// <summary>
    /// Compiles the shader file at <paramref name="absolutePath"/> and records in <paramref name="includes"/> what it
    /// <c>#include</c>d, even when the compile fails, so fixing a broken include reloads the shader.
    /// </summary>
    public static byte[] CompileFile(string absolutePath, PlatformTarget target, IShaderIncludeMap? includes)
    {
        List<FilePath> included = new List<FilePath>();
        try
        {
            return Compile(
                ShaderSourceCompiler.ReadFxSource(absolutePath), target, absolutePath,
                includes == null ? null : path => included.Add(new FilePath(path)));
        }
        finally
        {
            includes?.SetIncludes(new FilePath(absolutePath), included);
        }
    }
}
