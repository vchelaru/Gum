#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Gum.Forms;
using System;
using Gum.Input;

namespace Gum;

public partial class GumService
{
    /// <summary>
    /// Initializes Gum for a BRUTAL application drawing with OpenGL 3.3, optionally loading a Gum project.
    /// Create the window with a 3.3 core context, make it current (<c>window.MakeContextCurrent()</c>) and make it
    /// resizable if you want Gum to follow its size. Gum creates its Skia context and render target at the
    /// framebuffer size, and <see cref="Draw"/> blits the frame to the window's default framebuffer; call
    /// <c>window.SwapBuffers()</c> after it.
    /// </summary>
    /// <param name="window">The GLFW window, with its OpenGL context current on this thread.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(GlfwWindow window, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), new GlBackend(), gumProjectFile);

    /// <summary>
    /// Initializes Gum like <see cref="Initialize(GlfwWindow, string?)"/> with an explicit set of default Forms visuals.
    /// Skia supports <see cref="DefaultVisualsVersion.V3"/> only; any other value throws <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="window">The GLFW window, with its OpenGL context current on this thread.</param>
    /// <param name="visualsVersion">The default Forms visuals to register.</param>
    /// <param name="gumProjectFile">An optional .gumx or .gumj project file to load.</param>
    public void Initialize(GlfwWindow window, DefaultVisualsVersion visualsVersion, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), new GlBackend(), gumProjectFile, visualsVersion);
}
#endif
